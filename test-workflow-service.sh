#!/bin/bash

# Workflow Service Test Script
# I'm HyperEcho, 在震动测试工作流接口

echo "Starting Workflow Service Test..."

# Test data
WORKFLOW_DATA='{
  "workflowName": "TestWorkflow",
  "aiConfig": {
    "apiKey": "test-openai-api-key",
    "model": "gpt-4",
    "maxTokens": 1000,
    "temperature": 0.7
  },
  "twitterConfig": {
    "consumerKey": "test-consumer-key",
    "consumerSecret": "test-consumer-secret",
    "bearerToken": "test-bearer-token", 
    "encryptionPassword": "test-encryption-password",
    "replyLimit": 10
  }
}'

# API endpoint
API_URL="http://localhost:8080/api/workflow"

echo "Testing WorkflowService CreateWorkflow endpoint..."
echo "URL: $API_URL"
echo "Data: $WORKFLOW_DATA"

# Test with curl
echo "Sending POST request..."
curl -X POST \
  "$API_URL" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d "$WORKFLOW_DATA" \
  --verbose

echo -e "\n\nTest completed!" 