#!/bin/bash

# 定义API端点
API_ENDPOINT="http://localhost:7004/api/noauth"

echo "Waiting for Sandbox.HttpApi.Host to be ready on port 7004..."
until curl -s http://localhost:7004/api/noauth > /dev/null; do
    echo "Sandbox.HttpApi.Host not ready, waiting..."
    sleep 5
done
echo "Sandbox.HttpApi.Host is ready. Running sandbox test..."

# 发送GET请求
echo "Sending GET request to $API_ENDPOINT"
curl -s "$API_ENDPOINT"

echo -e "\n\nSending POST request to $API_ENDPOINT/execute"
curl -s -X POST "$API_ENDPOINT/execute" \
     -H "Content-Type: application/json" \
     -d '{
  "code": "print(\"Hello from sandbox!\")\nimport sys\nprint(f\"Python version: {sys.version}\")",
  "language": "python",
  "resourceLimits": {
    "cpuLimitCores": 0.5,
    "memoryLimitBytes": 134217728,
    "timeoutSeconds": 10
  }
}'

echo -e "\n\nSandbox test complete."