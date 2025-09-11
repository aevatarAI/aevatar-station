#!/bin/bash

# Deploy MCP Servers from Configuration File
# Reads mcp-servers.config.json and deploys all enabled servers to MCP Gateway

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}🚀 Deploying MCP Servers from Configuration${NC}"
echo -e "${BLUE}===========================================${NC}"

# Configuration
CONFIG_FILE="${1:-mcp-servers.config.json}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Check if config file exists
if [ ! -f "$CONFIG_FILE" ]; then
    echo -e "${RED}❌ Configuration file not found: $CONFIG_FILE${NC}"
    echo -e "${YELLOW}💡 Usage: $0 [config-file]${NC}"
    echo -e "${YELLOW}💡 Example: $0 mcp-servers.config.json${NC}"
    exit 1
fi

echo -e "${BLUE}📋 Using configuration: $CONFIG_FILE${NC}"

# Parse configuration
GATEWAY_URL=$(jq -r '.gateway.url' "$CONFIG_FILE")
REGISTRY_URL=$(jq -r '.registry.url' "$CONFIG_FILE")
REGISTRY_NAMESPACE=$(jq -r '.registry.namespace' "$CONFIG_FILE")

echo -e "${BLUE}📋 Configuration Summary:${NC}"
echo -e "  🌐 Gateway: $GATEWAY_URL"
echo -e "  🐳 Registry: $REGISTRY_URL"
echo -e "  📦 Namespace: $REGISTRY_NAMESPACE"
echo ""

# Test gateway connectivity
echo -e "${BLUE}📋 Testing Gateway Connectivity${NC}"
if curl -s "$GATEWAY_URL/adapters" >/dev/null 2>&1 || curl -s "$GATEWAY_URL/" | grep -q "404"; then
    echo -e "${GREEN}✅ MCP Gateway is accessible${NC}"
else
    echo -e "${RED}❌ MCP Gateway is not accessible at $GATEWAY_URL${NC}"
    echo -e "${YELLOW}💡 Please ensure MCP Gateway is running${NC}"
    exit 1
fi

# Function to build npm-based MCP server
build_npm_server() {
    local server_name="$1"
    local npm_package="$2"
    local version="$3"
    local environment="$4"
    
    echo -e "${BLUE}🔨 Building npm-based server: $server_name${NC}"
    
    # Create temporary Dockerfile
    local temp_dir="/tmp/mcp-build-$server_name"
    mkdir -p "$temp_dir"
    
    cat > "$temp_dir/Dockerfile" << EOF
FROM node:18-alpine

WORKDIR /app

# Install system dependencies
RUN apk add --no-cache curl bash sqlite git

# Create directories
RUN mkdir -p /workspace /data

# Install the specific MCP server
RUN npm install -g $npm_package@$version

# Create startup script
RUN echo '#!/bin/bash' > /usr/local/bin/start-server.sh && \\
    echo 'echo "Starting $npm_package"' >> /usr/local/bin/start-server.sh && \\
    echo 'exec npx $npm_package \$@' >> /usr/local/bin/start-server.sh && \\
    chmod +x /usr/local/bin/start-server.sh

# Set default environment
$(echo "$environment" | jq -r 'to_entries[] | "ENV " + .key + "=" + (.value | tostring)' | sed 's/^/# /')

EXPOSE 3000

HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \\
  CMD curl -f http://localhost:3000/health || pgrep -f "mcp" || exit 1

CMD ["/usr/local/bin/start-server.sh"]
EOF
    
    local image_name="$REGISTRY_URL/$REGISTRY_NAMESPACE/$server_name:1.0.0"
    
    cd "$temp_dir"
    if docker build -t "$image_name" .; then
        echo -e "${GREEN}✅ Built $server_name image${NC}"
        
        if docker push "$image_name"; then
            echo -e "${GREEN}✅ Pushed $server_name image${NC}"
            rm -rf "$temp_dir"
            return 0
        else
            echo -e "${RED}❌ Failed to push $server_name image${NC}"
            rm -rf "$temp_dir"
            return 1
        fi
    else
        echo -e "${RED}❌ Failed to build $server_name image${NC}"
        rm -rf "$temp_dir"
        return 1
    fi
}

