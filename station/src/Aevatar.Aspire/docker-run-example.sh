#!/bin/bash

# Example script showing how to run the Aevatar All-in-One container with mounted configurations

CONTAINER_NAME="aevatar-all-in-one"
IMAGE_NAME="aevatar/aspire:all-in-one"

# Create local configuration directory (optional)
mkdir -p ./config/mongodb
mkdir -p ./config/redis
mkdir -p ./config/elasticsearch
mkdir -p ./config/kafka
mkdir -p ./config/prometheus
mkdir -p ./config/grafana
mkdir -p ./config/blackbox
mkdir -p ./config/otel

# Create data directory for persistence
mkdir -p ./data

echo "======================================"
echo "Running Aevatar All-in-One Container"
echo "======================================"

# Stop and remove existing container if running
docker stop $CONTAINER_NAME 2>/dev/null || true
docker rm $CONTAINER_NAME 2>/dev/null || true

# Run container with mounted configurations and data
docker run -d \
    --name $CONTAINER_NAME \
    --restart unless-stopped \
    -p 27017:27017 \
    -p 27018:27018 \
    -p 6379:6379 \
    -p 9092:9092 \
    -p 2181:2181 \
    -p 9200:9200 \
    -p 6333:6333 \
    -p 6334:6334 \
    -p 16686:16686 \
    -p 14250:14250 \
    -p 4317:4317 \
    -p 4318:4318 \
    -p 4315:4315 \
    -p 9090:9090 \
    -p 9091:9091 \
    -p 3000:3000 \
    -p 9115:9115 \
    -v $(pwd)/data:/data \
    -v $(pwd)/config:/external-config:ro \
    $IMAGE_NAME

echo "Container started with name: $CONTAINER_NAME"
echo ""
echo "To mount custom configurations, place them in ./config/ directory:"
echo "  ./config/mongod.conf              - MongoDB main instance"
echo "  ./config/mongod-es.conf           - MongoDB ES instance"
echo "  ./config/redis/redis.conf         - Redis configuration"
echo "  ./config/elasticsearch.yml        - Elasticsearch configuration"
echo "  ./config/prometheus.yml           - Prometheus configuration"
echo "  ./config/grafana.ini              - Grafana configuration"
echo "  ./config/otel-collector-config.yaml - OpenTelemetry configuration"
echo "  ./config/blackbox.yml             - Blackbox Exporter configuration"
echo ""
echo "Services will be available at:"
echo "  MongoDB:       localhost:27017"
echo "  MongoDB ES:    localhost:27018" 
echo "  Redis:         localhost:6379"
echo "  Elasticsearch: http://localhost:9200"
echo "  Kafka:         localhost:9092"
echo "  Qdrant:        http://localhost:6333"
echo "  Jaeger UI:     http://localhost:16686"
echo "  Prometheus:    http://localhost:9091"
echo "  Grafana:       http://localhost:3000 (admin/admin)"
echo ""
echo "To view logs: docker logs -f $CONTAINER_NAME"
echo "To check status: docker exec $CONTAINER_NAME supervisorctl status" 