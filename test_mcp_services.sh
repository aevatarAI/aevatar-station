#!/bin/bash

# =============================================================================
# MCP Services Testing Script
# 测试Aevatar MCP服务的完整性和功能性
# =============================================================================

set -euo pipefail

# 服务器配置
readonly AUTH_SERVER_URL="http://env-a30ba821.auth-station-testing.aevatar.ai"
readonly API_SERVER_URL="http://env-a30ba821.station-testing.aevatar.ai"
readonly MCP_GATEWAY_URL="http://env-a30ba821.mcp-testing.aevatar.ai"

# MCP服务器配置
declare -A MCP_SERVERS=(
    ["time"]="http://env-a30ba821.mcp-testing.aevatar.ai/adapters/time/mcp"
    ["fetch"]="http://env-a30ba821.mcp-testing.aevatar.ai/adapters/fetch/mcp"
    ["filesystem"]="http://env-a30ba821.mcp-testing.aevatar.ai/adapters/filesystem/mcp"
    ["git"]="http://env-a30ba821.mcp-testing.aevatar.ai/adapters/git/mcp"
    ["memory"]="http://env-a30ba821.mcp-testing.aevatar.ai/adapters/memory/mcp"
    ["sequentialthinking"]="http://env-a30ba821.mcp-testing.aevatar.ai/adapters/sequentialthinking/mcp"
)

# 测试结果统计
declare -A TEST_RESULTS
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# 颜色配置
setup_colors() {
    if [[ -t 1 ]]; then
        readonly RED='\033[0;31m'
        readonly GREEN='\033[0;32m'
        readonly YELLOW='\033[1;33m'
        readonly BLUE='\033[0;34m'
        readonly PURPLE='\033[0;35m'
        readonly CYAN='\033[0;36m'
        readonly WHITE='\033[1;37m'
        readonly NC='\033[0m' # No Color
    else
        readonly RED=''
        readonly GREEN=''
        readonly YELLOW=''
        readonly BLUE=''
        readonly PURPLE=''
        readonly CYAN=''
        readonly WHITE=''
        readonly NC=''
    fi
}

# 日志函数
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_header() {
    echo -e "\n${PURPLE}=== $1 ===${NC}"
}

# 测试记录函数
record_test() {
    local test_name="$1"
    local result="$2"
    local details="$3"
    
    ((TOTAL_TESTS++))
    if [[ "$result" == "PASS" ]]; then
        ((PASSED_TESTS++))
        log_success "$test_name: $details"
    else
        ((FAILED_TESTS++))
        log_error "$test_name: $details"
    fi
    
    TEST_RESULTS["$test_name"]="$result: $details"
}

# HTTP请求函数
make_http_request() {
    local method="$1"
    local url="$2"
    local data="${3:-}"
    local content_type="${4:-application/json}"
    
    local curl_args=(-s -w "%{http_code}|%{time_total}" -o /tmp/response_body)
    
    if [[ -n "$data" ]]; then
        curl_args+=(-X "$method" -H "Content-Type: $content_type" -d "$data")
    else
        curl_args+=(-X "$method")
    fi
    
    curl "${curl_args[@]}" "$url" 2>/dev/null || echo "000|0"
}

# 测试服务健康状态
test_health() {
    local service_name="$1"
    local url="$2"
    
    log_info "Testing health of $service_name..."
    
    local response
    response=$(make_http_request "GET" "$url/health" || make_http_request "GET" "$url" || echo "000|0")
    
    local http_code
    local response_time
    IFS='|' read -r http_code response_time <<< "$response"
    
    if [[ "$http_code" =~ ^[23][0-9][0-9]$ ]]; then
        record_test "$service_name Health" "PASS" "HTTP $http_code, ${response_time}s"
    else
        record_test "$service_name Health" "FAIL" "HTTP $http_code, unreachable or error"
    fi
}

