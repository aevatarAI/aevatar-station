# Kubernetes-Enabled Docker Regression Testing Solution

## Overview

This document outlines a comprehensive solution for running regression tests locally with full Kubernetes support. The solution uses Docker to containerize all dependencies and provides a local Kubernetes cluster (via Kind) to support tests that create actual Kubernetes resources through the `RegisterClientAuthentication` endpoint.

## Problem Statement

The current `@station/scripts/regression_test.py` script has several challenges for local development:
- Requires multiple environment variables (AUTH_HOST, API_HOST, CLIENT_ID, CLIENT_SECRET)
- Depends on running AuthServer, Silo, and HttpApi.Host services
- **Creates actual Kubernetes pods through API endpoints** (via `UserController.RegisterClientAuthentication`)
- The `RegisterClientAuthentication` endpoint creates developer pods on K8s for regression testing
- Complex local setup with potential environment conflicts
- Need for a real Kubernetes cluster to simulate staging environment

## Solution Architecture

### Task Analysis
The regression test requires:
1. A real Kubernetes API to create pods/deployments/services
2. Multiple services (AuthServer, Silo, HttpApi.Host) with K8s client configuration
3. Proper authentication flow that triggers K8s resource creation
4. Environment that mimics staging behavior

### Breakdown (MECE)
1. **Local Kubernetes Cluster**: Kind (Kubernetes in Docker) for K8s API
2. **Containerized Services**: Docker Compose with all required services
3. **Kubernetes Integration**: Services configured with kubeconfig to create K8s resources
4. **Test Data Management**: Seed data and test user setup
5. **Network Configuration**: Service discovery between Docker and Kind
6. **Test Execution Wrapper**: Script to orchestrate cluster creation, service startup, and test execution
7. **Resource Cleanup**: Automated cleanup of K8s resources after tests

## Implementation Details

### 1. Docker Compose Architecture with Kind Integration

**File: `station/scripts/docker-compose.regression.yml`**

```yaml
# docker-compose.regression.yml
version: '3.8'

services:
  # Kind cluster for Kubernetes API
  kind:
    image: kindest/node:v1.29.0
    container_name: regression-kind-cluster
    privileged: true
    ports:
      - "6443:6443"  # Kubernetes API
      - "30000-32767:30000-32767"  # NodePort range
    environment:
      - KUBECONFIG=/etc/kubernetes/admin.conf
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - /lib/modules:/lib/modules:ro
      - /sys:/sys:ro
      - kind-data:/var/lib/docker
    networks:
      - regression-network
    healthcheck:
      test: ["CMD", "kubectl", "cluster-info"]
      interval: 10s
      timeout: 5s
      retries: 30

  # Kind cluster setup - creates namespace and exports kubeconfig
  kind-setup:
    image: bitnami/kubectl:latest
    depends_on:
      kind:
        condition: service_healthy
    volumes:
      - ./setup-kind.sh:/setup-kind.sh:ro
      - ./kubeconfig:/kubeconfig
    entrypoint: ["/bin/bash", "/setup-kind.sh"]
    networks:
      - regression-network

  mongodb:
    image: mongo:7
    environment:
      MONGO_INITDB_ROOT_USERNAME: admin
      MONGO_INITDB_ROOT_PASSWORD: password123
    volumes:
      - mongodb_data:/data/db
    networks:
      - regression-network
    
  redis:
    image: redis:7-alpine
    networks:
      - regression-network
    
  authserver:
    build: 
      context: ../src/Aevatar.AuthServer
      dockerfile: Dockerfile
    environment:
      ConnectionStrings__Default: "mongodb://admin:password123@mongodb:27017/AevatarAuth"
      ASPNETCORE_ENVIRONMENT: "Development"
      # Kubernetes configuration
      Kubernetes__KubeConfigPath: "/app/kubeconfig"
      Kubernetes__AppNameSpace: "aevatar-apps"
    volumes:
      - ./kubeconfig:/app/kubeconfig:ro
    depends_on:
      - mongodb
      - kind-setup
    networks:
      - regression-network
    
  silo:
    build:
      context: ../src/Aevatar.Silo  
      dockerfile: Dockerfile
    environment:
      ConnectionStrings__Default: "mongodb://admin:password123@mongodb:27017/AevatarSilo"
      Orleans__Clustering__Provider: "MongoDb"
      Orleans__Clustering__ConnectionString: "mongodb://admin:password123@mongodb:27017"
      # Kubernetes configuration
      Kubernetes__KubeConfigPath: "/app/kubeconfig"
      Kubernetes__AppNameSpace: "aevatar-apps"
    volumes:
      - ./kubeconfig:/app/kubeconfig:ro
    depends_on:
      - mongodb
      - redis
      - authserver
      - kind-setup
    networks:
      - regression-network
    
  api:
    build:
      context: ../src/Aevatar.HttpApi.Host
      dockerfile: Dockerfile
    environment:
      ConnectionStrings__Default: "mongodb://admin:password123@mongodb:27017/AevatarApi"
      AuthServer: "http://authserver:80"
      Orleans__ClusterId: "regression-cluster"
      Orleans__ServiceId: "regression-service"
      # Kubernetes configuration for creating developer pods
      Kubernetes__KubeConfigPath: "/app/kubeconfig"
      Kubernetes__AppNameSpace: "aevatar-apps"
      Kubernetes__AppPodReplicas: "1"
      Kubernetes__RequestCpuCore: "100m"
      Kubernetes__RequestMemory: "128Mi"
    volumes:
      - ./kubeconfig:/app/kubeconfig:ro
    depends_on:
      - silo
      - kind-setup
    ports:
      - "8080:80"
    networks:
      - regression-network
      
  regression-tests:
    build:
      context: .
      dockerfile: Dockerfile.regression
    environment:
      AUTH_HOST: "http://authserver:80"
      API_HOST: "http://api:80"
      CLIENT_ID: "AevatarTestClient"
      CLIENT_SECRET: "test-secret-key"
      ADMIN_USERNAME: "admin"
      ADMIN_PASSWORD: "1q2W3e*"
      PYTHONPATH: "/app"
      RUNNING_IN_DOCKER: "true"
      KUBECONFIG: "/app/kubeconfig"
    depends_on:
      - api
    volumes:
      - ./test-results:/app/test-results
      - ./kubeconfig:/app/kubeconfig:ro
    networks:
      - regression-network

networks:
  regression-network:
    driver: bridge

volumes:
  mongodb_data:
  kind-data:
```

