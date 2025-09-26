#!/bin/bash

# =============================================================================
# Aevatar CLI Quick Debug Script
# =============================================================================
# 
# This script allows you to quickly run and debug the Aevatar.Cli tool without
# publishing it as a global tool. Uses 'dotnet run' for immediate execution.
#
# Usage:
#   ./debug-cli.sh [cli-arguments...]
#
# Examples:
#   ./debug-cli.sh --help                    # Show CLI help
#   ./debug-cli.sh version                   # Show version
#   ./debug-cli.sh some-command --option     # Run with arguments
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

# Main execution
main() {
    print_separator
    print_info "Aevatar CLI Quick Debug Script"
    print_separator
    
    validate_environment
    
    print_info "Changing to CLI project directory: $CLI_PROJECT_DIR"
    cd "$PROJECT_ROOT/$CLI_PROJECT_DIR"
    
    # Show current directory
    print_info "Current working directory: $(pwd)"
    
    # Build the project first to ensure it's up to date
    print_info "Building project..."
    if dotnet build --configuration Debug --no-restore --verbosity minimal; then
        print_success "Build completed successfully"
    else
        print_error "Build failed"
        exit 1
    fi
    
    print_separator
    print_info "Running CLI with arguments: $*"
    print_info "Use Ctrl+C to stop the application"
    print_separator
    
    # Run the CLI with all passed arguments
    # Use DEBUG configuration to get more detailed logging
    export DOTNET_ENVIRONMENT=Development
    dotnet run --configuration Debug --no-build -- "$@"
    
    local exit_code=$?
    
    print_separator
    if [ $exit_code -eq 0 ]; then
        print_success "CLI execution completed successfully"
    else
        print_warning "CLI execution finished with exit code: $exit_code"
    fi
    
    return $exit_code
}

# Handle Ctrl+C gracefully
trap 'echo -e "\n${YELLOW}[INFO]${NC} Script interrupted by user"; exit 130' SIGINT

# Show usage if --help is passed
if [[ "$1" == "--help" || "$1" == "-h" ]]; then
    echo "Aevatar CLI Quick Debug Script"
    echo ""
    echo "Usage: $0 [cli-arguments...]"
    echo ""
    echo "This script builds and runs the Aevatar.Cli tool in debug mode without"
    echo "publishing it as a global tool."
    echo ""
    echo "Examples:"
    echo "  $0 --help                    # Show CLI help"
    echo "  $0 version                   # Show CLI version"
    echo "  $0 some-command --option     # Run CLI with arguments"
    echo ""
    echo "The script will:"
    echo "  1. Validate the environment and project structure"
    echo "  2. Build the CLI project in Debug configuration"
    echo "  3. Run the CLI with the provided arguments"
    echo ""
    exit 0
fi

# Execute main function with all arguments
main "$@"
