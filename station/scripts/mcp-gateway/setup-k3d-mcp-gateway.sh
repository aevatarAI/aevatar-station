#!/bin/bash

# Setup k3d (k3s in Docker) for MCP Gateway on macOS
# k3d is the macOS-friendly way to run k3s

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}🚀 Setting up k3d (k3s in Docker) for MCP Gateway${NC}"
echo -e "${BLUE}===============================================${NC}"

# Configuration
CLUSTER_NAME="mcp-gateway-cluster"
REGISTRY_PORT="5003"  # Changed from 5001 to avoid conflicts
GATEWAY_PORT="8000"

# Step 1: Check if k3d is installed
echo -e "${BLUE}📋 Step 1: Checking k3d Installation${NC}"

if command -v k3d &> /dev/null; then
    echo -e "${GREEN}✅ k3d is already installed${NC}"
    k3d version
else
    echo -e "${BLUE}📥 Installing k3d via Homebrew...${NC}"
    
    if command -v brew &> /dev/null; then
        brew install k3d
    else
        echo -e "${BLUE}📥 Installing k3d via curl...${NC}"
        curl -s https://raw.githubusercontent.com/k3d-io/k3d/main/install.sh | bash
    fi
    
    if command -v k3d &> /dev/null; then
        echo -e "${GREEN}✅ k3d installed successfully${NC}"
        k3d version
    else
        echo -e "${RED}❌ k3d installation failed${NC}"
        exit 1
    fi
fi

# Step 2: Create k3d cluster with registry
echo -e "${BLUE}📋 Step 2: Creating k3d Cluster${NC}"

# Delete existing cluster if it exists
if k3d cluster list | grep -q "$CLUSTER_NAME"; then
    echo -e "${YELLOW}🔄 Removing existing cluster${NC}"
    k3d cluster delete "$CLUSTER_NAME"
fi

# Create cluster with port mapping only
echo -e "${BLUE}🏗️  Creating k3d cluster${NC}"
k3d cluster create "$CLUSTER_NAME" \
    --port "$GATEWAY_PORT:80@loadbalancer" \
    --agents 1 \
    --wait

# Create separate registry after cluster creation
echo -e "${BLUE}🐳 Creating separate Docker registry${NC}"
docker run -d -p $REGISTRY_PORT:5000 --name k3d-registry registry:2.7

# Verify cluster
if kubectl cluster-info &> /dev/null; then
    echo -e "${GREEN}✅ k3d cluster created successfully${NC}"
    kubectl get nodes
else
    echo -e "${RED}❌ k3d cluster creation failed${NC}"
    exit 1
fi

# Step 3: Verify registry
echo -e "${BLUE}📋 Step 3: Verifying Local Registry${NC}"

sleep 5
if curl -s http://localhost:$REGISTRY_PORT/v2/_catalog > /dev/null; then
    echo -e "${GREEN}✅ Local registry is accessible${NC}"
else
    echo -e "${RED}❌ Local registry is not accessible${NC}"
    exit 1
fi

# Step 4: Prepare MCP Gateway image
echo -e "${BLUE}📋 Step 4: Preparing MCP Gateway Image${NC}"

OFFICIAL_IMAGE="ghcr.io/microsoft/mcp-gateway:latest"
LOCAL_IMAGE="localhost:$REGISTRY_PORT/mcp-gateway:latest"

echo -e "${BLUE}📥 Pulling and preparing MCP Gateway image${NC}"
if docker pull "$OFFICIAL_IMAGE"; then
    echo -e "${GREEN}✅ Official image pulled${NC}"
    
    # Tag and push to local registry
    docker tag "$OFFICIAL_IMAGE" "$LOCAL_IMAGE"
    docker push "$LOCAL_IMAGE"
    echo -e "${GREEN}✅ Image ready in k3d registry${NC}"
else
    echo -e "${RED}❌ Failed to pull official image${NC}"
    exit 1
fi

# Step 5: Create MCP Gateway deployment
echo -e "${BLUE}📋 Step 5: Deploying MCP Gateway${NC}"

cat > k3d-mcp-gateway.yaml << EOF
apiVersion: v1
kind: Namespace
metadata:
  name: adapter
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mcpgateway
  namespace: adapter
  labels:
    app: mcpgateway
