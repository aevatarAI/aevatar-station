#!/bin/bash

# =============================================================================
# Quick MCP Services Test Script
# 快速测试所有MCP服务的可用性
# =============================================================================

# 服务器URL
AUTH_SERVER="http://env-a30ba821.auth-station-testing.aevatar.ai"
API_SERVER="http://env-a30ba821.station-testing.aevatar.ai"
MCP_GATEWAY="http://env-a30ba821.mcp-testing.aevatar.ai"

# MCP服务器列表
declare -A SERVERS=(
    ["Auth Server"]="$AUTH_SERVER"
    ["API Server"]="$API_SERVER"
    ["MCP Gateway"]="$MCP_GATEWAY"
    ["Time Server"]="$MCP_GATEWAY/adapters/time/mcp"
    ["Fetch Server"]="$MCP_GATEWAY/adapters/fetch/mcp"
    ["Filesystem Server"]="$MCP_GATEWAY/adapters/filesystem/mcp"
    ["Git Server"]="$MCP_GATEWAY/adapters/git/mcp"
    ["Memory Server"]="$MCP_GATEWAY/adapters/memory/mcp"
    ["Sequential Thinking"]="$MCP_GATEWAY/adapters/sequentialthinking/mcp"
)

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}=== Quick MCP Services Health Check ===${NC}\n"

# 快速健康检查
for name in "${!SERVERS[@]}"; do
    url="${SERVERS[$name]}"
    echo -n "Testing $name... "
    
    # 尝试多个端点
    status_code=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 "$url" 2>/dev/null || echo "000")
    
    if [[ "$status_code" == "000" ]]; then
        # 尝试健康检查端点
        status_code=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 "$url/health" 2>/dev/null || echo "000")
    fi
    
    if [[ "$status_code" =~ ^[23][0-9][0-9]$ ]]; then
        echo -e "${GREEN}✓ OK (HTTP $status_code)${NC}"
    else
        echo -e "${RED}✗ FAIL (HTTP $status_code)${NC}"
    fi
done

echo -e "\n${BLUE}=== Testing MCP Tools List ===${NC}\n"

# 测试MCP服务器的工具列表
MCP_SERVERS=(
    "time:$MCP_GATEWAY/adapters/time/mcp"
    "fetch:$MCP_GATEWAY/adapters/fetch/mcp"
    "filesystem:$MCP_GATEWAY/adapters/filesystem/mcp"
    "git:$MCP_GATEWAY/adapters/git/mcp"
    "memory:$MCP_GATEWAY/adapters/memory/mcp"
    "sequentialthinking:$MCP_GATEWAY/adapters/sequentialthinking/mcp"
)

for server in "${MCP_SERVERS[@]}"; do
    IFS=':' read -r name url <<< "$server"
    echo -n "Getting tools from $name... "
    
    # MCP tools/list 请求
    tools_request='{
        "jsonrpc": "2.0",
        "id": 1,
        "method": "tools/list",
        "params": {}
    }'
    
    response=$(curl -s --max-time 10 \
        -H "Content-Type: application/json" \
        -d "$tools_request" \
        "$url" 2>/dev/null)
    
    if echo "$response" | grep -q '"result"' && echo "$response" | grep -q '"tools"' 2>/dev/null; then
        tool_count=$(echo "$response" | grep -o '"name"' | wc -l | tr -d ' ')
        echo -e "${GREEN}✓ $tool_count tools${NC}"
    else
        echo -e "${YELLOW}? Unknown response${NC}"
    fi
done

echo -e "\n${BLUE}=== Quick API Test ===${NC}\n"

# 测试API端点
api_endpoints=(
    "/health"
    "/api/mcp-gateway/health"
    "/api/mcp-gateway/servers"
)

for endpoint in "${api_endpoints[@]}"; do
    echo -n "Testing API $endpoint... "
    status_code=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 "$API_SERVER$endpoint" 2>/dev/null || echo "000")
    
    if [[ "$status_code" =~ ^[23][0-9][0-9]$ ]]; then
        echo -e "${GREEN}✓ OK (HTTP $status_code)${NC}"
    else
        echo -e "${RED}✗ FAIL (HTTP $status_code)${NC}"
    fi
done

echo -e "\n${BLUE}=== Test Complete ===${NC}"


