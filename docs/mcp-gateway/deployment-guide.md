# MCP Gateway Deployment Guide

## English Documentation

### Overview

This guide provides step-by-step instructions for deploying and configuring Microsoft MCP Gateway with Aevatar integration in both development and production environments.

### Prerequisites

- Kubernetes cluster (for production deployment)
- Docker and Docker Desktop (for local development)
- .NET 9 SDK
- Azure CLI (for Azure deployment)
- kubectl configured for your cluster

### Local Development Deployment

#### 1. **Setup Local Docker Registry**

```bash
# Start local Docker registry
docker run -d -p 5000:5000 --name registry registry:2.7

# Verify registry is running
curl http://localhost:5000/v2/_catalog
```

#### 2. **Build and Push MCP Server Images**

```bash
# Example: Build a filesystem MCP server
docker build -f mcp-servers/filesystem/Dockerfile mcp-servers/filesystem -t localhost:5000/mcp-filesystem:1.0.0
docker push localhost:5000/mcp-filesystem:1.0.0

# Build additional servers as needed
docker build -f mcp-servers/sqlite/Dockerfile mcp-servers/sqlite -t localhost:5000/mcp-sqlite:1.0.0
docker push localhost:5000/mcp-sqlite:1.0.0
```

#### 3. **Deploy MCP Gateway to Local Kubernetes**

```bash
# Clone Microsoft MCP Gateway repository
git clone https://github.com/microsoft/mcp-gateway.git
cd mcp-gateway

# Deploy to local Kubernetes
kubectl apply -f deployment/k8s/local-deployment.yml

# Verify deployment
kubectl get pods -n adapter
kubectl get services -n adapter
```

#### 4. **Port Forward Gateway Service**

```bash
# Forward gateway service port
kubectl port-forward -n adapter svc/mcpgateway-service 8000:8000

# Verify gateway is accessible
curl http://localhost:8000/health
```

#### 5. **Configure Aevatar Integration**

Update your Aevatar configuration:

```json
{
  "MCPGateway": {
    "GatewayBaseUrl": "http://localhost:8000",
    "AuthToken": "dev-token-12345",
    "EnableDetailedLogging": true
  }
}
```

#### 6. **Create and Test Adapters**

```bash
# Create a filesystem adapter
curl -X POST http://localhost:8000/adapters \
  -H "Content-Type: application/json" \
  -d '{
    "name": "filesystem-adapter",
    "imageName": "mcp-filesystem",
    "imageVersion": "1.0.0",
    "description": "Filesystem MCP server"
  }'

# Verify adapter is running
curl http://localhost:8000/adapters/filesystem-adapter/status
```

### Production Deployment

#### 1. **Azure Deployment**

##### Setup Azure Environment

```bash
# Login to Azure
az login

# Create resource group
az group create --name mcp-gateway-rg --location eastus

# Deploy MCP Gateway infrastructure
az deployment group create \
  --resource-group mcp-gateway-rg \
  --template-file deployment/azure/main.bicep \
  --parameters resourceLabel=mcpgw clientId=your-entra-app-id
```

##### Configure Entra ID Authentication

1. Create App Registration in Azure Portal
2. Configure API permissions and scopes
3. Authorize Azure CLI and VS Code as client applications
4. Copy Client ID for deployment parameters

#### 2. **Build and Deploy to Azure Container Registry**

```bash
# Build MCP server images in ACR
az acr build -r "mgregmcpgw" \
  -f mcp-servers/filesystem/Dockerfile \
  mcp-servers/filesystem \
  -t "mgregmcpgw.azurecr.io/mcp-filesystem:1.0.0"

# Deploy additional servers
az acr build -r "mgregmcpgw" \
  -f mcp-servers/sqlite/Dockerfile \
  mcp-servers/sqlite \
  -t "mgregmcpgw.azurecr.io/mcp-sqlite:1.0.0"
```

#### 3. **Configure Production Aevatar**

```json
{
  "MCPGateway": {
    "GatewayBaseUrl": "https://mcpgw.eastus.cloudapp.azure.com",
    "AuthToken": "Bearer ${MCP_GATEWAY_TOKEN}",
    "RequestTimeout": "00:01:00",
    "EnableSessionAffinity": true,
    "MaxRetryAttempts": 5,
    "RetryDelay": "00:00:02",
    "DefaultHeaders": {
      "X-Environment": "production",
      "X-Client-Version": "1.0.0"
    }
  }
}
```

#### 4. **Production Testing**

```bash
# Get Azure access token
TOKEN=$(az account get-access-token --resource your-client-id --query accessToken -o tsv)

# Test adapter creation
curl -X POST https://mcpgw.eastus.cloudapp.azure.com/adapters \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "prod-filesystem",
    "imageName": "mcp-filesystem",
    "imageVersion": "1.0.0",
    "description": "Production filesystem server"
  }'

# Test MCP connection
curl https://mcpgw.eastus.cloudapp.azure.com/adapters/prod-filesystem/mcp \
  -H "Authorization: Bearer $TOKEN"
```

### Monitoring and Operations

#### **Health Monitoring**

```bash
# Check gateway health
curl http://localhost:8000/health

# Check adapter status
curl http://localhost:8000/adapters/my-adapter/status

# Get adapter metrics
curl http://localhost:8000/adapters/my-adapter/metrics
```

#### **Log Management**

```bash
# Get recent logs
curl http://localhost:8000/adapters/my-adapter/logs?lines=100

# Follow logs in real-time
curl http://localhost:8000/adapters/my-adapter/logs?follow=true
```

