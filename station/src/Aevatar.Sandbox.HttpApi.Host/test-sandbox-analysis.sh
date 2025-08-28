#!/bin/bash

# 定义API端点
API_ENDPOINT="http://localhost:7004/api/noauth/execute"

# 定义要执行的Python代码 - 简单数据分析示例
PYTHON_CODE=$(cat << 'EOF'
import json
import math
import random
import statistics

# 生成一些随机数据
data = [random.uniform(10, 30) for _ in range(20)]

# 基本统计分析
mean = statistics.mean(data)
median = statistics.median(data)
stdev = statistics.stdev(data)
min_val = min(data)
max_val = max(data)

# 输出结果
print("===== SIMPLE DATA ANALYSIS =====")
print(f"Sample size: {len(data)}")
print(f"Mean: {mean:.2f}")
print(f"Median: {median:.2f}")
print(f"Standard Deviation: {stdev:.2f}")
print(f"Range: {min_val:.2f} to {max_val:.2f}")
print(f"Range width: {max_val - min_val:.2f}")

# 简单分类
categories = {
    "low": [x for x in data if x < 15],
    "medium": [x for x in data if 15 <= x < 25],
    "high": [x for x in data if x >= 25]
}

print("\nData Distribution:")
print(f"Low values (<15): {len(categories['low'])} items ({len(categories['low'])/len(data)*100:.1f}%)")
print(f"Medium values (15-25): {len(categories['medium'])} items ({len(categories['medium'])/len(data)*100:.1f}%)")
print(f"High values (>25): {len(categories['high'])} items ({len(categories['high'])/len(data)*100:.1f}%)")

print("\nRaw Data:")
print(json.dumps(data))
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

echo "Sending request to execute simple data analysis code..."
echo "API Endpoint: $API_ENDPOINT"
echo "-----------------------------------"

# 发送请求
RESPONSE=$(curl -s -X POST "$API_ENDPOINT" \
     -H "Content-Type: application/json" \
     -d "$REQUEST_BODY")

echo "Response:"
echo "$RESPONSE" | jq .

echo ""
echo "-----------------------------------"
echo "Execution Results:"
echo "$RESPONSE" | jq -r '.output // "No output returned"'

echo ""
echo "-----------------------------------"
echo "Test completed."