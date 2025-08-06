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

# Test client configuration (matches regression_test.py defaults)
TEST_CLIENT_ID="${CLIENT_ID:-AevatarTestClient}"
TEST_CLIENT_SECRET="${CLIENT_SECRET:-test-secret-key}"
ADMIN_USERNAME="${ADMIN_USERNAME:-admin}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-1q2W3e*}"
CORS_URLS="${CORS_URLS:-http://localhost:3000,http://localhost:3001}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${GREEN}[INFO]${NC} $1" >&2
}

print_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1" >&2
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1" >&2
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

get_admin_token() {
    local max_attempts=5
    local attempt=1
    
    print_status "Getting admin authentication token..."
    
    # First, let's check if we can access the authserver container
    print_status "Checking AuthServer container accessibility..."
    if ! docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" exec -T authserver sh -c "echo 'Container accessible'" >/dev/null 2>&1; then
        print_error "Cannot access AuthServer container via docker-compose exec"
        return 1
    fi
    
    while [ $attempt -le $max_attempts ]; do
        print_status "Attempt $attempt/$max_attempts: Trying to get admin token..."
        
        # Use docker run with the correct network name to get the token
        print_status "Using temporary curl container to authenticate..."
        local token_response
        token_response=$(docker run --rm \
            --network="${PROJECT_NAME}_regression-network" \
            curlimages/curl:latest \
            --silent --show-error --fail \
            --connect-timeout 10 \
            --max-time 30 \
            'http://authserver:8082/connect/token' \
            -H 'Content-Type: application/x-www-form-urlencoded' \
            -H 'Accept: application/json' \
            --data-urlencode 'grant_type=password' \
            --data-urlencode "username=$ADMIN_USERNAME" \
            --data-urlencode "password=$ADMIN_PASSWORD" \
            --data-urlencode 'scope=Aevatar' \
            --data-urlencode 'client_id=AevatarAuthServer' 2>/dev/null)
        
        print_status "Response from authentication request: '$token_response'"
        
        # Check if we got a response
        if [ -n "$token_response" ]; then
            # Try to extract token with jq if available, otherwise use awk
            local ACCESS_TOKEN
            if command -v jq &> /dev/null; then
                ACCESS_TOKEN=$(echo "$token_response" | jq -r '.access_token' 2>/dev/null)
            else
                # Use awk to properly extract the token
                ACCESS_TOKEN=$(echo "$token_response" | awk -F'"' '/access_token/{print $4}')
            fi
            # Check if token was obtained
            if [ -n "$ACCESS_TOKEN" ] && [ "$ACCESS_TOKEN" != "null" ] && [ "$ACCESS_TOKEN" != "" ]; then
                print_status "✓ Successfully obtained admin access token"
                echo "$ACCESS_TOKEN"
                return 0
            else
                print_status "Token response received but failed to extract access_token: $token_response"
            fi
        else
            print_status "No response received from AuthServer"
        fi
        
        print_status "Attempt $attempt/$max_attempts failed, retrying in 5 seconds..."
        sleep 5
        ((attempt++))
    done
    
    print_error "✗ Failed to obtain admin access token after $max_attempts attempts"
    print_status "Debug: Checking AuthServer container status..."
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" ps authserver || true
    print_status "Debug: Checking AuthServer logs..."
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" logs --tail=20 authserver || true
    return 1
}

