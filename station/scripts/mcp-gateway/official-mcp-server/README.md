# Universal Official MCP Server

## 概述

这是一个**通用的Docker镜像**，支持所有官方MCP服务器。只需要一个Dockerfile和一个启动脚本，就能部署任何官方MCP服务器！

## 🎯 **设计理念**

### 问题
- 每个MCP服务器都需要单独的Dockerfile
- 管理多个镜像增加复杂性
- 重复的配置和维护工作

### 解决方案
- ✅ **一个Dockerfile** - 安装所有官方MCP服务器
- ✅ **环境变量控制** - 通过`MCP_SERVER_TYPE`指定启动哪个服务器
- ✅ **零重复代码** - 最大化代码复用

## 📁 **文件结构**

```
official-mcp-server/
├── Dockerfile              # 通用Dockerfile (40行)
├── start-mcp-server.sh     # 通用启动脚本 (100行)
└── README.md               # 本文档
```

**就这么简单！只需要3个文件！**

## 🚀 **支持的MCP服务器**

| 服务器类型 | npm包 | 功能描述 |
|-----------|-------|----------|
| `filesystem` | `@modelcontextprotocol/server-filesystem` | 文件系统操作 |
| `sqlite` | `@modelcontextprotocol/server-sqlite` | SQLite数据库操作 |
| `github` | `@modelcontextprotocol/server-github` | GitHub仓库操作 |
| `git` | `@modelcontextprotocol/server-git` | Git仓库操作 |
| `brave-search` | `@modelcontextprotocol/server-brave-search` | 网页搜索 |

## 📋 **使用方法**

### 1. 构建通用镜像

```bash
# 只需要构建一次！
docker build -t localhost:5003/universal-mcp-server:1.0.0 .
docker push localhost:5003/universal-mcp-server:1.0.0
```

### 2. 部署不同的MCP服务器

#### **文件系统服务器**
```bash
curl -X POST http://localhost:8000/adapters \
  -H "Content-Type: application/json" \
  -d '{
    "name": "filesystem-server",
    "imageName": "universal-mcp-server",
    "imageVersion": "1.0.0",
    "environment": {
      "MCP_SERVER_TYPE": "filesystem",
      "WORKSPACE_PATH": "/workspace"
    }
  }'
```

#### **SQLite服务器**
```bash
curl -X POST http://localhost:8000/adapters \
  -H "Content-Type: application/json" \
  -d '{
    "name": "sqlite-server",
    "imageName": "universal-mcp-server", 
    "imageVersion": "1.0.0",
    "environment": {
      "MCP_SERVER_TYPE": "sqlite",
      "DATABASE_PATH": "/data/myapp.db"
    }
  }'
```

#### **Git服务器**
```bash
curl -X POST http://localhost:8000/adapters \
  -H "Content-Type: application/json" \
  -d '{
    "name": "git-server",
    "imageName": "universal-mcp-server",
    "imageVersion": "1.0.0", 
    "environment": {
      "MCP_SERVER_TYPE": "git",
      "GIT_REPOSITORY_PATH": "/workspace"
    }
  }'
```

## 🧪 **测试**

```bash
# 运行完整测试
./test-universal-mcp-server.sh

# 这个脚本会：
# 1. 构建通用镜像
# 2. 部署3个不同的MCP服务器
# 3. 测试每个服务器的功能
# 4. 演示通用方案的强大
```

## 🎯 **环境变量参考**

### 通用变量
- `MCP_SERVER_TYPE` - 服务器类型 (必需)
- `MCP_SERVER_PORT` - 端口 (默认: 3000)
- `NODE_ENV` - Node.js环境 (默认: production)

### 文件系统服务器
- `WORKSPACE_PATH` - 工作目录 (默认: /workspace)

### SQLite服务器  
- `DATABASE_PATH` - 数据库文件路径 (默认: /data/mcp.db)

### GitHub服务器
- `GITHUB_PERSONAL_ACCESS_TOKEN` - GitHub访问令牌 (必需)

### Git服务器
- `GIT_REPOSITORY_PATH` - Git仓库路径 (默认: /workspace)

### Brave搜索服务器
- `BRAVE_SEARCH_API_KEY` - Brave搜索API密钥 (必需)

## 💡 **最佳实践**

### 1. 镜像管理
```bash
# 构建一次，到处使用
docker build -t universal-mcp-server:1.0.0 .

# 标记不同版本
docker tag universal-mcp-server:1.0.0 universal-mcp-server:latest
```

### 2. 环境配置
```bash
# 开发环境
"environment": {
  "MCP_SERVER_TYPE": "filesystem",
  "NODE_ENV": "development",
  "WORKSPACE_PATH": "/workspace"
}

# 生产环境  
"environment": {
  "MCP_SERVER_TYPE": "sqlite",
  "NODE_ENV": "production", 
  "DATABASE_PATH": "/data/production.db"
}
```

### 3. 监控和日志
```bash
# 查看服务器日志
curl http://localhost:8000/adapters/{server-name}/logs

# 检查服务器状态
curl http://localhost:8000/adapters/{server-name}/status
```

## 🔧 **故障排除**

### 常见问题

1. **服务器启动失败**
   ```bash
   # 检查环境变量
   kubectl describe pod {pod-name} -n adapter
   
   # 查看启动日志
   kubectl logs {pod-name} -n adapter
   ```

2. **MCP协议错误**
   ```bash
   # 测试初始化
   curl -X POST http://localhost:8000/adapters/{name}/mcp \
     -d '{"jsonrpc":"2.0","method":"initialize","id":"1"}'
   ```

3. **权限问题**
   ```bash
   # 检查文件权限 (filesystem服务器)
   # 检查数据库权限 (sqlite服务器)
   ```

## 🎉 **总结**

**这就是MCP服务器部署的最佳实践！**

- 🚀 **极简**: 只需要1个Dockerfile + 1个脚本
- 🔄 **复用**: 一个镜像支持所有官方服务器  
- 📦 **标准**: 使用官方npm包，零自定义代码
- ⚡ **快速**: 几分钟完成任何MCP服务器的部署

**这证明了MCP Gateway + 官方MCP服务器的强大组合！** 🎯