### 2. Kind Setup Script

**File: `station/scripts/setup-kind.sh`**

```bash
#!/bin/bash
# ABOUTME: Initialize Kind cluster for regression testing
# ABOUTME: Creates namespace and exports kubeconfig for services

set -e

echo "Setting up Kind cluster for regression testing..."

# Wait for Kind API to be available
echo "Waiting for Kubernetes API..."
for i in {1..60}; do
    if kubectl --server=https://regression-kind-cluster:6443 --insecure-skip-tls-verify cluster-info &>/dev/null; then
        echo "✓ Kubernetes API is ready"
        break
    fi
    echo "Attempt $i/60: Waiting for API..."
    sleep 2
done

# Get cluster credentials
echo "Exporting kubeconfig..."
kubectl --server=https://regression-kind-cluster:6443 --insecure-skip-tls-verify \
    config view --raw > /kubeconfig

# Update kubeconfig to use container name as server
sed -i 's|server:.*|server: https://kind:6443|' /kubeconfig

# Create namespace for test applications
echo "Creating aevatar-apps namespace..."
kubectl --kubeconfig=/kubeconfig create namespace aevatar-apps || true

# Create any required RBAC resources
echo "Setting up RBAC..."
kubectl --kubeconfig=/kubeconfig apply -f - <<EOF
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: aevatar-deployer
rules:
- apiGroups: ["", "apps", "networking.k8s.io"]
  resources: ["deployments", "services", "configmaps", "pods", "ingresses"]
  verbs: ["create", "get", "list", "update", "patch", "delete"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: aevatar-deployer-binding
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: aevatar-deployer
subjects:
- kind: ServiceAccount
  name: default
  namespace: aevatar-apps
EOF

echo "✓ Kind cluster setup complete"
echo "Namespace: aevatar-apps"
echo "Kubeconfig exported to: /kubeconfig"
```

### 3. Test Container Dockerfile

**File: `station/scripts/Dockerfile.regression`**

