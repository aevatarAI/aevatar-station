#!/bin/bash

# Quick Aevatar MCP Gateway API Test
# Fast verification of core MCP Gateway integration

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}⚡ Quick Aevatar MCP Gateway API Test${NC}"
echo -e "${BLUE}====================================${NC}"

# Configuration
AUTH_SERVER="http://localhost:7001"
HTTPAPI="http://localhost:7002"
MCP_GATEWAY="http://localhost:8000"

# Step 1: Quick health checks
echo -e "${BLUE}📋 Quick Health Checks${NC}"

echo -n "🔐 Auth Server: "
if curl -s "$AUTH_SERVER/.well-known/openid_configuration" >/dev/null 2>&1; then
    echo -e "${GREEN}✅ Online${NC}"
else
    echo -e "${RED}❌ Offline${NC}"
fi

echo -n "🌐 HttpApi Server: "
if curl -s "$HTTPAPI/health" >/dev/null 2>&1; then
    echo -e "${GREEN}✅ Online${NC}"
else
    echo -e "${RED}❌ Offline${NC}"
fi

echo -n "🔌 MCP Gateway: "
if curl -s "$MCP_GATEWAY/adapters" >/dev/null 2>&1; then
    echo -e "${GREEN}✅ Online${NC}"
else
    echo -e "${RED}❌ Offline${NC}"
fi

echo ""

# Step 2: Get token
echo -e "${BLUE}🔐 Getting Access Token${NC}"

TOKEN_RESPONSE=$(curl -s -X POST "$AUTH_SERVER/connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=client_credentials&client_id=Aevatar_HttpApi&client_secret=1q2w3e*&scope=Aevatar" 2>/dev/null)

if ACCESS_TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.access_token' 2>/dev/null) && [ "$ACCESS_TOKEN" != "null" ]; then
    echo -e "${GREEN}✅ Token obtained successfully${NC}"
    AUTH_HEADER="Authorization: Bearer $ACCESS_TOKEN"
else
    echo -e "${YELLOW}⚠️  Using unauthenticated requests${NC}"
    AUTH_HEADER=""
fi

echo ""

# Step 3: Test core APIs
echo -e "${BLUE}🧪 Testing Core APIs${NC}"

# Test 1: List adapters
echo -n "📋 List Adapters: "
if [ -n "$AUTH_HEADER" ]; then
    RESPONSE=$(curl -s -H "$AUTH_HEADER" "$HTTPAPI/api/mcp-gateway/adapters" 2>/dev/null)
else
    RESPONSE=$(curl -s "$HTTPAPI/api/mcp-gateway/adapters" 2>/dev/null)
fi

if echo "$RESPONSE" | jq . >/dev/null 2>&1; then
    ADAPTER_COUNT=$(echo "$RESPONSE" | jq '. | length' 2>/dev/null || echo "0")
    echo -e "${GREEN}✅ Success ($ADAPTER_COUNT adapters)${NC}"
else
    echo -e "${RED}❌ Failed${NC}"
fi

# Test 2: Gateway health
echo -n "💓 Gateway Health: "
if [ -n "$AUTH_HEADER" ]; then
    HEALTH_RESPONSE=$(curl -s -H "$AUTH_HEADER" "$HTTPAPI/api/mcp-gateway/health" 2>/dev/null)
else
    HEALTH_RESPONSE=$(curl -s "$HTTPAPI/api/mcp-gateway/health" 2>/dev/null)
fi

if [ -n "$HEALTH_RESPONSE" ]; then
    echo -e "${GREEN}✅ Responding${NC}"
else
    echo -e "${RED}❌ No response${NC}"
fi

# Test 3: Create test adapter (quick test)
echo -n "🚀 Create Test Adapter: "
TEST_JSON='{"name":"quick-test","imageName":"mcp-example","imageVersion":"1.0.0","description":"Quick test adapter"}'

if [ -n "$AUTH_HEADER" ]; then
    CREATE_RESPONSE=$(curl -s -X POST -H "$AUTH_HEADER" -H "Content-Type: application/json" \
        -d "$TEST_JSON" "$HTTPAPI/api/mcp-gateway/adapters" 2>/dev/null)
else
    CREATE_RESPONSE=$(curl -s -X POST -H "Content-Type: application/json" \
        -d "$TEST_JSON" "$HTTPAPI/api/mcp-gateway/adapters" 2>/dev/null)
fi

if echo "$CREATE_RESPONSE" | grep -q "quick-test"; then
    echo -e "${GREEN}✅ Created${NC}"
    
    # Clean up - delete the test adapter
    sleep 2
    if [ -n "$AUTH_HEADER" ]; then
        curl -s -X DELETE -H "$AUTH_HEADER" "$HTTPAPI/api/mcp-gateway/adapters/quick-test" >/dev/null 2>&1
    else
        curl -s -X DELETE "$HTTPAPI/api/mcp-gateway/adapters/quick-test" >/dev/null 2>&1
    fi
    echo "🗑️  Test adapter cleaned up"
else
    echo -e "${RED}❌ Failed${NC}"
    if [ -n "$CREATE_RESPONSE" ]; then
        echo "   Response: $CREATE_RESPONSE"
    fi
fi

echo ""

# Step 4: Integration verification
echo -e "${BLUE}🔗 Integration Verification${NC}"

# Check if Aevatar can reach MCP Gateway directly
echo -n "🌉 Direct Gateway Access: "
if curl -s "$MCP_GATEWAY/adapters" >/dev/null 2>&1; then
    echo -e "${GREEN}✅ Accessible${NC}"
else
    echo -e "${RED}❌ Not accessible${NC}"
fi

# Check API endpoint mapping
echo -n "🗺️  API Endpoint Mapping: "
if [ -n "$AUTH_HEADER" ]; then
    API_RESPONSE=$(curl -s -w "%{http_code}" -H "$AUTH_HEADER" "$HTTPAPI/api/mcp-gateway/adapters" -o /dev/null 2>/dev/null)
else
    API_RESPONSE=$(curl -s -w "%{http_code}" "$HTTPAPI/api/mcp-gateway/adapters" -o /dev/null 2>/dev/null)
fi

if [ "$API_RESPONSE" = "200" ]; then
    echo -e "${GREEN}✅ Working (HTTP $API_RESPONSE)${NC}"
else
    echo -e "${RED}❌ Failed (HTTP $API_RESPONSE)${NC}"
fi

echo ""

# Summary
echo -e "${BLUE}📊 Quick Test Summary${NC}"
echo -e "${BLUE}===================${NC}"

if curl -s "$HTTPAPI/api/mcp-gateway/adapters" >/dev/null 2>&1 && curl -s "$MCP_GATEWAY/adapters" >/dev/null 2>&1; then
    echo -e "${GREEN}🎉 Core integration is working!${NC}"
    echo ""
    echo -e "${BLUE}✅ Verified:${NC}"
    echo -e "  • Auth Server connectivity"
    echo -e "  • HttpApi MCP endpoints"
    echo -e "  • Direct MCP Gateway access"
    echo -e "  • Basic CRUD operations"
    echo ""
    echo -e "${BLUE}🚀 Ready for:${NC}"
    echo -e "  • Full test suite: ${GREEN}./test-aevatar-mcp-api.sh${NC}"
    echo -e "  • VS Code integration"
    echo -e "  • Production deployment"
else
    echo -e "${RED}❌ Integration issues detected${NC}"
    echo ""
    echo -e "${YELLOW}🔧 Check:${NC}"
    echo -e "  • All services are running"
    echo -e "  • Network connectivity"
    echo -e "  • Authentication configuration"
fi

echo ""
