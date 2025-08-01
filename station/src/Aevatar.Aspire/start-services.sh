#!/bin/bash

set -e

echo "======================================"
echo "Starting Aevatar All-in-One Container"
echo "======================================"

# Create log directory
mkdir -p /var/log

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

# Function to check if a service is healthy via HTTP
wait_for_http_service() {
    local service_name=$1
    local url=$2
    local max_wait=${3:-60}
    
    echo "Waiting for $service_name to be ready at $url..."
    
    for i in $(seq 1 $max_wait); do
        if curl -f $url >/dev/null 2>&1; then
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
mkdir -p /etc/blackbox /etc/grafana
chown -R aevatar:aevatar /etc/blackbox /etc/grafana

# Create Grafana configuration if not exists
if [ ! -f /etc/grafana/grafana.ini ]; then
    cat > /etc/grafana/grafana.ini << EOF
[server]
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
org_role = Viewer
EOF
fi

# Create Qdrant configuration if not exists
mkdir -p /opt/qdrant/config
if [ ! -f /opt/qdrant/config/production.yaml ]; then
    cat > /opt/qdrant/config/production.yaml << EOF
service:
  host: 0.0.0.0
  http_port: 6333
  grpc_port: 6334

storage:
  storage_path: /data/qdrant

log_level: INFO
EOF
fi

echo "Starting supervisor with all services..."

# Start supervisor
exec /usr/bin/supervisord -c /etc/supervisor/supervisord.conf 