register_test_client() {
    local admin_token="$1"
    local api_url="http://localhost:8080"  # API service external port (mapped from 8001)
    
    print_status "Registering test client: $TEST_CLIENT_ID"
    
    # Get a fresh token to avoid expiration issues
    print_status "Getting fresh token for client registration..."
    local fresh_token
    if ! fresh_token=$(get_admin_token); then
        print_error "Failed to get fresh admin token for client registration"
        return 1
    fi
    admin_token="$fresh_token"
    
    # URL encode CORS URLs
    local cors_urls_encoded
    if command -v jq &> /dev/null; then
        cors_urls_encoded=$(printf "%s" "$CORS_URLS" | jq -s -R -r @uri)
    else
        # More comprehensive URL encoding for common characters
        cors_urls_encoded=$(printf "%s" "$CORS_URLS" | sed 's/ /%20/g' | sed 's/,/%2C/g' | sed 's/:/%3A/g' | sed 's|/|%2F|g')
    fi
    
    # Make registration request
    local response_file=$(mktemp)
    local http_status
    local full_url="$api_url/api/users/registerClient?clientId=$TEST_CLIENT_ID&clientSecret=$TEST_CLIENT_SECRET&corsUrls=$cors_urls_encoded"
    
    print_status "Making registration request to: $full_url"
    print_status "Admin token: $admin_token"
    
    http_status=$(curl --silent --show-error \
        --connect-timeout 10 \
        --max-time 30 \
        -o "$response_file" \
        -w "%{http_code}" \
        -X POST \
        "$full_url" \
        -H 'Accept: */*' \
        -H "Authorization: Bearer $admin_token" \
        -H 'X-Requested-With: XMLHttpRequest' \
        2>/dev/null)
    
    # Log response for debugging
    if [ -s "$response_file" ]; then
        local response_content=$(cat "$response_file")
        print_status "Registration response (HTTP $http_status): $response_content"
    else
        print_status "No response content received (HTTP $http_status)"
        # Try to get error details with verbose curl
        if [ "$http_status" != "200" ] && [ "$http_status" != "201" ]; then
            print_status "Retrying with verbose output for debugging..."
            local verbose_response=$(curl --verbose \
                --connect-timeout 10 \
                --max-time 30 \
                -X POST \
                "$full_url" \
                -H 'Accept: */*' \
                -H "Authorization: Bearer $admin_token" \
                -H 'X-Requested-With: XMLHttpRequest' \
                2>&1)
            print_status "Verbose curl output: $verbose_response"
        fi
    fi
    
    # Clean up response file
    rm -f "$response_file"
    
    # Check if registration was successful (200 or 201 status codes)
    if [ "$http_status" = "200" ] || [ "$http_status" = "201" ]; then
        print_status "✓ Successfully registered test client: $TEST_CLIENT_ID"
        return 0
    elif [ "$http_status" = "409" ]; then
        print_warning "Test client already exists: $TEST_CLIENT_ID (HTTP $http_status)"
        return 0  # This is OK - client already registered
    else
        print_error "✗ Failed to register test client. HTTP status: $http_status"
        return 1
    fi
}

setup_test_client() {
    print_status "Setting up test client for regression tests..."
    
    # Get admin token
    local admin_token
    if ! admin_token=$(get_admin_token); then
        print_error "Failed to get admin authentication token"
        return 1
    fi
    
    # Register test client
    if ! register_test_client "$admin_token"; then
        print_error "Failed to register test client"
        return 1
    fi
    
    print_status "✓ Test client setup completed successfully"
    return 0
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
    
    # Run database migration and seeding
    print_status "Running database migration and seeding..."
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" up dbmigrator
    
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
    
    # Setup test client for regression tests
    if ! setup_test_client; then
        print_error "Test client setup failed"
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
    echo -e "${GREEN}📝 Running: pytest regression_test.py -v${NC}"
    echo -e "${GREEN}================================================${NC}"
    set +e  # Don't exit on test failures
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" run --rm regression-tests pytest regression_test.py -v "$@"
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
    --no-cleanup      Don't cleanup containers after tests (for debugging)
    --logs           Show service logs after test completion
    --setup-client-only   Run only the test client setup (requires services to be running)
    --help           Show this help

PYTEST_ARGS:
    Any additional arguments are passed to pytest

ENVIRONMENT VARIABLES:
    CLIENT_ID         Test client ID (default: AevatarTestClient)
    CLIENT_SECRET     Test client secret (default: test-secret-key)
    ADMIN_USERNAME    Admin username for client registration (default: admin)
    ADMIN_PASSWORD    Admin password for client registration (default: 1q2W3e*)
    CORS_URLS         Comma-separated CORS URLs (default: http://localhost:3000,http://localhost:3001)

EXAMPLES:
    $0                           # Run all tests
    $0 -k "test_login"          # Run specific test
    $0 --maxfail=1              # Stop on first failure
    $0 --no-cleanup --logs      # Keep containers running and show logs
    CLIENT_ID=MyTestClient CLIENT_SECRET=MySecret $0  # Custom test client

RESULTS:
    Test results are saved to: $RESULTS_DIR
    
EOF
}

# Handle command line arguments
CLEANUP_ON_EXIT=true
SHOW_LOGS=false
PYTEST_ARGS=()

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
        --setup-client-only)
            # Run only the client setup function
            print_status "Running client setup only..."
            cd "$SCRIPT_DIR"
            
            # Check if services are running
            if ! docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" ps | grep -q "authserver.*Up"; then
                print_error "AuthServer is not running. Start services first with: $0 --start-services"
                exit 1
            fi
            
            # Run the setup
            if setup_test_client; then
                print_status "✓ Client setup completed successfully"
                exit 0
            else
                print_error "✗ Client setup failed"
                exit 1
            fi
            ;;
        *)
            # Store remaining arguments for pytest
            PYTEST_ARGS+=("$1")
            shift
            ;;
    esac
done

# Run main function with pytest arguments
main "${PYTEST_ARGS[@]}"
TEST_RESULT=$?

# Show logs if requested
if [ "$SHOW_LOGS" = true ]; then
    print_status "Service logs:"
    docker-compose -f "$COMPOSE_FILE" -p "$PROJECT_NAME" logs
fi

exit $TEST_RESULT