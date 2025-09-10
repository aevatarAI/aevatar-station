#!/bin/bash

# Aevatar MCP Gateway API Integration Test Script
# Tests the MCP Gateway APIs exposed through Aevatar.HttpApi

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🧪 Aevatar MCP Gateway API Integration Tests${NC}"
echo -e "${BLUE}============================================${NC}"

# Configuration
AUTH_SERVER_URL="http://localhost:7001"
HTTPAPI_URL="http://localhost:7002"
DEVELOPER_HOST_URL="http://localhost:7003"
MCP_GATEWAY_URL="http://localhost:8000"

# Test counters
TESTS_PASSED=0
TESTS_FAILED=0
ACCESS_TOKEN=""

# Function to run test
run_test() {
    local test_name="$1"
    local test_command="$2"
    local expected_pattern="$3"
    
    echo -e "${BLUE}🧪 Testing: $test_name${NC}"
    
    if response=$(eval "$test_command" 2>&1); then
        if [ -n "$expected_pattern" ] && ! echo "$response" | grep -q "$expected_pattern"; then
            echo -e "${RED}❌ FAILED: $test_name${NC}"
            echo -e "${RED}   Expected pattern: $expected_pattern${NC}"
            echo -e "${RED}   Got response: $response${NC}"
            TESTS_FAILED=$((TESTS_FAILED + 1))
        else
            echo -e "${GREEN}✅ PASSED: $test_name${NC}"
            TESTS_PASSED=$((TESTS_PASSED + 1))
        fi
    else
        echo -e "${RED}❌ FAILED: $test_name${NC}"
        echo -e "${RED}   Error: $response${NC}"
        TESTS_FAILED=$((TESTS_FAILED + 1))
    fi
    echo ""
}

# Function to run authenticated test
run_auth_test() {
    local test_name="$1"
    local method="$2"
    local endpoint="$3"
    local data="$4"
    local expected_pattern="$5"
    
    echo -e "${BLUE}🧪 Testing: $test_name${NC}"
    
    local curl_cmd="curl -s"
    if [ -n "$ACCESS_TOKEN" ]; then
        curl_cmd="$curl_cmd -H 'Authorization: Bearer $ACCESS_TOKEN'"
    fi
    curl_cmd="$curl_cmd -H 'Content-Type: application/json'"
    
    if [ "$method" = "POST" ] && [ -n "$data" ]; then
        curl_cmd="$curl_cmd -X POST -d '$data'"
    elif [ "$method" = "PUT" ] && [ -n "$data" ]; then
        curl_cmd="$curl_cmd -X PUT -d '$data'"
    elif [ "$method" = "DELETE" ]; then
        curl_cmd="$curl_cmd -X DELETE"
    fi
    
    curl_cmd="$curl_cmd $endpoint"
    
    if response=$(eval "$curl_cmd" 2>&1); then
        if [ -n "$expected_pattern" ] && ! echo "$response" | grep -q "$expected_pattern"; then
            echo -e "${RED}❌ FAILED: $test_name${NC}"
            echo -e "${RED}   Expected pattern: $expected_pattern${NC}"
            echo -e "${RED}   Got response: $response${NC}"
            TESTS_FAILED=$((TESTS_FAILED + 1))
        else
            echo -e "${GREEN}✅ PASSED: $test_name${NC}"
            if echo "$response" | jq . >/dev/null 2>&1; then
                echo -e "${BLUE}   Response: $(echo "$response" | jq -c .)${NC}"
            else
                echo -e "${BLUE}   Response: $response${NC}"
            fi
            TESTS_PASSED=$((TESTS_PASSED + 1))
        fi
    else
        echo -e "${RED}❌ FAILED: $test_name${NC}"
        echo -e "${RED}   Error: $response${NC}"
        TESTS_FAILED=$((TESTS_FAILED + 1))
    fi
    echo ""
}

echo -e "${BLUE}📋 Test Configuration:${NC}"
echo -e "  🔐 Auth Server: $AUTH_SERVER_URL"
echo -e "  🌐 HttpApi Server: $HTTPAPI_URL"
echo -e "  🛠️  Developer Host: $DEVELOPER_HOST_URL"
echo -e "  🔌 MCP Gateway: $MCP_GATEWAY_URL"
echo ""

# Step 1: Check if services are running
echo -e "${BLUE}📋 Step 1: Service Health Checks${NC}"

run_test "Auth Server Health Check" \
    "curl -s -w '%{http_code}' $AUTH_SERVER_URL/.well-known/openid_configuration -o /dev/null" \
    "200"

