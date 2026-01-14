#!/bin/bash

# =============================================================================
# Start Services Script for Aevatar Station
# Starts kubectl port-forward, AuthServer, Silo, and HttpApi for testing
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
SRC_DIR="$PROJECT_ROOT/src"

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# PID file location
PID_DIR="$SCRIPT_DIR/.pids"
mkdir -p "$PID_DIR"

# Default ports
AUTH_PORT=8082
HTTPAPI_PORT=8001
SILO_PORT=11111
MONGO_FORWARD_PORT=27018

# Kubernetes config
K8S_NAMESPACE="dapp-factory-shared"
MONGO_POD="mongo-0"
KUBECONFIG_FILE="/Users/liyingpei/Downloads/aelf-shared-k8s-prod-new.yaml"

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_step() {
    echo -e "${BLUE}[STEP]${NC} $1"
}

# Check if port is in use
check_port() {
    local port=$1
    if lsof -i :$port > /dev/null 2>&1; then
        return 0  # Port is in use
    fi
    return 1  # Port is free
}

# Start kubectl port-forward for MongoDB
start_mongo_portforward() {
    log_step "Setting up MongoDB port-forward..."
    
    if check_port $MONGO_FORWARD_PORT; then
        log_info "MongoDB port-forward already running on port $MONGO_FORWARD_PORT ✓"
        return 0
    fi
    
    # Check if kubectl is available
    if ! command -v kubectl &> /dev/null; then
        log_error "kubectl not found. Please install kubectl first."
        return 1
    fi
    
    # Check if kubeconfig file exists
    if [ -f "$KUBECONFIG_FILE" ]; then
        export KUBECONFIG="$KUBECONFIG_FILE"
        log_info "Using kubeconfig: $KUBECONFIG_FILE"
    else
        log_warn "Kubeconfig file not found: $KUBECONFIG_FILE"
        return 1
    fi
    
    # Try multiple MongoDB pods to find the primary
    local mongo_pods=("mongo-0" "mongo-1" "mongo-2")
    local success=false
    
    for pod in "${mongo_pods[@]}"; do
        log_info "Attempting port-forward to $pod..."
        
        # Kill any existing port-forward on this port
        lsof -ti :$MONGO_FORWARD_PORT | xargs kill -9 2>/dev/null || true
        sleep 1
        
        # Start port-forward
        kubectl -n $K8S_NAMESPACE port-forward pod/$pod $MONGO_FORWARD_PORT:27017 > "$PID_DIR/portforward.log" 2>&1 &
        local pid=$!
        echo $pid > "$PID_DIR/portforward.pid"
        
        # Wait for port-forward to be ready
        sleep 5
        
        if check_port $MONGO_FORWARD_PORT; then
            # Test connection
            if mongo "mongodb://localhost:$MONGO_FORWARD_PORT" --eval "db.isMaster()" > /dev/null 2>&1; then
                log_info "MongoDB port-forward to $pod started successfully (PID: $pid) ✓"
                success=true
                break
            else
                log_warn "Port-forward to $pod started but connection test failed"
                kill $pid 2>/dev/null || true
                rm -f "$PID_DIR/portforward.pid"
            fi
        else
            log_warn "Failed to start port-forward to $pod"
            kill $pid 2>/dev/null || true
            rm -f "$PID_DIR/portforward.pid"
        fi
    done
    
    if [ "$success" = false ]; then
        log_error "Failed to start MongoDB port-forward to any pod"
        cat "$PID_DIR/portforward.log" 2>/dev/null || true
        return 1
    fi
    
    return 0
}

# Start a .NET service
start_service() {
    local name=$1
    local project_dir=$2
    local project_file=$3
    local port=$4
    
    log_step "Starting $name on port $port..."
    
    if check_port $port; then
        log_warn "$name might already be running on port $port"
    fi
    
    cd "$project_dir"
    
    # Run in background and save PID
    ASPNETCORE_ENVIRONMENT=Development dotnet run --project "$project_file" --no-build > "$PID_DIR/${name}.log" 2>&1 &
    local pid=$!
    echo $pid > "$PID_DIR/${name}.pid"
    
    log_info "$name started with PID $pid"
    
    # Wait a bit for the service to start
    sleep 2
}

