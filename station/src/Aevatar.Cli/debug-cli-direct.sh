#!/bin/bash

# =============================================================================
# Aevatar CLI Direct Execution Debug Script
# =============================================================================
# 
# This script builds the CLI project once and then directly executes the 
# compiled binary. Fastest execution for repeated testing with same code.
#
# Usage:
#   ./debug-cli-direct.sh [cli-arguments...]
#
# Examples:
#   ./debug-cli-direct.sh --help              # Run compiled CLI with help
#   ./debug-cli-direct.sh version             # Run compiled CLI with version
#   ./debug-cli-direct.sh --rebuild           # Force rebuild before execution
#
# =============================================================================

set -e  # Exit on any error

# Configuration
CLI_PROJECT_DIR="."
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$SCRIPT_DIR"
BINARY_PATH="$PROJECT_ROOT/$CLI_PROJECT_DIR/bin/Debug/net9.0/Aevatar.Cli"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
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

print_build() {
    echo -e "${CYAN}[BUILD]${NC} $1"
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

# Check if binary needs rebuilding
needs_rebuild() {
    local force_rebuild=false
    
    # Check for --rebuild flag in arguments
    for arg in "$@"; do
        if [[ "$arg" == "--rebuild" ]]; then
            force_rebuild=true
            break
        fi
    done
    
    if [ "$force_rebuild" = true ]; then
        print_info "Force rebuild requested via --rebuild flag"
        return 0
    fi
    
    # Check if binary exists
    if [ ! -f "$BINARY_PATH" ]; then
        print_info "Binary not found, build required: $BINARY_PATH"
        return 0
    fi
    
    # Check if any source files are newer than binary
    local binary_timestamp=$(stat -f %m "$BINARY_PATH" 2>/dev/null || echo 0)
    local newest_source=$(find "$PROJECT_ROOT/$CLI_PROJECT_DIR" -name "*.cs" -o -name "*.csproj" -newer "$BINARY_PATH" 2>/dev/null | head -1)
    
    if [ ! -z "$newest_source" ]; then
        print_info "Source files are newer than binary, rebuild required"
        print_info "Newer file detected: $newest_source"
        return 0
    fi
    
    return 1
}

# Build the project
build_project() {
    print_build "Building CLI project..."
    print_info "Project: $CLI_PROJECT_DIR"
    
    cd "$PROJECT_ROOT/$CLI_PROJECT_DIR"
    
    # Clean and restore first for a fresh build
    print_build "Cleaning previous build..."
    dotnet clean --configuration Debug --verbosity minimal
    
    print_build "Restoring packages..."
    dotnet restore --verbosity minimal
    
    print_build "Building project..."
    if dotnet build --configuration Debug --verbosity minimal; then
        print_success "Build completed successfully"
        
        # Verify binary exists
        if [ -f "$BINARY_PATH" ]; then
            print_success "Binary created: $BINARY_PATH"
            print_info "Binary size: $(ls -lh "$BINARY_PATH" | awk '{print $5}')"
            print_info "Binary timestamp: $(ls -l "$BINARY_PATH" | awk '{print $6, $7, $8}')"
        else
            print_error "Binary not found after build: $BINARY_PATH"
            exit 1
        fi
    else
        print_error "Build failed"
        exit 1
    fi
    
    cd "$PROJECT_ROOT"
}

# Execute the binary directly
execute_binary() {
    local args=()
    
    # Filter out --rebuild from arguments
    for arg in "$@"; do
        if [[ "$arg" != "--rebuild" ]]; then
            args+=("$arg")
        fi
    done
    
    print_separator
    if [ ${#args[@]} -gt 0 ]; then
        print_info "Executing CLI with arguments: ${args[*]}"
    else
        print_info "Executing CLI without arguments"
    fi
    print_info "Binary: $BINARY_PATH"
    print_separator
    
    # Set environment for debugging
    export DOTNET_ENVIRONMENT=Development
    
    # Execute the binary
    "$BINARY_PATH" "${args[@]}"
    local exit_code=$?
    
    print_separator
    if [ $exit_code -eq 0 ]; then
        print_success "CLI execution completed successfully"
    else
        print_warning "CLI execution finished with exit code: $exit_code"
    fi
    
    return $exit_code
}

# Show binary information
show_binary_info() {
    if [ -f "$BINARY_PATH" ]; then
        print_info "Binary Information:"
        print_info "  Path: $BINARY_PATH"
        print_info "  Size: $(ls -lh "$BINARY_PATH" | awk '{print $5}')"
        print_info "  Modified: $(ls -l "$BINARY_PATH" | awk '{print $6, $7, $8}')"
        
        # Show .NET version info from binary
        if command -v file &> /dev/null; then
            local file_info=$(file "$BINARY_PATH")
            print_info "  Type: $file_info"
        fi
    else
        print_warning "Binary not found: $BINARY_PATH"
    fi
}

# Main execution
main() {
    print_separator
    print_info "Aevatar CLI Direct Execution Debug Script"
    print_separator
    
    validate_environment
    
    # Check if we need to rebuild
    if needs_rebuild "$@"; then
        build_project
    else
        print_success "Binary is up to date, skipping build"
        show_binary_info
    fi
    
    # Execute the binary
    execute_binary "$@"
}

# Handle Ctrl+C gracefully
trap 'echo -e "\n${YELLOW}[INFO]${NC} Script interrupted by user"; exit 130' SIGINT

# Show usage if --help is passed (but not if --help is for the CLI)
if [[ "$1" == "--help" || "$1" == "-h" ]] && [[ "$2" != "--rebuild" ]] && [[ $# -eq 1 ]]; then
    echo "Aevatar CLI Direct Execution Debug Script"
    echo ""
    echo "Usage: $0 [--rebuild] [cli-arguments...]"
    echo ""
    echo "This script builds the CLI project once (if needed) and then directly"
    echo "executes the compiled binary for fastest repeated execution."
    echo ""
    echo "Options:"
    echo "  --rebuild              Force rebuild even if binary is up to date"
    echo ""
    echo "Examples:"
    echo "  $0 --help              # Show this help (add any arg to pass to CLI)"
    echo "  $0 version              # Run CLI version command"
    echo "  $0 --rebuild version    # Force rebuild then run version command"
    echo "  $0 cmd --option         # Run CLI with arguments"
    echo ""
    echo "The script will:"
    echo "  1. Check if binary needs rebuilding (source files newer or --rebuild)"
    echo "  2. Build project if needed (clean, restore, build)"
    echo "  3. Execute the compiled binary directly"
    echo ""
    echo "Binary location: $BINARY_PATH"
    echo ""
    exit 0
fi

# Execute main function with all arguments
main "$@"