#### **Troubleshooting**

Common issues and solutions:

1. **Connection Timeout**
   ```json
   // Increase timeout in configuration
   "RequestTimeout": "00:02:00"
   ```

2. **Authentication Failures**
   ```bash
   # Verify token validity
   curl -H "Authorization: Bearer $TOKEN" http://localhost:8000/health
   ```

3. **Adapter Not Found**
   ```bash
   # List all adapters
   curl http://localhost:8000/adapters
   ```

### Scaling Considerations

#### **Horizontal Scaling**
- Configure multiple gateway instances behind a load balancer
- Use sticky sessions for session affinity
- Implement distributed caching for session data

#### **Vertical Scaling**
- Adjust resource limits for MCP server pods
- Monitor CPU and memory usage
- Configure auto-scaling based on connection metrics

---

## 中文文档

### 概述

本指南提供了在开发和生产环境中部署和配置Microsoft MCP Gateway与Aevatar集成的分步说明。

### 前置条件

- Kubernetes集群（用于生产部署）
- Docker和Docker Desktop（用于本地开发）
- .NET 9 SDK
- Azure CLI（用于Azure部署）
- 已配置的kubectl

### 本地开发部署

#### 1. **设置本地Docker注册表**

```bash
# 启动本地Docker注册表
docker run -d -p 5000:5000 --name registry registry:2.7

# 验证注册表正在运行
curl http://localhost:5000/v2/_catalog
```

#### 2. **构建和推送MCP服务器镜像**

```bash
# 示例：构建文件系统MCP服务器
docker build -f mcp-servers/filesystem/Dockerfile mcp-servers/filesystem -t localhost:5000/mcp-filesystem:1.0.0
docker push localhost:5000/mcp-filesystem:1.0.0

# 根据需要构建其他服务器
docker build -f mcp-servers/sqlite/Dockerfile mcp-servers/sqlite -t localhost:5000/mcp-sqlite:1.0.0
docker push localhost:5000/mcp-sqlite:1.0.0
```

#### 3. **部署MCP Gateway到本地Kubernetes**

```bash
# 克隆Microsoft MCP Gateway仓库
git clone https://github.com/microsoft/mcp-gateway.git
cd mcp-gateway

# 部署到本地Kubernetes
kubectl apply -f deployment/k8s/local-deployment.yml

# 验证部署
kubectl get pods -n adapter
kubectl get services -n adapter
```

#### 4. **配置Aevatar集成**

更新您的Aevatar配置：

```json
{
  "MCPGateway": {
    "GatewayBaseUrl": "http://localhost:8000",
    "AuthToken": "dev-token-12345",
    "EnableDetailedLogging": true
  }
}
```

#### 5. **创建和测试适配器**

```bash
# 创建文件系统适配器
curl -X POST http://localhost:8000/adapters \
  -H "Content-Type: application/json" \
  -d '{
    "name": "filesystem-adapter",
    "imageName": "mcp-filesystem",
    "imageVersion": "1.0.0",
    "description": "文件系统MCP服务器"
  }'

# 验证适配器正在运行
curl http://localhost:8000/adapters/filesystem-adapter/status
```

### 生产部署

#### 1. **Azure部署**

##### 设置Azure环境

```bash
# 登录Azure
az login

# 创建资源组
az group create --name mcp-gateway-rg --location eastus

# 部署MCP Gateway基础设施
az deployment group create \
  --resource-group mcp-gateway-rg \
  --template-file deployment/azure/main.bicep \
  --parameters resourceLabel=mcpgw clientId=your-entra-app-id
```

#### 2. **生产配置**

```json
{
  "MCPGateway": {
    "GatewayBaseUrl": "https://mcpgw.eastus.cloudapp.azure.com",
    "AuthToken": "Bearer ${MCP_GATEWAY_TOKEN}",
    "RequestTimeout": "00:01:00",
    "EnableSessionAffinity": true,
    "MaxRetryAttempts": 5,
    "RetryDelay": "00:00:02",
    "DefaultHeaders": {
      "X-Environment": "production",
      "X-Client-Version": "1.0.0"
    }
  }
}
```

### 监控和运维

#### **健康监控**

```bash
# 检查网关健康状态
curl http://localhost:8000/health

# 检查适配器状态
curl http://localhost:8000/adapters/my-adapter/status

# 获取适配器指标
curl http://localhost:8000/adapters/my-adapter/metrics
```

#### **日志管理**

```bash
# 获取最近的日志
curl http://localhost:8000/adapters/my-adapter/logs?lines=100

# 实时跟踪日志
curl http://localhost:8000/adapters/my-adapter/logs?follow=true
```

#### **故障排除**

常见问题和解决方案：

1. **连接超时**
   ```json
   // 在配置中增加超时时间
   "RequestTimeout": "00:02:00"
   ```

2. **身份验证失败**
   ```bash
   # 验证令牌有效性
   curl -H "Authorization: Bearer $TOKEN" http://localhost:8000/health
   ```

3. **适配器未找到**
   ```bash
   # 列出所有适配器
   curl http://localhost:8000/adapters
   ```

### 扩展考虑

#### **水平扩展**
- 在负载均衡器后配置多个网关实例
- 使用粘性会话实现会话亲和性
- 为会话数据实施分布式缓存

#### **垂直扩展**
- 调整MCP服务器pod的资源限制
- 监控CPU和内存使用情况
- 基于连接指标配置自动扩展
