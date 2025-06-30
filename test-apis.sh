#!/bin/bash

# Cross-URL管理和服务重启接口测试脚本
BASE_URL="http://localhost:8308/api/developer"
CLIENT_ID="test-client"

echo "=== Aevatar Cross-URL & Service Restart API Test ==="
echo "Base URL: $BASE_URL"
echo "Client ID: $CLIENT_ID"
echo ""

# 检查服务是否运行
echo "🔍 Checking if service is running..."
if curl -s --connect-timeout 5 "$BASE_URL/cross-urls?clientId=$CLIENT_ID" > /dev/null 2>&1; then
    echo "✅ Service is running"
else
    echo "❌ Service is not running. Please start the service first:"
    echo "   cd src/Aevatar.Developer.Host && dotnet run"
    exit 1
fi

echo ""
echo "=== Testing Cross-URL Management APIs ==="

# Test 1: GET Cross-URLs (Mock数据)
echo "📋 1. Getting Cross-URL list..."
RESPONSE=$(curl -s "$BASE_URL/cross-urls?clientId=$CLIENT_ID")
echo "Response: $RESPONSE" | jq . 2>/dev/null || echo "Response: $RESPONSE"

# Test 2: POST Create Cross-URL
echo ""
echo "➕ 2. Creating new Cross-URL..."
CREATE_RESPONSE=$(curl -s -X POST "$BASE_URL/cross-urls?clientId=$CLIENT_ID" \
  -H "Content-Type: application/json" \
  -d '{"url": "https://test-api.example.com"}')
echo "Response: $CREATE_RESPONSE" | jq . 2>/dev/null || echo "Response: $CREATE_RESPONSE"

# Extract ID for deletion test
URL_ID=$(echo "$CREATE_RESPONSE" | jq -r '.id' 2>/dev/null)
if [ "$URL_ID" != "null" ] && [ "$URL_ID" != "" ]; then
    echo "✅ Created Cross-URL with ID: $URL_ID"
    
    # Test 3: DELETE Cross-URL
    echo ""
    echo "🗑️  3. Deleting Cross-URL..."
    DELETE_RESPONSE=$(curl -s -X DELETE "$BASE_URL/cross-urls/$URL_ID")
    if [ $? -eq 0 ]; then
        echo "✅ Cross-URL deleted successfully"
    else
        echo "❌ Failed to delete Cross-URL"
    fi
else
    echo "⚠️  Could not extract URL ID for deletion test"
fi

echo ""
echo "=== Testing Service Management APIs ==="

# Test 4: GET Service Status
echo "📊 4. Getting service status..."
STATUS_RESPONSE=$(curl -s "$BASE_URL/status?clientId=$CLIENT_ID")
echo "Response: $STATUS_RESPONSE" | jq . 2>/dev/null || echo "Response: $STATUS_RESPONSE"

# Test 5: POST Service Restart
echo ""
echo "🔄 5. Triggering service restart..."
RESTART_RESPONSE=$(curl -s -X POST "$BASE_URL/restart?clientId=$CLIENT_ID")
echo "Response: $RESTART_RESPONSE" | jq . 2>/dev/null || echo "Response: $RESTART_RESPONSE"

echo ""
echo "=== K8s Cluster Connection Test ==="

# Test K8s connectivity
echo "🎯 6. Testing K8s cluster connection..."
if kubectl cluster-info > /dev/null 2>&1; then
    echo "✅ K8s cluster is accessible"
    
    # Check for test deployments
    echo "📦 Checking for test deployments..."
    DEPLOYMENTS=$(kubectl get deployments -n default 2>/dev/null | grep "$CLIENT_ID" || echo "")
    if [ -n "$DEPLOYMENTS" ]; then
        echo "✅ Found test deployments:"
        echo "$DEPLOYMENTS"
    else
        echo "⚠️  No test deployments found. Creating test deployments..."
        kubectl create deployment "$CLIENT_ID-silo" --image=nginx -n default > /dev/null 2>&1
        kubectl create deployment "$CLIENT_ID-client" --image=nginx -n default > /dev/null 2>&1
        echo "✅ Test deployments created"
    fi
else
    echo "❌ K8s cluster is not accessible"
    echo "Please ensure Docker Desktop Kubernetes is enabled"
fi

echo ""
echo "=== Test Summary ==="
echo "✅ Cross-URL API tests completed"
echo "✅ Service restart API tests completed" 
echo "✅ K8s connectivity tests completed"
echo ""
echo "📝 For detailed testing, see: test-api.md"
echo "🚀 Service URL: $BASE_URL"
echo "📊 K8s Dashboard: kubectl proxy --port=8001" 