# Function to deploy server via Gateway API
deploy_server() {
    local server_name="$1"
    local server_config="$2"
    
    echo -e "${BLUE}🚀 Deploying $server_name${NC}"
    
    local image_name=$(echo "$server_config" | jq -r '.image // ("'$REGISTRY_NAMESPACE'/" + "'$server_name'")')
    local description=$(echo "$server_config" | jq -r '.description')
    local environment=$(echo "$server_config" | jq '.environment')
    local tags=$(echo "$server_config" | jq '.tags')
    
    local deployment_json=$(jq -n \
        --arg name "$server_name" \
        --arg imageName "$image_name" \
        --arg description "$description" \
        --argjson environment "$environment" \
        --argjson tags "$tags" \
        '{
            name: $name,
            imageName: $imageName,
            imageVersion: "1.0.0",
            description: $description,
            environment: $environment,
            tags: $tags
        }')
    
    echo -e "${BLUE}📋 Deployment JSON:${NC}"
    echo "$deployment_json" | jq '.'
    
    local response=$(curl -s -X POST "$GATEWAY_URL/adapters" \
        -H "Content-Type: application/json" \
        -d "$deployment_json")
    
    if echo "$response" | grep -q "$server_name"; then
        echo -e "${GREEN}✅ $server_name deployed successfully${NC}"
        return 0
    else
        echo -e "${RED}❌ $server_name deployment failed${NC}"
        echo "Response: $response"
        return 1
    fi
}

# Main deployment loop
echo -e "${BLUE}📋 Processing Server Configurations${NC}"

DEPLOYED_COUNT=0
FAILED_COUNT=0

# Get list of enabled servers
ENABLED_SERVERS=$(jq -r '.servers | to_entries[] | select(.value.enabled == true) | .key' "$CONFIG_FILE")

if [ -z "$ENABLED_SERVERS" ]; then
    echo -e "${YELLOW}⚠️  No enabled servers found in configuration${NC}"
    exit 0
fi

echo -e "${BLUE}📋 Enabled servers: $(echo "$ENABLED_SERVERS" | tr '\n' ' ')${NC}"
echo ""

for server_name in $ENABLED_SERVERS; do
    echo -e "${BLUE}📋 Processing: $server_name${NC}"
    
    SERVER_CONFIG=$(jq ".servers.$server_name" "$CONFIG_FILE")
    SERVER_TYPE=$(echo "$SERVER_CONFIG" | jq -r '.type')
    
    case "$SERVER_TYPE" in
        "npm")
            NPM_PACKAGE=$(echo "$SERVER_CONFIG" | jq -r '.package')
            NPM_VERSION=$(echo "$SERVER_CONFIG" | jq -r '.version // "latest"')
            ENVIRONMENT=$(echo "$SERVER_CONFIG" | jq '.environment')
            
            echo -e "${BLUE}📦 npm package: $NPM_PACKAGE@$NPM_VERSION${NC}"
            
            # Build npm-based server
            if build_npm_server "$server_name" "$NPM_PACKAGE" "$NPM_VERSION" "$ENVIRONMENT"; then
                # Deploy to gateway
                if deploy_server "$server_name" "$SERVER_CONFIG"; then
                    DEPLOYED_COUNT=$((DEPLOYED_COUNT + 1))
                else
                    FAILED_COUNT=$((FAILED_COUNT + 1))
                fi
            else
                FAILED_COUNT=$((FAILED_COUNT + 1))
            fi
            ;;
            
        "custom")
            CUSTOM_IMAGE=$(echo "$SERVER_CONFIG" | jq -r '.image')
            echo -e "${BLUE}🐳 Custom image: $CUSTOM_IMAGE${NC}"
            
            # For custom images, assume they're already built
            # Just deploy to gateway
            if deploy_server "$server_name" "$SERVER_CONFIG"; then
                DEPLOYED_COUNT=$((DEPLOYED_COUNT + 1))
            else
                FAILED_COUNT=$((FAILED_COUNT + 1))
            fi
            ;;
            
        "docker")
            DOCKER_IMAGE=$(echo "$SERVER_CONFIG" | jq -r '.image')
            echo -e "${BLUE}🐋 Docker image: $DOCKER_IMAGE${NC}"
            
            # Pull and retag docker image
            if docker pull "$DOCKER_IMAGE"; then
                LOCAL_IMAGE="$REGISTRY_URL/$REGISTRY_NAMESPACE/$server_name:1.0.0"
                docker tag "$DOCKER_IMAGE" "$LOCAL_IMAGE"
                docker push "$LOCAL_IMAGE"
                
                # Update server config with local image
                UPDATED_CONFIG=$(echo "$SERVER_CONFIG" | jq --arg img "$REGISTRY_NAMESPACE/$server_name" '.image = $img')
                
                if deploy_server "$server_name" "$UPDATED_CONFIG"; then
                    DEPLOYED_COUNT=$((DEPLOYED_COUNT + 1))
                else
                    FAILED_COUNT=$((FAILED_COUNT + 1))
                fi
            else
                echo -e "${RED}❌ Failed to pull Docker image: $DOCKER_IMAGE${NC}"
                FAILED_COUNT=$((FAILED_COUNT + 1))
            fi
            ;;
            
        *)
            echo -e "${RED}❌ Unknown server type: $SERVER_TYPE${NC}"
            FAILED_COUNT=$((FAILED_COUNT + 1))
            ;;
    esac
    
    echo ""