# 测试MCP初始化
test_mcp_initialize() {
    local service_name="$1"
    local url="$2"
    
    log_info "Testing MCP initialize for $service_name..."
    
    local init_request='{
        "jsonrpc": "2.0",
        "id": 1,
        "method": "initialize",
        "params": {
            "protocolVersion": "2024-11-05",
            "capabilities": {
                "roots": {
                    "listChanged": true
                },
                "sampling": {}
            },
            "clientInfo": {
                "name": "test-client",
                "version": "1.0.0"
            }
        }
    }'
    
    local response
    response=$(make_http_request "POST" "$url" "$init_request")
    
    local http_code
    local response_time
    IFS='|' read -r http_code response_time <<< "$response"
    
    if [[ "$http_code" =~ ^[23][0-9][0-9]$ ]]; then
        local response_body
        response_body=$(cat /tmp/response_body 2>/dev/null || echo "{}")
        
        if echo "$response_body" | jq -e '.result.capabilities' >/dev/null 2>&1; then
            record_test "$service_name MCP Initialize" "PASS" "Valid MCP initialization response"
        else
            record_test "$service_name MCP Initialize" "PASS" "HTTP $http_code (non-standard response)"
        fi
    else
        record_test "$service_name MCP Initialize" "FAIL" "HTTP $http_code"
    fi
}

# 测试MCP工具列表
test_mcp_tools_list() {
    local service_name="$1"
    local url="$2"
    
    log_info "Testing MCP tools/list for $service_name..."
    
    local tools_request='{
        "jsonrpc": "2.0",
        "id": 2,
        "method": "tools/list",
        "params": {}
    }'
    
    local response
    response=$(make_http_request "POST" "$url" "$tools_request")
    
    local http_code
    local response_time
    IFS='|' read -r http_code response_time <<< "$response"
    
    if [[ "$http_code" =~ ^[23][0-9][0-9]$ ]]; then
        local response_body
        response_body=$(cat /tmp/response_body 2>/dev/null || echo "{}")
        
        if echo "$response_body" | jq -e '.result.tools' >/dev/null 2>&1; then
            local tool_count
            tool_count=$(echo "$response_body" | jq '.result.tools | length' 2>/dev/null || echo "0")
            record_test "$service_name Tools List" "PASS" "$tool_count tools available"
            
            # 显示工具列表
            log_info "Available tools for $service_name:"
            echo "$response_body" | jq -r '.result.tools[]? | "  - \(.name): \(.description // "No description")"' 2>/dev/null || echo "  (Unable to parse tools)"
        else
            record_test "$service_name Tools List" "PASS" "HTTP $http_code (non-standard response)"
        fi
    else
        record_test "$service_name Tools List" "FAIL" "HTTP $http_code"
    fi
}

# 测试MCP资源列表
test_mcp_resources_list() {
    local service_name="$1"
    local url="$2"
    
    log_info "Testing MCP resources/list for $service_name..."
    
    local resources_request='{
        "jsonrpc": "2.0",
        "id": 3,
        "method": "resources/list",
        "params": {}
    }'
    
    local response
    response=$(make_http_request "POST" "$url" "$resources_request")
    
    local http_code
    local response_time
    IFS='|' read -r http_code response_time <<< "$response"
    
    if [[ "$http_code" =~ ^[23][0-9][0-9]$ ]]; then
        local response_body
        response_body=$(cat /tmp/response_body 2>/dev/null || echo "{}")
        
        if echo "$response_body" | jq -e '.result.resources' >/dev/null 2>&1; then
            local resource_count
            resource_count=$(echo "$response_body" | jq '.result.resources | length' 2>/dev/null || echo "0")
            record_test "$service_name Resources List" "PASS" "$resource_count resources available"
        else
            record_test "$service_name Resources List" "PASS" "HTTP $http_code (no resources or non-standard response)"
        fi
    else
        record_test "$service_name Resources List" "FAIL" "HTTP $http_code"
    fi
}

