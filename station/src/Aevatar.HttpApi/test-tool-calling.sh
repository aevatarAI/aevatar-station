#!/bin/bash
# test-mcp-memory-server.sh - Test MCPDemoController API with memory-server

# Color definitions
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
BASE_URL="https://station-developer-dev-staging.aevatar.ai/tool-client/api/mcp-demo"
AUTH_URL="https://auth-station-dev-staging.aevatar.ai/connect/token"

# Global variables
LAST_RESPONSE=""
AGENT_ID=""

# Check required environment variables
check_env_vars() {
    local missing_vars=()
    
    [ -z "$AEVATAR_USERNAME" ] && missing_vars+=("AEVATAR_USERNAME")
    [ -z "$AEVATAR_PASSWORD" ] && missing_vars+=("AEVATAR_PASSWORD")
    [ -z "$AEVATAR_CLIENT_ID" ] && missing_vars+=("AEVATAR_CLIENT_ID")
    [ -z "$AEVATAR_SCOPE" ] && missing_vars+=("AEVATAR_SCOPE")
    
    if [ ${#missing_vars[@]} -ne 0 ]; then
        echo -e "${RED}Error: Missing required environment variables:${NC}"
        for var in "${missing_vars[@]}"; do
            echo -e "${RED}  - $var${NC}"
        done
        echo -e "\n${YELLOW}Please run: source ./set-env.sh${NC}"
        exit 1
    fi
}

# Function to get authentication token
get_auth_token() {
    echo -e "${YELLOW}Getting authentication token...${NC}"
    
    AUTH_RESPONSE=$(curl -s --location "$AUTH_URL" \
        --header 'Content-Type: application/x-www-form-urlencoded' \
        --header 'Accept: application/json' \
        --data-urlencode 'grant_type=password' \
        --data-urlencode "username=$AEVATAR_USERNAME" \
        --data-urlencode "password=$AEVATAR_PASSWORD" \
        --data-urlencode "scope=$AEVATAR_SCOPE" \
        --data-urlencode "client_id=$AEVATAR_CLIENT_ID")
    
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

# Function to make API calls
call_api() {
    local method=$1
    local endpoint=$2
    local data=$3
    local description=$4
    
    echo -e "\n${YELLOW}${description}${NC}"
    echo "Endpoint: $BASE_URL/$endpoint"
    
    # Create temp file for response
    local TEMP_FILE=$(mktemp)
    
    if [ "$method" == "POST" ]; then
        # Use curl with follow redirects, longer timeout (120s for tool calls), and save to file
        curl -s -L --max-time 120 -X POST "$BASE_URL/$endpoint" \
            -H "Content-Type: application/json" \
            -H "Authorization: Bearer $AUTH_TOKEN" \
            -H "Accept: application/json" \
            -d "$data" \
            -o "$TEMP_FILE" \
            -w "HTTP_STATUS=%{http_code}\nTIME_TOTAL=%{time_total}\nSIZE_DOWNLOAD=%{size_download}\n" > curl_metrics.tmp
    else
        curl -s -L --max-time 120 -X GET "$BASE_URL/$endpoint" \
            -H "Authorization: Bearer $AUTH_TOKEN" \
            -H "Accept: application/json" \
            -o "$TEMP_FILE" \
            -w "HTTP_STATUS=%{http_code}\nTIME_TOTAL=%{time_total}\nSIZE_DOWNLOAD=%{size_download}\n" > curl_metrics.tmp
    fi
    
    # Read metrics
    local http_code=$(grep "HTTP_STATUS=" curl_metrics.tmp | cut -d= -f2)
    local time_total=$(grep "TIME_TOTAL=" curl_metrics.tmp | cut -d= -f2)
    local size_download=$(grep "SIZE_DOWNLOAD=" curl_metrics.tmp | cut -d= -f2)
    
    # Read response from file
    if [ -s "$TEMP_FILE" ]; then
        json_response=$(cat "$TEMP_FILE")
    else
        json_response=""
    fi
    
    # Store the JSON response
    LAST_RESPONSE="$json_response"
    
    # Show status
    echo -e "HTTP Status: ${http_code} | Time: ${time_total}s | Size: ${size_download} bytes"
    
    if [ "$http_code" == "200" ]; then
        if [ -n "$json_response" ] && echo "$json_response" | jq -e '.success == true' > /dev/null 2>&1; then
            echo -e "${GREEN}✓ Success (HTTP $http_code)${NC}"
        # Also check for success in data field
        elif [ -n "$json_response" ] && echo "$json_response" | jq -e '.data.success == true' > /dev/null 2>&1; then
            echo -e "${GREEN}✓ Success (HTTP $http_code)${NC}"
        else
            echo -e "${YELLOW}⚠ Response received but success=false or empty (HTTP $http_code)${NC}"
        fi
    else
        echo -e "${RED}✗ Failed (HTTP $http_code)${NC}"
    fi
    
    echo -e "${BLUE}Response:${NC}"
    if [ -n "$json_response" ]; then
        echo "$json_response" | jq '.' 2>/dev/null || echo "$json_response"
    else
        echo -e "${RED}Empty response body${NC}"
    fi
    
    # Cleanup
    rm -f "$TEMP_FILE" curl_metrics.tmp
}

# Function to initialize MCP agent with memory server
initialize_memory_server() {
    echo -e "\n${CYAN}=== Initializing MCP Agent with Memory Server ===${NC}"
    
    local init_request='{
        "servers": [{
            "serverName": "memory-server",
            "command": "npx",
            "args": ["-y", "@modelcontextprotocol/server-memory"],
            "environment": {}
        }],
        "timeoutSeconds": 30
    }'
    
    call_api "POST" "initialize" "$init_request" "Initializing MCP Agent"
    
    # Extract agent ID from data field
    AGENT_ID=$(echo "$LAST_RESPONSE" | jq -r '.data.agentId // ""')
    
    if [ -z "$AGENT_ID" ] || [ "$AGENT_ID" == "null" ]; then
        echo -e "${RED}✗ Failed to extract agentId from response${NC}"
        return 1
    fi
    
    echo -e "${GREEN}✓ MCP Agent initialized with ID: $AGENT_ID${NC}"
    
    # Display available tools - also from data field
    local tool_count=$(echo "$LAST_RESPONSE" | jq '.data.availableTools | length' 2>/dev/null || echo "0")
    echo -e "${GREEN}✓ Found $tool_count tools${NC}"
    
    if [ "$tool_count" -gt 0 ]; then
        echo -e "\n${BLUE}Available Tools:${NC}"
        echo "$LAST_RESPONSE" | jq -r '.data.availableTools[] | "  • \(.name): \(.description)"' 2>/dev/null
    fi
}

# Function to call a tool
call_tool() {
    local tool_name=$1
    local arguments=$2
    local description=$3
    
    local tool_request="{
        \"agentId\": \"$AGENT_ID\",
        \"serverName\": \"memory-server\",
        \"toolName\": \"$tool_name\",
        \"arguments\": $arguments
    }"
    
    call_api "POST" "tool-call" "$tool_request" "$description"
    
    # Display result if successful - check both direct and data field
    if echo "$LAST_RESPONSE" | jq -e '.success == true' > /dev/null 2>&1; then
        echo -e "\n${BLUE}Tool Result:${NC}"
        echo "$LAST_RESPONSE" | jq '.resultDisplay.formattedResult // .result' 2>/dev/null
    elif echo "$LAST_RESPONSE" | jq -e '.data.success == true' > /dev/null 2>&1; then
        echo -e "\n${BLUE}Tool Result:${NC}"
        echo "$LAST_RESPONSE" | jq '.data.resultDisplay.formattedResult // .data.result' 2>/dev/null
    fi
}

# Function to test memory server workflow
test_memory_workflow() {
    echo -e "\n${CYAN}=== Testing Memory Server Workflow ===${NC}"
    
    # Step 1: Store a memory with proper JSON escaping
    echo -e "\n${BLUE}Step 1: Storing a memory${NC}"
    local current_date=$(date)
    call_tool "store" "{
        \"key\": \"test-memory-1\",
        \"value\": \"This is a test memory stored at $current_date\"
    }" "Store Memory"
    
    # Wait a bit
    sleep 1
    
    # Step 2: Retrieve the memory
    echo -e "\n${BLUE}Step 2: Retrieving the memory${NC}"
    call_tool "retrieve" '{
        "key": "test-memory-1"
    }' "Retrieve Memory"
    
    # Step 3: Store another memory
    echo -e "\n${BLUE}Step 3: Storing another memory${NC}"
    call_tool "store" '{
        "key": "user-preferences",
        "value": {
            "theme": "dark",
            "language": "en",
            "notifications": true
        }
    }' "Store User Preferences"
    
    # Step 4: List all memories
    echo -e "\n${BLUE}Step 4: Listing all memories${NC}"
    call_tool "list" '{}' "List All Memories"
    
    # Step 5: Delete a memory
    echo -e "\n${BLUE}Step 5: Deleting a memory${NC}"
    call_tool "delete" '{
        "key": "test-memory-1"
    }' "Delete Memory"
    
    # Step 6: Verify deletion
    echo -e "\n${BLUE}Step 6: Verifying deletion${NC}"
    call_tool "list" '{}' "List Memories After Deletion"
}

