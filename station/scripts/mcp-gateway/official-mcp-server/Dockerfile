FROM node:18-alpine

WORKDIR /app

# Install common system dependencies
RUN apk add --no-cache \
    sqlite \
    git \
    curl \
    bash

# Create common directories
RUN mkdir -p /workspace /data

# Install available official MCP servers globally
# Note: Only install packages that actually exist in npm registry
RUN npm install -g \
    @modelcontextprotocol/server-filesystem

# Copy startup script
COPY start-mcp-server.sh /usr/local/bin/start-mcp-server.sh
RUN chmod +x /usr/local/bin/start-mcp-server.sh

# Default environment variables
ENV MCP_SERVER_TYPE=filesystem
ENV MCP_SERVER_PORT=3000
ENV WORKSPACE_PATH=/workspace
ENV DATABASE_PATH=/data/mcp.db
ENV NODE_ENV=production

# Expose default port
EXPOSE 3000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:3000/health || pgrep -f "mcp" || exit 1

# Start MCP server based on environment variable
CMD ["/usr/local/bin/start-mcp-server.sh"]
