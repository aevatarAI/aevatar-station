#!/bin/bash

# Microsoft MCP Gateway Local Deployment Script
# Based on: https://microsoft.github.io/mcp-gateway/
# Handles port conflicts and provides complete local setup

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
DEFAULT_REGISTRY_PORT=5001  # Changed from 5000 to avoid conflicts
GATEWAY_PORT=8000
MCP_GATEWAY_REPO="https://github.com/microsoft/mcp-gateway.git"
WORK_DIR="$HOME/mcp-gateway-local"
REGISTRY_NAME="local-registry"

echo -e "${BLUE}🚀 Microsoft MCP Gateway Local Deployment${NC}"
echo -e "${BLUE}=============================================${NC}"

# Function to check if port is available
check_port() {
    local port=$1
    if lsof -Pi :$port -sTCP:LISTEN -t >/dev/null 2>&1; then
        return 1  # Port is in use
    else
        return 0  # Port is available
    fi
}

# Function to find available port
find_available_port() {
    local start_port=$1
    local port=$start_port
    
    while ! check_port $port; do
        echo -e "${YELLOW}⚠️  Port $port is in use, trying $((port + 1))${NC}" >&2
        port=$((port + 1))
    done
    
    echo $port
}

# Step 1: Environment Check
echo -e "${BLUE}📋 Step 1: Checking Prerequisites${NC}"

# Check Docker
if ! command -v docker &> /dev/null; then
    echo -e "${RED}❌ Docker is not installed. Please install Docker Desktop.${NC}"
    exit 1
fi

if ! docker info &> /dev/null; then
    echo -e "${RED}❌ Docker is not running. Please start Docker Desktop.${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Docker is available${NC}"

# Check kubectl
if ! command -v kubectl &> /dev/null; then
    echo -e "${RED}❌ kubectl is not installed. Please install kubectl.${NC}"
    exit 1
fi

# Check if Kubernetes is running
if ! kubectl cluster-info &> /dev/null; then
    echo -e "${RED}❌ Kubernetes cluster is not accessible. Please enable Kubernetes in Docker Desktop.${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Kubernetes is available${NC}"

# Check .NET 8 SDK
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}❌ .NET SDK is not installed. Please install .NET 8 SDK.${NC}"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version | cut -d. -f1)
if [ "$DOTNET_VERSION" -lt 8 ]; then
    echo -e "${RED}❌ .NET 8 or higher is required. Current version: $(dotnet --version)${NC}"
    exit 1
fi

echo -e "${GREEN}✅ .NET 8 SDK is available${NC}"

# Step 2: Find available registry port
echo -e "${BLUE}📋 Step 2: Setting up Local Docker Registry${NC}"

REGISTRY_PORT=$(find_available_port $DEFAULT_REGISTRY_PORT)
echo -e "${GREEN}✅ Using registry port: $REGISTRY_PORT${NC}"

# Stop existing registry if running
if docker ps -a --format 'table {{.Names}}' | grep -q "^$REGISTRY_NAME$"; then
    echo -e "${YELLOW}🔄 Stopping existing registry container${NC}"
    docker stop $REGISTRY_NAME || true
    docker rm $REGISTRY_NAME || true
fi

# Start local Docker registry
echo -e "${BLUE}🐳 Starting local Docker registry on port $REGISTRY_PORT${NC}"
docker run -d -p "$REGISTRY_PORT:5000" --name $REGISTRY_NAME registry:2.7

# Verify registry is running
sleep 3
if ! curl -s http://localhost:$REGISTRY_PORT/v2/_catalog > /dev/null; then
    echo -e "${RED}❌ Failed to start Docker registry${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Local Docker registry is running at localhost:$REGISTRY_PORT${NC}"

# Step 3: Setup MCP Gateway Repository (optimized for official image)
echo -e "${BLUE}📋 Step 3: Setting up MCP Gateway Repository${NC}"

# Check if we can use official image directly without source code
USE_OFFICIAL_IMAGE_ONLY=false
echo -e "${BLUE}🔍 Checking if official image is available${NC}"
if docker pull ghcr.io/microsoft/mcp-gateway:latest >/dev/null 2>&1; then
    echo -e "${GREEN}✅ Official image is available, source code clone optional${NC}"
    USE_OFFICIAL_IMAGE_ONLY=true
