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
    "replyLimit": 10,
    "userName": "test-twitter-username",
    "userId": "test-twitter-user-id",
    "token": "test-oauth-token",
    "tokenSecret": "test-oauth-token-secret"
  }
}'

# Test data without account binding
WORKFLOW_DATA_NO_BINDING='{
  "workflowName": "TestWorkflowNoBinding",
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

echo "=== Test 1: Workflow with Twitter Account Binding ==="
echo "Data: $WORKFLOW_DATA"

# Test with curl - Twitter binding
echo "Sending POST request with Twitter account binding..."
curl -X POST \
  "$API_URL" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d "$WORKFLOW_DATA" \
  --verbose

echo -e "\n\n=== Test 2: Workflow without Twitter Account Binding ==="
echo "Data: $WORKFLOW_DATA_NO_BINDING"

# Test with curl - No binding
echo "Sending POST request without Twitter account binding..."
curl -X POST \
  "$API_URL" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d "$WORKFLOW_DATA_NO_BINDING" \
  --verbose

echo -e "\n\nTest completed!" 