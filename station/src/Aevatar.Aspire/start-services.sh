#!/bin/bash

set -e

echo "======================================"
echo "Starting Aevatar All-in-One Container"
echo "======================================"

# External config mount point
EXTERNAL_CONFIG="/external-config"

# Create log directory
mkdir -p /var/log

# Function to copy config if exists externally, otherwise create default
setup_config() {
    local config_name=$1
    local external_path="$EXTERNAL_CONFIG/$config_name"
    local internal_path=$2
    local default_content=$3
    
    if [ -f "$external_path" ]; then
        echo "Using external configuration: $config_name"
        cp "$external_path" "$internal_path"
    else
        echo "Creating default configuration: $config_name"
        mkdir -p "$(dirname "$internal_path")"
        echo "$default_content" > "$internal_path"
    fi
}

# Function to wait for a service to be ready
wait_for_service() {
    local service_name=$1
    local host=$2
    local port=$3
    local max_wait=${4:-60}
    
    echo "Waiting for $service_name to be ready on $host:$port..."
    
    for i in $(seq 1 $max_wait); do
        if nc -z $host $port 2>/dev/null; then
            echo "$service_name is ready!"
            return 0
        fi
        echo "Waiting for $service_name... ($i/$max_wait)"
        sleep 1
    done
    
    echo "WARNING: $service_name did not become ready within $max_wait seconds"
    return 1
}

# Initialize data directories with proper permissions
echo "Initializing data directories..."
chown -R aevatar:aevatar /data
chmod -R 755 /data

# Create necessary configuration directories
mkdir -p /etc/blackbox /etc/grafana /etc/redis /etc/supervisor/conf.d /etc/prometheus
mkdir -p /opt/qdrant/config /etc/grafana/provisioning/datasources
chown -R aevatar:aevatar /etc/blackbox /etc/grafana

echo "Setting up configuration files..."

# MongoDB configurations
setup_config "mongod.conf" "/etc/mongod.conf" 'storage:
  dbPath: /data/mongodb
net:
  port: 27017
  bindIp: 0.0.0.0
systemLog:
  destination: file
  path: /var/log/mongodb.log
  logAppend: true'

setup_config "mongod-es.conf" "/etc/mongod-es.conf" 'storage:
  dbPath: /data/mongodb-es
net:
  port: 27018
  bindIp: 0.0.0.0
systemLog:
  destination: file
  path: /var/log/mongodb-es.log
  logAppend: true'

# Redis configuration
setup_config "redis.conf" "/etc/redis/redis.conf" 'bind 0.0.0.0
port 6379
dir /data/redis
logfile /var/log/redis.log'

# Elasticsearch configuration
setup_config "elasticsearch.yml" "/etc/elasticsearch/elasticsearch.yml" 'cluster.name: aevatar-es
node.name: aevatar-node
path.data: /data/elasticsearch
path.logs: /var/log
network.host: 0.0.0.0
http.port: 9200
discovery.type: single-node
xpack.security.enabled: false'

# Kafka configuration
setup_config "server.properties" "/opt/kafka/config/server.properties" 'broker.id=1
listeners=PLAINTEXT://0.0.0.0:9092
log.dirs=/data/kafka
zookeeper.connect=localhost:2181
num.network.threads=3
num.io.threads=8
socket.send.buffer.bytes=102400
socket.receive.buffer.bytes=102400
socket.request.max.bytes=104857600
log.retention.hours=168
log.segment.bytes=1073741824
log.retention.check.interval.ms=300000'

# Zookeeper configuration
setup_config "zookeeper.properties" "/opt/kafka/config/zookeeper.properties" 'dataDir=/data/zookeeper
clientPort=2181
maxClientCnxns=0
admin.enableServer=false'

# Qdrant configuration
setup_config "qdrant.yaml" "/opt/qdrant/config/production.yaml" 'service:
  host: 0.0.0.0
  http_port: 6333
  grpc_port: 6334

storage:
  storage_path: /data/qdrant

log_level: INFO'

# OpenTelemetry Collector configuration
setup_config "otel-collector-config.yaml" "/etc/otel-collector-config.yaml" 'receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4315

processors:
  batch:
    send_batch_size: 1000
    timeout: 10s
  memory_limiter:
    check_interval: 1s
    limit_mib: 1000

exporters:
  otlp:
    endpoint: "localhost:4317"
    tls:
      insecure: true
  debug:
    verbosity: detailed
  prometheus:
    endpoint: "0.0.0.0:9090"
  elasticsearch:
    endpoints: ["http://localhost:9200"]

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch, memory_limiter]
      exporters: [otlp, elasticsearch, debug]
    metrics:
      receivers: [otlp]
      processors: [batch, memory_limiter]
      exporters: [prometheus, debug]
    logs:
      receivers: [otlp]
      processors: [batch, memory_limiter]
      exporters: [elasticsearch, debug]'

# Prometheus configuration
setup_config "prometheus.yml" "/etc/prometheus/prometheus.yml" 'global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: '\''opentelemetry-collector'\''
    static_configs:
      - targets: ['\''localhost:9090'\'']
  
  - job_name: '\''prometheus'\''
    static_configs:
      - targets: ['\''localhost:9091'\'']'

# Blackbox Exporter configuration
setup_config "blackbox.yml" "/etc/blackbox/blackbox.yml" 'modules:
  http_2xx:
    prober: http
    timeout: 5s
    http:
      valid_http_versions: ["HTTP/1.1", "HTTP/2.0"]
      valid_status_codes: [200]
      method: GET
      follow_redirects: true
      fail_if_ssl: false
      fail_if_not_ssl: false'

