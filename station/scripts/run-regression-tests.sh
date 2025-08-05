#!/bin/bash
# ABOUTME: One-command script to run regression tests with Kind Kubernetes cluster
# ABOUTME: Handles Kind setup, service startup, health checks, test execution, and cleanup

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="$SCRIPT_DIR/docker-compose.regression.yml"
PROJECT_NAME="aevatar-regression"
RESULTS_DIR="$SCRIPT_DIR/test-results"
K3S_CLUSTER_NAME="regression-cluster"
KUBECONFIG_PATH="$SCRIPT_DIR/shared/kubeconfig-host"

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
        # For API service, test from host since port mapping might be different
        if [ "$service_name" = "api" ]; then
            if curl -f "http://localhost:8080/health" &>/dev/null; then
                print_status "✓ $service_name is ready"
                return 0
            fi
        else
            # For other services, try to test from within the container
            if docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" exec -T "$service_name" curl -f "$health_url" &>/dev/null; then
                print_status "✓ $service_name is ready"
                return 0
            fi
        fi
        
        print_status "Attempt $attempt/$max_attempts: $service_name not ready, waiting..."
        sleep 5
        ((attempt++))
    done
    
    print_error "✗ $service_name failed to become ready after $max_attempts attempts"
    return 1
}

wait_for_k3s_ready() {
    local max_attempts=60
    local attempt=1
    
    print_status "Waiting for K3s cluster to be ready..."
    
    while [ $attempt -le $max_attempts ]; do
        if [ -f "$KUBECONFIG_PATH" ] && kubectl --kubeconfig="$KUBECONFIG_PATH" --insecure-skip-tls-verify cluster-info &>/dev/null; then
            print_status "✓ K3s cluster is ready"
            return 0
        fi
        
        print_status "Attempt $attempt/$max_attempts: K3s not ready, waiting..."
        sleep 3
        ((attempt++))
    done
    
    print_error "✗ K3s cluster failed to become ready"
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
    
    # Start K3s cluster first
    print_status "Starting K3s Kubernetes cluster..."
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" up -d k3s
    
    # Wait a bit for K3s to initialize
    sleep 15
    
    # Setup K3s cluster (namespace, RBAC, export kubeconfig)
    print_status "Setting up K3s cluster..."
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" up k3s-setup
    
    # Verify K3s is ready
    if ! wait_for_k3s_ready; then
        print_error "K3s cluster setup failed"
        docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" logs k3s k3s-setup
        exit 1
    fi
    
    # Build and start infrastructure services
    print_status "Starting infrastructure services (MongoDB, Redis, Kafka)..."
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" up -d --build mongodb redis zookeeper kafka
    
    print_status "Waiting for infrastructure to be ready..."
    sleep 30
    
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
    if ! wait_for_service "api" "http://localhost:8001/health"; then
        print_error "API service health check failed"
        print_status "Checking service logs..."
        docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" logs api
        exit 1
    fi
    
    # Show K8s cluster info
    print_status "Kubernetes cluster info:"
    kubectl --kubeconfig="$KUBECONFIG_PATH" --insecure-skip-tls-verify cluster-info || true
    kubectl --kubeconfig="$KUBECONFIG_PATH" --insecure-skip-tls-verify get nodes || true
    
    # Run tests
    print_status "🚀 Starting regression_test.py execution..."
    echo -e "${GREEN}================================================${NC}"
    echo -e "${GREEN}🧪 REGRESSION TEST EXECUTION STARTING NOW!${NC}"
    echo -e "${GREEN}📝 Running: regression_test.py${NC}"
    echo -e "${GREEN}================================================${NC}"
    set +e  # Don't exit on test failures
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" run --rm regression-tests python regression_test.py "$@"
    TEST_EXIT_CODE=$?
    set -e
    echo -e "${GREEN}================================================${NC}"
    echo -e "${GREEN}🏁 REGRESSION TEST EXECUTION COMPLETED!${NC}"
    echo -e "${GREEN}📊 Exit Code: $TEST_EXIT_CODE${NC}"
    echo -e "${GREEN}================================================${NC}"
    
    # Show K8s resources created during tests
    print_status "Kubernetes resources created during tests:"
    kubectl --kubeconfig="$KUBECONFIG_PATH" --insecure-skip-tls-verify get all -n aevatar-apps || true
    
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