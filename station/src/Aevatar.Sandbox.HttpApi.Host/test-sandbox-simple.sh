#!/bin/bash

# 定义API端点
API_ENDPOINT="http://localhost:7004/api/noauth/execute"

# 定义要执行的Python代码 - 简单示例
PYTHON_CODE=$(cat << 'EOF'
print("Hello from sandbox!")
import sys
print(f"Python version: {sys.version}")
import math
print(f"Pi value: {math.pi}")
EOF
)

# 使用jq正确转义Python代码
ESCAPED_CODE=$(echo "$PYTHON_CODE" | jq -Rs .)

# 构建请求体
REQUEST_BODY=$(cat << EOF
{
  "code": $ESCAPED_CODE,
  "language": "python",
  "resourceLimits": {
    "cpuLimitCores": 0.5,
    "memoryLimitBytes": 134217728,
    "timeoutSeconds": 10
  }
}
EOF
)

echo "Sending request to execute simple Python code..."
echo "API Endpoint: $API_ENDPOINT"
echo "-----------------------------------"
echo "Request Body:"
echo "$REQUEST_BODY" | jq .
echo "-----------------------------------"

# 发送请求
RESPONSE=$(curl -s -X POST "$API_ENDPOINT" \
     -H "Content-Type: application/json" \
     -d "$REQUEST_BODY")

echo "Response:"
echo "$RESPONSE" | jq . 2>/dev/null || echo "$RESPONSE"

echo ""
echo "-----------------------------------"
echo "Test completed."