```dockerfile
# Dockerfile.regression
FROM python:3.11-slim

WORKDIR /app

# Install system dependencies including kubectl for K8s cleanup
RUN apt-get update && apt-get install -y \
    curl \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Install kubectl
RUN curl -LO "https://dl.k8s.io/release/v1.29.0/bin/linux/amd64/kubectl" \
    && chmod +x kubectl \
    && mv kubectl /usr/local/bin/

# Install Python dependencies
COPY requirements-test.txt .
RUN pip install --no-cache-dir -r requirements-test.txt

# Install core test dependencies
RUN pip install pytest requests urllib3

# Copy test script and cleanup script
COPY regression_test.py .
COPY cleanup_k8s_resources.py .

# Create test results directory
RUN mkdir -p test-results

# Create a non-root user for security
RUN useradd -m -u 1000 testuser && chown -R testuser:testuser /app
USER testuser

# Default command runs tests with K8s cleanup
CMD ["python", "-m", "pytest", "regression_test.py", "-v", "--tb=short", "--junitxml=test-results/regression-results.xml"]
```

### 4. Test Dependencies

**File: `station/scripts/requirements-test.txt`**

```txt
pytest>=7.0.0
requests>=2.28.0
urllib3>=1.26.0
pytest-html>=3.1.0
pytest-json-report>=1.5.0
```

### 5. Service Dockerfiles (Required)

Since the services don't have existing Dockerfiles, they need to be created:

**File: `station/src/Aevatar.AuthServer/Dockerfile`**

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY Directory.Build.props ./
COPY ["src/Aevatar.AuthServer/Aevatar.AuthServer.csproj", "src/Aevatar.AuthServer/"]
COPY ["src/Aevatar.Domain.Shared/Aevatar.Domain.Shared.csproj", "src/Aevatar.Domain.Shared/"]
COPY ["src/Aevatar.MongoDB/Aevatar.MongoDB.csproj", "src/Aevatar.MongoDB/"]
# Add other dependencies as needed

# Restore packages
RUN dotnet restore "src/Aevatar.AuthServer/Aevatar.AuthServer.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/src/Aevatar.AuthServer"
RUN dotnet build "Aevatar.AuthServer.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Aevatar.AuthServer.csproj" -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Aevatar.AuthServer.dll"]
```

**Similar Dockerfiles needed for:**
- `station/src/Aevatar.Silo/Dockerfile`
- `station/src/Aevatar.HttpApi.Host/Dockerfile`

### 6. Kubernetes Resource Cleanup

**File: `station/scripts/cleanup_k8s_resources.py`**

```python
#!/usr/bin/env python3
# ABOUTME: Cleanup Kubernetes resources created during regression testing

import os
import subprocess
import logging

logger = logging.getLogger(__name__)

def cleanup_k8s_resources():
    """Clean up all K8s resources created during tests"""
    kubeconfig = os.getenv("KUBECONFIG", "/app/kubeconfig")
    namespace = "aevatar-apps"
    
    if not os.path.exists(kubeconfig):
        logger.warning(f"Kubeconfig not found at {kubeconfig}")
        return
    
    try:
        # Delete all resources in the test namespace
        logger.info(f"Cleaning up resources in namespace: {namespace}")
        
        resources = [
            "deployments",
            "services", 
            "configmaps",
            "pods",
            "ingresses"
        ]
        
        for resource in resources:
            cmd = [
                "kubectl", "--kubeconfig", kubeconfig,
                "delete", resource, "--all",
                "-n", namespace,
                "--timeout=30s"
            ]
            
            logger.info(f"Deleting all {resource}...")
            result = subprocess.run(cmd, capture_output=True, text=True)
            
            if result.returncode == 0:
                logger.info(f"✓ Deleted {resource}: {result.stdout}")
            else:
                logger.warning(f"Failed to delete {resource}: {result.stderr}")
                
    except Exception as e:
        logger.error(f"Error during K8s cleanup: {e}")

if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    cleanup_k8s_resources()
```

### 7. Test Runner Script

**File: `station/scripts/run-regression-tests.sh`**

```bash
#!/bin/bash
# ABOUTME: One-command script to run regression tests with Kind Kubernetes cluster
# ABOUTME: Handles Kind setup, service startup, health checks, test execution, and cleanup

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="$SCRIPT_DIR/docker-compose.regression.yml"
PROJECT_NAME="aevatar-regression"
RESULTS_DIR="$SCRIPT_DIR/test-results"
KIND_CLUSTER_NAME="regression-cluster"
KUBECONFIG_PATH="$SCRIPT_DIR/kubeconfig"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

