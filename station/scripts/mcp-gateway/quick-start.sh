#!/bin/bash

# Microsoft MCP Gateway Quick Start Script
# Simplified version for immediate testing

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}⚡ MCP Gateway Quick Start${NC}"
echo -e "${BLUE}=========================${NC}"

# Check if main deployment script exists
if [ ! -f "./deploy-mcp-gateway.sh" ]; then
    echo -e "${RED}❌ deploy-mcp-gateway.sh not found in current directory${NC}"
    echo -e "${YELLOW}💡 Please run this script from the station/scripts/mcp-gateway directory${NC}"
    exit 1
fi

echo -e "${BLUE}🚀 Starting MCP Gateway deployment...${NC}"
echo ""

# Run the main deployment script
./deploy-mcp-gateway.sh

# Check if deployment was successful
if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}🎉 Quick Start Completed Successfully!${NC}"
    echo ""
    echo -e "${BLUE}🔗 Next Steps:${NC}"
    echo -e "  1. Test the deployment: ${GREEN}./test-mcp-gateway.sh${NC}"
    echo -e "  2. Try the API: ${GREEN}curl http://localhost:8000/health${NC}"
    echo -e "  3. View adapters: ${GREEN}curl http://localhost:8000/adapters${NC}"
    echo -e "  4. Clean up when done: ${GREEN}./cleanup-mcp-gateway.sh${NC}"
    echo ""
    echo -e "${YELLOW}📚 For detailed usage, see: ${GREEN}README-LOCAL-DEPLOYMENT.md${NC}"
else
    echo ""
    echo -e "${RED}❌ Quick Start Failed!${NC}"
    echo -e "${YELLOW}💡 Check the error messages above and try again${NC}"
    echo -e "${YELLOW}💡 For troubleshooting, see: ${GREEN}README-LOCAL-DEPLOYMENT.md${NC}"
    exit 1
fi