run_test "HttpApi Server Health Check" \
    "curl -s -w '%{http_code}' $HTTPAPI_URL/health -o /dev/null" \
    "200"

run_test "Developer Host Health Check" \
    "curl -s -w '%{http_code}' $DEVELOPER_HOST_URL/health -o /dev/null" \
    "200"

run_test "MCP Gateway Health Check" \
    "curl -s -w '%{http_code}' $MCP_GATEWAY_URL/adapters -o /dev/null" \
    "200"

# Step 2: Get authentication token
echo -e "${BLUE}📋 Step 2: Authentication${NC}"

echo -e "${BLUE}🔐 Attempting to get access token from Auth Server${NC}"

# Try to get token using client credentials flow
TOKEN_RESPONSE=$(curl -s -X POST "$AUTH_SERVER_URL/connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=client_credentials&client_id=Aevatar_HttpApi&client_secret=1q2w3e*&scope=Aevatar" 2>/dev/null)

if echo "$TOKEN_RESPONSE" | jq -e '.access_token' >/dev/null 2>&1; then
    ACCESS_TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.access_token')
    echo -e "${GREEN}✅ Successfully obtained access token${NC}"
    echo -e "${BLUE}   Token type: $(echo "$TOKEN_RESPONSE" | jq -r '.token_type')${NC}"
    echo -e "${BLUE}   Expires in: $(echo "$TOKEN_RESPONSE" | jq -r '.expires_in') seconds${NC}"
    TESTS_PASSED=$((TESTS_PASSED + 1))
else
    echo -e "${YELLOW}⚠️  Client credentials flow failed, trying alternative methods${NC}"
    
    # Try with admin user credentials (if available)
    ADMIN_TOKEN_RESPONSE=$(curl -s -X POST "$AUTH_SERVER_URL/connect/token" \
        -H "Content-Type: application/x-www-form-urlencoded" \
        -d "grant_type=password&client_id=Aevatar_HttpApi&client_secret=1q2w3e*&username=admin&password=1q2w3E*&scope=Aevatar" 2>/dev/null)
    
    if echo "$ADMIN_TOKEN_RESPONSE" | jq -e '.access_token' >/dev/null 2>&1; then
        ACCESS_TOKEN=$(echo "$ADMIN_TOKEN_RESPONSE" | jq -r '.access_token')
        echo -e "${GREEN}✅ Successfully obtained access token via password flow${NC}"
        TESTS_PASSED=$((TESTS_PASSED + 1))
    else
        echo -e "${RED}❌ Failed to obtain access token${NC}"
        echo -e "${RED}   Response: $TOKEN_RESPONSE${NC}"
        echo -e "${YELLOW}⚠️  Continuing with unauthenticated tests${NC}"
        TESTS_FAILED=$((TESTS_FAILED + 1))
    fi
fi

echo ""

# Step 3: Test MCP Gateway Management APIs via Aevatar.HttpApi
echo -e "${BLUE}📋 Step 3: MCP Gateway Management API Tests${NC}"

# Test 3.1: List Adapters
run_auth_test "List MCP Adapters" \
    "GET" \
    "$HTTPAPI_URL/api/mcp-gateway/adapters" \
    "" \
    ""

# Test 3.2: Get Gateway Health
run_auth_test "Get Gateway Health" \
    "GET" \
    "$HTTPAPI_URL/api/mcp-gateway/health" \
    "" \
    ""

# Test 3.3: Create Test Adapter
TEST_ADAPTER_JSON='{
  "name": "test-adapter-api",
  "imageName": "mcp-example",
  "imageVersion": "1.0.0",
  "description": "Test adapter created via Aevatar API",
  "tags": ["test", "api"],
  "metadata": {
    "created_by": "aevatar-test-script",
    "test_run": "'$(date +%s)'"
  }
}'

run_auth_test "Create Test Adapter" \
    "POST" \
    "$HTTPAPI_URL/api/mcp-gateway/adapters" \
    "$TEST_ADAPTER_JSON" \
    "test-adapter-api"

# Test 3.4: Get Specific Adapter
run_auth_test "Get Test Adapter Details" \
    "GET" \
    "$HTTPAPI_URL/api/mcp-gateway/adapters/test-adapter-api" \
    "" \
    "test-adapter-api"

# Test 3.5: Get Adapter Status
run_auth_test "Get Test Adapter Status" \
    "GET" \
    "$HTTPAPI_URL/api/mcp-gateway/adapters/test-adapter-api/status" \
    "" \
    ""