else
    echo -e "${YELLOW}⚠️  Official image not available, will need source code${NC}"
    USE_OFFICIAL_IMAGE_ONLY=false
fi

if [ "$USE_OFFICIAL_IMAGE_ONLY" = false ]; then
    # Only clone if we need source code for building
    if [ -d "$WORK_DIR" ]; then
        echo -e "${YELLOW}🔄 Removing existing work directory${NC}"
        rm -rf "$WORK_DIR"
    fi

    echo -e "${BLUE}📁 Creating work directory: $WORK_DIR${NC}"
    mkdir -p "$WORK_DIR"
    cd "$WORK_DIR" || {
        echo -e "${RED}❌ Failed to change to work directory: $WORK_DIR${NC}"
        exit 1
    }

    echo -e "${BLUE}📥 Cloning MCP Gateway repository${NC}"
    git clone $MCP_GATEWAY_REPO .
else
    # Create minimal work directory for configuration
    echo -e "${BLUE}📁 Creating minimal work directory: $WORK_DIR${NC}"
    mkdir -p "$WORK_DIR"
    cd "$WORK_DIR" || {
        echo -e "${RED}❌ Failed to change to work directory: $WORK_DIR${NC}"
        exit 1
    }
    
    # Download deployment files only
    echo -e "${BLUE}📥 Downloading deployment configurations${NC}"
    mkdir -p deployment/k8s
    curl -s -o deployment/k8s/local-deployment.yml \
        https://raw.githubusercontent.com/microsoft/mcp-gateway/main/deployment/k8s/local-deployment.yml || {
        echo -e "${YELLOW}⚠️  Failed to download deployment config, cloning full repository${NC}"
        git clone $MCP_GATEWAY_REPO .
    }
fi

# Step 4: Build MCP Example Server Image (if needed)
echo -e "${BLUE}📋 Step 4: Setting up MCP Example Server${NC}"

# Check if we already have the example server image
if docker images localhost:$REGISTRY_PORT/mcp-example:1.0.0 | grep -q "mcp-example" && \
   curl -s http://localhost:$REGISTRY_PORT/v2/mcp-example/tags/list | grep -q "1.0.0"; then
    echo -e "${GREEN}✅ MCP example server image already available${NC}"
else
    if [ -d "mcp-example-server" ]; then
        echo -e "${BLUE}🔨 Building MCP example server image${NC}"
        docker build -f mcp-example-server/Dockerfile mcp-example-server -t localhost:$REGISTRY_PORT/mcp-example:1.0.0

        echo -e "${BLUE}📤 Pushing MCP example server to local registry${NC}"
        docker push localhost:$REGISTRY_PORT/mcp-example:1.0.0

        echo -e "${GREEN}✅ MCP example server image built and pushed${NC}"
    else
        echo -e "${YELLOW}⚠️  mcp-example-server directory not found, skipping example server build${NC}"
        echo -e "${YELLOW}⚠️  You can still use the MCP Gateway without the example server${NC}"
    fi
fi

# Step 5: Setup MCP Gateway Service
echo -e "${BLUE}📋 Step 5: Setting up MCP Gateway Service${NC}"

# Skip .NET build if using official image
if [ "$USE_OFFICIAL_IMAGE_ONLY" = true ]; then
    echo -e "${BLUE}🚀 Using official image, skipping .NET build${NC}"
    # We'll handle the image in the next step
else
    if [ ! -f "dotnet/Microsoft.McpGateway.sln" ]; then
        echo -e "${RED}❌ MCP Gateway solution file not found${NC}"
        exit 1
    fi

    echo -e "${BLUE}🔨 Building MCP Gateway service from source${NC}"
    cd dotnet
fi

# Build the solution (only if building from source)
if [ "$USE_OFFICIAL_IMAGE_ONLY" = false ]; then
    dotnet restore Microsoft.McpGateway.sln
    dotnet build Microsoft.McpGateway.sln -c Release
