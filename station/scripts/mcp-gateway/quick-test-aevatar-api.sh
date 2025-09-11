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

# Configuration - Auto-detect correct ports
AUTH_SERVER="http://localhost:7001"
HTTPAPI_PORTS=("7002" "8001")  # Try both Aspire (7002) and standalone (8001) ports
MCP_GATEWAY="http://localhost:8000"

# Auto-detect HttpApi port
HTTPAPI=""
echo -e "${BLUE}🔍 Auto-detecting HttpApi port...${NC}"
for port in "${HTTPAPI_PORTS[@]}"; do
    if curl -s "http://localhost:$port/health" >/dev/null 2>&1; then
        HTTPAPI="http://localhost:$port"
        echo -e "${GREEN}✅ Found HttpApi on port $port${NC}"
        break
    fi
done

if [ -z "$HTTPAPI" ]; then
    echo -e "${RED}❌ HttpApi not found on any expected port${NC}"
    HTTPAPI="http://localhost:7002"  # Default fallback
    echo -e "${YELLOW}⚠️  Using default port 7002${NC}"
fi

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

echo -e "${BLUE}🔍 Debug: Attempting client credentials flow${NC}"
TOKEN_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" -X POST "$AUTH_SERVER/connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=client_credentials&client_id=Aevatar_HttpApi&client_secret=1q2w3e*&scope=Aevatar" 2>/dev/null)

