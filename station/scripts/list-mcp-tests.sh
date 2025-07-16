#!/bin/bash
# list-mcp-tests.sh - List available MCP test scripts

# Color definitions
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== Available MCP Test Scripts ===${NC}\n"

echo -e "${CYAN}Authentication:${NC}"
echo -e "  ${BLUE}./get-auth-token.sh${NC} - Get authentication token only"
echo ""

echo -e "${CYAN}MCP Server Tests:${NC}"
echo -e "  ${BLUE}./test-memory-server-mcp.sh${NC} - Test memory-server (knowledge graph)"
echo -e "  ${BLUE}./test-context7-mcp.sh${NC} - Test context7 (library documentation)"
echo ""

echo -e "${CYAN}Setup:${NC}"
echo -e "  1. ${YELLOW}source ./set-env.sh${NC} - Load environment variables"
echo -e "  2. Run any test script above"
echo ""

echo -e "${GREEN}Documentation:${NC}"
echo -e "  ${BLUE}MCP-TEST-SCRIPTS-README.md${NC} - Detailed documentation"
echo -e "  ${BLUE}MEMORY-SERVER-KG-README.md${NC} - Memory server guide"
echo -e "  ${BLUE}CONTEXT7-README.md${NC} - Context7 documentation guide"
echo ""

# Check if environment is set
if [ -z "$AEVATAR_USERNAME" ]; then
    echo -e "${YELLOW}⚠ Environment not loaded. Run: source ./set-env.sh${NC}"
else
    echo -e "${GREEN}✓ Environment loaded for user: $AEVATAR_USERNAME${NC}"
fi 