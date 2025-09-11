# Official MCP Servers Deployment Guide

## 概述

使用 `npx` 和官方 MCP 服务器是部署到 MCP Gateway 的最佳方案。这种方法具有以下优势：

- ✅ **零代码实现** - 使用官方维护的服务器
- ✅ **生产就绪** - 经过充分测试的实现
- ✅ **极简部署** - 只需要 Dockerfile
- ✅ **官方支持** - 持续更新和维护

## 🎯 **为什么使用官方MCP服务器？**

### 传统方式 vs 官方方式

| 方面 | 自定义实现 | 官方npx方式 |
|------|------------|-------------|
| **开发时间** | 几天到几周 | 几分钟 |
| **代码量** | 数百行 | ~10行Dockerfile |
| **维护负担** | 需要自己维护 | 官方维护 |
| **功能完整性** | 需要自己实现 | 功能完整 |
| **协议兼容性** | 可能有问题 | 100%兼容 |

### 官方可用的MCP服务器

- **@modelcontextprotocol/server-filesystem** - 文件系统操作
- **@modelcontextprotocol/server-sqlite** - SQLite数据库操作
- **@modelcontextprotocol/server-github** - GitHub仓库操作
- **@modelcontextprotocol/server-brave-search** - 网页搜索
- **@modelcontextprotocol/server-git** - Git仓库操作

## 🚀 **部署步骤**

### 1. 创建Dockerfile（仅此而已！）

#### **文件系统服务器**
```dockerfile
FROM node:18-alpine
WORKDIR /app
RUN mkdir -p /workspace
RUN npm install -g @modelcontextprotocol/server-filesystem
EXPOSE 3000
ENV WORKSPACE_PATH=/workspace
CMD ["npx", "@modelcontextprotocol/server-filesystem", "/workspace"]
```

#### **SQLite服务器**
```dockerfile
FROM node:18-alpine
WORKDIR /app
RUN apk add --no-cache sqlite
RUN mkdir -p /data
RUN npm install -g @modelcontextprotocol/server-sqlite
RUN sqlite3 /data/mcp.db "CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT);"
EXPOSE 3000
CMD ["npx", "@modelcontextprotocol/server-sqlite", "/data/mcp.db"]
```

### 2. 构建并推送镜像

```bash
# 构建
docker build -t localhost:5002/mcp-filesystem-official:1.0.0 .

# 推送到本地registry
docker push localhost:5002/mcp-filesystem-official:1.0.0
```

### 3. 通过MCP Gateway API部署

```bash
curl -X POST http://localhost:8000/adapters \\
  -H "Content-Type: application/json" \\
  -d '{
    "name": "filesystem-official",
    "imageName": "mcp-filesystem-official", 
    "imageVersion": "1.0.0",
    "description": "Official filesystem MCP server",
    "environment": {
      "WORKSPACE_PATH": "/workspace"
    }
  }'
```

### 4. 使用MCP服务器

```bash
# 初始化连接
curl -X POST http://localhost:8000/adapters/filesystem-official/mcp \\
  -H "Content-Type: application/json" \\
  -d '{"jsonrpc":"2.0","method":"initialize","id":"1"}'

# 列出可用工具
curl -X POST http://localhost:8000/adapters/filesystem-official/mcp \\
  -H "Content-Type: application/json" \\
  -d '{"jsonrpc":"2.0","method":"tools/list","id":"2"}'
```

## 🧪 **测试脚本**

我们提供了完整的测试脚本：

```bash
# 测试官方MCP服务器
./test-official-mcp-servers.sh

# 基本功能测试
./test-current-mcp-gateway.sh

# Aevatar集成测试
./quick-test-aevatar-api.sh
```

## 🎯 **VS Code集成**

创建 `.vscode/mcp.json`:

```json
{
  "servers": {
    "filesystem": {
      "url": "http://localhost:8000/adapters/filesystem-official/mcp",
      "description": "Official filesystem operations"
    },
    "sqlite": {
      "url": "http://localhost:8000/adapters/sqlite-official/mcp", 
      "description": "Official SQLite database operations"
    }
  }
}
```

## 💡 **关键优势**

### **1. 极简部署**
- 只需要 ~10行 Dockerfile
- 一个 API 调用完成部署
- 零自定义代码

### **2. 生产就绪**
- 官方维护和更新
- 完整的MCP协议实现
- 经过充分测试

### **3. 功能丰富**
- 文件系统：读写、列表、搜索
- SQLite：查询、更新、架构操作
- GitHub：仓库操作、文件访问

### **4. 扩展性**
- 轻松添加新的官方服务器
- 通过环境变量配置
- 支持水平扩展

## 🔧 **故障排除**

### 常见问题

1. **镜像构建失败**
   ```bash
   # 检查网络连接
   docker pull node:18-alpine
   
   # 检查npm registry
   npm config get registry
   ```

2. **部署失败**
   ```bash
   # 检查MCP Gateway状态
   curl http://localhost:8000/adapters
   
   # 检查镜像是否在registry中
   curl localhost:5002/v2/_catalog
   ```

3. **服务器无响应**
   ```bash
   # 检查Pod状态
   kubectl get pods -n adapter
   
   # 查看日志
   curl http://localhost:8000/adapters/{name}/logs
   ```

## 🎉 **结论**

**使用官方MCP服务器 + MCP Gateway = 完美的MCP部署解决方案！**

- 🚀 **快速**: 几分钟完成部署
- 🔧 **简单**: 只需要Dockerfile
- 🛡️ **可靠**: 官方维护的代码
- 📈 **可扩展**: 支持企业级部署

这就是MCP Gateway的真正价值 - 让复杂的MCP服务器管理变得简单！