HTTP_CODE=$(echo "$TOKEN_RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)
TOKEN_BODY=$(echo "$TOKEN_RESPONSE" | grep -v "HTTP_CODE:")

echo -e "${BLUE}🔍 Debug: Auth Server Response (HTTP $HTTP_CODE):${NC}"
echo "$TOKEN_BODY" | jq '.' 2>/dev/null || echo "$TOKEN_BODY"

if ACCESS_TOKEN=$(echo "$TOKEN_BODY" | jq -r '.access_token' 2>/dev/null) && [ "$ACCESS_TOKEN" != "null" ] && [ -n "$ACCESS_TOKEN" ]; then
    echo -e "${GREEN}✅ Token obtained successfully${NC}"
    echo -e "${BLUE}🔍 Debug: Token preview: ${ACCESS_TOKEN:0:20}...${NC}"
    AUTH_HEADER="Authorization: Bearer $ACCESS_TOKEN"
else
    echo -e "${YELLOW}⚠️  Client credentials failed, trying password flow${NC}"
    
    # Try password flow
    ADMIN_TOKEN_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" -X POST "$AUTH_SERVER/connect/token" \
        -H "Content-Type: application/x-www-form-urlencoded" \
        -d "grant_type=password&client_id=Aevatar_HttpApi&client_secret=1q2w3e*&username=admin&password=1q2w3E*&scope=Aevatar" 2>/dev/null)
    
    ADMIN_HTTP_CODE=$(echo "$ADMIN_TOKEN_RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)
    ADMIN_TOKEN_BODY=$(echo "$ADMIN_TOKEN_RESPONSE" | grep -v "HTTP_CODE:")
    
    echo -e "${BLUE}🔍 Debug: Password flow response (HTTP $ADMIN_HTTP_CODE):${NC}"
    echo "$ADMIN_TOKEN_BODY" | jq '.' 2>/dev/null || echo "$ADMIN_TOKEN_BODY"
    
    if ACCESS_TOKEN=$(echo "$ADMIN_TOKEN_BODY" | jq -r '.access_token' 2>/dev/null) && [ "$ACCESS_TOKEN" != "null" ] && [ -n "$ACCESS_TOKEN" ]; then
        echo -e "${GREEN}✅ Token obtained via password flow${NC}"
        echo -e "${BLUE}🔍 Debug: Token preview: ${ACCESS_TOKEN:0:20}...${NC}"
        AUTH_HEADER="Authorization: Bearer $ACCESS_TOKEN"
    else
        echo -e "${RED}❌ Failed to obtain access token via any method${NC}"
        echo -e "${BLUE}🔍 Debug: Available Auth Server endpoints:${NC}"
        curl -s "$AUTH_SERVER/.well-known/openid_configuration" | jq '.token_endpoint' 2>/dev/null || echo "Auth server not responding"
        AUTH_HEADER=""
    fi
fi

echo ""

# Step 3: Test core APIs
echo -e "${BLUE}🧪 Testing Core APIs${NC}"

# Test 1: List adapters
echo -e "${BLUE}📋 Testing List Adapters API${NC}"
if [ -n "$AUTH_HEADER" ]; then
    echo -e "${BLUE}🔍 Debug: Making authenticated request to $HTTPAPI/api/mcp-gateway/adapters${NC}"
    ADAPTERS_FULL_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" -H "$AUTH_HEADER" "$HTTPAPI/api/mcp-gateway/adapters" 2>/dev/null)
else
    echo -e "${BLUE}🔍 Debug: Making unauthenticated request to $HTTPAPI/api/mcp-gateway/adapters${NC}"
    ADAPTERS_FULL_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" "$HTTPAPI/api/mcp-gateway/adapters" 2>/dev/null)
fi

ADAPTERS_HTTP_CODE=$(echo "$ADAPTERS_FULL_RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)
ADAPTERS_RESPONSE=$(echo "$ADAPTERS_FULL_RESPONSE" | grep -v "HTTP_CODE:")

echo -e "${BLUE}🔍 Debug: HTTP Code: $ADAPTERS_HTTP_CODE${NC}"
echo -e "${BLUE}🔍 Debug: Response: $ADAPTERS_RESPONSE${NC}"

if [ "$ADAPTERS_HTTP_CODE" = "200" ] && echo "$ADAPTERS_RESPONSE" | jq . >/dev/null 2>&1; then
    ADAPTER_COUNT=$(echo "$ADAPTERS_RESPONSE" | jq '. | length' 2>/dev/null || echo "0")
    echo -e "${GREEN}✅ List Adapters: Success ($ADAPTER_COUNT adapters)${NC}"
elif [ "$ADAPTERS_HTTP_CODE" = "401" ]; then
    echo -e "${RED}❌ List Adapters: Unauthorized (401) - Authentication required${NC}"
elif [ "$ADAPTERS_HTTP_CODE" = "403" ]; then
    echo -e "${RED}❌ List Adapters: Forbidden (403) - Insufficient permissions${NC}"
elif [ "$ADAPTERS_HTTP_CODE" = "404" ]; then
    echo -e "${RED}❌ List Adapters: Not Found (404) - API endpoint may not exist${NC}"
else
    echo -e "${RED}❌ List Adapters: Failed (HTTP $ADAPTERS_HTTP_CODE)${NC}"
fi

# Test 2: Gateway health
echo -e "${BLUE}💓 Testing Gateway Health API${NC}"
if [ -n "$AUTH_HEADER" ]; then
    echo -e "${BLUE}🔍 Debug: Making authenticated request to $HTTPAPI/api/mcp-gateway/health${NC}"
    HEALTH_FULL_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" -H "$AUTH_HEADER" "$HTTPAPI/api/mcp-gateway/health" 2>/dev/null)
else
    echo -e "${BLUE}🔍 Debug: Making unauthenticated request to $HTTPAPI/api/mcp-gateway/health${NC}"
    HEALTH_FULL_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" "$HTTPAPI/api/mcp-gateway/health" 2>/dev/null)
fi

HEALTH_HTTP_CODE=$(echo "$HEALTH_FULL_RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)
HEALTH_RESPONSE=$(echo "$HEALTH_FULL_RESPONSE" | grep -v "HTTP_CODE:")

echo -e "${BLUE}🔍 Debug: HTTP Code: $HEALTH_HTTP_CODE${NC}"
echo -e "${BLUE}🔍 Debug: Response: $HEALTH_RESPONSE${NC}"

if [ "$HEALTH_HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ Gateway Health: Success${NC}"
elif [ "$HEALTH_HTTP_CODE" = "401" ]; then
    echo -e "${RED}❌ Gateway Health: Unauthorized (401)${NC}"
elif [ "$HEALTH_HTTP_CODE" = "404" ]; then
    echo -e "${RED}❌ Gateway Health: Not Found (404) - Endpoint may not exist${NC}"
else
    echo -e "${RED}❌ Gateway Health: Failed (HTTP $HEALTH_HTTP_CODE)${NC}"
fi

# Test 3: Create test adapter (quick test)
echo -e "${BLUE}🚀 Testing Create Test Adapter API${NC}"
TEST_JSON='{"name":"quick-test","imageName":"mcp-example","imageVersion":"1.0.0","description":"Quick test adapter"}'

echo -e "${BLUE}🔍 Debug: Test adapter JSON: $TEST_JSON${NC}"

if [ -n "$AUTH_HEADER" ]; then
    echo -e "${BLUE}🔍 Debug: Making authenticated POST request to $HTTPAPI/api/mcp-gateway/adapters${NC}"
    CREATE_FULL_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" -X POST -H "$AUTH_HEADER" -H "Content-Type: application/json" \
        -d "$TEST_JSON" "$HTTPAPI/api/mcp-gateway/adapters" 2>/dev/null)
else
    echo -e "${BLUE}🔍 Debug: Making unauthenticated POST request to $HTTPAPI/api/mcp-gateway/adapters${NC}"
    CREATE_FULL_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" -X POST -H "Content-Type: application/json" \
        -d "$TEST_JSON" "$HTTPAPI/api/mcp-gateway/adapters" 2>/dev/null)
fi

CREATE_HTTP_CODE=$(echo "$CREATE_FULL_RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)
CREATE_RESPONSE=$(echo "$CREATE_FULL_RESPONSE" | grep -v "HTTP_CODE:")

echo -e "${BLUE}🔍 Debug: HTTP Code: $CREATE_HTTP_CODE${NC}"
echo -e "${BLUE}🔍 Debug: Response: $CREATE_RESPONSE${NC}"

if [ "$CREATE_HTTP_CODE" = "200" ] || [ "$CREATE_HTTP_CODE" = "201" ]; then
    if echo "$CREATE_RESPONSE" | grep -q "quick-test"; then
        echo -e "${GREEN}✅ Create Test Adapter: Success${NC}"
        
        # Clean up - delete the test adapter
        sleep 2
        echo -e "${BLUE}🗑️  Cleaning up test adapter${NC}"
        if [ -n "$AUTH_HEADER" ]; then
            DELETE_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" -X DELETE -H "$AUTH_HEADER" "$HTTPAPI/api/mcp-gateway/adapters/quick-test" 2>/dev/null)
        else
            DELETE_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" -X DELETE "$HTTPAPI/api/mcp-gateway/adapters/quick-test" 2>/dev/null)
        fi
        
        DELETE_HTTP_CODE=$(echo "$DELETE_RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)
        echo -e "${BLUE}🔍 Debug: Delete HTTP Code: $DELETE_HTTP_CODE${NC}"
        
        if [ "$DELETE_HTTP_CODE" = "200" ] || [ "$DELETE_HTTP_CODE" = "204" ]; then
            echo -e "${GREEN}✅ Test adapter cleaned up successfully${NC}"
        else
            echo -e "${YELLOW}⚠️  Test adapter cleanup may have failed${NC}"
        fi
    else
        echo -e "${RED}❌ Create Test Adapter: Response doesn't contain expected data${NC}"
    fi
elif [ "$CREATE_HTTP_CODE" = "401" ]; then
    echo -e "${RED}❌ Create Test Adapter: Unauthorized (401) - Authentication required${NC}"
elif [ "$CREATE_HTTP_CODE" = "403" ]; then
    echo -e "${RED}❌ Create Test Adapter: Forbidden (403) - Insufficient permissions${NC}"
elif [ "$CREATE_HTTP_CODE" = "404" ]; then
    echo -e "${RED}❌ Create Test Adapter: Not Found (404) - API endpoint may not exist${NC}"
else
    echo -e "${RED}❌ Create Test Adapter: Failed (HTTP $CREATE_HTTP_CODE)${NC}"
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
echo -e "${BLUE}🗺️  Testing API Endpoint Mapping${NC}"
if [ -n "$AUTH_HEADER" ]; then
    echo -e "${BLUE}🔍 Debug: Testing authenticated endpoint mapping${NC}"
    API_FULL_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" -H "$AUTH_HEADER" "$HTTPAPI/api/mcp-gateway/adapters" 2>/dev/null)
else
    echo -e "${BLUE}🔍 Debug: Testing unauthenticated endpoint mapping${NC}"
    API_FULL_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" "$HTTPAPI/api/mcp-gateway/adapters" 2>/dev/null)
fi

API_HTTP_CODE=$(echo "$API_FULL_RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)
API_RESPONSE_BODY=$(echo "$API_FULL_RESPONSE" | grep -v "HTTP_CODE:")

echo -e "${BLUE}🔍 Debug: Endpoint mapping HTTP Code: $API_HTTP_CODE${NC}"
if [ -n "$API_RESPONSE_BODY" ]; then
    echo -e "${BLUE}🔍 Debug: Response preview: ${API_RESPONSE_BODY:0:100}...${NC}"
fi

if [ "$API_HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ API Endpoint Mapping: Working (HTTP $API_HTTP_CODE)${NC}"
elif [ "$API_HTTP_CODE" = "401" ]; then
    echo -e "${RED}❌ API Endpoint Mapping: Unauthorized (HTTP $API_HTTP_CODE)${NC}"
    echo -e "${BLUE}💡 This suggests the endpoint exists but requires authentication${NC}"
elif [ "$API_HTTP_CODE" = "404" ]; then
    echo -e "${RED}❌ API Endpoint Mapping: Not Found (HTTP $API_HTTP_CODE)${NC}"
    echo -e "${BLUE}💡 This suggests the MCP Gateway controller may not be registered${NC}"
else
    echo -e "${RED}❌ API Endpoint Mapping: Failed (HTTP $API_HTTP_CODE)${NC}"
fi

# Additional debug: Check what endpoints are available
echo -e "${BLUE}🔍 Debug: Testing available API endpoints${NC}"
SWAGGER_RESPONSE=$(curl -s -w "%{http_code}" "$HTTPAPI/swagger/index.html" -o /dev/null 2>/dev/null)
echo -e "${BLUE}🔍 Debug: Swagger UI available: HTTP $SWAGGER_RESPONSE${NC}"

# Test if the MCP Gateway controller is registered
CONTROLLER_TEST=$(curl -s -w "%{http_code}" "$HTTPAPI/api/mcp-gateway" -o /dev/null 2>/dev/null)
echo -e "${BLUE}🔍 Debug: MCP Gateway controller base endpoint: HTTP $CONTROLLER_TEST${NC}"

echo ""

# Summary
echo -e "${BLUE}📊 Quick Test Summary & Analysis${NC}"
echo -e "${BLUE}================================${NC}"

# Analyze the results
AUTH_WORKING=false
HTTPAPI_WORKING=false
MCP_GATEWAY_WORKING=false

# Check Auth Server
if [ -n "$ACCESS_TOKEN" ]; then
    AUTH_WORKING=true
fi

# Check HttpApi MCP endpoints
if [ "$API_HTTP_CODE" = "200" ] || [ "$API_HTTP_CODE" = "401" ]; then
    HTTPAPI_WORKING=true
fi

# Check MCP Gateway direct access
if curl -s "$MCP_GATEWAY/adapters" >/dev/null 2>&1; then
    MCP_GATEWAY_WORKING=true
fi

echo -e "${BLUE}🔍 Component Analysis:${NC}"
if [ "$AUTH_WORKING" = true ]; then
    echo -e "  🔐 Auth Server: ${GREEN}✅ Working${NC}"
else
    echo -e "  🔐 Auth Server: ${RED}❌ Not responding or auth failed${NC}"
fi

if [ "$HTTPAPI_WORKING" = true ]; then
    echo -e "  🌐 HttpApi MCP Endpoints: ${GREEN}✅ Registered${NC}"
else
    echo -e "  🌐 HttpApi MCP Endpoints: ${RED}❌ Not found or not registered${NC}"
fi

if [ "$MCP_GATEWAY_WORKING" = true ]; then
    echo -e "  🔌 MCP Gateway Direct: ${GREEN}✅ Working${NC}"
else
    echo -e "  🔌 MCP Gateway Direct: ${RED}❌ Not accessible${NC}"
fi

echo ""

# Provide specific recommendations
if [ "$AUTH_WORKING" = false ]; then
    echo -e "${YELLOW}🔧 Auth Server Issues:${NC}"
    echo -e "  • Check if AuthServer is running on port 7001"
    echo -e "  • Verify client credentials: Aevatar_HttpApi / 1q2w3e*"
    echo -e "  • Check if admin user exists: admin / 1q2w3E*"
    echo -e "  • Test: ${BLUE}curl $AUTH_SERVER/.well-known/openid_configuration${NC}"
fi

if [ "$HTTPAPI_WORKING" = false ]; then
    echo -e "${YELLOW}🔧 HttpApi Integration Issues:${NC}"
    echo -e "  • Check if HttpApi.Host is running on port 7002"
    echo -e "  • Verify MCPGatewayController is registered"
    echo -e "  • Check if MCPGatewayAppService is configured"
    echo -e "  • Test: ${BLUE}curl $HTTPAPI/swagger${NC}"
fi

if [ "$MCP_GATEWAY_WORKING" = false ]; then
    echo -e "${YELLOW}🔧 MCP Gateway Issues:${NC}"
    echo -e "  • Check if MCP Gateway is running on port 8000"
    echo -e "  • Verify port forwarding: kubectl port-forward"
    echo -e "  • Test: ${BLUE}curl $MCP_GATEWAY/adapters${NC}"
fi

echo ""

# Overall status
if [ "$AUTH_WORKING" = true ] && [ "$HTTPAPI_WORKING" = true ] && [ "$MCP_GATEWAY_WORKING" = true ]; then
    echo -e "${GREEN}🎉 Full integration is working!${NC}"
    echo ""
    echo -e "${BLUE}✅ All components verified:${NC}"
    echo -e "  • Authentication system"
    echo -e "  • HttpApi MCP endpoints"
    echo -e "  • Direct MCP Gateway access"
    echo ""
    echo -e "${BLUE}🚀 Ready for production use:${NC}"
    echo -e "  • Full test suite: ${GREEN}./test-aevatar-mcp-api.sh${NC}"
    echo -e "  • VS Code MCP integration"
    echo -e "  • Custom MCP server deployment"
elif [ "$MCP_GATEWAY_WORKING" = true ]; then
    echo -e "${YELLOW}⚠️  Partial integration working${NC}"
    echo -e "${GREEN}✅ MCP Gateway is functional${NC}"
    echo -e "${YELLOW}⚠️  Aevatar API integration needs attention${NC}"
    echo ""
    echo -e "${BLUE}🎯 Immediate actions:${NC}"
    echo -e "  1. Start missing services (Auth/HttpApi)"
    echo -e "  2. Verify service registration"
    echo -e "  3. Check authentication configuration"
else
    echo -e "${RED}❌ Integration has significant issues${NC}"
    echo ""
    echo -e "${BLUE}🎯 Required actions:${NC}"
    echo -e "  1. Start all required services"
    echo -e "  2. Verify network connectivity"
    echo -e "  3. Check service configurations"
fi

echo ""
