#!/bin/bash
# get-auth-token.sh - Reusable authentication token getter

# Color definitions
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
AUTH_URL="https://auth-station-dev-staging.aevatar.ai/connect/token"

# Check required environment variables
check_auth_env_vars() {
    local missing_vars=()
    
    [ -z "$AEVATAR_USERNAME" ] && missing_vars+=("AEVATAR_USERNAME")
    [ -z "$AEVATAR_PASSWORD" ] && missing_vars+=("AEVATAR_PASSWORD")
    [ -z "$AEVATAR_CLIENT_ID" ] && missing_vars+=("AEVATAR_CLIENT_ID")
    [ -z "$AEVATAR_SCOPE" ] && missing_vars+=("AEVATAR_SCOPE")
    
    if [ ${#missing_vars[@]} -ne 0 ]; then
        echo -e "${RED}Error: Missing required environment variables:${NC}"
        for var in "${missing_vars[@]}"; do
            echo -e "${RED}  - $var${NC}"
        done
        echo -e "\n${YELLOW}Please run: source ./set-env.sh${NC}"
        return 1
    fi
    return 0
}

# Function to get authentication token
get_auth_token() {
    echo -e "${YELLOW}Getting authentication token...${NC}"
    
    AUTH_RESPONSE=$(curl -s --location "$AUTH_URL" \
        --header 'Content-Type: application/x-www-form-urlencoded' \
        --header 'Accept: application/json' \
        --data-urlencode 'grant_type=password' \
        --data-urlencode "username=$AEVATAR_USERNAME" \
        --data-urlencode "password=$AEVATAR_PASSWORD" \
        --data-urlencode "scope=$AEVATAR_SCOPE" \
        --data-urlencode "client_id=$AEVATAR_CLIENT_ID")
    
    AUTH_TOKEN=$(echo $AUTH_RESPONSE | jq -r '.access_token')
    
    if [ "$AUTH_TOKEN" == "null" ] || [ -z "$AUTH_TOKEN" ]; then
        echo -e "${RED}✗ Failed to get authentication token${NC}"
        echo "Response: $AUTH_RESPONSE"
        return 1
    else
        echo -e "${GREEN}✓ Authentication token obtained successfully${NC}"
        echo -e "Token (first 50 chars): ${AUTH_TOKEN:0:50}..."
        export AUTH_TOKEN
        return 0
    fi
}

# Main execution if run directly
if [ "${BASH_SOURCE[0]}" == "${0}" ]; then
    check_auth_env_vars && get_auth_token
fi 