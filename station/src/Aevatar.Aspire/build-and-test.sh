#!/bin/bash

set -e

echo "======================================"
echo "Building Aevatar All-in-One Container"
echo "======================================"

CONTAINER_NAME="aevatar-all-in-one"
IMAGE_NAME="aevatar/aspire:all-in-one"

# Function to cleanup
cleanup() {
    echo "Cleaning up..."
    docker stop $CONTAINER_NAME 2>/dev/null || true
    docker rm $CONTAINER_NAME 2>/dev/null || true
}

# Set trap to cleanup on exit
trap cleanup EXIT

# Build the Docker image
echo "Building Docker image: $IMAGE_NAME"
docker build -t $IMAGE_NAME -f Dockerfile.all-in-one .

# Run the container
echo "Starting container: $CONTAINER_NAME"
docker run -d \
    --name $CONTAINER_NAME \
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
    $IMAGE_NAME

# Wait for services to start
echo "Waiting for services to start up..."
sleep 30

# Test individual services
echo "Testing services..."

# Test MongoDB
echo "Testing MongoDB (port 27017)..."
if nc -z localhost 27017; then
    echo "✅ MongoDB is responding"
else
    echo "❌ MongoDB is not responding"
fi

# Test MongoDB ES
echo "Testing MongoDB ES (port 27018)..."
if nc -z localhost 27018; then
    echo "✅ MongoDB ES is responding"
else
    echo "❌ MongoDB ES is not responding"
fi

# Test Redis
echo "Testing Redis (port 6379)..."
if nc -z localhost 6379; then
    echo "✅ Redis is responding"
else
    echo "❌ Redis is not responding"
fi

# Test Elasticsearch
echo "Testing Elasticsearch (port 9200)..."
if curl -f http://localhost:9200/_cluster/health >/dev/null 2>&1; then
    echo "✅ Elasticsearch is responding"
else
    echo "❌ Elasticsearch is not responding"
fi

# Test Kafka
echo "Testing Kafka (port 9092)..."
if nc -z localhost 9092; then
    echo "✅ Kafka is responding"
else
    echo "❌ Kafka is not responding"
fi

# Test Qdrant
echo "Testing Qdrant (port 6333)..."
if curl -f http://localhost:6333/health >/dev/null 2>&1; then
    echo "✅ Qdrant is responding"
else
    echo "❌ Qdrant is not responding"
fi

# Test Jaeger
echo "Testing Jaeger (port 16686)..."
if curl -f http://localhost:16686/api/services >/dev/null 2>&1; then
    echo "✅ Jaeger is responding"
else
    echo "❌ Jaeger is not responding"
fi

# Test Prometheus
echo "Testing Prometheus (port 9091)..."
if curl -f http://localhost:9091/-/healthy >/dev/null 2>&1; then
    echo "✅ Prometheus is responding"
else
    echo "❌ Prometheus is not responding"
fi

# Test Grafana
echo "Testing Grafana (port 3000)..."
if curl -f http://localhost:3000/api/health >/dev/null 2>&1; then
    echo "✅ Grafana is responding"
else
    echo "❌ Grafana is not responding"
fi

# Show container logs for debugging
echo ""
echo "=== Container Logs ==="
docker logs --tail 50 $CONTAINER_NAME

echo ""
echo "=== Service Status ==="
docker exec $CONTAINER_NAME supervisorctl status

echo ""
echo "======================================"
echo "Test completed!"
echo "======================================" 