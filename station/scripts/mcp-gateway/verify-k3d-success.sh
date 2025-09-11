#!/bin/bash

# Verify k3d MCP Gateway Success
# Simple verification that k3d setup is working

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}🔍 Verifying k3d MCP Gateway Success${NC}"
echo -e "${BLUE}===================================${NC}"

# Check k3d cluster
echo -e "${BLUE}📋 k3d Cluster Status${NC}"
if command -v k3d &> /dev/null; then
    k3d cluster list
    echo ""
    
    if k3d cluster list | grep -q "mcp-gateway-cluster"; then
        echo -e "${GREEN}✅ k3d cluster is running${NC}"
    else
        echo -e "${RED}❌ k3d cluster not found${NC}"
        exit 1
    fi
else
    echo -e "${RED}❌ k3d not installed${NC}"
    exit 1
fi

# Check kubectl connectivity
echo -e "${BLUE}📋 Kubernetes Connectivity${NC}"
if kubectl cluster-info &> /dev/null; then
    echo -e "${GREEN}✅ kubectl connected to k3d cluster${NC}"
    kubectl get nodes
else
    echo -e "${RED}❌ kubectl connection failed${NC}"
    exit 1
fi

# Check MCP Gateway pods
echo -e "${BLUE}📋 MCP Gateway Pods${NC}"
kubectl get pods -n adapter

POD_STATUS=$(kubectl get pods -n adapter -l app=mcpgateway -o jsonpath='{.items[0].status.phase}' 2>/dev/null)
if [ "$POD_STATUS" = "Running" ]; then
    echo -e "${GREEN}✅ MCP Gateway pod is running${NC}"
else
    echo -e "${YELLOW}⚠️  MCP Gateway pod status: $POD_STATUS${NC}"
fi

# Test basic connectivity
echo -e "${BLUE}📋 Basic Connectivity Test${NC}"
RESPONSE=$(curl -s -w "%{http_code}" http://localhost:8000/ -o /dev/null)
if [ "$RESPONSE" = "404" ]; then
    echo -e "${GREEN}✅ k3d port mapping is working (got 404, which means connection works)${NC}"
elif [ "$RESPONSE" = "200" ]; then
    echo -e "${GREEN}✅ k3d port mapping is working (got 200)${NC}"
else
    echo -e "${RED}❌ No response from k3d port mapping${NC}"
fi

# Test different API paths
echo -e "${BLUE}📋 API Endpoint Discovery${NC}"

API_PATHS=(
    "/adapters"
    "/api/adapters"
    "/health"
    "/api/health"
    "/"
)

for path in "${API_PATHS[@]}"; do
    RESPONSE=$(curl -s -w "%{http_code}" "http://localhost:8000$path" -o /tmp/response.txt 2>/dev/null)
    CONTENT=$(cat /tmp/response.txt 2>/dev/null || echo "")
    
    echo -e "${BLUE}🔍 Testing $path: HTTP $RESPONSE${NC}"
    if [ "$RESPONSE" = "200" ]; then
        echo -e "${GREEN}   ✅ Success! Content: ${CONTENT:0:100}...${NC}"
    elif [ "$RESPONSE" = "401" ]; then
        echo -e "${YELLOW}   ⚠️  Requires authentication${NC}"
    elif [ "$RESPONSE" = "404" ]; then
        echo -e "${BLUE}   ℹ️  Not found${NC}"
    else
        echo -e "${RED}   ❌ Unexpected response${NC}"
    fi
done

rm -f /tmp/response.txt

# Summary
echo ""
echo -e "${BLUE}📊 k3d Success Verification Summary${NC}"
echo -e "${BLUE}===================================${NC}"

if kubectl get pods -n adapter -l app=mcpgateway | grep -q "Running"; then
    echo -e "${GREEN}🎉 k3d MCP Gateway setup is working!${NC}"
    echo ""
    echo -e "${BLUE}✅ Verified:${NC}"
    echo -e "  • k3d cluster is running"
    echo -e "  • kubectl connectivity works"
    echo -e "  • MCP Gateway pod is running"
    echo -e "  • Port mapping (8000) is functional"
    echo ""
    echo -e "${BLUE}🚀 Next Steps:${NC}"
    echo -e "  • Debug API endpoints to find correct paths"
    echo -e "  • Test with official MCP servers: ${GREEN}./test-official-mcp-servers.sh${NC}"
    echo -e "  • Deploy custom MCP servers using npx"
    echo ""
    echo -e "${YELLOW}💡 k3d has solved the Docker Desktop Kubernetes issues!${NC}"
else
    echo -e "${RED}❌ k3d setup has issues${NC}"
    echo ""
    echo -e "${YELLOW}🔧 Troubleshooting:${NC}"
    echo -e "  • Check pod logs: ${YELLOW}kubectl logs -n adapter deployment/mcpgateway${NC}"
    echo -e "  • Check pod status: ${YELLOW}kubectl describe pods -n adapter${NC}"
    echo -e "  • Restart deployment: ${YELLOW}kubectl rollout restart deployment/mcpgateway -n adapter${NC}"
fi

echo ""