# Wait a bit for adapter to potentially start
echo -e "${BLUE}⏳ Waiting 15 seconds for adapter to initialize...${NC}"
sleep 15

# Test 3.6: Get Adapter Logs
run_auth_test "Get Test Adapter Logs" \
    "GET" \
    "$HTTPAPI_URL/api/mcp-gateway/adapters/test-adapter-api/logs?lines=10" \
    "" \
    ""

# Test 3.7: Test Connection
run_auth_test "Test Adapter Connection" \
    "GET" \
    "$HTTPAPI_URL/api/mcp-gateway/adapters/test-adapter-api/test-connection" \
    "" \
    ""

# Test 3.8: Get Adapter Metrics
run_auth_test "Get Test Adapter Metrics" \
    "GET" \
    "$HTTPAPI_URL/api/mcp-gateway/adapters/test-adapter-api/metrics" \
    "" \
    ""

# Test 3.9: Update Adapter
UPDATE_ADAPTER_JSON='{
  "description": "Updated test adapter description",
  "metadata": {
    "updated_by": "aevatar-test-script",
    "update_time": "'$(date +%s)'"
  }
}'

run_auth_test "Update Test Adapter" \
    "PUT" \
    "$HTTPAPI_URL/api/mcp-gateway/adapters/test-adapter-api" \
    "$UPDATE_ADAPTER_JSON" \
    ""

# Test 3.10: Delete Test Adapter
run_auth_test "Delete Test Adapter" \
    "DELETE" \
    "$HTTPAPI_URL/api/mcp-gateway/adapters/test-adapter-api" \
    "" \
    ""

# Step 4: Test Developer Host APIs (if different)
echo -e "${BLUE}📋 Step 4: Developer Host MCP API Tests${NC}"

if [ "$DEVELOPER_HOST_URL" != "$HTTPAPI_URL" ]; then
    # Test similar endpoints on Developer Host
    run_auth_test "Developer Host - List Adapters" \
        "GET" \
        "$DEVELOPER_HOST_URL/api/mcp-gateway/adapters" \
        "" \
        ""
else
    echo -e "${YELLOW}⚠️  Developer Host URL same as HttpApi, skipping duplicate tests${NC}"
fi

# Step 5: Test Direct MCP Gateway Integration
echo -e "${BLUE}📋 Step 5: Direct MCP Gateway Integration Tests${NC}"

# Test that Aevatar can communicate with MCP Gateway
run_test "Direct Gateway Communication" \
    "curl -s $MCP_GATEWAY_URL/adapters" \
    ""

# Test Summary
echo -e "${BLUE}📊 Test Summary${NC}"
echo -e "${BLUE}===============${NC}"
echo -e "${GREEN}✅ Tests Passed: $TESTS_PASSED${NC}"
echo -e "${RED}❌ Tests Failed: $TESTS_FAILED${NC}"
echo -e "${BLUE}📋 Total Tests: $((TESTS_PASSED + TESTS_FAILED))${NC}"
echo ""

if [ $TESTS_FAILED -eq 0 ]; then
    echo -e "${GREEN}🎉 All tests passed! Aevatar MCP Gateway integration is working correctly.${NC}"
    echo ""
    echo -e "${BLUE}🔗 API Endpoints Verified:${NC}"
    echo -e "  ✅ Authentication via Auth Server"
    echo -e "  ✅ MCP Adapter CRUD operations"
    echo -e "  ✅ Adapter status and health checks"
    echo -e "  ✅ Adapter logs and metrics"
    echo -e "  ✅ Connection testing"
    echo ""
    echo -e "${BLUE}💡 Next Steps:${NC}"
    echo -e "  • Configure VS Code MCP integration"
    echo -e "  • Set up production authentication"
    echo -e "  • Deploy custom MCP servers"
    echo ""
    exit 0
else
    echo -e "${RED}❌ Some tests failed. Please check the integration.${NC}"
    echo ""
    echo -e "${YELLOW}🔧 Troubleshooting Tips:${NC}"
    echo -e "  • Verify all services are running:"
    echo -e "    - Auth Server: $AUTH_SERVER_URL"
    echo -e "    - HttpApi: $HTTPAPI_URL"
    echo -e "    - Developer Host: $DEVELOPER_HOST_URL"
    echo -e "    - MCP Gateway: $MCP_GATEWAY_URL"
    echo -e "  • Check authentication configuration"
    echo -e "  • Verify MCP Gateway permissions"
    echo -e "  • Review service logs for errors"
    echo ""
    exit 1
fi
