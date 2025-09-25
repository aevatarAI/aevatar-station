#!/bin/bash

# =============================================================================
# MCP Tools Detailed Testing Script
# 详细测试每个MCP服务器的工具功能
# =============================================================================

set -euo pipefail

# MCP Gateway URL
readonly MCP_GATEWAY="http://env-a30ba821.mcp-testing.aevatar.ai"

# 颜色设置
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m'

# 日志函数
log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }
log_header() { echo -e "\n${PURPLE}=== $1 ===${NC}"; }

# MCP请求函数
mcp_request() {
    local url="$1"
    local method="$2"
    local params="$3"
    local id="${4:-1}"
    
    local request="{
        \"jsonrpc\": \"2.0\",
        \"id\": $id,
        \"method\": \"$method\",
        \"params\": $params
    }"
    
    curl -s --max-time 30 \
        -H "Content-Type: application/json" \
        -d "$request" \
        "$url" 2>/dev/null || echo '{"error": "request_failed"}'
}

# 测试MCP服务器初始化
test_mcp_initialize() {
    local name="$1"
    local url="$2"
    
    log_info "Initializing MCP connection to $name..."
    
    local params='{
        "protocolVersion": "2024-11-05",
        "capabilities": {
            "roots": {"listChanged": true},
            "sampling": {}
        },
        "clientInfo": {
            "name": "mcp-test-client",
            "version": "1.0.0"
        }
    }'
    
    local response
    response=$(mcp_request "$url" "initialize" "$params" 1)
    
    if echo "$response" | jq -e '.result.capabilities' >/dev/null 2>&1; then
        log_success "Successfully initialized $name"
        echo "$response" | jq -r '.result.capabilities | keys[]' | sed 's/^/  Capability: /'
        return 0
    else
        log_error "Failed to initialize $name"
        echo "$response" | jq -r '.error.message // "Unknown error"' 2>/dev/null || echo "  Invalid JSON response"
        return 1
    fi
}

# 获取并显示工具列表
test_tools_list() {
    local name="$1"
    local url="$2"
    
    log_info "Getting tools list from $name..."
    
    local response
    response=$(mcp_request "$url" "tools/list" "{}" 2)
    
    if echo "$response" | jq -e '.result.tools' >/dev/null 2>&1; then
        local tool_count
        tool_count=$(echo "$response" | jq '.result.tools | length')
        log_success "Found $tool_count tools in $name"
        
        echo "$response" | jq -r '.result.tools[] | "  🔧 \(.name): \(.description // "No description")"'
        
        # 返回工具列表用于后续测试
        echo "$response" | jq -r '.result.tools[].name'
        return 0
    else
        log_error "Failed to get tools from $name"
        echo "$response" | jq -r '.error.message // "Unknown error"' 2>/dev/null || echo "  Invalid JSON response"
        return 1
    fi
}

# 测试特定工具调用
test_tool_call() {
    local name="$1"
    local url="$2"
    local tool_name="$3"
    local arguments="$4"
    
    log_info "Testing tool '$tool_name' in $name..."
    
    local params="{
        \"name\": \"$tool_name\",
        \"arguments\": $arguments
    }"
    
    local response
    response=$(mcp_request "$url" "tools/call" "$params" 3)
    
    if echo "$response" | jq -e '.result' >/dev/null 2>&1; then
        log_success "Tool '$tool_name' executed successfully"
        echo "$response" | jq -r '.result.content[]? | "  📄 \(.type): \(.text // .data // "No content")"' 2>/dev/null || echo "  ✅ Tool executed (no content to display)"
        return 0
    else
        log_warning "Tool '$tool_name' failed or returned error"
        echo "$response" | jq -r '.error.message // "Unknown error"' 2>/dev/null || echo "  Invalid JSON response"
        return 1
    fi
}

# 获取资源列表
test_resources_list() {
    local name="$1"
    local url="$2"
    
    log_info "Getting resources list from $name..."
    
    local response
    response=$(mcp_request "$url" "resources/list" "{}" 4)
    
    if echo "$response" | jq -e '.result.resources' >/dev/null 2>&1; then
        local resource_count
        resource_count=$(echo "$response" | jq '.result.resources | length')
        if [[ "$resource_count" -gt 0 ]]; then
            log_success "Found $resource_count resources in $name"
            echo "$response" | jq -r '.result.resources[] | "  📁 \(.uri): \(.name // "No name") - \(.description // "No description")"'
        else
            log_info "No resources available in $name"
        fi
        return 0
    else
        log_info "No resources endpoint or resources available in $name"
        return 1
    fi
}

# 测试时间服务器特定功能
test_time_server() {
    local url="$MCP_GATEWAY/adapters/time/mcp"
    
    log_header "Testing Time Server"
    
    if test_mcp_initialize "Time Server" "$url"; then
        local tools
        tools=$(test_tools_list "Time Server" "$url")
        
        # 测试获取当前时间
        if echo "$tools" | grep -q "get_current_time"; then
            test_tool_call "Time Server" "$url" "get_current_time" "{}"
        fi
        
        # 测试时区转换
        if echo "$tools" | grep -q "convert_timezone"; then
            test_tool_call "Time Server" "$url" "convert_timezone" '{"datetime": "2024-01-01T12:00:00Z", "from_timezone": "UTC", "to_timezone": "America/New_York"}'
        fi
        
        test_resources_list "Time Server" "$url"
    fi
}

