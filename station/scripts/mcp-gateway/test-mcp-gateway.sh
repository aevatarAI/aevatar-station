#!/bin/bash

# Microsoft MCP Gateway Local Testing Script
# Comprehensive testing of deployed MCP Gateway

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🧪 MCP Gateway Comprehensive Testing${NC}"
echo -e "${BLUE}====================================${NC}"

# Default work directory
WORK_DIR="$HOME/mcp-gateway-local"
CONFIG_FILE="$WORK_DIR/config.env"

# Load configuration if exists
if [ -f "$CONFIG_FILE" ]; then
    echo -e "${BLUE}📋 Loading configuration from $CONFIG_FILE${NC}"
    source "$CONFIG_FILE"
else
    echo -e "${RED}❌ Configuration file not found. Please run deploy-mcp-gateway.sh first.${NC}"
    exit 1
fi

GATEWAY_URL="http://localhost:$GATEWAY_PORT"

# Test counter
TESTS_PASSED=0
TESTS_FAILED=0

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

# Function to run JSON test
run_json_test() {
    local test_name="$1"
    local test_command="$2"
    local json_path="$3"
    local expected_value="$4"
    
    echo -e "${BLUE}🧪 Testing: $test_name${NC}"
    
    if response=$(eval "$test_command" 2>&1); then
        if command -v jq >/dev/null 2>&1; then
            actual_value=$(echo "$response" | jq -r "$json_path" 2>/dev/null || echo "null")
            if [ "$actual_value" = "$expected_value" ]; then
                echo -e "${GREEN}✅ PASSED: $test_name${NC}"
                echo -e "${GREEN}   Expected: $expected_value, Got: $actual_value${NC}"
                TESTS_PASSED=$((TESTS_PASSED + 1))
            else
                echo -e "${RED}❌ FAILED: $test_name${NC}"
                echo -e "${RED}   Expected: $expected_value, Got: $actual_value${NC}"
                TESTS_FAILED=$((TESTS_FAILED + 1))
            fi
        else
            # Fallback without jq
            if echo "$response" | grep -q "$expected_value"; then
                echo -e "${GREEN}✅ PASSED: $test_name${NC}"
                TESTS_PASSED=$((TESTS_PASSED + 1))
            else
                echo -e "${RED}❌ FAILED: $test_name${NC}"
                echo -e "${RED}   Expected to find: $expected_value${NC}"
                echo -e "${RED}   In response: $response${NC}"
                TESTS_FAILED=$((TESTS_FAILED + 1))
            fi
        fi
    else
        echo -e "${RED}❌ FAILED: $test_name${NC}"
        echo -e "${RED}   Error: $response${NC}"
        TESTS_FAILED=$((TESTS_FAILED + 1))
    fi
    echo ""
}

echo -e "${BLUE}📋 Test Configuration:${NC}"
echo -e "  🌐 Gateway URL: $GATEWAY_URL"
echo -e "  📁 Work Directory: $WORK_DIR"
echo -e "  🐳 Registry Port: $REGISTRY_PORT"
echo ""

# Test 1: Health Check
run_test "Health Check" \
    "curl -s -w '%{http_code}' $GATEWAY_URL/health" \
    "200"

# Test 2: List Adapters
run_test "List Adapters Endpoint" \
    "curl -s -w '%{http_code}' $GATEWAY_URL/adapters" \
    "200"

# Test 3: Get Specific Adapter (if exists)
run_test "Get MCP Example Adapter" \
    "curl -s $GATEWAY_URL/adapters/mcp-example" \
    "mcp-example"

# Test 4: Adapter Status
run_test "Get Adapter Status" \
    "curl -s -w '%{http_code}' $GATEWAY_URL/adapters/mcp-example/status" \
    "200"

# Test 5: Adapter Logs
run_test "Get Adapter Logs" \
    "curl -s -w '%{http_code}' $GATEWAY_URL/adapters/mcp-example/logs" \
    "200"

# Test 6: MCP Endpoint (Streamable HTTP)
run_test "MCP Streamable HTTP Endpoint" \
    "curl -s -w '%{http_code}' -X POST $GATEWAY_URL/adapters/mcp-example/mcp -H 'Content-Type: application/json' -d '{}'" \
    ""  # Don't check response content, just that it doesn't error

# Test 7: SSE Endpoint
run_test "MCP SSE Endpoint" \
    "curl -s -w '%{http_code}' $GATEWAY_URL/adapters/mcp-example/sse" \
    ""  # Don't check response content, just that it doesn't error

# Test 8: Create New Adapter
echo -e "${BLUE}🧪 Testing: Create New Test Adapter${NC}"
NEW_ADAPTER_JSON='{
  "name": "test-adapter-2",
  "imageName": "mcp-example",
  "imageVersion": "1.0.0",
  "description": "Second test adapter"
}'

CREATE_RESPONSE=$(curl -s -w '\n%{http_code}' -X POST $GATEWAY_URL/adapters \
    -H "Content-Type: application/json" \
    -d "$NEW_ADAPTER_JSON")

HTTP_CODE=$(echo "$CREATE_RESPONSE" | tail -n1)
RESPONSE_BODY=$(echo "$CREATE_RESPONSE" | head -n -1)

