# Hybrid Architecture: Aspire + MCP Gateway API Integration

## Overview

This example demonstrates how Aspire orchestration and MCP Gateway API management work together seamlessly, each handling their respective responsibilities without conflict.

## Architecture Layers

### Layer 1: Aspire Orchestration (Static Infrastructure)

**Responsibility**: Development environment setup and infrastructure management

```csharp
// Aevatar.Aspire/Program.cs - Infrastructure and Platform Services
public static async Task<int> Main(string[] args)
{
    var builder = DistributedApplication.CreateBuilder(args);

    // === INFRASTRUCTURE SERVICES (Aspire Managed) ===
    var mongodb = builder.AddMongoDB("mongodb", 27018);
    var redis = builder.AddRedis("redis", 6380);
    var elasticsearch = builder.AddElasticsearch("elasticsearch", 9201);

    // === PLATFORM SERVICES (Aspire Managed) ===
    var authServer = builder.AddProject("authserver", "../Aevatar.AuthServer/Aevatar.AuthServer.csproj")
        .WithHttpEndpoint(port: 7001, name: "authserver-http");

    var httpApiHost = builder.AddProject("httpapi", "../Aevatar.HttpApi.Host/Aevatar.HttpApi.Host.csproj")
        .WithHttpEndpoint(port: 7002, name: "httpapi-http");

    // === MCP GATEWAY SERVICE (Aspire Managed) ===
    var mcpGateway = builder.AddProject("mcp-gateway", "../Aevatar.MCPGateway/Aevatar.MCPGateway.csproj")
        .WithReference(mongodb)
        .WithReference(redis)
        .WaitFor(mongodb)
        .WaitFor(redis)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("MongoDB__ConnectionString", "{mongodb.connectionString}")
        .WithEnvironment("Redis__Configuration", "{redis.connectionString}")
        // Enable API management capabilities
        .WithEnvironment("MCPGateway__EnableDynamicManagement", "true")
        .WithEnvironment("MCPGateway__EnableKubernetesIntegration", "true")
        .WithEnvironment("MCPGateway__DefaultNamespace", "mcp-servers")
        .WithHttpEndpoint(port: 7004, name: "mcp-gateway-http");

    // === OPTIONAL: PRE-CONFIGURED MCP SERVERS (Aspire Managed) ===
    // These are development-time servers, always available
    var mcpFilesystemDev = builder.AddContainer("mcp-filesystem-dev", "localhost:5000/mcp-filesystem:1.0.0")
        .WithHttpEndpoint(port: 3001, name: "filesystem-dev-mcp")
        .WithEnvironment("MCP_SERVER_NAME", "filesystem-dev")
        .WithLabel("mcp.gateway.auto-register", "true")
        .WithLabel("mcp.gateway.adapter-name", "filesystem-dev");

    var app = builder.Build();
    
    Console.WriteLine("🚀 Aevatar Platform with MCP Gateway Started!");
    Console.WriteLine("📊 Aspire Dashboard: http://localhost:15000");
    Console.WriteLine("🔌 MCP Gateway API: http://localhost:7004");
    Console.WriteLine("📚 API Documentation: http://localhost:7004/swagger");
    
    app.Run();
    return 0;
}
```

### Layer 2: MCP Gateway API Management (Dynamic Runtime)

**Responsibility**: Runtime MCP server lifecycle management

```bash
# === DEVELOPMENT WORKFLOW ===

# 1. Start platform with Aspire
cd station/src/Aevatar.Aspire
dotnet run

# 2. Platform is ready, now use MCP Gateway APIs for dynamic management

# Create a production filesystem adapter
curl -X POST http://localhost:7004/adapters \
  -H "Content-Type: application/json" \
  -d '{
    "name": "prod-filesystem",
    "imageName": "mcp-filesystem",
    "imageVersion": "2.1.0",
    "description": "Production filesystem server with enhanced security",
    "environment": {
      "WORKSPACE_PATH": "/secure-workspace",
      "SECURITY_LEVEL": "high",
      "LOG_LEVEL": "WARN"
    },
    "resourceLimits": {
      "cpuLimit": 2.0,
      "memoryLimitMB": 1024,
      "maxConnections": 50
    },
    "tags": ["production", "filesystem", "secure"]
  }'

# Create a custom Python tools adapter
curl -X POST http://localhost:7004/adapters \
  -H "Content-Type: application/json" \
  -d '{
    "name": "python-analytics",
    "imageName": "custom/mcp-python-analytics",
    "imageVersion": "3.0.0",
    "description": "Custom Python analytics tools",
    "environment": {
      "PYTHON_VERSION": "3.11",
      "ANALYTICS_MODE": "advanced",
      "DATA_SOURCE": "elasticsearch"
    }
  }'

# Scale up for high load
curl -X PUT http://localhost:7004/adapters/prod-filesystem \
  -H "Content-Type: application/json" \
  -d '{
    "resourceLimits": {
      "cpuLimit": 4.0,
      "memoryLimitMB": 2048,
      "maxConnections": 100
    }
  }'

# Monitor real-time metrics
curl http://localhost:7004/adapters/prod-filesystem/metrics

# Get live logs
curl http://localhost:7004/adapters/prod-filesystem/logs?follow=true

# Clean up when done
curl -X DELETE http://localhost:7004/adapters/python-analytics
```

## Key Benefits of Hybrid Approach

### ✅ **Best of Both Worlds**

