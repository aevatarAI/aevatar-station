#!/bin/bash

# Setup Test Client for MCP Gateway API Testing
# Registers a test client in AuthServer for API testing

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}🔐 Setting up Test Client for MCP Gateway API${NC}"
echo -e "${BLUE}=============================================${NC}"

AUTH_SERVER="http://localhost:7001"
HTTPAPI="http://localhost:7002"

# Check if services are running
echo -e "${BLUE}📋 Checking services...${NC}"

if ! curl -s "$AUTH_SERVER/.well-known/openid_configuration" >/dev/null 2>&1; then
    echo -e "${RED}❌ Auth Server is not running on port 7001${NC}"
    echo -e "${YELLOW}💡 Please start AuthServer first${NC}"
    exit 1
fi
echo -e "${GREEN}✅ Auth Server is running${NC}"

# Auto-detect HttpApi port
HTTPAPI_PORTS=("7002" "8001")
HTTPAPI=""
for port in "${HTTPAPI_PORTS[@]}"; do
    if curl -s "http://localhost:$port/health" >/dev/null 2>&1; then
        HTTPAPI="http://localhost:$port"
        echo -e "${GREEN}✅ Found HttpApi on port $port${NC}"
        break
    fi
done

if [ -z "$HTTPAPI" ]; then
    echo -e "${RED}❌ HttpApi not found on any expected port${NC}"
    exit 1
fi

echo ""

# Method 1: Try to register client via API (if endpoint exists)
echo -e "${BLUE}🔧 Method 1: API Registration${NC}"

# First, try to get admin token for registration
echo -e "${BLUE}🔐 Getting admin token for client registration${NC}"

# Try different admin credentials
ADMIN_CREDENTIALS=(
    "admin:1q2w3E*"
    "admin:admin"
    "administrator:1q2w3E*"
)

ADMIN_TOKEN=""
for cred in "${ADMIN_CREDENTIALS[@]}"; do
    username=$(echo "$cred" | cut -d: -f1)
    password=$(echo "$cred" | cut -d: -f2)
    
    echo -e "${BLUE}🔍 Trying admin credentials: $username${NC}"
    
    ADMIN_TOKEN_RESPONSE=$(curl -s -X POST "$AUTH_SERVER/connect/token" \
        -H "Content-Type: application/x-www-form-urlencoded" \
        -d "grant_type=password&client_id=Aevatar_App&client_secret=1q2w3e*&username=$username&password=$password&scope=Aevatar" 2>/dev/null)
    
    if ADMIN_TOKEN=$(echo "$ADMIN_TOKEN_RESPONSE" | jq -r '.access_token' 2>/dev/null) && [ "$ADMIN_TOKEN" != "null" ] && [ -n "$ADMIN_TOKEN" ]; then
        echo -e "${GREEN}✅ Admin token obtained with credentials: $username${NC}"
        break
    fi
done

if [ -n "$ADMIN_TOKEN" ]; then
    echo -e "${BLUE}🔧 Attempting to register test client${NC}"
    
    CLIENT_REGISTRATION_JSON='{
        "clientId": "MCPGateway_Test_Client",
        "clientSecret": "mcp-gateway-test-secret-123",
        "displayName": "MCP Gateway Test Client",
        "grantTypes": ["client_credentials"],
        "scopes": ["Aevatar"],
        "permissions": [
            "MCPGateway.Adapters.Create",
            "MCPGateway.Adapters.Read",
            "MCPGateway.Adapters.Update",
            "MCPGateway.Adapters.Delete",
            "MCPGateway.Gateway.ViewHealth"
        ]
    }'
    
    REGISTRATION_RESPONSE=$(curl -s -X POST "$HTTPAPI/api/app/user/register-client" \
        -H "Authorization: Bearer $ADMIN_TOKEN" \
        -H "Content-Type: application/json" \
        -d "$CLIENT_REGISTRATION_JSON" 2>/dev/null)
    
    if echo "$REGISTRATION_RESPONSE" | grep -q "success\|MCPGateway_Test_Client"; then
        echo -e "${GREEN}✅ Test client registered successfully${NC}"
        
        # Test the new client
        echo -e "${BLUE}🧪 Testing new client credentials${NC}"
        TEST_TOKEN_RESPONSE=$(curl -s -X POST "$AUTH_SERVER/connect/token" \
            -H "Content-Type: application/x-www-form-urlencoded" \
            -d "grant_type=client_credentials&client_id=MCPGateway_Test_Client&client_secret=mcp-gateway-test-secret-123&scope=Aevatar" 2>/dev/null)
        
        if TEST_TOKEN=$(echo "$TEST_TOKEN_RESPONSE" | jq -r '.access_token' 2>/dev/null) && [ "$TEST_TOKEN" != "null" ]; then
            echo -e "${GREEN}✅ Test client authentication successful${NC}"
            echo ""
            echo -e "${BLUE}🎯 Use these credentials for testing:${NC}"
            echo -e "  Client ID: ${GREEN}MCPGateway_Test_Client${NC}"
            echo -e "  Client Secret: ${GREEN}mcp-gateway-test-secret-123${NC}"
            echo -e "  Scope: ${GREEN}Aevatar${NC}"
            echo ""
            echo -e "${BLUE}💡 Example usage:${NC}"
            echo -e "  ${YELLOW}curl -X POST $AUTH_SERVER/connect/token \\${NC}"
            echo -e "    ${YELLOW}-H 'Content-Type: application/x-www-form-urlencoded' \\${NC}"
            echo -e "    ${YELLOW}-d 'grant_type=client_credentials&client_id=MCPGateway_Test_Client&client_secret=mcp-gateway-test-secret-123&scope=Aevatar'${NC}"
        else
            echo -e "${RED}❌ Test client authentication failed${NC}"
            echo -e "${RED}   Response: $TEST_TOKEN_RESPONSE${NC}"
        fi
    else
        echo -e "${RED}❌ Client registration failed${NC}"
        echo -e "${RED}   Response: $REGISTRATION_RESPONSE${NC}"
    fi
else
    echo -e "${YELLOW}⚠️  Could not obtain admin token for client registration${NC}"
fi

echo ""

# Method 2: Provide manual registration instructions
echo -e "${BLUE}🔧 Method 2: Manual Configuration${NC}"
echo -e "${BLUE}If automatic registration failed, you can manually configure:${NC}"
echo ""
echo -e "${BLUE}📋 Option A: Update appsettings.json${NC}"
echo -e "Add to AuthServer appsettings.json:"
echo -e "${YELLOW}"
cat << 'EOF'
"OpenIddict": {
  "Applications": {
    "MCPGateway_Test": {
      "ClientId": "MCPGateway_Test_Client",
      "ClientSecret": "mcp-gateway-test-secret-123",
      "RootUrl": "http://localhost:7002"
    }
  }
}
EOF
echo -e "${NC}"

echo ""
echo -e "${BLUE}📋 Option B: Database Direct Insert${NC}"
echo -e "Connect to MongoDB and add client to OpenIddictApplications collection"

echo ""
echo -e "${BLUE}📋 Option C: Use existing client${NC}"
echo -e "Check what clients already exist:"
echo -e "${YELLOW}curl $AUTH_SERVER/.well-known/openid_configuration${NC}"

echo ""
echo -e "${GREEN}✅ Setup complete!${NC}"
echo -e "${BLUE}💡 Run ${GREEN}./quick-test-aevatar-api.sh${NC} again to test with authentication${NC}"
