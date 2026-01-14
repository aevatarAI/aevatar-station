#!/bin/bash

# =============================================================================
# Stop Services Script for Aevatar Station
# Stops all running Aevatar services and kubectl port-forward
# =============================================================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PID_DIR="$SCRIPT_DIR/.pids"

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Stop a service by PID file
stop_service() {
    local name=$1
    local pid_file="$PID_DIR/${name}.pid"
    
    if [ -f "$pid_file" ]; then
        local pid=$(cat "$pid_file")
        if kill -0 $pid 2>/dev/null; then
            log_info "Stopping $name (PID: $pid)..."
            kill $pid 2>/dev/null
            
            # Wait for graceful shutdown
            local count=0
            while kill -0 $pid 2>/dev/null && [ $count -lt 10 ]; do
                sleep 1
                count=$((count + 1))
            done
            
            # Force kill if still running
            if kill -0 $pid 2>/dev/null; then
                log_warn "Force killing $name..."
                kill -9 $pid 2>/dev/null
            fi
            
            log_info "$name stopped ✓"
        else
            log_warn "$name is not running (stale PID file)"
        fi
        rm -f "$pid_file"
    else
        log_warn "No PID file found for $name"
    fi
}

# Kill any remaining dotnet processes for our projects
cleanup_dotnet() {
    log_info "Cleaning up any remaining dotnet processes..."
    
    # Find and kill dotnet processes related to our projects
    pkill -f "Aevatar.Silo" 2>/dev/null || true
    pkill -f "Aevatar.AuthServer" 2>/dev/null || true
    pkill -f "Aevatar.HttpApi.Host" 2>/dev/null || true
    
    sleep 1
}

# Stop kubectl port-forward
stop_portforward() {
    log_info "Stopping kubectl port-forward..."
    
    local pid_file="$PID_DIR/portforward.pid"
    if [ -f "$pid_file" ]; then
        local pid=$(cat "$pid_file")
        if kill -0 $pid 2>/dev/null; then
            kill $pid 2>/dev/null
            log_info "Port-forward stopped ✓"
        fi
        rm -f "$pid_file"
    fi
    
    # Also kill any remaining port-forward processes
    pkill -f "kubectl.*port-forward.*mongo" 2>/dev/null || true
}

# Show status
show_status() {
    echo ""
    log_info "Checking remaining processes..."
    
    # Check for any remaining Aevatar processes
    local remaining=$(pgrep -f "Aevatar" 2>/dev/null | wc -l | tr -d ' ')
    if [ "$remaining" -gt 0 ]; then
        log_warn "Found $remaining remaining Aevatar processes:"
        pgrep -fl "Aevatar" 2>/dev/null || true
    else
        log_info "No remaining Aevatar processes ✓"
    fi
    
    # Check ports
    for port in 8001 8082 11111 27018; do
        if lsof -i :$port > /dev/null 2>&1; then
            log_warn "Port $port is still in use"
        fi
    done
}

# Main function
main() {
    local clean_logs=false
    local force_kill=false
    
    # Parse arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --clean)
                clean_logs=true
                shift
                ;;
            --force)
                force_kill=true
                shift
                ;;
            --help|-h)
                echo "Usage: $0 [OPTIONS]"
                echo ""
                echo "Options:"
                echo "  --clean    Remove log files after stopping"
                echo "  --force    Force kill all Aevatar processes"
                echo "  --help, -h Show this help message"
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                exit 1
                ;;
        esac
    done
    
    log_info "========================================"
    log_info "Stopping Aevatar Station Services"
    log_info "========================================"
    
    # Stop services in reverse order
    stop_service "httpapi"
    stop_service "auth"
    stop_service "silo"
    stop_portforward
    
    # Cleanup any remaining processes
    cleanup_dotnet
    
    # Force kill if requested
    if [ "$force_kill" = true ]; then
        log_info "Force killing all Aevatar processes..."
        pkill -9 -f "Aevatar" 2>/dev/null || true
        pkill -9 -f "kubectl.*port-forward" 2>/dev/null || true
    fi
    
    # Clean up log files if requested
    if [ "$clean_logs" = true ]; then
        log_info "Cleaning up log files..."
        rm -rf "$PID_DIR"/*.log
        log_info "Log files removed ✓"
    fi
    
    show_status
    
    log_info "========================================"
    log_info "All services stopped"
    log_info "========================================"
}

main "$@"