fi

# Check if we have the publish profile, if not create a simple one (only if building from source)
if [ "$USE_OFFICIAL_IMAGE_ONLY" = false ]; then
    PUBLISH_PROFILE_DIR="Microsoft.McpGateway.Service/Properties/PublishProfiles"
    PUBLISH_PROFILE="$PUBLISH_PROFILE_DIR/localhost_${REGISTRY_PORT}.pubxml"

    mkdir -p "$PUBLISH_PROFILE_DIR"

    cat > "$PUBLISH_PROFILE" << EOF
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <WebPublishMethod>Custom</WebPublishMethod>
    <DockerImageTag>localhost:$REGISTRY_PORT/mcp-gateway:latest</DockerImageTag>
    <ContainerImageTag>localhost:$REGISTRY_PORT/mcp-gateway:latest</ContainerImageTag>
    <PublishUrl>localhost:$REGISTRY_PORT</PublishUrl>
    <Configuration>Release</Configuration>
  </PropertyGroup>
</Project>
EOF
fi

# Publish the container image
echo -e "${BLUE}📤 Publishing MCP Gateway container image${NC}"

# Try multiple approaches to build/obtain the MCP Gateway image
IMAGE_BUILT=false

# Approach 1: Use official Microsoft Docker image (NEW - PREFERRED)
echo -e "${BLUE}🔨 Approach 1: Using official Microsoft MCP Gateway image${NC}"
OFFICIAL_IMAGE="ghcr.io/microsoft/mcp-gateway:latest"
LOCAL_IMAGE="localhost:$REGISTRY_PORT/mcp-gateway:latest"

echo -e "${BLUE}📥 Pulling official Microsoft MCP Gateway image${NC}"
if docker pull "$OFFICIAL_IMAGE"; then
    echo -e "${GREEN}✅ Official image pulled successfully${NC}"
    
    # Tag and push to local registry for Kubernetes
    echo -e "${BLUE}🏷️  Tagging image for local registry${NC}"
    if docker tag "$OFFICIAL_IMAGE" "$LOCAL_IMAGE"; then
        echo -e "${GREEN}✅ Image tagged successfully${NC}"
        
        echo -e "${BLUE}📤 Pushing to local registry${NC}"
        if docker push "$LOCAL_IMAGE"; then
            IMAGE_BUILT=true
            echo -e "${GREEN}✅ Official MCP Gateway image ready in local registry${NC}"
        else
            echo -e "${YELLOW}⚠️  Failed to push to local registry, trying alternative approaches${NC}"
            IMAGE_BUILT=false
        fi
    else
        echo -e "${YELLOW}⚠️  Failed to tag image, trying alternative approaches${NC}"
        IMAGE_BUILT=false
    fi
else
    echo -e "${YELLOW}⚠️  Failed to pull official image, trying alternative approaches${NC}"
    IMAGE_BUILT=false
fi

# Approach 2: Try dotnet publish with container (fallback)
if [ "$IMAGE_BUILT" = false ] && [ "$USE_OFFICIAL_IMAGE_ONLY" = false ]; then
    echo -e "${BLUE}🔨 Approach 2: Using dotnet publish with container${NC}"
    if dotnet publish Microsoft.McpGateway.Service/src/Microsoft.McpGateway.Service.csproj -c Release /p:PublishProfile=localhost_${REGISTRY_PORT}.pubxml 2>/dev/null; then
        echo -e "${GREEN}✅ dotnet publish completed${NC}"
        
        # Verify the image was actually created and pushed
        sleep 2
        if curl -s http://localhost:$REGISTRY_PORT/v2/_catalog | grep -q "mcp-gateway"; then
            IMAGE_BUILT=true
            echo -e "${GREEN}✅ Container published successfully via dotnet publish${NC}"
        else
            echo -e "${YELLOW}⚠️  dotnet publish succeeded but image not found in registry${NC}"
            echo -e "${YELLOW}⚠️  Falling back to manual Docker build${NC}"
            IMAGE_BUILT=false
        fi
    else
        echo -e "${YELLOW}⚠️  dotnet publish failed, trying alternative approaches${NC}"
        IMAGE_BUILT=false
    fi
