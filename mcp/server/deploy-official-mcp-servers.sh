#!/bin/bash

# Deploy Official MCP Servers Script
# This script deploys official MCP server Docker images using the MCP Gateway API

set -e

# Default configuration
DEFAULT_GATEWAY_URL="http://localhost:8000"
REGISTRY="docker.io"  # Docker Hub registry

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to show usage information
show_usage() {
    echo "Usage: $0 [OPTIONS] [GATEWAY_URL]"
    echo ""
    echo "Deploy official MCP servers to the specified MCP Gateway."
    echo ""
    echo "OPTIONS:"
    echo "  -h, --help              Show this help message"
    echo "  -u, --gateway-url URL   MCP Gateway URL (default: $DEFAULT_GATEWAY_URL)"
    echo ""
    echo "ARGUMENTS:"
    echo "  GATEWAY_URL             MCP Gateway URL (alternative to --gateway-url)"
    echo ""
    echo "ENVIRONMENT VARIABLES:"
    echo "  GATEWAY_URL             MCP Gateway URL (lowest priority)"
    echo ""
    echo "EXAMPLES:"
    echo "  $0                                          # Use default URL"
    echo "  $0 http://my-gateway:8000                   # Use positional argument"
    echo "  $0 --gateway-url http://my-gateway:8000     # Use named option"
    echo "  GATEWAY_URL=http://my-gateway:8000 $0       # Use environment variable"
    echo ""
}

# Function to validate URL format
validate_url() {
    local url=$1
    if [[ ! $url =~ ^https?:// ]]; then
        print_error "Invalid URL format: $url"
        print_error "URL must start with http:// or https://"
        return 1
    fi
    return 0
}

# Function to parse command line arguments
parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -h|--help)
                show_usage
                exit 0
                ;;
            -u|--gateway-url)
                if [[ -n $2 && $2 != -* ]]; then
                    GATEWAY_URL="$2"
                    shift 2
                else
                    print_error "Option $1 requires a URL argument"
                    show_usage
                    exit 1
                fi
                ;;
            -*)
                print_error "Unknown option: $1"
                show_usage
                exit 1
                ;;
            *)
                # Positional argument - assume it's the gateway URL
                if [[ -z $GATEWAY_URL ]]; then
                    GATEWAY_URL="$1"
                else
                    print_error "Multiple gateway URLs provided"
                    show_usage
                    exit 1
                fi
                shift
                ;;
        esac
    done
}

# Function to determine gateway URL with priority order
determine_gateway_url() {
    # Priority: Command line argument > Environment variable > Default
    if [[ -z $GATEWAY_URL ]]; then
        GATEWAY_URL="${GATEWAY_URL:-$DEFAULT_GATEWAY_URL}"
    fi

    # Validate URL format
    if ! validate_url "$GATEWAY_URL"; then
        exit 1
    fi

    print_status "Using MCP Gateway URL: $GATEWAY_URL"
}

# Function to deploy an MCP server
deploy_mcp_server() {
    local name=$1
    local image_name=$2
    local image_version=${3:-latest}
    local description=$4

    print_status "Deploying $name MCP server..."

    curl -X POST "$GATEWAY_URL/adapters" \
        -H "Content-Type: application/json" \
        -d "{
            \"name\": \"$name\",
            \"imageName\": \"$image_name\",
            \"imageVersion\": \"$image_version\",
            \"description\": \"$description\"
        }" \
        --fail --silent --show-error || {
        print_error "Failed to deploy $name"
        return 1
    }

    print_status "$name deployed successfully"
}

# Function to check if MCP Gateway is running
check_gateway() {
    print_status "Checking MCP Gateway availability..."

    if ! curl -f -s "$GATEWAY_URL/adapters" > /dev/null; then
        print_error "MCP Gateway is not accessible at $GATEWAY_URL"
        print_error "Please ensure the gateway is running and accessible"
        exit 1
    fi

    print_status "MCP Gateway is accessible"
}

# Main deployment function
main() {
    echo "==================================="
    echo "Official MCP Servers Deployment"
    echo "==================================="

    # Parse command line arguments
    parse_arguments "$@"

    # Determine the gateway URL to use
    determine_gateway_url

    check_gateway

    # Deploy official MCP servers
    print_status "Starting deployment of official MCP servers..."

    # Time Server
    deploy_mcp_server "time" "mcp/time" "latest" "Official MCP Time Server - provides time-related tools and utilities"

    # Fetch Server
    deploy_mcp_server "fetch" "mcp/fetch" "latest" "Official MCP Fetch Server - provides HTTP request and web scraping capabilities"

    # Filesystem Server
    deploy_mcp_server "filesystem" "mcp/filesystem" "latest" "Official MCP Filesystem Server - provides file system operations"

    # Git Server
    deploy_mcp_server "git" "mcp/git" "latest" "Official MCP Git Server - provides git repository operations"

    # Memory Server
    deploy_mcp_server "memory" "mcp/memory" "latest" "Official MCP Memory Server - knowledge graph memory server for persistent information storage"

    # Sequential Thinking Server
    deploy_mcp_server "sequentialthinking" "mcp/sequentialthinking" "latest" "Official MCP Sequential Thinking Server - provides structured thinking capabilities"

    echo ""
    print_status "All MCP servers deployed successfully!"

    echo ""
    echo "==================================="
    echo "Access Information"
    echo "==================================="
    echo "You can now access these MCP servers through the gateway:"
    echo ""
    echo "• Time Server:             $GATEWAY_URL/adapters/time/mcp"
    echo "• Fetch Server:            $GATEWAY_URL/adapters/fetch/mcp"
    echo "• Filesystem Server:       $GATEWAY_URL/adapters/filesystem/mcp"
    echo "• Git Server:              $GATEWAY_URL/adapters/git/mcp"
    echo "• Memory Server:           $GATEWAY_URL/adapters/memory/mcp"
    echo "• Sequential Thinking:     $GATEWAY_URL/adapters/sequentialthinking/mcp"
    echo ""
    echo "Example VS Code mcp.json configuration:"
    echo '{'
    echo '  "servers": {'
    echo '    "time": {'
    echo "      \"url\": \"$GATEWAY_URL/adapters/time/mcp\""
    echo '    },'
    echo '    "fetch": {'
    echo "      \"url\": \"$GATEWAY_URL/adapters/fetch/mcp\""
    echo '    },'
    echo '    "filesystem": {'
    echo "      \"url\": \"$GATEWAY_URL/adapters/filesystem/mcp\""
    echo '    },'
    echo '    "git": {'
    echo "      \"url\": \"$GATEWAY_URL/adapters/git/mcp\""
    echo '    },'
    echo '    "memory": {'
    echo "      \"url\": \"$GATEWAY_URL/adapters/memory/mcp\""
    echo '    },'
    echo '    "sequentialthinking": {'
    echo "      \"url\": \"$GATEWAY_URL/adapters/sequentialthinking/mcp\""
    echo '    }'
    echo '  }'
    echo '}'
}

# Run the main function with all arguments
main "$@"