check_dependencies() {
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed. Please install Docker Desktop."
        exit 1
    fi
    
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        print_error "Docker Compose is not available. Please install Docker Compose."
        exit 1
    fi
}

cleanup() {
    print_status "Cleaning up..."
    
    # Clean up K8s resources first
    if [ -f "$KUBECONFIG_PATH" ]; then
        print_status "Cleaning up Kubernetes resources..."
        docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" run --rm \
            regression-tests python cleanup_k8s_resources.py || true
    fi
    
    # Stop and remove containers
    print_status "Stopping containers..."
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" down -v --remove-orphans
    
    # Remove kubeconfig
    rm -f "$KUBECONFIG_PATH"
}

wait_for_service() {
    local service_name=$1
    local health_url=$2
    local max_attempts=30
    local attempt=1
    
    print_status "Waiting for $service_name to be ready..."
    
    while [ $attempt -le $max_attempts ]; do
        if docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" exec -T "$service_name" curl -f "$health_url" &>/dev/null; then
            print_status "✓ $service_name is ready"
            return 0
        fi
        
        print_status "Attempt $attempt/$max_attempts: $service_name not ready, waiting..."
        sleep 5
        ((attempt++))
    done
    
    print_error "✗ $service_name failed to become ready after $max_attempts attempts"
    return 1
}

wait_for_kind_ready() {
    local max_attempts=60
    local attempt=1
    
    print_status "Waiting for Kind cluster to be ready..."
    
    while [ $attempt -le $max_attempts ]; do
        if [ -f "$KUBECONFIG_PATH" ] && kubectl --kubeconfig="$KUBECONFIG_PATH" cluster-info &>/dev/null; then
            print_status "✓ Kind cluster is ready"
            return 0
        fi
        
        print_status "Attempt $attempt/$max_attempts: Kind not ready, waiting..."
        sleep 3
        ((attempt++))
    done
    
    print_error "✗ Kind cluster failed to become ready"
    return 1
}

# Set trap for cleanup on exit
trap cleanup EXIT

main() {
    cd "$SCRIPT_DIR"
    
    print_status "Checking dependencies..."
    check_dependencies
    
    print_status "Starting regression test environment with Kubernetes..."
    
    # Create results directory
    mkdir -p "$RESULTS_DIR"
    
    # Start Kind cluster first
    print_status "Starting Kind Kubernetes cluster..."
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" up -d kind
    
    # Wait a bit for Kind to initialize
    sleep 10
    
    # Setup Kind cluster (namespace, RBAC, export kubeconfig)
    print_status "Setting up Kind cluster..."
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" up kind-setup
    
    # Verify Kind is ready
    if ! wait_for_kind_ready; then
        print_error "Kind cluster setup failed"
        docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" logs kind kind-setup
        exit 1
    fi
    
    # Build and start infrastructure services
    print_status "Starting infrastructure services (MongoDB, Redis)..."
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" up -d --build mongodb redis
    
    print_status "Waiting for database to be ready..."
    sleep 15
    
    # Start application services in order
    print_status "Starting AuthServer..."
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" up -d authserver
    sleep 20
    
    print_status "Starting Silo..."
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" up -d silo
    sleep 25
    
    print_status "Starting API service..."
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" up -d api
    sleep 15
    
    # Health checks
    print_status "Performing health checks..."
    
    # Check API health
    if ! wait_for_service "api" "http://localhost:80/health"; then
        print_error "API service health check failed"
        print_status "Checking service logs..."
        docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" logs api
        exit 1
    fi
    
    # Show K8s cluster info
    print_status "Kubernetes cluster info:"
    kubectl --kubeconfig="$KUBECONFIG_PATH" cluster-info || true
    kubectl --kubeconfig="$KUBECONFIG_PATH" get nodes || true
    
    # Run tests
    print_status "Running regression tests..."
    set +e  # Don't exit on test failures
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" run --rm regression-tests "$@"
    TEST_EXIT_CODE=$?
    set -e
    
    # Show K8s resources created during tests
    print_status "Kubernetes resources created during tests:"
    kubectl --kubeconfig="$KUBECONFIG_PATH" get all -n aevatar-apps || true
    
    # Copy test results
    print_status "Copying test results..."
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" run --rm \
        -v "$RESULTS_DIR:/host/results" \
        regression-tests cp -r /app/test-results/* /host/results/ 2>/dev/null || true
    
    if [ $TEST_EXIT_CODE -eq 0 ]; then
        print_status "✓ All regression tests passed successfully!"
    else
        print_error "✗ Some regression tests failed (exit code: $TEST_EXIT_CODE)"
        print_status "Check test results in: $RESULTS_DIR"
    fi
    
    return $TEST_EXIT_CODE
}

