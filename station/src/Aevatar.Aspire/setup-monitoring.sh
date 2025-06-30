#!/bin/bash

# Dynamic host IP detection for cross-platform Docker monitoring setup

echo "🔍 Detecting Docker host IP..."

# Method 1: Try host.docker.internal (Docker Desktop)
if docker run --rm alpine nslookup host.docker.internal >/dev/null 2>&1; then
    HOST_IP="host.docker.internal"
    echo "✅ Using Docker Desktop: $HOST_IP"
    
# Method 2: Try gateway IP (Linux Docker)
elif command -v docker >/dev/null 2>&1; then
    GATEWAY_IP=$(docker network inspect bridge --format='{{(index .IPAM.Config 0).Gateway}}' 2>/dev/null)
    if [[ -n "$GATEWAY_IP" ]]; then
        HOST_IP="$GATEWAY_IP"
        echo "✅ Using Docker bridge gateway: $HOST_IP"
    else
        # Method 3: Fallback to localhost (host network mode)
        HOST_IP="localhost"
        echo "⚠️  Fallback to localhost (requires host network mode)"
    fi
else
    echo "❌ Docker not found"
    exit 1
fi

# Update prometheus.yml with detected IP
echo "📝 Updating prometheus.yml with HOST_IP=$HOST_IP"

# Create a temporary prometheus.yml with the correct IP
sed "s/host\.docker\.internal/$HOST_IP/g" prometheus.yml.template > prometheus.yml

echo "🚀 Starting monitoring stack..."
export HOST_IP="$HOST_IP"
docker-compose up -d

echo "✅ Monitoring setup complete!"
echo "📊 Prometheus: http://localhost:9091"
echo "🔍 Blackbox Exporter: http://localhost:9115" 