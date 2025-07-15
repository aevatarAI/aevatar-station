#!/bin/bash
# test-tool-calling-with-auth.sh - Test TestToolCallingController APIs with authentication

# Color definitions
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
BASE_URL="https://station-developer-dev-staging.aevatar.ai/tool-client/api/test-tool-calling"
AUTH_URL="https://auth-station-dev-staging.aevatar.ai/connect/token"

# Function to get authentication token
get_auth_token() {
    echo -e "${YELLOW}Getting authentication token...${NC}"
    
    AUTH_RESPONSE=$(curl -s --location "$AUTH_URL" \
        --header 'Content-Type: application/x-www-form-urlencoded' \
        --header 'Accept: application/json' \
        --data-urlencode 'grant_type=password' \
        --data-urlencode 'username=admin' \
        --data-urlencode 'password=1q2W3e*' \
        --data-urlencode 'scope=Aevatar' \
        --data-urlencode 'client_id=AevatarAuthServer')
    
    AUTH_TOKEN=$(echo $AUTH_RESPONSE | jq -r '.access_token')
    
    if [ "$AUTH_TOKEN" == "null" ] || [ -z "$AUTH_TOKEN" ]; then
        echo -e "${RED}✗ Failed to get authentication token${NC}"
        echo "Response: $AUTH_RESPONSE"
        exit 1
    else
        echo -e "${GREEN}✓ Authentication token obtained successfully${NC}"
        echo -e "Token (first 50 chars): ${AUTH_TOKEN:0:50}..."
        export AUTH_TOKEN
    fi
}

# Function to test an API endpoint
test_api() {
    local endpoint=$1
    local data=$2
    local description=$3
    
    echo -e "\n${YELLOW}Testing: $description${NC}"
    echo "Endpoint: $BASE_URL/$endpoint"
    
    response=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/$endpoint" \
        -H "Content-Type: application/json" \
        -H "Authorization: Bearer $AUTH_TOKEN" \
        -d "$data")
    
    http_code=$(echo "$response" | tail -n1)
    json_response=$(echo "$response" | head -n-1)
    
    if [ "$http_code" == "200" ]; then
        if echo "$json_response" | jq -e '.success == true' > /dev/null 2>&1; then
            echo -e "${GREEN}✓ Success (HTTP $http_code)${NC}"
        else
            echo -e "${YELLOW}⚠ Response received but success=false (HTTP $http_code)${NC}"
        fi
    else
        echo -e "${RED}✗ Failed (HTTP $http_code)${NC}"
    fi
    
    echo "Response:"
    echo "$json_response" | jq '.' || echo "$json_response"
}

# Main execution
echo -e "${GREEN}=== TestToolCallingController API Test Suite ===${NC}"
echo "Base URL: $BASE_URL"
echo "Auth URL: $AUTH_URL"

# Get authentication token
get_auth_token

# Test 1: Execute GAgent Event
test_api "execute-gagent-event" '{
    "gAgentType": "Aevatar.Application.Grains.Agents.TestAgent.AgentTest",
    "eventType": "Aevatar.Application.Grains.Agents.TestAgent.FrontTestCreateEvent",
    "eventData": "{\"Name\": \"test-event-001\"}"
}' "Execute GAgent Event"

# Test 2: Test MCPGAgent - Weather Server
test_api "test-mcp-gagent" '{
    "mcpServerName": "weather-server",
    "dockerImage": "node:18-alpine",
    "npmCommand": "npx -y @modelcontextprotocol/weather-server",
    "autoStartServer": true,
    "environment": {
        "NODE_ENV": "development"
    },
    "testToolName": "get-weather",
    "testToolParameters": {
        "location": "Beijing"
    }
}' "MCPGAgent with Weather Server"

# Test 3: Test MCPGAgent - Filesystem Server
test_api "test-mcp-gagent" '{
    "mcpServerName": "filesystem-server",
    "npmCommand": "npx -y @modelcontextprotocol/filesystem-server",
    "autoStartServer": true,
    "environment": {
        "MCP_ALLOWED_PATHS": "/tmp/mcp-test"
    }
}' "MCPGAgent with Filesystem Server"

# Test 8: Error case - Invalid GAgent Type
test_api "execute-gagent-event" '{
    "gAgentType": "Invalid.GAgent.Type",
    "eventType": "Aevatar.Core.EventBase",
    "eventData": "{}"
}' "Error Case - Invalid GAgent Type"

echo -e "\n${GREEN}=== All tests completed ===${NC}"
echo -e "${YELLOW}Note: Replace YOUR_OPENAI_API_KEY with actual key if testing with real LLM${NC}"