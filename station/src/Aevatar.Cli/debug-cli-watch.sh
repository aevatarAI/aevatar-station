#!/bin/bash

# =============================================================================
# Aevatar CLI Watch & Auto-Rebuild Debug Script
# =============================================================================
# 
# This script watches for file changes in the CLI project and automatically
# rebuilds and restarts the CLI application. Perfect for active development.
#
# Usage:
#   ./debug-cli-watch.sh [cli-arguments...]
#
# Examples:
#   ./debug-cli-watch.sh --help              # Watch and run CLI with help
#   ./debug-cli-watch.sh version             # Watch and run CLI with version command
#
# Requirements:
#   - fswatch (install with: brew install fswatch on macOS)
#   - or use dotnet watch run (built-in .NET SDK feature)
#
# =============================================================================

set -e  # Exit on any error

# Configuration
CLI_PROJECT_DIR="."
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$SCRIPT_DIR"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
NC='\033[0m' # No Color

# Functions
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_watch() {
    echo -e "${PURPLE}[WATCH]${NC} $1"
}

print_separator() {
    echo "=================================================================="
}

# Validate environment
validate_environment() {
    print_info "Validating environment..."
    
    # Check if dotnet is installed
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK is not installed or not in PATH"
        exit 1
    fi
    
    # Check .NET version
    local dotnet_version=$(dotnet --version)
    print_info "Using .NET SDK version: $dotnet_version"
    
    # Check if project directory exists
    if [ ! -d "$PROJECT_ROOT/$CLI_PROJECT_DIR" ]; then
        print_error "CLI project directory not found: $PROJECT_ROOT/$CLI_PROJECT_DIR"
        exit 1
    fi
    
    # Check if project file exists
    if [ ! -f "$PROJECT_ROOT/$CLI_PROJECT_DIR/Aevatar.Cli.csproj" ]; then
        print_error "CLI project file not found: $PROJECT_ROOT/$CLI_PROJECT_DIR/Aevatar.Cli.csproj"
        exit 1
    fi
    
    print_success "Environment validation completed"
}

# Check if fswatch is available (optional, fallback to dotnet watch)
check_fswatch() {
    if command -v fswatch &> /dev/null; then
        print_info "fswatch is available - using advanced file watching"
        return 0
    else
        print_warning "fswatch not found - falling back to dotnet watch run"
        print_info "For better performance, install fswatch: brew install fswatch"
        return 1
    fi
}

# Run with dotnet watch (built-in .NET feature)
run_with_dotnet_watch() {
    print_watch "Starting dotnet watch mode..."
    print_info "The application will automatically rebuild and restart when files change"
    print_info "Use Ctrl+C to stop watching"
    
    export DOTNET_ENVIRONMENT=Development
    export DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true
    
    # Use dotnet watch run with arguments
    if [ $# -gt 0 ]; then
        print_info "Running with arguments: $*"
        dotnet watch run --configuration Debug --project "$PROJECT_ROOT/$CLI_PROJECT_DIR" -- "$@"
    else
        print_info "Running without arguments (will show help)"
        dotnet watch run --configuration Debug --project "$PROJECT_ROOT/$CLI_PROJECT_DIR"
    fi
}

# Run with custom fswatch implementation
run_with_fswatch() {
    local cli_args="$*"
    local pid=""
    
    print_watch "Starting advanced file watching with fswatch..."
    print_info "Watching for changes in: $PROJECT_ROOT/$CLI_PROJECT_DIR"
    print_info "Use Ctrl+C to stop watching"
    
    # Function to build and run CLI
    build_and_run() {
        print_separator
        print_watch "File change detected - rebuilding..."
        
        # Kill previous process if running
        if [ ! -z "$pid" ] && kill -0 "$pid" 2>/dev/null; then
            print_info "Stopping previous CLI instance (PID: $pid)"
            kill "$pid" 2>/dev/null || true
            wait "$pid" 2>/dev/null || true
        fi
        
        cd "$PROJECT_ROOT/$CLI_PROJECT_DIR"
        
        # Build the project
        if dotnet build --configuration Debug --verbosity minimal; then
            print_success "Build completed - starting CLI"
            
            # Run CLI in background
            export DOTNET_ENVIRONMENT=Development
            if [ ! -z "$cli_args" ]; then
                dotnet run --configuration Debug --no-build -- $cli_args &
            else
                dotnet run --configuration Debug --no-build &
            fi
            pid=$!
            print_info "CLI started with PID: $pid"
        else
            print_error "Build failed - not starting CLI"
        fi
        
        cd "$PROJECT_ROOT"
    }
    
    # Initial build and run
    build_and_run
    
    # Watch for file changes
    fswatch -o -r -e "bin/" -e "obj/" -e "\.git" "$PROJECT_ROOT/$CLI_PROJECT_DIR" | while read num; do
        print_watch "Detected $num file changes"
        sleep 0.5  # Debounce rapid changes
        build_and_run
    done
}

# Handle cleanup on script exit
cleanup() {
    print_info "Cleaning up..."
    # Kill any background processes
    if [ ! -z "$pid" ] && kill -0 "$pid" 2>/dev/null; then
        print_info "Stopping CLI process (PID: $pid)"
        kill "$pid" 2>/dev/null || true
    fi
    exit 0
}

# Main execution
main() {
    print_separator
    print_info "Aevatar CLI Watch & Auto-Rebuild Debug Script"
    print_separator
    
    validate_environment
    
    # Check which watch method to use
    if check_fswatch; then
        run_with_fswatch "$@"
    else
        run_with_dotnet_watch "$@"
    fi
}

# Handle Ctrl+C gracefully
trap cleanup SIGINT SIGTERM

# Show usage if --help is passed
if [[ "$1" == "--help" || "$1" == "-h" ]]; then
    echo "Aevatar CLI Watch & Auto-Rebuild Debug Script"
    echo ""
    echo "Usage: $0 [cli-arguments...]"
    echo ""
    echo "This script watches for file changes in the CLI project and automatically"
    echo "rebuilds and restarts the CLI application when changes are detected."
    echo ""
    echo "Examples:"
    echo "  $0                           # Watch and run CLI without arguments"
    echo "  $0 --help                    # Watch and run CLI with --help"
    echo "  $0 version                   # Watch and run CLI with version command"
    echo "  $0 some-command --option     # Watch and run CLI with arguments"
    echo ""
    echo "The script will use one of two watch methods:"
    echo "  1. fswatch (if available) - More responsive file watching"
    echo "  2. dotnet watch run - Built-in .NET SDK feature (fallback)"
    echo ""
    echo "To install fswatch on macOS: brew install fswatch"
    echo ""
    echo "Use Ctrl+C to stop the file watching and exit."
    echo ""
    exit 0
fi

# Execute main function with all arguments
main "$@"
