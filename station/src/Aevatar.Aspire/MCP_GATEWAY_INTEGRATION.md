# MCP Gateway Integration with Aevatar.Aspire

## Overview

This integration adds Microsoft MCP Gateway to the Aevatar.Aspire orchestration platform, enabling dynamic MCP server management while preserving the full API capabilities of the official MCP Gateway.

## Architecture

```
Aspire Orchestration Layer:
├── MCP Gateway Container (Port 7004) ← Official Microsoft MCP Gateway
├── Example MCP Servers (Port 3001+) ← Development MCP servers  
├── Aevatar Services (Port 7001-7003) ← Enhanced with MCP Gateway integration
└── Infrastructure (MongoDB, Redis, etc.) ← Shared infrastructure

Runtime Management Layer:
├── MCP Gateway REST API ← Full dynamic management capabilities
├── Aevatar MCP API ← Integrated with Aevatar permissions
└── Direct Docker/K8s Management ← Container orchestration
```

## Quick Start

### 1. Start the Platform

```bash
cd station/src/Aevatar.Aspire
dotnet run
```

This will start:
- ✅ **MCP Gateway** on http://localhost:7004
- ✅ **MCP Gateway API Docs** on http://localhost:7004/swagger  
- ✅ **Aevatar HttpApi.Host** on http://localhost:7002 (with MCP Gateway integration)
- ✅ **Aevatar Developer.Host** on http://localhost:7003 (with MCP Gateway integration)
- ✅ **Example MCP Server** on http://localhost:3001 (filesystem-dev)
- ✅ **Aspire Dashboard** on http://localhost:15000

### 2. Test Dynamic MCP Server Management

After startup (wait ~35 seconds), test the API capabilities:

```bash
# Create a new MCP server dynamically
curl -X POST http://localhost:7004/adapters \
  -H "Content-Type: application/json" \
  -d '{
    "name": "my-custom-server",
    "imageName": "mcp-filesystem",
    "imageVersion": "1.0.0",
    "description": "My dynamically created MCP server"
  }'

# Check server status
curl http://localhost:7004/adapters/my-custom-server/status

# Get server logs
curl http://localhost:7004/adapters/my-custom-server/logs

# Scale server resources
curl -X PUT http://localhost:7004/adapters/my-custom-server \
  -H "Content-Type: application/json" \
  -d '{
    "resourceLimits": {
      "cpuLimit": 2.0,
      "memoryLimitMB": 1024
    }
  }'

# Delete server when done
curl -X DELETE http://localhost:7004/adapters/my-custom-server
```

### 3. Test Aevatar Integration

```bash
# Test through Aevatar API (with permission control)
curl http://localhost:7002/api/mcp-gateway/gateway/health

# Test through Aevatar Developer API
curl http://localhost:7003/api/mcp-gateway/adapters
```

## Key Benefits

### ✅ **Preserved API Capabilities**
- **Full REST API**: All MCP Gateway APIs remain functional
- **Dynamic Management**: Create/delete/update MCP servers at runtime
- **Session Routing**: Session-aware load balancing
- **Real-time Monitoring**: Live metrics and logs

### ✅ **Enhanced Developer Experience**  
- **One-Command Startup**: `dotnet run` starts entire ecosystem
- **Unified Dashboard**: Aspire Dashboard shows all services
- **Integrated Logging**: Centralized log viewing
- **Service Discovery**: Automatic service-to-service communication

### ✅ **Production Ready**
- **Scalable Architecture**: Kubernetes-ready deployment
- **Enterprise Integration**: Aevatar permissions and monitoring
- **Flexible Deployment**: Container or project-based deployment

## Configuration

### Environment Variables (Automatic)

The integration automatically configures:

```bash
# MCP Gateway configuration
MCPGateway__EnableDynamicManagement=true
MCPGateway__EnableAPIAccess=true  
MCPGateway__ContainerRuntime=Docker
MCPGateway__AuthToken=Bearer aspire-dev-token-12345

# Aevatar services configuration
MCPGateway__GatewayBaseUrl=http://localhost:7004
MCPGateway__RequestTimeout=00:01:00
MCPGateway__EnableSessionAffinity=true
```

### Custom Configuration

Modify `appsettings.json` to customize:

```json
{
  "MCPGatewayConfig": {
    "port": 7004,
    "enableDynamicManagement": true,
    "containerRuntime": "Docker",
    "imageRegistry": "your-registry.com"
  }
}
```

## Troubleshooting

### Common Issues

1. **MCP Gateway not starting**
   - Check Docker is running
   - Verify port 7004 is available
   - Check logs in Aspire Dashboard

2. **API calls failing**
   - Ensure MCP Gateway is fully started (wait 35+ seconds)
   - Check authentication token
   - Verify network connectivity

3. **MCP servers not deploying**
   - Check Docker daemon is running
   - Verify MCP server images are available
   - Check resource limits

### Debug Commands

```bash
# Check MCP Gateway logs
curl http://localhost:7004/health

# Check Aspire Dashboard
open http://localhost:15000

# Test connectivity
curl -v http://localhost:7004/adapters
```

## Conclusion

This integration provides the **best of both worlds**:

- 🚀 **Aspire**: Simplified development orchestration
- 🔌 **MCP Gateway API**: Full dynamic management capabilities  
- 🎯 **Aevatar Integration**: Enterprise permissions and monitoring

**No functionality is lost** - all MCP Gateway API capabilities remain fully functional while gaining the benefits of Aspire orchestration!
