#!/bin/bash
# This script tests the sandbox code execution through the Sandbox.HttpApi.Host API

# Wait for the Sandbox.HttpApi.Host to be ready
echo "Waiting for Sandbox.HttpApi.Host to be ready..."
until curl -s http://localhost:7004/swagger/index.html > /dev/null; do
  echo "Waiting for Sandbox.HttpApi.Host..."
  sleep 5
done

echo "Sandbox.HttpApi.Host is ready, testing sandbox code execution..."

# Test Python code execution
echo "Testing Python code execution..."
curl -X 'POST' \
  'http://localhost:7004/api/sandbox/execute' \
  -H 'accept: text/plain' \
  -H 'Content-Type: application/json' \
  -d '{
  "code": "print(\"Hello from k3s sandbox!\")",
  "language": "python",
  "timeout": 10
}'

echo -e "\n\nTest complete!"