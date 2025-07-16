#!/bin/bash
# test-context7-mcp.sh - Test MCPDemoController API with context7 MCP server

# Color definitions
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
BASE_URL="https://station-developer-dev-staging.aevatar.ai/tool-client/api/mcp-demo"

# Source the authentication script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/get-auth-token.sh"

# Global variables
LAST_RESPONSE=""
AGENT_ID=""

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

# Function to initialize context7 server
initialize_context7() {
    echo -e "\n${CYAN}=== Initializing MCP Agent with Context7 Server ===${NC}"
    
    local init_request='{
        "servers": [{
            "serverName": "context7",
            "command": "npx",
            "args": ["-y", "@upstash/context7-mcp@latest"],
            "environment": {}
        }],
        "timeoutSeconds": 30
    }'
    
    call_api "POST" "initialize" "$init_request" "Initializing Context7 MCP Agent"
    
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
        \"serverName\": \"context7\",
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

# Function to test context7 workflow
test_context7_workflow() {
    echo -e "\n${CYAN}=== Testing Context7 Documentation Retrieval Workflow ===${NC}"
    
    # Wait for tools to be discovered
    echo -e "\n${YELLOW}Waiting for context7 tools to be discovered...${NC}"
    sleep 5
    
    # Get discovered tools
    echo -e "\n${BLUE}Step 1: Discovering available tools${NC}"
    call_api "GET" "tools/$AGENT_ID/context7" "" "Get Available Tools"
    
    # Context7 should provide resolve-library-id and get-library-docs tools
    echo -e "\n${CYAN}Context7 provides library documentation retrieval tools${NC}"
    
    # Step 2: Resolve library IDs for popular libraries
    echo -e "\n${BLUE}Step 2: Resolving library IDs${NC}"
    
    # Test with React
    echo -e "\n${YELLOW}Resolving React library ID...${NC}"
    call_tool "resolve-library-id" '{
        "libraryName": "react"
    }' "Resolve React Library ID"
    
    # Test with Vue
    echo -e "\n${YELLOW}Resolving Vue library ID...${NC}"
    call_tool "resolve-library-id" '{
        "libraryName": "vue"
    }' "Resolve Vue Library ID"
    
    # Test with Express
    echo -e "\n${YELLOW}Resolving Express library ID...${NC}"
    call_tool "resolve-library-id" '{
        "libraryName": "express"
    }' "Resolve Express Library ID"
    
    # Step 3: Get library documentation
    echo -e "\n${BLUE}Step 3: Getting library documentation${NC}"
    
    # Get React hooks documentation
    echo -e "\n${YELLOW}Getting React hooks documentation...${NC}"
    call_tool "get-library-docs" '{
        "context7CompatibleLibraryID": "/facebook/react",
        "topic": "hooks",
        "tokens": 5000
    }' "Get React Hooks Docs"
    
    # Get Vue composition API documentation
    echo -e "\n${YELLOW}Getting Vue composition API documentation...${NC}"
    call_tool "get-library-docs" '{
        "context7CompatibleLibraryID": "/vuejs/core",
        "topic": "composition api",
        "tokens": 3000
    }' "Get Vue Composition API Docs"
    
    # Get Express routing documentation
    echo -e "\n${YELLOW}Getting Express routing documentation...${NC}"
    call_tool "get-library-docs" '{
        "context7CompatibleLibraryID": "/expressjs/express",
        "topic": "routing",
        "tokens": 4000
    }' "Get Express Routing Docs"
    
    # Step 4: Test with specific version
    echo -e "\n${BLUE}Step 4: Testing version-specific documentation${NC}"
    
    echo -e "\n${YELLOW}Getting Next.js v14 documentation...${NC}"
    call_tool "get-library-docs" '{
        "context7CompatibleLibraryID": "/vercel/next.js/v14.0.0",
        "topic": "app router",
        "tokens": 5000
    }' "Get Next.js v14 App Router Docs"
    
    # Step 5: Test edge cases
    echo -e "\n${BLUE}Step 5: Testing edge cases${NC}"
    
    # Test with non-existent library
    echo -e "\n${YELLOW}Testing with non-existent library...${NC}"
    call_tool "resolve-library-id" '{
        "libraryName": "this-library-does-not-exist-12345"
    }' "Resolve Non-existent Library"
    
    # Test with ambiguous name
    echo -e "\n${YELLOW}Testing with ambiguous name...${NC}"
    call_tool "resolve-library-id" '{
        "libraryName": "router"
    }' "Resolve Ambiguous Library Name"
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
echo -e "${GREEN}=== Context7 MCP Server Test Suite ===${NC}"
echo "Base URL: $BASE_URL"

# Check required environment variables and get authentication token
if ! check_auth_env_vars; then
    exit 1
fi

if ! get_auth_token; then
    exit 1
fi

# Initialize context7 server
initialize_context7

if [ -n "$AGENT_ID" ] && [ "$AGENT_ID" != "null" ]; then
    # Wait for server to fully start
    echo -e "\n${YELLOW}Waiting for context7 server to fully start...${NC}"
    sleep 3
    
    # Test context7 workflow
    test_context7_workflow
    
    # Check server states
    check_server_states
    
    # Get history
    get_history
    
    echo -e "\n${GREEN}=== All tests completed ===${NC}"
    echo -e "${CYAN}Agent ID: $AGENT_ID${NC}"
else
    echo -e "\n${RED}=== Failed to initialize context7 MCP agent ===${NC}"
fi

echo -e "\n${YELLOW}Note: Context7 provides library documentation retrieval for AI applications.${NC}"
echo -e "${YELLOW}It helps AI agents access up-to-date documentation for various programming libraries.${NC}"
echo -e "${YELLOW}Available tools: resolve-library-id, get-library-docs${NC}"
echo -e "${YELLOW}Check https://context7.com for more information.${NC}" 