# 测试Fetch服务器特定功能
test_fetch_server() {
    local url="$MCP_GATEWAY/adapters/fetch/mcp"
    
    log_header "Testing Fetch Server"
    
    if test_mcp_initialize "Fetch Server" "$url"; then
        local tools
        tools=$(test_tools_list "Fetch Server" "$url")
        
        # 测试HTTP GET请求
        if echo "$tools" | grep -q -E "(fetch|get|http)"; then
            local fetch_tool
            fetch_tool=$(echo "$tools" | grep -E "(fetch|get|http)" | head -1)
            test_tool_call "Fetch Server" "$url" "$fetch_tool" '{"url": "https://httpbin.org/json"}'
        fi
        
        test_resources_list "Fetch Server" "$url"
    fi
}

# 测试文件系统服务器特定功能
test_filesystem_server() {
    local url="$MCP_GATEWAY/adapters/filesystem/mcp"
    
    log_header "Testing Filesystem Server"
    
    if test_mcp_initialize "Filesystem Server" "$url"; then
        local tools
        tools=$(test_tools_list "Filesystem Server" "$url")
        
        # 测试列出目录
        if echo "$tools" | grep -q -E "(list|ls|dir)"; then
            local list_tool
            list_tool=$(echo "$tools" | grep -E "(list|ls|dir)" | head -1)
            test_tool_call "Filesystem Server" "$url" "$list_tool" '{"path": "/"}'
        fi
        
        test_resources_list "Filesystem Server" "$url"
    fi
}

# 测试Git服务器特定功能
test_git_server() {
    local url="$MCP_GATEWAY/adapters/git/mcp"
    
    log_header "Testing Git Server"
    
    if test_mcp_initialize "Git Server" "$url"; then
        local tools
        tools=$(test_tools_list "Git Server" "$url")
        
        # 测试Git状态
        if echo "$tools" | grep -q "status"; then
            test_tool_call "Git Server" "$url" "git_status" '{"repo_path": "."}'
        fi
        
        test_resources_list "Git Server" "$url"
    fi
}

# 测试内存服务器特定功能
test_memory_server() {
    local url="$MCP_GATEWAY/adapters/memory/mcp"
    
    log_header "Testing Memory Server"
    
    if test_mcp_initialize "Memory Server" "$url"; then
        local tools
        tools=$(test_tools_list "Memory Server" "$url")
        
        # 测试存储记忆
        if echo "$tools" | grep -q -E "(store|save|create)"; then
            local store_tool
            store_tool=$(echo "$tools" | grep -E "(store|save|create)" | head -1)
            test_tool_call "Memory Server" "$url" "$store_tool" '{"key": "test_key", "value": "test_value", "metadata": {"test": true}}'
        fi
        
        # 测试检索记忆
        if echo "$tools" | grep -q -E "(get|retrieve|search)"; then
            local get_tool
            get_tool=$(echo "$tools" | grep -E "(get|retrieve|search)" | head -1)
            test_tool_call "Memory Server" "$url" "$get_tool" '{"key": "test_key"}'
        fi
        
        test_resources_list "Memory Server" "$url"
    fi
}

# 测试顺序思维服务器特定功能
test_sequential_thinking_server() {
    local url="$MCP_GATEWAY/adapters/sequentialthinking/mcp"
    
    log_header "Testing Sequential Thinking Server"
    
    if test_mcp_initialize "Sequential Thinking Server" "$url"; then
        local tools
        tools=$(test_tools_list "Sequential Thinking Server" "$url")
        
        # 测试思维链
        if echo "$tools" | grep -q -E "(think|reasoning|sequential)"; then
            local think_tool
            think_tool=$(echo "$tools" | grep -E "(think|reasoning|sequential)" | head -1)
            test_tool_call "Sequential Thinking Server" "$url" "$think_tool" '{"thought": "This is a test thought", "nextThoughtNeeded": false, "thoughtNumber": 1, "totalThoughts": 1}'
        fi
        
        test_resources_list "Sequential Thinking Server" "$url"
    fi
}

# 检查依赖
check_dependencies() {
    local deps=("curl" "jq")
    local missing_deps=()
    
    for dep in "${deps[@]}"; do
        if ! command -v "$dep" &> /dev/null; then
            missing_deps+=("$dep")
        fi
    done
    
    if [[ ${#missing_deps[@]} -gt 0 ]]; then
        log_error "Missing dependencies: ${missing_deps[*]}"
        log_info "Please install: brew install ${missing_deps[*]}"
        exit 1
    fi
}

# 主函数
main() {
    echo -e "${PURPLE}"
    echo "============================================================================"
    echo "                    MCP Tools Detailed Testing Script"
    echo "                   Testing Individual MCP Server Tools"
    echo "============================================================================"
    echo -e "${NC}"
    
    check_dependencies
    
    # 测试所有MCP服务器
    test_time_server
    test_fetch_server
    test_filesystem_server
    test_git_server
    test_memory_server
    test_sequential_thinking_server
    
    log_header "All MCP Tools Tests Complete"
    log_info "Check the output above for detailed results"
}

# 脚本入口点
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi


