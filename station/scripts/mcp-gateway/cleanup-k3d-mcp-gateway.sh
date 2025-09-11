#!/bin/bash

# Cleanup k3d MCP Gateway Setup
# Removes k3d cluster and related resources

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}🧹 k3d MCP Gateway Cleanup${NC}"
echo -e "${BLUE}============================${NC}"

# Load configuration if exists
if [ -f "k3d-config.env" ]; then
    echo -e "${BLUE}📋 Loading configuration${NC}"
    source k3d-config.env
else
    echo -e "${YELLOW}⚠️  Configuration file not found, using defaults${NC}"
    CLUSTER_NAME="mcp-gateway-cluster"
    REGISTRY_PORT="5001"
    GATEWAY_PORT="8000"
fi

# Step 1: List current k3d clusters
echo -e "${BLUE}📋 Step 1: Current k3d Clusters${NC}"
if command -v k3d &> /dev/null; then
    k3d cluster list
else
    echo -e "${YELLOW}⚠️  k3d not found${NC}"
fi

# Step 2: Delete k3d cluster
echo -e "${BLUE}📋 Step 2: Removing k3d Cluster${NC}"

if command -v k3d &> /dev/null && k3d cluster list | grep -q "$CLUSTER_NAME"; then
    echo -e "${BLUE}🗑️  Deleting k3d cluster: $CLUSTER_NAME${NC}"
    k3d cluster delete "$CLUSTER_NAME"
    echo -e "${GREEN}✅ k3d cluster removed${NC}"
else
    echo -e "${YELLOW}⚠️  k3d cluster not found${NC}"
fi

# Step 3: Clean up Docker images
echo -e "${BLUE}📋 Step 3: Docker Cleanup${NC}"

read -p "$(echo -e ${YELLOW}❓ Do you want to remove MCP Gateway Docker images? [y/N]: ${NC})" -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${BLUE}🗑️  Removing MCP Gateway Docker images${NC}"
    
    # Remove k3d related images
    docker images --format "table {{.Repository}}:{{.Tag}}" | grep -E "(k3d|mcp-gateway|rancher)" | while read image; do
        if [ -n "$image" ] && [ "$image" != "REPOSITORY:TAG" ]; then
            echo -e "${BLUE}🗑️  Removing image: $image${NC}"
            docker rmi "$image" || true
        fi
    done
    
    echo -e "${GREEN}✅ Docker images cleaned up${NC}"
else
    echo -e "${YELLOW}⏭️  Keeping Docker images${NC}"
fi

# Step 4: Clean up configuration files
echo -e "${BLUE}📋 Step 4: File Cleanup${NC}"

FILES_TO_REMOVE=(
    "k3d-mcp-gateway.yaml"
    "k3d-config.env"
)

for file in "${FILES_TO_REMOVE[@]}"; do
    if [ -f "$file" ]; then
        rm -f "$file"
        echo -e "${GREEN}✅ Removed $file${NC}"
    fi
done

# Step 5: Restore original kubeconfig (if backup exists)
echo -e "${BLUE}📋 Step 5: Kubeconfig Management${NC}"

if [ -f ~/.kube/config.backup.* ]; then
    read -p "$(echo -e ${YELLOW}❓ Do you want to restore original kubeconfig? [y/N]: ${NC})" -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        LATEST_BACKUP=$(ls -t ~/.kube/config.backup.* | head -1)
        echo -e "${BLUE}🔄 Restoring kubeconfig from $LATEST_BACKUP${NC}"
        cp "$LATEST_BACKUP" ~/.kube/config
        echo -e "${GREEN}✅ Original kubeconfig restored${NC}"
    else
        echo -e "${YELLOW}⏭️  Keeping current kubeconfig${NC}"
    fi
else
    echo -e "${YELLOW}⚠️  No kubeconfig backup found${NC}"
fi

# Step 6: Optional k3d uninstall
echo -e "${BLUE}📋 Step 6: k3d Uninstall Option${NC}"

read -p "$(echo -e ${YELLOW}❓ Do you want to uninstall k3d completely? [y/N]: ${NC})" -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    if command -v brew &> /dev/null; then
        echo -e "${BLUE}🗑️  Uninstalling k3d via Homebrew${NC}"
        brew uninstall k3d || true
    else
        echo -e "${BLUE}🗑️  Removing k3d binary${NC}"
        sudo rm -f /usr/local/bin/k3d || true
    fi
    echo -e "${GREEN}✅ k3d uninstalled${NC}"
else
    echo -e "${YELLOW}⏭️  Keeping k3d for future use${NC}"
fi

echo ""
echo -e "${GREEN}🎉 k3d MCP Gateway Cleanup Completed!${NC}"
echo -e "${GREEN}=====================================${NC}"
echo ""
echo -e "${BLUE}📋 Cleanup Summary:${NC}"
echo -e "  ✅ k3d cluster removed"
echo -e "  ✅ Configuration files cleaned"
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo -e "  ✅ Docker images cleaned"
    echo -e "  ✅ Original kubeconfig restored"
    echo -e "  ✅ k3d uninstalled"
fi
echo ""
echo -e "${YELLOW}💡 Tips:${NC}"
echo -e "  • To reinstall: ${GREEN}./setup-k3d-mcp-gateway.sh${NC}"
echo -e "  • To check Docker: ${YELLOW}docker ps${NC}"
echo -e "  • To check kubectl: ${YELLOW}kubectl cluster-info${NC}"
echo ""