# Grafana configuration
setup_config "grafana.ini" "/etc/grafana/grafana.ini" '[server]
http_addr = 0.0.0.0
http_port = 3000

[database]
type = sqlite3
path = /data/grafana/grafana.db

[paths]
data = /data/grafana
logs = /var/log
plugins = /data/grafana/plugins
provisioning = /etc/grafana/provisioning

[security]
admin_user = admin
admin_password = admin

[auth.anonymous]
enabled = true
org_role = Viewer'

# Grafana datasource configuration
setup_config "datasource.yaml" "/etc/grafana/provisioning/datasources/datasource.yaml" 'apiVersion: 1
datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://localhost:9091
    isDefault: true'

echo "Creating Supervisor configurations..."

# Create main supervisor configuration
cat > /etc/supervisor/supervisord.conf << 'EOF'
[unix_http_server]
file=/tmp/supervisor.sock

[supervisord]
logfile=/var/log/supervisord.log
logfile_maxbytes=50MB
logfile_backups=10
loglevel=info
pidfile=/tmp/supervisord.pid
nodaemon=true
minfds=1024
minprocs=200

[rpcinterface:supervisor]
supervisor.rpcinterface_factory = supervisor.rpcinterface:make_main_rpcinterface

[supervisorctl]
serverurl=unix:///tmp/supervisor.sock

[include]
files = /etc/supervisor/conf.d/*.conf
EOF

# Create individual service configurations
cat > /etc/supervisor/conf.d/mongodb.conf << 'EOF'
[program:mongodb]
command=/usr/bin/mongod --config /etc/mongod.conf
directory=/data/mongodb
user=aevatar
autostart=true
autorestart=true
redirect_stderr=true
stdout_logfile=/var/log/mongodb-supervisor.log
priority=100

[program:mongodb-es]
command=/usr/bin/mongod --config /etc/mongod-es.conf
directory=/data/mongodb-es
user=aevatar
autostart=true
autorestart=true
redirect_stderr=true
stdout_logfile=/var/log/mongodb-es-supervisor.log
priority=101
EOF

cat > /etc/supervisor/conf.d/redis.conf << 'EOF'
[program:redis]
command=/usr/bin/redis-server /etc/redis/redis.conf
directory=/data/redis
user=aevatar
autostart=true
autorestart=true
redirect_stderr=true
stdout_logfile=/var/log/redis-supervisor.log
priority=102
EOF

cat > /etc/supervisor/conf.d/elasticsearch.conf << 'EOF'
[program:elasticsearch]
command=/usr/share/elasticsearch/bin/elasticsearch
directory=/data/elasticsearch
user=aevatar
autostart=true
autorestart=true
redirect_stderr=true
stdout_logfile=/var/log/elasticsearch-supervisor.log
environment=ES_JAVA_OPTS="-Xms512m -Xmx512m"
priority=103
EOF

cat > /etc/supervisor/conf.d/kafka.conf << 'EOF'
[program:zookeeper]
command=/opt/kafka/bin/zookeeper-server-start.sh /opt/kafka/config/zookeeper.properties
directory=/data/zookeeper
user=aevatar
autostart=true
autorestart=true
redirect_stderr=true
stdout_logfile=/var/log/zookeeper-supervisor.log
priority=104

[program:kafka]
command=/opt/kafka/bin/kafka-server-start.sh /opt/kafka/config/server.properties
directory=/data/kafka
user=aevatar
autostart=true
autorestart=true
redirect_stderr=true
stdout_logfile=/var/log/kafka-supervisor.log
priority=105
EOF

cat > /etc/supervisor/conf.d/monitoring.conf << 'EOF'
[program:qdrant]
command=/opt/qdrant/qdrant --config-path /opt/qdrant/config/production.yaml
directory=/data/qdrant
user=aevatar
autostart=true
autorestart=true
redirect_stderr=true
stdout_logfile=/var/log/qdrant-supervisor.log
priority=106

[program:jaeger]
command=/opt/jaeger/jaeger-all-in-one --collector.otlp.enabled
directory=/opt/jaeger
user=aevatar
autostart=true
autorestart=true
redirect_stderr=true
stdout_logfile=/var/log/jaeger-supervisor.log
priority=107

[program:otel-collector]
command=/opt/otel/otelcol-contrib --config=/etc/otel-collector-config.yaml
directory=/opt/otel
user=aevatar
autostart=true
autorestart=true
redirect_stderr=true
stdout_logfile=/var/log/otel-collector-supervisor.log
priority=108

[program:prometheus]
command=/opt/prometheus/prometheus --config.file=/etc/prometheus/prometheus.yml --storage.tsdb.path=/data/prometheus --web.listen-address=0.0.0.0:9091
directory=/data/prometheus
user=aevatar
autostart=true
autorestart=true
redirect_stderr=true
stdout_logfile=/var/log/prometheus-supervisor.log
priority=109

[program:grafana]
command=/opt/grafana/bin/grafana-server --config=/etc/grafana/grafana.ini --homepath=/opt/grafana
directory=/data/grafana
user=aevatar
autostart=true
autorestart=true
redirect_stderr=true
stdout_logfile=/var/log/grafana-supervisor.log
priority=110

[program:blackbox-exporter]
command=/opt/blackbox/blackbox_exporter --config.file=/etc/blackbox/blackbox.yml
directory=/opt/blackbox
user=aevatar
autostart=true
autorestart=true
redirect_stderr=true
stdout_logfile=/var/log/blackbox-supervisor.log
priority=111
EOF

echo "Starting supervisor with all services..."

# Start supervisor
exec /usr/bin/supervisord -c /etc/supervisor/supervisord.conf 