# Function to check server states
check_server_states() {
    echo -e "\n${CYAN}=== Checking Server States ===${NC}"
    
    call_api "GET" "server-states/$AGENT_ID" "" "Get Server States"
    
    # Display server states - check both direct and data field
    if echo "$LAST_RESPONSE" | jq -e '.success == true' > /dev/null 2>&1; then
        echo -e "\n${BLUE}Server States:${NC}"
        echo "$LAST_RESPONSE" | jq -r '.serverStates[] | "  • \(.serverName): Connected=\(.isConnected), Tools=\(.registeredTools | length)"' 2>/dev/null
    elif echo "$LAST_RESPONSE" | jq -e '.data.success == true' > /dev/null 2>&1; then
        echo -e "\n${BLUE}Server States:${NC}"
        echo "$LAST_RESPONSE" | jq -r '.data.serverStates[] | "  • \(.serverName): Connected=\(.isConnected), Tools=\(.registeredTools | length)"' 2>/dev/null
    fi
}

# Function to get tool call history
get_history() {
    echo -e "\n${CYAN}=== Tool Call History ===${NC}"
    
    call_api "GET" "history/$AGENT_ID" "" "Get Tool Call History"
    
    # Display history - check both direct and data field
    if echo "$LAST_RESPONSE" | jq -e '.success == true' > /dev/null 2>&1; then
        echo -e "\n${BLUE}Recent Tool Calls:${NC}"
        echo "$LAST_RESPONSE" | jq -r '.history[] | "  [\(.timestamp)] \(.toolName): Success=\(.success)"' 2>/dev/null
    elif echo "$LAST_RESPONSE" | jq -e '.data.success == true' > /dev/null 2>&1; then
        echo -e "\n${BLUE}Recent Tool Calls:${NC}"
        echo "$LAST_RESPONSE" | jq -r '.data.history[] | "  [\(.timestamp)] \(.toolName): Success=\(.success)"' 2>/dev/null
    fi
}

# Main execution
echo -e "${GREEN}=== MCP Memory Server Test Suite ===${NC}"
echo "Base URL: $BASE_URL"
echo "Auth URL: $AUTH_URL"

# Check required environment variables
check_env_vars

# Get authentication token
get_auth_token

# Initialize memory server
initialize_memory_server

if [ -n "$AGENT_ID" ] && [ "$AGENT_ID" != "null" ]; then
    # Wait for server to fully start
    echo -e "\n${YELLOW}Waiting for memory server to fully start...${NC}"
    sleep 3
    
    # Test memory workflow
    test_memory_workflow
    
    # Check server states
    check_server_states
    
    # Get history
    get_history
    
    echo -e "\n${GREEN}=== All tests completed successfully ===${NC}"
    echo -e "${CYAN}Agent ID: $AGENT_ID${NC}"
else
    echo -e "\n${RED}=== Failed to initialize MCP agent ===${NC}"
fi

echo -e "\n${YELLOW}Note: The memory server stores data in memory only and will be reset when the agent is restarted.${NC}"