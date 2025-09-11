#!/bin/bash

# Debug Auth Server Configuration

echo "🔍 Debugging Auth Server Configuration"
echo "====================================="

AUTH_SERVER="http://localhost:7001"

echo "📋 Step 1: Check Auth Server Discovery"
curl -s "$AUTH_SERVER/.well-known/openid_configuration" | jq '.' 2>/dev/null || {
    echo "❌ OpenID configuration not available"
    exit 1
}

echo ""
echo "📋 Step 2: Check Token Endpoint"
TOKEN_ENDPOINT=$(curl -s "$AUTH_SERVER/.well-known/openid_configuration" | jq -r '.token_endpoint' 2>/dev/null)
echo "Token endpoint: $TOKEN_ENDPOINT"

echo ""
echo "📋 Step 3: Test Token Request (detailed)"
echo "Request details:"
echo "POST $TOKEN_ENDPOINT"
echo "Content-Type: application/x-www-form-urlencoded"
echo "Body: grant_type=client_credentials&client_id=Aevatar_App&client_secret=1q2w3e*&scope=Aevatar"

echo ""
echo "Response:"
curl -v -X POST "$TOKEN_ENDPOINT" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=client_credentials&client_id=Aevatar_App&client_secret=1q2w3e*&scope=Aevatar"
