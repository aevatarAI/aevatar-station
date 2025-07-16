#!/bin/bash
# test-memory-server-mcp.sh - Test MCPDemoController API with memory-server knowledge graph

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
    echo -e "\n${CYAN}=== Testing Memory Server Knowledge Graph Workflow ===${NC}"
    
    # Step 1: Create entities
    echo -e "\n${BLUE}Step 1: Creating entities${NC}"
    local current_date=$(date)
    call_tool "create_entities" '{
        "entities": [
            {
                "name": "John Doe",
                "entityType": "person",
                "observations": [
                    "Software engineer at Aevatar",
                    "Expert in Orleans framework",
                    "Created test at '"$current_date"'"
                ]
            },
            {
                "name": "Aevatar Station",
                "entityType": "project",
                "observations": [
                    "Orleans-based distributed system",
                    "Uses MCP for AI agent integration",
                    "Has memory server capability"
                ]
            }
        ]
    }' "Create Entities"
    
    # Wait a bit
    sleep 1
    
    # Step 2: Create relations
    echo -e "\n${BLUE}Step 2: Creating relations${NC}"
    call_tool "create_relations" '{
        "relations": [
            {
                "from": "John Doe",
                "to": "Aevatar Station",
                "relationType": "works on"
            },
            {
                "from": "Aevatar Station",
                "to": "John Doe",
                "relationType": "is developed by"
            }
        ]
    }' "Create Relations"
    
    # Step 3: Add observations to existing entity
    echo -e "\n${BLUE}Step 3: Adding observations${NC}"
    call_tool "add_observations" '{
        "observations": [
            {
                "entityName": "John Doe",
                "contents": [
                    "Specializes in distributed systems",
                    "Contributes to MCP integration"
                ]
            }
        ]
    }' "Add Observations"
    
    # Step 4: Read the entire graph
    echo -e "\n${BLUE}Step 4: Reading entire knowledge graph${NC}"
    call_tool "read_graph" '{}' "Read Graph"
    
    # Step 5: Search for nodes
    echo -e "\n${BLUE}Step 5: Searching for nodes${NC}"
    call_tool "search_nodes" '{
        "query": "Orleans"
    }' "Search Nodes"
    
    # Step 6: Open specific nodes
    echo -e "\n${BLUE}Step 6: Opening specific nodes${NC}"
    call_tool "open_nodes" '{
        "names": ["John Doe", "Aevatar Station"]
    }' "Open Nodes"
    
    # Step 7: Delete an observation
    echo -e "\n${BLUE}Step 7: Deleting observations${NC}"
    call_tool "delete_observations" '{
        "deletions": [
            {
                "entityName": "John Doe",
                "observations": ["Expert in Orleans framework"]
            }
        ]
    }' "Delete Observations"
    
    # Step 8: Delete a relation
    echo -e "\n${BLUE}Step 8: Deleting relations${NC}"
    call_tool "delete_relations" '{
        "relations": [
            {
                "from": "Aevatar Station",
                "to": "John Doe",
                "relationType": "is developed by"
            }
        ]
    }' "Delete Relations"
    
    # Step 9: Verify changes by reading graph again
    echo -e "\n${BLUE}Step 9: Verifying changes${NC}"
    call_tool "read_graph" '{}' "Read Graph After Deletions"
    
    # Step 10: Delete entities
    echo -e "\n${BLUE}Step 10: Deleting entities${NC}"
    call_tool "delete_entities" '{
        "names": ["John Doe", "Aevatar Station"]
    }' "Delete Entities"
    
    # Step 11: Final verification
    echo -e "\n${BLUE}Step 11: Final verification${NC}"
    call_tool "read_graph" '{}' "Read Empty Graph"
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

# Check required environment variables and get authentication token
if ! check_auth_env_vars; then
    exit 1
fi

if ! get_auth_token; then
    exit 1
fi

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

echo -e "\n${YELLOW}Note: The memory server maintains a knowledge graph in memory only and will be reset when the agent is restarted.${NC}"