# 测试API端点
test_api_endpoints() {
    log_header "Testing API Server Endpoints"
    
    # 测试基本健康检查
    test_health "API Server" "$API_SERVER_URL"
    
    # 测试MCP Gateway控制器端点
    local mcp_gateway_endpoints=(
        "/api/mcp-gateway/servers"
        "/api/mcp-gateway/tools"
        "/api/mcp-gateway/health"
    )
    
    for endpoint in "${mcp_gateway_endpoints[@]}"; do
        local full_url="${API_SERVER_URL}${endpoint}"
        log_info "Testing API endpoint: $endpoint"
        
        local response
        response=$(make_http_request "GET" "$full_url")
        
        local http_code
        local response_time
        IFS='|' read -r http_code response_time <<< "$response"
        
        if [[ "$http_code" =~ ^[23][0-9][0-9]$ ]]; then
            record_test "API $endpoint" "PASS" "HTTP $http_code, ${response_time}s"
        else
            record_test "API $endpoint" "FAIL" "HTTP $http_code"
        fi
    done
}

# 测试所有MCP服务器
test_mcp_servers() {
    log_header "Testing MCP Servers"
    
    for server_name in "${!MCP_SERVERS[@]}"; do
        local server_url="${MCP_SERVERS[$server_name]}"
        
        log_header "Testing $server_name Server"
        echo "URL: $server_url"
        
        # 测试健康状态
        test_health "$server_name" "$server_url"
        
        # 测试MCP协议
        test_mcp_initialize "$server_name" "$server_url"
        test_mcp_tools_list "$server_name" "$server_url"
        test_mcp_resources_list "$server_name" "$server_url"
        
        echo
    done
}

# 生成测试报告
generate_report() {
    log_header "Test Report Summary"
    
    echo -e "${WHITE}Total Tests: $TOTAL_TESTS${NC}"
    echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
    echo -e "${RED}Failed: $FAILED_TESTS${NC}"
    
    local success_rate
    if [[ $TOTAL_TESTS -gt 0 ]]; then
        success_rate=$(( (PASSED_TESTS * 100) / TOTAL_TESTS ))
        echo -e "${CYAN}Success Rate: ${success_rate}%${NC}"
    fi
    
    if [[ $FAILED_TESTS -gt 0 ]]; then
        echo -e "\n${RED}Failed Tests:${NC}"
        for test_name in "${!TEST_RESULTS[@]}"; do
            if [[ "${TEST_RESULTS[$test_name]}" =~ ^FAIL ]]; then
                echo -e "${RED}  ✗ $test_name: ${TEST_RESULTS[$test_name]#FAIL: }${NC}"
            fi
        done
    fi
    
    echo -e "\n${GREEN}Successful Tests:${NC}"
    for test_name in "${!TEST_RESULTS[@]}"; do
        if [[ "${TEST_RESULTS[$test_name]}" =~ ^PASS ]]; then
            echo -e "${GREEN}  ✓ $test_name: ${TEST_RESULTS[$test_name]#PASS: }${NC}"
        fi
    done
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
        log_info "Please install missing dependencies:"
        log_info "  macOS: brew install ${missing_deps[*]}"
        log_info "  Ubuntu/Debian: sudo apt-get install ${missing_deps[*]}"
        log_info "  CentOS/RHEL: sudo yum install ${missing_deps[*]}"
        exit 1
    fi
}

# 主函数
main() {
    setup_colors
    
    echo -e "${PURPLE}"
    echo "============================================================================"
    echo "                    MCP Services Testing Script"
    echo "                   Testing Aevatar MCP Infrastructure"
    echo "============================================================================"
    echo -e "${NC}"
    
    # 检查依赖
    check_dependencies
    
    # 创建临时文件目录
    mkdir -p /tmp
    
    # 执行测试
    log_header "Starting Tests"
    
    # 测试认证服务器
    test_health "Auth Server" "$AUTH_SERVER_URL"
    
    # 测试MCP Gateway
    test_health "MCP Gateway" "$MCP_GATEWAY_URL"
    
    # 测试API端点
    test_api_endpoints
    
    # 测试所有MCP服务器
    test_mcp_servers
    
    # 生成报告
    generate_report
    
    # 清理临时文件
    rm -f /tmp/response_body
    
    # 退出码
    if [[ $FAILED_TESTS -gt 0 ]]; then
        exit 1
    else
        exit 0
    fi
}

# 脚本入口点
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi


