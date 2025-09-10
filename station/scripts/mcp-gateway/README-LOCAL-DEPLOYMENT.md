# Microsoft MCP Gateway 本地部署指南

## 概述

本指南提供了在本地环境部署 [Microsoft MCP Gateway](https://microsoft.github.io/mcp-gateway/) 的完整解决方案，包括自动化脚本和详细说明。

## 🎯 **解决的问题**

- ✅ **端口冲突处理**: 自动检测并避开5000端口冲突
- ✅ **完整自动化**: 一键部署整个MCP Gateway环境  
- ✅ **环境检查**: 自动验证所有必需的依赖项
- ✅ **错误处理**: 提供详细的错误信息和解决方案
- ✅ **资源清理**: 完整的清理脚本，避免资源残留

## 📋 **前置要求**

### 必需软件
- **Docker Desktop** (已启动，并启用Kubernetes)
- **.NET 8 SDK** 或更高版本
- **kubectl** (Kubernetes命令行工具)
- **Git** (用于克隆仓库)
- **curl** (用于API测试)

### 系统要求
- **macOS/Linux** (Windows需要WSL2)
- **可用内存**: 至少4GB
- **磁盘空间**: 至少5GB

## 🚀 **快速开始**

### 1. 下载脚本

```bash
# 进入脚本目录
cd station/scripts/mcp-gateway

# 给脚本执行权限
chmod +x *.sh
```

### 2. 一键部署

```bash
# 运行部署脚本
./deploy-mcp-gateway.sh
```

脚本将自动执行以下步骤：
- ✅ 检查环境依赖
- ✅ 启动本地Docker Registry (端口5001或其他可用端口)
- ✅ 克隆Microsoft MCP Gateway仓库
- ✅ 构建MCP示例服务器镜像
- ✅ 构建MCP Gateway服务镜像
- ✅ 部署到Kubernetes
- ✅ 设置端口转发
- ✅ 创建测试适配器
- ✅ 显示访问信息

### 3. 验证部署

```bash
# 运行测试脚本
./test-mcp-gateway.sh
```

### 4. 清理环境

```bash
# 清理所有资源
./cleanup-mcp-gateway.sh
```

## 📊 **部署后的访问信息**

部署成功后，你将看到类似以下的访问信息：

```
🎉 MCP Gateway Deployment Completed Successfully!
=================================================

📋 Access Information:
  🌐 MCP Gateway API: http://localhost:8000
  📚 API Documentation: http://localhost:8000/swagger
  🔍 Health Check: http://localhost:8000/health
  🐳 Local Registry: http://localhost:5001

📋 Test MCP Server:
  🔗 Streamable HTTP: http://localhost:8000/adapters/mcp-example/mcp
  🔗 SSE Endpoint: http://localhost:8000/adapters/mcp-example/sse
```

## 🔌 **API使用示例**

### 管理API

```bash
# 健康检查
curl http://localhost:8000/health

# 列出所有适配器
curl http://localhost:8000/adapters

# 获取特定适配器信息
curl http://localhost:8000/adapters/mcp-example

# 获取适配器状态
curl http://localhost:8000/adapters/mcp-example/status

# 获取适配器日志
curl http://localhost:8000/adapters/mcp-example/logs

# 创建新适配器
curl -X POST http://localhost:8000/adapters \
  -H "Content-Type: application/json" \
  -d '{
    "name": "my-adapter",
    "imageName": "mcp-example",
    "imageVersion": "1.0.0",
    "description": "My custom MCP server"
  }'

# 删除适配器
curl -X DELETE http://localhost:8000/adapters/my-adapter
```

### MCP连接端点

```bash
# Streamable HTTP连接
curl -X POST http://localhost:8000/adapters/mcp-example/mcp \
  -H "Content-Type: application/json" \
  -d '{}'

# SSE连接
curl http://localhost:8000/adapters/mcp-example/sse
```

## 🧪 **VS Code集成**

创建 `.vscode/mcp.json` 文件来在VS Code中使用MCP服务器：

```json
{
  "servers": {
    "mcp-example": {
      "url": "http://localhost:8000/adapters/mcp-example/mcp"
    },
    "my-custom-server": {
      "url": "http://localhost:8000/adapters/my-custom-server/mcp"
    }
  }
}
```

## 🔧 **故障排除**

### 常见问题

#### 1. 端口冲突
```bash
# 问题：端口5000已被占用
# 解决：脚本会自动检测并使用其他端口（如5001, 5002等）
```

#### 2. Docker未启动
```bash
# 问题：Docker is not running
# 解决：启动Docker Desktop并确保Kubernetes已启用
```

#### 3. .NET版本不匹配
```bash
# 问题：.NET 8 or higher is required
# 解决：安装.NET 8 SDK
brew install dotnet@8  # macOS
```

#### 4. Kubernetes未就绪
```bash
# 问题：Kubernetes cluster is not accessible
# 解决：在Docker Desktop中启用Kubernetes
```

### 调试命令

```bash
# 检查Kubernetes资源
kubectl get all -n adapter

# 查看MCP Gateway日志
kubectl logs -n adapter deployment/mcpgateway-service

# 检查端口转发进程
ps aux | grep 'kubectl port-forward'

# 测试连接
curl -v http://localhost:8000/health

# 检查本地镜像仓库
curl http://localhost:5001/v2/_catalog
```

## 📁 **文件结构**

部署后会创建以下文件：

```
$HOME/mcp-gateway-local/
├── config.env                    # 配置信息
├── port-forward.pid              # 端口转发进程ID
├── deployment/                   # Kubernetes部署文件
├── dotnet/                      # .NET源码
├── mcp-example-server/          # 示例MCP服务器
└── ...                         # Microsoft MCP Gateway源码
```

## 🔄 **高级用法**

### 自定义MCP服务器

1. **创建Dockerfile**:
```dockerfile
FROM node:18-alpine
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
EXPOSE 3000
CMD ["npm", "start"]
```

2. **构建并推送镜像**:
```bash
# 假设registry端口是5001
docker build -t localhost:5001/my-mcp-server:1.0.0 .
docker push localhost:5001/my-mcp-server:1.0.0
```

3. **通过API部署**:
```bash
curl -X POST http://localhost:8000/adapters \
  -H "Content-Type: application/json" \
  -d '{
    "name": "my-mcp-server",
    "imageName": "my-mcp-server",
    "imageVersion": "1.0.0",
    "description": "My custom MCP server"
  }'
```

### 配置管理

配置信息保存在 `$HOME/mcp-gateway-local/config.env`:

```bash
REGISTRY_PORT=5001
GATEWAY_PORT=8000
WORK_DIR=/Users/username/mcp-gateway-local
PORT_FORWARD_PID=12345
```

## 📈 **监控和观察性**

### Kubernetes监控

```bash
# 查看资源使用情况
kubectl top pods -n adapter

# 查看事件
kubectl get events -n adapter --sort-by=.metadata.creationTimestamp

# 描述服务
kubectl describe svc mcpgateway-service -n adapter
```

### 应用监控

```bash
# 健康检查
watch -n 5 'curl -s http://localhost:8000/health'

# 监控适配器状态
watch -n 10 'curl -s http://localhost:8000/adapters | jq .'
```

## 🚀 **生产环境迁移**

本地部署成功后，可以参考官方文档部署到生产环境：

1. **Azure部署**: 参考 [Deploy to Azure](https://microsoft.github.io/mcp-gateway/#getting-started---deploy-to-azure)
2. **生产配置**: 配置HTTPS、认证、监控等
3. **扩展性**: 根据负载调整资源配置

## 📚 **相关资源**

- [Microsoft MCP Gateway 官方文档](https://microsoft.github.io/mcp-gateway/)
- [Model Context Protocol 规范](https://modelcontextprotocol.io/)
- [VS Code MCP 集成指南](https://code.visualstudio.com/docs/copilot/mcp)
- [Kubernetes 文档](https://kubernetes.io/docs/)

## 🤝 **贡献和支持**

如果遇到问题或有改进建议，请：

1. 检查 [故障排除](#-故障排除) 部分
2. 查看官方 [GitHub Issues](https://github.com/microsoft/mcp-gateway/issues)
3. 提交新的Issue或Pull Request

## 📄 **许可证**

本脚本遵循MIT许可证。Microsoft MCP Gateway遵循其自身的许可证条款。
