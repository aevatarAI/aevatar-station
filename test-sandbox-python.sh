#!/bin/bash

echo "Waiting for Sandbox.HttpApi.Host to be ready on port 7004..."
until curl -s http://localhost:7004/swagger/index.html > /dev/null; do
    echo "Sandbox.HttpApi.Host not ready, waiting..."
    sleep 5
done
echo "Sandbox.HttpApi.Host is ready. Running sandbox test..."

# 发送请求并格式化输出
echo "Sending request to http://localhost:7004/api/sandbox/execute"
curl -X POST "http://localhost:7004/api/sandbox/execute" \
     -H "Content-Type: application/json" \
     -d '{
  "code": "print(\"Hello from sandbox!\")\nimport sys\nprint(sys.version)",
  "language": "python",
  "resourceLimits": {
    "cpuLimitCores": 0.5,
    "memoryLimitBytes": 134217728,
    "timeoutSeconds": 10
  }
}'

echo -e "\n\nSandbox test complete."