fi

# Approach 3: Try to find and use Dockerfile
if [ "$IMAGE_BUILT" = false ] && [ "$USE_OFFICIAL_IMAGE_ONLY" = false ]; then
    echo -e "${BLUE}🔨 Approach 3: Looking for Dockerfile${NC}"
    
    # Look for Dockerfile in common locations
    DOCKERFILE_LOCATIONS=(
        "Microsoft.McpGateway.Service/Dockerfile"
        "Microsoft.McpGateway.Service/src/Dockerfile"
        "Dockerfile"
        "src/Dockerfile"
    )
    
    for dockerfile in "${DOCKERFILE_LOCATIONS[@]}"; do
        if [ -f "$dockerfile" ]; then
            echo -e "${BLUE}📁 Found Dockerfile at: $dockerfile${NC}"
            docker build -f "$dockerfile" . -t localhost:$REGISTRY_PORT/mcp-gateway:latest
            docker push localhost:$REGISTRY_PORT/mcp-gateway:latest
            IMAGE_BUILT=true
            break
        fi
    done
fi

# Approach 4: Manual Docker build without Dockerfile
if [ "$IMAGE_BUILT" = false ] && [ "$USE_OFFICIAL_IMAGE_ONLY" = false ]; then
    echo -e "${BLUE}🔨 Approach 4: Creating Dockerfile automatically${NC}"
    
    # Create a comprehensive Dockerfile for MCP Gateway
    echo -e "${BLUE}📝 Creating optimized Dockerfile for MCP Gateway${NC}"
    cat > Dockerfile << 'EOF'
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file and project files for better layer caching
COPY Microsoft.McpGateway.sln .
COPY Directory.Packages.props .
COPY Microsoft.McpGateway.Service/src/Microsoft.McpGateway.Service.csproj Microsoft.McpGateway.Service/src/
COPY Microsoft.McpGateway.Management/src/Microsoft.McpGateway.Management.csproj Microsoft.McpGateway.Management/src/

# Restore dependencies
RUN dotnet restore Microsoft.McpGateway.Service/src/Microsoft.McpGateway.Service.csproj

# Copy all source code
COPY . .

# Build the application
WORKDIR /src/Microsoft.McpGateway.Service/src
RUN dotnet build Microsoft.McpGateway.Service.csproj -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish Microsoft.McpGateway.Service.csproj -c Release -o /app/publish --no-restore

# Final runtime image
FROM runtime AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8000
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_EnableDiagnostics=0

# Create a non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Microsoft.McpGateway.Service.dll"]
EOF
    
    echo -e "${BLUE}🔨 Building MCP Gateway container image${NC}"
    if docker build -f Dockerfile . -t localhost:$REGISTRY_PORT/mcp-gateway:latest; then
        echo -e "${GREEN}✅ MCP Gateway image built successfully${NC}"
        
        echo -e "${BLUE}📤 Pushing MCP Gateway image to registry${NC}"
        if docker push localhost:$REGISTRY_PORT/mcp-gateway:latest; then
            IMAGE_BUILT=true
            echo -e "${GREEN}✅ Container built and pushed successfully${NC}"
        else
            echo -e "${RED}❌ Failed to push MCP Gateway image${NC}"
            exit 1
        fi
    else
        echo -e "${RED}❌ Failed to build MCP Gateway image${NC}"
        exit 1
    fi
    
    # Clean up Dockerfile
    rm Dockerfile
fi

if [ "$IMAGE_BUILT" = false ]; then
    echo -e "${RED}❌ Failed to build MCP Gateway container image${NC}"
    exit 1
fi

# Verify the image exists in the registry
echo -e "${BLUE}🔍 Verifying MCP Gateway image in registry${NC}"
sleep 3  # Give registry time to update