done

# Wait for servers to initialize
if [ $DEPLOYED_COUNT -gt 0 ]; then
    echo -e "${BLUE}📋 Waiting for Servers to Initialize${NC}"
    echo -e "${BLUE}⏳ Waiting 30 seconds...${NC}"
    sleep 30
    
    # Test deployed servers
    echo -e "${BLUE}📋 Testing Deployed Servers${NC}"
    
    for server_name in $ENABLED_SERVERS; do
        SERVER_CONFIG=$(jq ".servers.$server_name" "$CONFIG_FILE")
        
        echo -e "${BLUE}🧪 Testing $server_name${NC}"
        
        # Test status
        STATUS_RESPONSE=$(curl -s "$GATEWAY_URL/adapters/$server_name/status" 2>/dev/null)
        if echo "$STATUS_RESPONSE" | grep -q "Healthy\|Running"; then
            echo -e "${GREEN}✅ $server_name: Healthy${NC}"
        else
            echo -e "${YELLOW}⚠️  $server_name: $(echo "$STATUS_RESPONSE" | jq -r '.replicaStatus // "Unknown"' 2>/dev/null)${NC}"
        fi
    done
fi

# Summary
echo ""
echo -e "${BLUE}📊 Deployment Summary${NC}"
echo -e "${BLUE}=====================${NC}"
echo -e "${GREEN}✅ Successfully deployed: $DEPLOYED_COUNT servers${NC}"
echo -e "${RED}❌ Failed deployments: $FAILED_COUNT servers${NC}"
echo -e "${BLUE}📋 Total processed: $((DEPLOYED_COUNT + FAILED_COUNT)) servers${NC}"
echo ""

if [ $DEPLOYED_COUNT -gt 0 ]; then
    echo -e "${GREEN}🎉 Configuration-driven deployment successful!${NC}"
    echo ""
    echo -e "${BLUE}📋 Deployed servers:${NC}"
    curl -s "$GATEWAY_URL/adapters" | jq -r '.[] | "  • " + .name + " (" + .description + ")"' 2>/dev/null || \
        curl -s "$GATEWAY_URL/adapters" | grep -o '"name":"[^"]*"' | sed 's/"name":"//g' | sed 's/"//g' | sed 's/^/  • /'
    echo ""
    echo -e "${BLUE}🔗 VS Code MCP Configuration:${NC}"
    echo -e "${YELLOW}"
    echo "{"
    echo "  \"servers\": {"
    for server_name in $ENABLED_SERVERS; do
        echo "    \"$server_name\": {"
        echo "      \"url\": \"$GATEWAY_URL/adapters/$server_name/mcp\","
        DESCRIPTION=$(jq -r ".servers.$server_name.description" "$CONFIG_FILE")
        echo "      \"description\": \"$DESCRIPTION\""
        echo "    },"
    done | sed '$ s/,$//'
    echo "  }"
    echo "}"
    echo -e "${NC}"
    echo ""
    echo -e "${BLUE}💡 Benefits of Configuration-Driven Approach:${NC}"
    echo -e "  ✅ Declarative - Infrastructure as Code"
    echo -e "  ✅ Reproducible - Same config, same result"
    echo -e "  ✅ Version controlled - Config can be committed to git"
    echo -e "  ✅ Team sharing - Share configs across team members"
    echo -e "  ✅ Environment management - Different configs for dev/staging/prod"
else
    echo -e "${RED}❌ No servers deployed successfully${NC}"
    echo ""
    echo -e "${YELLOW}🔧 Troubleshooting:${NC}"
    echo -e "  • Check MCP Gateway: $GATEWAY_URL"
    echo -e "  • Verify registry: $REGISTRY_URL"
    echo -e "  • Review configuration: $CONFIG_FILE"
    echo -e "  • Check Docker connectivity"
fi

echo ""