show_help() {
    cat << EOF
Regression Test Runner

USAGE:
    $0 [OPTIONS] [PYTEST_ARGS]

OPTIONS:
    --no-cleanup    Don't cleanup containers after tests (for debugging)
    --logs         Show service logs after test completion
    --help         Show this help

PYTEST_ARGS:
    Any additional arguments are passed to pytest

EXAMPLES:
    $0                           # Run all tests
    $0 -k "test_login"          # Run specific test
    $0 --maxfail=1              # Stop on first failure
    $0 --no-cleanup --logs      # Keep containers running and show logs

RESULTS:
    Test results are saved to: $RESULTS_DIR
    
EOF
}

# Handle command line arguments
CLEANUP_ON_EXIT=true
SHOW_LOGS=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --no-cleanup)
            CLEANUP_ON_EXIT=false
            trap - EXIT  # Remove cleanup trap
            shift
            ;;
        --logs)
            SHOW_LOGS=true
            shift
            ;;
        --help|-h)
            show_help
            exit 0
            ;;
        *)
            # Pass remaining arguments to pytest
            break
            ;;
    esac
done

# Run main function with remaining arguments
main "$@"
TEST_RESULT=$?

# Show logs if requested
if [ "$SHOW_LOGS" = true ]; then
    print_status "Service logs:"
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" logs
fi

exit $TEST_RESULT
```

### 8. Environment Configuration Template

**File: `station/scripts/.env.regression.template`**

```bash
# Regression Test Environment Configuration
# Copy this file to .env.regression and modify as needed

# Docker configuration
COMPOSE_PROJECT_NAME=aevatar-regression
DOCKER_BUILDKIT=1

# Kind cluster configuration
KIND_CLUSTER_NAME=regression-cluster
KIND_IMAGE_VERSION=v1.29.0

# Service URLs (Docker internal network)
AUTH_HOST=http://authserver:80
API_HOST=http://api:80

# Test client credentials
CLIENT_ID=AevatarTestClient
CLIENT_SECRET=test-secret-key-change-in-production

# Admin credentials for permission tests
ADMIN_USERNAME=admin
ADMIN_PASSWORD=1q2W3e*

# Database configuration
MONGO_ROOT_USER=admin
MONGO_ROOT_PASSWORD=password123
MONGO_DATABASE=AevatarTest

# Kubernetes configuration
K8S_NAMESPACE=aevatar-apps
K8S_APP_REPLICAS=1
K8S_REQUEST_CPU=100m
K8S_REQUEST_MEMORY=128Mi

# Test configuration
PYTEST_ARGS=-v --tb=short --maxfail=5

# Optional: Custom timeouts
TEST_TIMEOUT=30
MAX_RETRIES=3
K8S_RESOURCE_CLEANUP_TIMEOUT=60
```

### 9. Enhanced Test Script Integration

**Modification to `station/scripts/regression_test.py`:**

```python
# Add at the top of regression_test.py after imports:

import os
import sys
import subprocess
from cleanup_k8s_resources import cleanup_k8s_resources

# Docker environment detection
RUNNING_IN_DOCKER = os.path.exists('/.dockerenv')

# Environment-aware configuration
if RUNNING_IN_DOCKER:
    # Use internal Docker network URLs
    AUTH_HOST = os.getenv("AUTH_HOST", "http://authserver:80")
    API_HOST = os.getenv("API_HOST", "http://api:80")
    # Disable SSL verification in Docker (internal network)
    VERIFY_SSL = False
    KUBECONFIG = os.getenv("KUBECONFIG", "/app/kubeconfig")
else:
    # Use localhost URLs for local development
    AUTH_HOST = os.getenv("AUTH_HOST", "https://localhost:44300")
    API_HOST = os.getenv("API_HOST", "https://localhost:44301")
    # Keep SSL verification for external services
    VERIFY_SSL = False  # Keep as False due to self-signed certs
    KUBECONFIG = None