# Wait for service to be ready
wait_for_service() {
    local name=$1
    local port=$2
    local max_wait=$3
    
    log_info "Waiting for $name to be ready..."
    
    local count=0
    while ! check_port $port && [ $count -lt $max_wait ]; do
        sleep 1
        count=$((count + 1))
        echo -n "."
    done
    echo ""
    
    if check_port $port; then
        log_info "$name is ready ✓"
        return 0
    else
        log_error "$name failed to start within ${max_wait}s"
        log_error "Check log: $PID_DIR/${name}.log"
        return 1
    fi
}

# Build projects
build_projects() {
    log_step "Building projects..."
    cd "$PROJECT_ROOT"
    
    # Build main projects
    dotnet build "$SRC_DIR/Aevatar.Silo/Aevatar.Silo.csproj" --configuration Debug --no-restore || {
        log_warn "Silo build failed, trying with restore..."
        dotnet build "$SRC_DIR/Aevatar.Silo/Aevatar.Silo.csproj" --configuration Debug
    }
    
    dotnet build "$SRC_DIR/Aevatar.AuthServer/Aevatar.AuthServer.csproj" --configuration Debug --no-restore || {
        log_warn "AuthServer build failed, trying with restore..."
        dotnet build "$SRC_DIR/Aevatar.AuthServer/Aevatar.AuthServer.csproj" --configuration Debug
    }
    
    dotnet build "$SRC_DIR/Aevatar.HttpApi.Host/Aevatar.HttpApi.Host.csproj" --configuration Debug --no-restore || {
        log_warn "HttpApi build failed, trying with restore..."
        dotnet build "$SRC_DIR/Aevatar.HttpApi.Host/Aevatar.HttpApi.Host.csproj" --configuration Debug
    }
    
    log_info "Build completed ✓"
}

# Main function
main() {
    local skip_build=false
    local skip_mongo=false
    
    # Parse arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --no-build)
                skip_build=true
                shift
                ;;
            --no-mongo)
                skip_mongo=true
                shift
                ;;
            --help|-h)
                echo "Usage: $0 [OPTIONS]"
                echo ""
                echo "Options:"
                echo "  --no-build    Skip building projects"
                echo "  --no-mongo    Skip MongoDB port-forward setup"
                echo "  --help, -h    Show this help message"
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                exit 1
                ;;
        esac
    done
    
    log_info "========================================"
    log_info "Starting Aevatar Station Services"
    log_info "========================================"
    
    # Step 0: Setup MongoDB port-forward (if needed)
    if [ "$skip_mongo" = false ]; then
        start_mongo_portforward || {
            log_warn "MongoDB port-forward failed, continuing with local MongoDB..."
        }
    fi
    
    # Step 1: Build projects
    if [ "$skip_build" = false ]; then
        build_projects
    else
        log_info "Skipping build (--no-build)"
    fi
    
    # Step 2: Start Silo first (Orleans)
    start_service "silo" "$SRC_DIR" "Aevatar.Silo/Aevatar.Silo.csproj" $SILO_PORT
    wait_for_service "silo" $SILO_PORT 30
    
    # Step 3: Start AuthServer
    start_service "auth" "$SRC_DIR" "Aevatar.AuthServer/Aevatar.AuthServer.csproj" $AUTH_PORT
    wait_for_service "auth" $AUTH_PORT 20
    
    # Step 4: Start HttpApi
    start_service "httpapi" "$SRC_DIR" "Aevatar.HttpApi.Host/Aevatar.HttpApi.Host.csproj" $HTTPAPI_PORT
    wait_for_service "httpapi" $HTTPAPI_PORT 20
    
    log_info "========================================"
    log_info "All services started!"
    log_info "========================================"
    echo ""
    log_info "Services:"
    log_info "  - Silo:        Port $SILO_PORT (Orleans)"
    log_info "  - AuthServer:  http://localhost:$AUTH_PORT"
    log_info "  - HttpApi:     http://localhost:$HTTPAPI_PORT"
    log_info "  - MongoDB:     localhost:$MONGO_FORWARD_PORT (forwarded)"
    echo ""
    log_info "API Endpoints:"
    log_info "  - Swagger:     http://localhost:$HTTPAPI_PORT/swagger"
    log_info "  - Export API:  http://localhost:$HTTPAPI_PORT/api/admin/export/summary"
    echo ""
    log_info "Logs:"
    log_info "  - Silo:        $PID_DIR/silo.log"
    log_info "  - AuthServer:  $PID_DIR/auth.log"
    log_info "  - HttpApi:     $PID_DIR/httpapi.log"
    log_info "  - PortForward: $PID_DIR/portforward.log"
    echo ""
    log_info "Use './stop-services.sh' to stop all services"
}

main "$@"
