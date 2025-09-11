#!/bin/bash

# Microsoft MCP Gateway Local Cleanup Script
# Cleans up all resources created by deploy-mcp-gateway.sh

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🧹 MCP Gateway Local Cleanup${NC}"
echo -e "${BLUE}=============================${NC}"

# Default work directory
WORK_DIR="$HOME/mcp-gateway-local"
CONFIG_FILE="$WORK_DIR/config.env"

# Load configuration if exists
if [ -f "$CONFIG_FILE" ]; then
    echo -e "${BLUE}📋 Loading configuration from $CONFIG_FILE${NC}"
    source "$CONFIG_FILE"
else
    echo -e "${YELLOW}⚠️  Configuration file not found, using defaults${NC}"
    REGISTRY_PORT=5001
    GATEWAY_PORT=8000
fi

# Step 1: Stop port forwarding
echo -e "${BLUE}📋 Step 1: Stopping Port Forwarding${NC}"

# Kill port forward process if PID file exists
if [ -f "$WORK_DIR/port-forward.pid" ]; then
    PID=$(cat "$WORK_DIR/port-forward.pid")
    if ps -p $PID > /dev/null 2>&1; then
        echo -e "${BLUE}🔪 Stopping port forward process (PID: $PID)${NC}"
        kill $PID || true
    fi
    rm -f "$WORK_DIR/port-forward.pid"
fi

# Kill any remaining port-forward processes
pkill -f "kubectl port-forward.*mcpgateway-service" || true
echo -e "${GREEN}✅ Port forwarding stopped${NC}"

# Step 2: Clean up Kubernetes resources
echo -e "${BLUE}📋 Step 2: Cleaning up Kubernetes Resources${NC}"

if kubectl get namespace adapter &> /dev/null; then
    echo -e "${BLUE}🗑️  Deleting Kubernetes namespace 'adapter'${NC}"
    kubectl delete namespace adapter --timeout=60s || {
        echo -e "${YELLOW}⚠️  Force deleting namespace${NC}"
        kubectl delete namespace adapter --force --grace-period=0 || true
    }
    echo -e "${GREEN}✅ Kubernetes resources cleaned up${NC}"
else
    echo -e "${YELLOW}⚠️  Kubernetes namespace 'adapter' not found${NC}"
fi

# Step 3: Stop and remove Docker registry
echo -e "${BLUE}📋 Step 3: Cleaning up Docker Registry${NC}"

if docker ps -a --format 'table {{.Names}}' | grep -q "^local-registry$"; then
    echo -e "${BLUE}🐳 Stopping and removing local Docker registry${NC}"
    docker stop local-registry || true
    docker rm local-registry || true
    echo -e "${GREEN}✅ Docker registry cleaned up${NC}"
else
    echo -e "${YELLOW}⚠️  Docker registry container not found${NC}"
fi

# Step 4: Clean up Docker images (optional)
echo -e "${BLUE}📋 Step 4: Cleaning up Docker Images${NC}"

read -p "$(echo -e ${YELLOW}❓ Do you want to remove MCP Gateway Docker images? [y/N]: ${NC})" -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${BLUE}🗑️  Removing MCP Gateway Docker images${NC}"
    
    # Remove images with our registry prefix
    if [ -n "$REGISTRY_PORT" ]; then
        docker images --format "table {{.Repository}}:{{.Tag}}" | grep "localhost:$REGISTRY_PORT" | while read image; do
            echo -e "${BLUE}🗑️  Removing image: $image${NC}"
            docker rmi "$image" || true
        done
    fi
    
    # Remove other MCP-related images
    docker images --format "table {{.Repository}}:{{.Tag}}" | grep -E "(mcp-gateway|mcp-example)" | while read image; do
        echo -e "${BLUE}🗑️  Removing image: $image${NC}"
        docker rmi "$image" || true
    done
    
    echo -e "${GREEN}✅ Docker images cleaned up${NC}"
else
    echo -e "${YELLOW}⏭️  Skipping Docker image cleanup${NC}"
fi

# Step 5: Clean up work directory
echo -e "${BLUE}📋 Step 5: Cleaning up Work Directory${NC}"

if [ -d "$WORK_DIR" ]; then
    read -p "$(echo -e ${YELLOW}❓ Do you want to remove the work directory $WORK_DIR? [y/N]: ${NC})" -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${BLUE}🗑️  Removing work directory${NC}"
        rm -rf "$WORK_DIR"
        echo -e "${GREEN}✅ Work directory cleaned up${NC}"
    else
        echo -e "${YELLOW}⏭️  Keeping work directory: $WORK_DIR${NC}"
    fi
else
    echo -e "${YELLOW}⚠️  Work directory not found${NC}"
fi

# Step 6: Docker system cleanup (optional)
echo -e "${BLUE}📋 Step 6: Docker System Cleanup${NC}"

read -p "$(echo -e ${YELLOW}❓ Do you want to run Docker system prune to clean up unused resources? [y/N]: ${NC})" -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${BLUE}🧹 Running Docker system prune${NC}"
    docker system prune -f
    echo -e "${GREEN}✅ Docker system cleanup completed${NC}"
else
    echo -e "${YELLOW}⏭️  Skipping Docker system cleanup${NC}"
fi

echo ""
echo -e "${GREEN}🎉 MCP Gateway Cleanup Completed!${NC}"
echo -e "${GREEN}===================================${NC}"
echo ""
echo -e "${BLUE}📋 Cleanup Summary:${NC}"
echo -e "  ✅ Port forwarding stopped"
echo -e "  ✅ Kubernetes resources removed"
echo -e "  ✅ Docker registry cleaned up"
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo -e "  ✅ Docker images cleaned up"
    echo -e "  ✅ Work directory removed"
    echo -e "  ✅ Docker system cleaned up"
fi
echo ""
echo -e "${YELLOW}💡 Tips:${NC}"
echo -e "  • You can now run ${GREEN}./deploy-mcp-gateway.sh${NC} again for a fresh deployment"
echo -e "  • To check for any remaining resources: ${YELLOW}kubectl get all -A | grep mcp${NC}"
echo -e "  • To check Docker containers: ${YELLOW}docker ps -a | grep mcp${NC}"
echo ""