#### **Aspire Provides** (Development Experience)
- 🎯 **One-Command Startup**: `dotnet run` starts entire ecosystem
- 📊 **Unified Dashboard**: All services in one view
- 🔧 **Consistent Configuration**: Environment variables and service discovery
- 🚀 **Fast Development Cycles**: Quick restart and debugging

#### **MCP Gateway API Provides** (Runtime Flexibility)
- 🌐 **Dynamic Server Management**: Create/delete servers on demand
- 📈 **Elastic Scaling**: Scale servers based on load
- 🔄 **Rolling Updates**: Update server versions without downtime
- 🎛️ **Runtime Configuration**: Modify server settings dynamically

### 🎨 **Practical Usage Scenarios**

#### **Scenario 1: Development Environment**
```bash
# Developer starts working
cd station/src/Aevatar.Aspire
dotnet run

# Platform ready with:
# - MCP Gateway running on port 7004
# - Basic filesystem MCP server (dev) on port 3001
# - All Aevatar services ready

# Developer needs a custom MCP server for testing
curl -X POST http://localhost:7004/adapters \
  -d '{"name": "test-server", "imageName": "my-custom-mcp", "imageVersion": "latest"}'

# Test complete, clean up
curl -X DELETE http://localhost:7004/adapters/test-server
```

#### **Scenario 2: Production Deployment**
```bash
# Production environment (Aspire deployed via Docker/K8s)
# MCP Gateway running in production cluster

# Business needs new MCP capability
curl -X POST https://prod-gateway.company.com/adapters \
  -H "Authorization: Bearer $PROD_TOKEN" \
  -d '{
    "name": "customer-analytics",
    "imageName": "company/mcp-analytics",
    "imageVersion": "1.2.0",
    "resourceLimits": {"cpuLimit": 4.0, "memoryLimitMB": 8192}
  }'

# Monitor and scale as needed
curl https://prod-gateway.company.com/adapters/customer-analytics/metrics
```

## Enhanced Integration Configuration

### Updated Aspire Configuration

```csharp
// Enhanced Program.cs that preserves API capabilities
var mcpGateway = builder.AddProject("mcp-gateway", "../Aevatar.MCPGateway/Aevatar.MCPGateway.csproj")
    .WithReference(mongodb)
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    
    // === ENABLE DYNAMIC API MANAGEMENT ===
    .WithEnvironment("MCPGateway__EnableDynamicManagement", "true")
    .WithEnvironment("MCPGateway__EnableAPIAccess", "true")
    .WithEnvironment("MCPGateway__EnableSwagger", "true")
    
    // === KUBERNETES/DOCKER INTEGRATION ===
    .WithEnvironment("MCPGateway__ContainerRuntime", "Docker") // or "Kubernetes"
    .WithEnvironment("MCPGateway__DefaultNamespace", "mcp-servers")
    .WithEnvironment("MCPGateway__ImageRegistry", "localhost:5000")
    
    // === SESSION AND ROUTING ===
    .WithEnvironment("MCPGateway__EnableSessionAffinity", "true")
    .WithEnvironment("MCPGateway__SessionStore", "Redis")
    
    .WithHttpEndpoint(port: 7004, name: "mcp-gateway-http");
```

### Enhanced appsettings.json

```json
{
  "MCPGateway": {
    "EnableDynamicManagement": true,
    "EnableAPIAccess": true,
    "ContainerRuntime": "Docker",
    "DefaultNamespace": "mcp-servers",
    "ImageRegistry": "localhost:5000",
    "API": {
      "EnableSwagger": true,
      "EnableCORS": true,
      "RateLimiting": {
        "RequestsPerMinute": 100,
        "BurstSize": 20
      }
    },
    "DynamicServers": {
      "EnableAutoCleanup": true,
      "DefaultResourceLimits": {
        "cpuLimit": 1.0,
        "memoryLimitMB": 512,
        "maxConnections": 50
      },
      "AllowedImages": [
        "localhost:5000/mcp-*",
        "mcr.microsoft.com/mcp-*",
        "company-registry/mcp-*"
      ]
    }
  }
}
```

## Comparison: Static vs Dynamic Management

| Aspect | Aspire (Static) | MCP Gateway API (Dynamic) |
|--------|-----------------|---------------------------|
| **Use Case** | Development setup | Runtime management |
| **Server Lifecycle** | Compile-time defined | Runtime API-driven |
| **Configuration** | appsettings.json | REST API calls |
| **Scaling** | Manual restart | Live scaling |
| **Updates** | Code deployment | API-based rolling updates |
| **Monitoring** | Aspire Dashboard | MCP Gateway metrics |

## Recommended Workflow

### Development Phase
1. **Use Aspire** for infrastructure and basic MCP servers
2. **Use MCP Gateway API** for testing custom servers
3. **Aspire Dashboard** for unified monitoring

### Production Phase  
1. **Deploy via Aspire** to Kubernetes/Docker
2. **Use MCP Gateway API** for all server management
3. **Production monitoring** via both Aspire and MCP Gateway

## Conclusion

**Aspire enhances rather than limits MCP Gateway's capabilities!**

- ✅ **Aspire**: Perfect for development orchestration and infrastructure
- ✅ **MCP Gateway API**: Perfect for runtime server management
- ✅ **Combined**: Best developer experience + full production flexibility

The integration provides a **progressive enhancement** where developers get the convenience of Aspire orchestration while retaining the full power of MCP Gateway's dynamic API management.