# Add pytest fixture for K8s resource tracking and cleanup
@pytest.fixture(scope="session", autouse=True)
def k8s_cleanup():
    """Cleanup K8s resources after all tests complete"""
    created_resources = []
    
    yield created_resources
    
    # Cleanup after all tests
    if RUNNING_IN_DOCKER and KUBECONFIG:
        logger.info("Cleaning up Kubernetes resources created during tests...")
        cleanup_k8s_resources()

# Helper function to verify K8s resources were created
def verify_k8s_deployment(deployment_name, namespace="aevatar-apps", timeout=60):
    """Verify that a K8s deployment was created successfully"""
    if not KUBECONFIG:
        logger.warning("Skipping K8s verification - no kubeconfig available")
        return False
        
    cmd = [
        "kubectl", "--kubeconfig", KUBECONFIG,
        "get", "deployment", deployment_name,
        "-n", namespace, "-o", "json"
    ]
    
    for attempt in range(timeout // 5):
        try:
            result = subprocess.run(cmd, capture_output=True, text=True)
            if result.returncode == 0:
                logger.info(f"✓ Deployment {deployment_name} exists in namespace {namespace}")
                return True
        except Exception as e:
            logger.error(f"Error checking deployment: {e}")
            
        time.sleep(5)
    
    logger.error(f"✗ Deployment {deployment_name} not found after {timeout}s")
    return False

# Update all requests calls to use VERIFY_SSL variable instead of verify=False
# Example:
# response = requests.post(url, data=data, verify=VERIFY_SSL)
```

## Benefits

### ✅ **Full Kubernetes Support**
- **Real K8s API**: Kind provides a complete Kubernetes cluster in Docker
- **Developer pod creation**: `RegisterClientAuthentication` can create actual K8s resources
- **Staging simulation**: Accurately mimics staging environment behavior
- **Resource validation**: Can verify K8s deployments, services, configmaps are created correctly

### ✅ **Simplicity**
- **Single command setup**: `./run-regression-tests.sh`
- **No manual cluster management**: Kind cluster is automatically created and configured
- **Consistent environment**: Same Kubernetes version and setup across all developer machines

### ✅ **Isolation**
- **Container isolation**: Tests run in isolated containers with dedicated K8s namespace
- **No local conflicts**: No interference with local development or existing K8s clusters
- **Clean state**: Fresh environment and K8s cluster for each test run
- **Automatic cleanup**: K8s resources are cleaned up after tests

### ✅ **Developer Experience**
- **Fast setup**: Only requires Docker installation (Kind runs inside Docker)
- **Clear feedback**: Colored output with status updates and K8s resource visibility
- **Comprehensive results**: Test results exported as JUnit XML and HTML
- **Debugging support**: Can inspect K8s resources created during tests
- **Resource monitoring**: Shows all K8s resources created in `aevatar-apps` namespace

### ✅ **CI/CD Ready**
- **Pipeline integration**: Same setup works in CI/CD pipelines with Docker-in-Docker
- **Structured results**: Test results in standard formats (JUnit XML)
- **Automated cleanup**: Containers and K8s resources are cleaned up automatically
- **Exit codes**: Proper exit codes for build system integration
- **No external dependencies**: Everything runs in Docker containers

## Developer Workflow

### Initial Setup
```bash
# One-time setup
git clone <repository>
cd station/scripts

# Copy environment template (optional customization)
cp .env.regression.template .env.regression

# Make script executable
chmod +x run-regression-tests.sh
```

### Running Tests
```bash
# Run all regression tests
./run-regression-tests.sh

# Run specific tests
./run-regression-tests.sh -k "test_login"

# Stop on first failure
./run-regression-tests.sh --maxfail=1

# Keep containers running for debugging
./run-regression-tests.sh --no-cleanup

# Show service logs after tests
./run-regression-tests.sh --logs
```

### Viewing Results
```bash
# View test results
cat test-results/regression-results.xml

# View HTML report (if generated)
open test-results/report.html

# Check individual test logs
ls test-results/
```

## Troubleshooting

### Common Issues

**Issue: Kind cluster fails to start**
```bash
# Check Kind logs
docker-compose -f docker-compose.regression.yml -p aevatar-regression logs kind

# Verify Docker has enough resources (Kind needs ~2GB RAM)
docker system info | grep -E "Total Memory|CPUs"

# Clean up and retry
docker-compose -f docker-compose.regression.yml -p aevatar-regression down -v
./run-regression-tests.sh
```

**Issue: Services can't connect to Kubernetes**
```bash
# Verify kubeconfig was created
ls -la kubeconfig

# Test K8s connectivity from a service
docker-compose -f docker-compose.regression.yml -p aevatar-regression exec api \
    kubectl --kubeconfig=/app/kubeconfig cluster-info

# Check service has kubeconfig mounted
docker-compose -f docker-compose.regression.yml -p aevatar-regression exec api ls -la /app/
```

**Issue: K8s resources not created**
```bash
# Check K8s namespace
kubectl --kubeconfig=kubeconfig get all -n aevatar-apps

# Check service logs for K8s errors
docker-compose -f docker-compose.regression.yml -p aevatar-regression logs api | grep -i k8s

# Verify K8s permissions
kubectl --kubeconfig=kubeconfig auth can-i create deployments -n aevatar-apps
```

**Issue: Tests timeout**
```bash
# Increase timeout in .env.regression
TEST_TIMEOUT=60

# Or run with extended pytest timeout
./run-regression-tests.sh --timeout=300

# Check if K8s operations are slow
kubectl --kubeconfig=kubeconfig get events -n aevatar-apps --sort-by='.lastTimestamp'
```

**Issue: Port conflicts**
```bash
# Check what's using ports (6443 for K8s API, 8080 for app)
lsof -i :6443
lsof -i :8080

# Stop conflicting services or change ports in docker-compose.yml
```

### Debug Mode
```bash
# Keep containers and K8s cluster running after tests
./run-regression-tests.sh --no-cleanup

# Inspect K8s resources created during tests
kubectl --kubeconfig=kubeconfig get all -n aevatar-apps
kubectl --kubeconfig=kubeconfig describe deployments -n aevatar-apps

# Connect to test container for debugging
docker-compose -f docker-compose.regression.yml -p aevatar-regression exec regression-tests bash

# Run tests manually inside container
docker-compose -f docker-compose.regression.yml -p aevatar-regression exec regression-tests \
    python -m pytest regression_test.py::test_specific -v -s

# Access Kind cluster directly
docker exec -it regression-kind-cluster kubectl get nodes
```

### Kubernetes Resource Inspection
```bash
# List all resources in test namespace
kubectl --kubeconfig=kubeconfig get all -n aevatar-apps

# Get pod logs
kubectl --kubeconfig=kubeconfig logs -n aevatar-apps deployment/your-deployment

# Describe deployment for details
kubectl --kubeconfig=kubeconfig describe deployment -n aevatar-apps your-deployment

# Watch resources being created in real-time
kubectl --kubeconfig=kubeconfig get pods -n aevatar-apps -w
```

## Future Enhancements

1. **Parallel Test Execution**: Add pytest-xdist for faster test runs
2. **Test Data Management**: Add fixtures for consistent test data setup and K8s resource templates
3. **Performance Monitoring**: Add test execution time tracking and K8s resource usage metrics
4. **Integration with IDE**: Add VS Code devcontainer configuration with kubectl tools
5. **Multi-environment Support**: Templates for different K8s configurations (staging, production-like)
6. **Resource Limits**: Implement resource quotas in the test namespace to prevent runaway pods
7. **Helm Chart Support**: Add ability to deploy test applications via Helm charts
8. **K8s Event Monitoring**: Stream K8s events during test execution for better debugging

## Conclusion

This Kubernetes-enabled Docker solution provides a complete testing environment that accurately simulates the staging environment. By incorporating Kind (Kubernetes in Docker), the solution enables:

- Full regression testing including K8s resource creation via `RegisterClientAuthentication`
- Complete isolation with no impact on local development
- One-command execution that sets up everything needed
- Accurate simulation of production behavior with real Kubernetes APIs

The solution eliminates the complexity of local Kubernetes setup while providing all the benefits of containerization and orchestration. It's designed to work seamlessly from local development to CI/CD pipelines, ensuring consistent test execution across all environments.

Most importantly, it solves the critical requirement of testing the actual K8s pod creation workflow that happens when new developers are registered in the system, making it a true end-to-end testing solution.