# Check registry catalog
REGISTRY_CATALOG=$(curl -s http://localhost:$REGISTRY_PORT/v2/_catalog)
if echo "$REGISTRY_CATALOG" | grep -q "mcp-gateway"; then
    echo -e "${GREEN}✅ MCP Gateway image found in registry catalog${NC}"
    
    # Also verify the specific tag exists
    if curl -s http://localhost:$REGISTRY_PORT/v2/mcp-gateway/tags/list | grep -q "latest"; then
        echo -e "${GREEN}✅ MCP Gateway latest tag verified${NC}"
    else
        echo -e "${YELLOW}⚠️  Latest tag not found, but image exists${NC}"
    fi
else
    echo -e "${RED}❌ MCP Gateway image not found in registry${NC}"
    echo -e "${BLUE}📋 Current registry contents:${NC}"
    echo "$REGISTRY_CATALOG" | jq '.' 2>/dev/null || echo "$REGISTRY_CATALOG"
    
    echo -e "${RED}❌ Critical Error: Image build process failed${NC}"
    echo -e "${BLUE}📋 Troubleshooting information:${NC}"
    echo -e "${BLUE}  • Registry URL: http://localhost:$REGISTRY_PORT${NC}"
    echo -e "${BLUE}  • Expected image: localhost:$REGISTRY_PORT/mcp-gateway:latest${NC}"
    echo -e "${BLUE}  • Build methods attempted: dotnet publish, Dockerfile search, manual build${NC}"
    
    # Show recent Docker images that might be related
    echo -e "${BLUE}📋 Recent Docker images:${NC}"
    docker images | head -10
    
    exit 1
fi

# Verify local image exists
if docker images localhost:$REGISTRY_PORT/mcp-gateway:latest | grep -q "mcp-gateway"; then
    echo -e "${GREEN}✅ MCP Gateway image verified locally${NC}"
else
    echo -e "${YELLOW}⚠️  Local image verification failed, but registry push succeeded${NC}"
fi

# Return to work directory
if [ "$USE_OFFICIAL_IMAGE_ONLY" = false ]; then
    cd ..
fi

echo -e "${GREEN}✅ MCP Gateway service ready${NC}"

# Step 6: Prepare Kubernetes Deployment
echo -e "${BLUE}📋 Step 6: Preparing Kubernetes Deployment${NC}"

# Create namespace
kubectl create namespace adapter --dry-run=client -o yaml | kubectl apply -f -

# Modify the deployment manifest to use our local registry
if [ ! -f "deployment/k8s/local-deployment.yml" ]; then
    echo -e "${RED}❌ Kubernetes deployment manifest not found${NC}"
    exit 1
fi

# Create a modified deployment file
MODIFIED_DEPLOYMENT="deployment/k8s/local-deployment-modified.yml"
cp "deployment/k8s/local-deployment.yml" "$MODIFIED_DEPLOYMENT"

# Replace image references to use our local registry
sed -i.bak "s|localhost:5000|localhost:$REGISTRY_PORT|g" "$MODIFIED_DEPLOYMENT"

# Also fix the MCP Gateway image name to match what we built
sed -i.bak "s|localhost:$REGISTRY_PORT/microsoft-mcpgateway-service:latest|localhost:$REGISTRY_PORT/mcp-gateway:latest|g" "$MODIFIED_DEPLOYMENT"

# Apply the deployment
echo -e "${BLUE}🚀 Deploying MCP Gateway to Kubernetes${NC}"
kubectl apply -f "$MODIFIED_DEPLOYMENT"

# Force restart deployment to pull the new image
echo -e "${BLUE}🔄 Restarting deployment to pull new image${NC}"
kubectl rollout restart deployment/mcpgateway -n adapter

# Wait for deployment to be ready with better monitoring
echo -e "${BLUE}⏳ Waiting for MCP Gateway deployment to be ready...${NC}"

# First wait for the deployment to be available
if kubectl wait --for=condition=available --timeout=300s deployment/mcpgateway -n adapter; then
    echo -e "${GREEN}✅ Deployment condition met${NC}"
else
    echo -e "${RED}❌ Deployment failed to become available within timeout${NC}"
    echo -e "${BLUE}📋 Checking deployment status:${NC}"
    kubectl get deployment mcpgateway -n adapter
    kubectl describe deployment mcpgateway -n adapter
    exit 1
fi

# Verify pods are actually running
echo -e "${BLUE}🔍 Verifying pod status...${NC}"
POD_STATUS=$(kubectl get pods -n adapter -l app=mcpgateway -o jsonpath='{.items[0].status.phase}')
if [ "$POD_STATUS" = "Running" ]; then
    echo -e "${GREEN}✅ MCP Gateway pod is running${NC}"
    
    # Get pod name for additional verification
    POD_NAME=$(kubectl get pods -n adapter -l app=mcpgateway -o jsonpath='{.items[0].metadata.name}')
    echo -e "${BLUE}📋 Pod name: $POD_NAME${NC}"
    
    # Check if container is ready
    READY_STATUS=$(kubectl get pod $POD_NAME -n adapter -o jsonpath='{.status.containerStatuses[0].ready}')
    if [ "$READY_STATUS" = "true" ]; then
        echo -e "${GREEN}✅ Container is ready and healthy${NC}"
    else
        echo -e "${YELLOW}⚠️  Container exists but may not be fully ready${NC}"
        kubectl describe pod $POD_NAME -n adapter
    fi
else
    echo -e "${RED}❌ Pod is not running. Status: $POD_STATUS${NC}"
    kubectl get pods -n adapter -l app=mcpgateway
    kubectl describe pods -n adapter -l app=mcpgateway
    exit 1
fi

echo -e "${GREEN}✅ MCP Gateway deployed successfully${NC}"

# Step 7: Setup Port Forwarding
echo -e "${BLUE}📋 Step 7: Setting up Port Forwarding${NC}"

# Kill any existing port-forward processes
pkill -f "kubectl port-forward.*mcpgateway-service" || true

# Find available port for gateway access
GATEWAY_PORT=$(find_available_port $GATEWAY_PORT)
echo -e "${GREEN}✅ Using gateway port: $GATEWAY_PORT${NC}"

# Start port forwarding in background
echo -e "${BLUE}🔗 Starting port forwarding to MCP Gateway${NC}"
kubectl port-forward -n adapter svc/mcpgateway-service $GATEWAY_PORT:8000 &
PORT_FORWARD_PID=$!

# Save PID for cleanup
echo $PORT_FORWARD_PID > "$WORK_DIR/port-forward.pid"

# Wait for port forwarding to be established
sleep 5

# Step 8: Test the deployment
echo -e "${BLUE}📋 Step 8: Testing MCP Gateway${NC}"

# Test multiple endpoints to ensure service is fully functional
echo -e "${BLUE}🔍 Testing MCP Gateway endpoints${NC}"

# Test 1: Basic connectivity
echo -e "${BLUE}🔗 Testing basic connectivity${NC}"
for i in {1..15}; do
    if curl -s -w '%{http_code}' http://localhost:$GATEWAY_PORT/ -o /dev/null | grep -q "200\|404"; then
        echo -e "${GREEN}✅ MCP Gateway is responding${NC}"
        break
    else
        if [ $i -eq 15 ]; then
            echo -e "${RED}❌ MCP Gateway connectivity test failed after 15 attempts${NC}"
            echo -e "${BLUE}📋 Checking port forwarding status:${NC}"
            ps aux | grep "kubectl port-forward" | grep -v grep || echo "No port forwarding found"
            exit 1
        fi
        echo -e "${YELLOW}⏳ Waiting for MCP Gateway to respond... (attempt $i/15)${NC}"
        sleep 2
    fi
done

# Test 2: Adapters endpoint (core functionality)
echo -e "${BLUE}📋 Testing adapters endpoint${NC}"
ADAPTERS_RESPONSE=$(curl -s -w '%{http_code}' http://localhost:$GATEWAY_PORT/adapters -o /tmp/adapters_response.json)
if echo "$ADAPTERS_RESPONSE" | grep -q "200"; then
    echo -e "${GREEN}✅ Adapters endpoint is working${NC}"
    echo -e "${BLUE}📋 Current adapters:${NC}"
    cat /tmp/adapters_response.json | jq '.' 2>/dev/null || cat /tmp/adapters_response.json
    rm -f /tmp/adapters_response.json
else
    echo -e "${RED}❌ Adapters endpoint test failed. HTTP code: $ADAPTERS_RESPONSE${NC}"
    exit 1
fi

# Step 9: Create test adapter
echo -e "${BLUE}📋 Step 9: Creating Test MCP Adapter${NC}"

# Create test adapter JSON with correct registry reference
TEST_ADAPTER_JSON=$(cat << EOF
{
  "name": "mcp-example-auto",
  "imageName": "mcp-example",
  "imageVersion": "1.0.0",
  "description": "Auto-deployed test MCP server via script",
  "registryUrl": "localhost:$REGISTRY_PORT"
}
EOF
)

echo -e "${BLUE}🧪 Creating test adapter${NC}"
ADAPTER_RESPONSE=$(curl -s -X POST http://localhost:$GATEWAY_PORT/adapters \
    -H "Content-Type: application/json" \
    -d "$TEST_ADAPTER_JSON")

if echo "$ADAPTER_RESPONSE" | grep -q "mcp-example"; then
    echo -e "${GREEN}✅ Test adapter created successfully${NC}"
else
    echo -e "${YELLOW}⚠️  Test adapter creation response: $ADAPTER_RESPONSE${NC}"
fi

# Step 10: Display success information
echo ""
echo -e "${GREEN}🎉 MCP Gateway Deployment Completed Successfully!${NC}"
echo -e "${GREEN}=================================================${NC}"
echo ""
echo -e "${BLUE}📋 Access Information:${NC}"
echo -e "  🌐 MCP Gateway API: ${GREEN}http://localhost:$GATEWAY_PORT${NC}"
echo -e "  📚 API Documentation: ${GREEN}http://localhost:$GATEWAY_PORT/swagger${NC} (if available)"
echo -e "  🔍 Health Check: ${GREEN}http://localhost:$GATEWAY_PORT/health${NC}"
echo -e "  🐳 Local Registry: ${GREEN}http://localhost:$REGISTRY_PORT${NC}"
echo ""
echo -e "${BLUE}📋 Test MCP Server:${NC}"
echo -e "  🔗 Streamable HTTP: ${GREEN}http://localhost:$GATEWAY_PORT/adapters/mcp-example/mcp${NC}"
echo -e "  🔗 SSE Endpoint: ${GREEN}http://localhost:$GATEWAY_PORT/adapters/mcp-example/sse${NC}"
echo ""
echo -e "${BLUE}📋 Management Commands:${NC}"
echo -e "  📝 List adapters: ${YELLOW}curl http://localhost:$GATEWAY_PORT/adapters${NC}"
echo -e "  📊 Adapter status: ${YELLOW}curl http://localhost:$GATEWAY_PORT/adapters/mcp-example/status${NC}"
echo -e "  📜 Adapter logs: ${YELLOW}curl http://localhost:$GATEWAY_PORT/adapters/mcp-example/logs${NC}"
echo ""
echo -e "${BLUE}📋 Files Created:${NC}"
echo -e "  📁 Work Directory: ${GREEN}$WORK_DIR${NC}"
echo -e "  🔧 Port Forward PID: ${GREEN}$WORK_DIR/port-forward.pid${NC}"
echo ""
echo -e "${YELLOW}💡 Tips:${NC}"
echo -e "  • Use ${GREEN}./cleanup-mcp-gateway.sh${NC} to clean up all resources"
echo -e "  • Use ${GREEN}./test-mcp-gateway.sh${NC} to run comprehensive tests"
echo -e "  • Check logs with: ${YELLOW}kubectl logs -n adapter deployment/mcpgateway-service${NC}"
echo ""

# Save configuration for other scripts
cat > "$WORK_DIR/config.env" << EOF
REGISTRY_PORT=$REGISTRY_PORT
GATEWAY_PORT=$GATEWAY_PORT
WORK_DIR=$WORK_DIR
PORT_FORWARD_PID=$PORT_FORWARD_PID
EOF

echo -e "${GREEN}✅ Configuration saved to $WORK_DIR/config.env${NC}"
