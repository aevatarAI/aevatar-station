#!/bin/bash

# Universal MCP Server Startup Script
# Starts different official MCP servers based on environment variables

set -e

echo "🚀 Starting Official MCP Server"
echo "Server Type: $MCP_SERVER_TYPE"
echo "Port: $MCP_SERVER_PORT"
echo "================================"

# Initialize based on server type
case "$MCP_SERVER_TYPE" in
    "filesystem")
        echo "📁 Starting Filesystem MCP Server"
        echo "Workspace: $WORKSPACE_PATH"
        
        # Ensure workspace exists
        mkdir -p "$WORKSPACE_PATH"
        
        # Create sample files for testing
        echo "Hello from MCP Filesystem Server!" > "$WORKSPACE_PATH/welcome.txt"
        echo '{"name": "sample", "type": "json"}' > "$WORKSPACE_PATH/sample.json"
        mkdir -p "$WORKSPACE_PATH/docs"
        echo "# Sample Documentation" > "$WORKSPACE_PATH/docs/README.md"
        
        echo "Starting filesystem server on $WORKSPACE_PATH"
        exec npx @modelcontextprotocol/server-filesystem "$WORKSPACE_PATH"
        ;;
        
    "github")
        echo "🐙 Starting GitHub MCP Server (DEPRECATED)"
        echo "⚠️  Warning: This package is deprecated"
        
        if [ -z "$GITHUB_PERSONAL_ACCESS_TOKEN" ]; then
            echo "❌ ERROR: GITHUB_PERSONAL_ACCESS_TOKEN is required for GitHub server"
            echo "Please set the environment variable and restart"
            exit 1
        fi
        
        echo "Starting GitHub server (token configured)"
        exec npx @modelcontextprotocol/server-github
        ;;
        
    *)
        echo "❌ ERROR: Unknown or unsupported MCP_SERVER_TYPE: $MCP_SERVER_TYPE"
        echo ""
        echo "✅ Currently supported types:"
        echo "  • filesystem - File system operations (✅ Available)"
        echo "  • github - GitHub operations (⚠️  Deprecated)"
        echo ""
        echo "❌ Not available in npm registry:"
        echo "  • sqlite - Not found in registry"
        echo "  • git - Not found in registry"  
        echo "  • brave-search - Not found in registry"
        echo ""
        echo "💡 For other functionality, consider:"
        echo "  • Creating custom MCP servers"
        echo "  • Using alternative packages"
        echo "  • Implementing functionality in filesystem server"
        echo ""
        echo "Available installed packages:"
        npm list -g --depth=0 | grep @modelcontextprotocol || echo "No MCP servers installed"
        exit 1
        ;;
esac