if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "201" ]; then
    echo -e "${GREEN}✅ PASSED: Create New Test Adapter${NC}"
    TESTS_PASSED=$((TESTS_PASSED + 1))
    
    # Test 9: Verify the new adapter exists
    run_test "Verify New Adapter Exists" \
        "curl -s $GATEWAY_URL/adapters/test-adapter-2" \
        "test-adapter-2"
    
    # Test 10: Delete the new adapter
    run_test "Delete Test Adapter" \
        "curl -s -w '%{http_code}' -X DELETE $GATEWAY_URL/adapters/test-adapter-2" \
        "200"
    
else
    echo -e "${RED}❌ FAILED: Create New Test Adapter${NC}"
    echo -e "${RED}   HTTP Code: $HTTP_CODE${NC}"
    echo -e "${RED}   Response: $RESPONSE_BODY${NC}"
    TESTS_FAILED=$((TESTS_FAILED + 1))
fi
echo ""

# Test 11: Kubernetes Integration
echo -e "${BLUE}🧪 Testing: Kubernetes Integration${NC}"
if kubectl get deployment mcpgateway -n adapter >/dev/null 2>&1 && kubectl get pods -n adapter | grep -q "mcpgateway"; then
    echo -e "${GREEN}✅ PASSED: MCP Gateway deployment and pods exist${NC}"
    TESTS_PASSED=$((TESTS_PASSED + 1))
else
    echo -e "${RED}❌ FAILED: MCP Gateway deployment or pods not found${NC}"
    kubectl get deployments -n adapter 2>/dev/null || echo "No deployments found"
    kubectl get pods -n adapter 2>/dev/null || echo "No pods found"
    TESTS_FAILED=$((TESTS_FAILED + 1))
fi
echo ""

# Test 12: Docker Registry
echo -e "${BLUE}🧪 Testing: Docker Registry${NC}"
if curl -s http://localhost:$REGISTRY_PORT/v2/_catalog | grep -q "repositories"; then
    echo -e "${GREEN}✅ PASSED: Docker registry is accessible${NC}"
    TESTS_PASSED=$((TESTS_PASSED + 1))
else
    echo -e "${RED}❌ FAILED: Docker registry is not accessible${NC}"
    TESTS_FAILED=$((TESTS_FAILED + 1))
fi
echo ""

# Test 13: Port Forwarding Health
echo -e "${BLUE}🧪 Testing: Port Forwarding Health${NC}"
if [ -f "$WORK_DIR/port-forward.pid" ]; then
    PID=$(cat "$WORK_DIR/port-forward.pid")
    if ps -p $PID > /dev/null 2>&1; then
        echo -e "${GREEN}✅ PASSED: Port forwarding process is running (PID: $PID)${NC}"
        TESTS_PASSED=$((TESTS_PASSED + 1))
    else
        echo -e "${RED}❌ FAILED: Port forwarding process is not running${NC}"
        TESTS_FAILED=$((TESTS_FAILED + 1))
    fi
else
    echo -e "${RED}❌ FAILED: Port forwarding PID file not found${NC}"
    TESTS_FAILED=$((TESTS_FAILED + 1))
fi
echo ""

# Test Summary
echo -e "${BLUE}📊 Test Summary${NC}"
echo -e "${BLUE}===============${NC}"
echo -e "${GREEN}✅ Tests Passed: $TESTS_PASSED${NC}"
echo -e "${RED}❌ Tests Failed: $TESTS_FAILED${NC}"
echo -e "${BLUE}📋 Total Tests: $((TESTS_PASSED + TESTS_FAILED))${NC}"
echo ""

if [ $TESTS_FAILED -eq 0 ]; then
    echo -e "${GREEN}🎉 All tests passed! MCP Gateway is working correctly.${NC}"
    echo ""
    echo -e "${BLUE}🔗 Quick Access Links:${NC}"
    echo -e "  🌐 MCP Gateway API: ${GREEN}$GATEWAY_URL${NC}"
    echo -e "  📚 API Documentation: ${GREEN}$GATEWAY_URL/swagger${NC} (if available)"
    echo -e "  🔍 Health Check: ${GREEN}$GATEWAY_URL/health${NC}"
    echo -e "  📋 List Adapters: ${GREEN}$GATEWAY_URL/adapters${NC}"
    echo ""
    echo -e "${BLUE}🧪 MCP Connection Examples:${NC}"
    echo -e "  🔗 Streamable HTTP: ${GREEN}$GATEWAY_URL/adapters/mcp-example/mcp${NC}"
    echo -e "  🔗 SSE Endpoint: ${GREEN}$GATEWAY_URL/adapters/mcp-example/sse${NC}"
    echo ""
    echo -e "${YELLOW}💡 VS Code MCP Configuration:${NC}"
    echo -e "  Create ${GREEN}.vscode/mcp.json${NC} with:"
    echo -e "  {\"servers\": {\"mcp-example\": {\"url\": \"$GATEWAY_URL/adapters/mcp-example/mcp\"}}}"
    echo ""
    exit 0
else
    echo -e "${RED}❌ Some tests failed. Please check the deployment.${NC}"
    echo ""
    echo -e "${YELLOW}🔧 Troubleshooting Tips:${NC}"
    echo -e "  • Check gateway logs: ${YELLOW}kubectl logs -n adapter deployment/mcpgateway-service${NC}"
    echo -e "  • Check adapter pods: ${YELLOW}kubectl get pods -n adapter${NC}"
    echo -e "  • Verify port forwarding: ${YELLOW}ps aux | grep 'kubectl port-forward'${NC}"
    echo -e "  • Test connectivity: ${YELLOW}curl -v $GATEWAY_URL/health${NC}"
    echo ""
    exit 1
fi