spec:
  replicas: 1
  selector:
    matchLabels:
      app: mcpgateway
  template:
    metadata:
      labels:
        app: mcpgateway
    spec:
      containers:
      - name: mcpgateway
        image: localhost:$REGISTRY_PORT/mcp-gateway:latest
        ports:
        - containerPort: 80
          name: http
        env:
        - name: ENV
          value: "development"
        - name: TZ
          value: "UTC"
        - name: APISERVER_JWT_SECRET_KEY
          value: "aevatar-dev-jwt-secret-key-12345"
        - name: SUPER_ADMIN_USERNAME
          value: "admin"
        - name: SUPER_ADMIN_PASSWORD
          value: "admin123"
        resources:
          requests:
            memory: "128Mi"
            cpu: "100m"
          limits:
            memory: "256Mi"
            cpu: "200m"
        livenessProbe:
          httpGet:
            path: /adapters
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /adapters
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: mcpgateway-service
  namespace: adapter
spec:
  selector:
    app: mcpgateway
  ports:
  - port: 8000
    targetPort: 80
    protocol: TCP
  type: ClusterIP
---
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  namespace: adapter
  name: app-manager
rules:
- apiGroups: [""]
  resources: ["pods", "services", "configmaps"]
  verbs: ["get", "list", "watch", "create", "update", "patch", "delete"]
- apiGroups: ["apps"]
  resources: ["deployments", "statefulsets"]
  verbs: ["get", "list", "watch", "create", "update", "patch", "delete"]
---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: mcpgateway-sa
  namespace: adapter
---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: app-manager-binding
  namespace: adapter
subjects:
- kind: ServiceAccount
  name: mcpgateway-sa
  namespace: adapter
roleRef:
  kind: Role
  name: app-manager
  apiGroup: rbac.authorization.k8s.io
EOF

# Apply deployment
kubectl apply -f k3d-mcp-gateway.yaml

# Wait for deployment
echo -e "${BLUE}⏳ Waiting for MCP Gateway to be ready...${NC}"
kubectl wait --for=condition=available --timeout=180s deployment/mcpgateway -n adapter

# Check status
kubectl get pods -n adapter

# Step 6: Test the deployment
echo -e "${BLUE}📋 Step 6: Testing MCP Gateway${NC}"

# k3d automatically exposes the service on the configured port
echo -e "${BLUE}🔍 Testing MCP Gateway on localhost:$GATEWAY_PORT${NC}"

for i in {1..15}; do
    if curl -s http://localhost:$GATEWAY_PORT/adapters > /dev/null; then
        echo -e "${GREEN}✅ MCP Gateway is responding${NC}"
        break
    else
        if [ $i -eq 15 ]; then
            echo -e "${RED}❌ MCP Gateway test failed${NC}"
            echo -e "${BLUE}📋 Checking service status:${NC}"
            kubectl get svc -n adapter
            kubectl describe svc mcpgateway-service -n adapter
            exit 1
        fi
        echo -e "${YELLOW}⏳ Waiting for MCP Gateway... (attempt $i/15)${NC}"
        sleep 3
    fi
done

# Step 7: Success summary
echo ""
echo -e "${GREEN}🎉 k3d MCP Gateway Setup Completed!${NC}"
echo -e "${GREEN}===================================${NC}"
echo ""
echo -e "${BLUE}📋 Access Information:${NC}"
echo -e "  🌐 MCP Gateway API: ${GREEN}http://localhost:$GATEWAY_PORT${NC}"
echo -e "  🐳 Local Registry: ${GREEN}http://localhost:$REGISTRY_PORT${NC}"
echo -e "  📊 k3d Cluster: ${GREEN}$CLUSTER_NAME${NC}"
echo ""
echo -e "${BLUE}📋 Useful Commands:${NC}"
echo -e "  🧪 Test Gateway: ${YELLOW}curl http://localhost:$GATEWAY_PORT/adapters${NC}"
echo -e "  📋 Check Pods: ${YELLOW}kubectl get pods -n adapter${NC}"
echo -e "  📜 View Logs: ${YELLOW}kubectl logs -n adapter deployment/mcpgateway${NC}"
echo -e "  🔄 Restart: ${YELLOW}kubectl rollout restart deployment/mcpgateway -n adapter${NC}"
echo ""
echo -e "${BLUE}🚀 Next Steps:${NC}"
echo -e "  • Test with official MCP servers: ${GREEN}./test-official-mcp-servers.sh${NC}"
echo -e "  • Test Aevatar integration: ${GREEN}./quick-test-aevatar-api.sh${NC}"
echo -e "  • Deploy custom MCP servers using npx"
echo ""
echo -e "${YELLOW}💡 To cleanup: ${GREEN}./cleanup-k3d-mcp-gateway.sh${NC}"
echo ""

# Save configuration
cat > k3d-config.env << EOF
CLUSTER_NAME=$CLUSTER_NAME
REGISTRY_PORT=$REGISTRY_PORT
GATEWAY_PORT=$GATEWAY_PORT
EOF

echo -e "${GREEN}✅ Configuration saved to k3d-config.env${NC}"
