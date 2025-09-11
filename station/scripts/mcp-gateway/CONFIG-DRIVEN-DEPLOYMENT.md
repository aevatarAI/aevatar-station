# Configuration-Driven MCP Server Deployment

## 🎯 **终极简化方案：配置文件驱动**

就像Cursor的MCP配置一样，现在可以通过**一个配置文件**来管理所有MCP服务器的部署！

## 💡 **设计理念**

### 问题
- 每个MCP服务器需要单独部署
- 手动管理多个服务器配置
- 难以版本控制和团队共享
- 环境间配置不一致

### 解决方案 ✨
```json
{
  "servers": {
    "filesystem": {
      "enabled": true,
      "type": "npm",
      "package": "@modelcontextprotocol/server-filesystem"
    },
    "my-custom-server": {
      "enabled": true, 
      "type": "custom",
      "image": "my-mcp-server:1.0.0"
    }
  }
}
```

**一个配置文件 + 一个命令 = 部署所有MCP服务器！**

## 🚀 **使用方法**

### 1. 创建配置文件

```json
{
  "version": "1.0.0",
  "registry": {
    "url": "localhost:5003",
    "namespace": "my-mcp-servers"
  },
  "gateway": {
    "url": "http://localhost:8000"
  },
  "servers": {
    "filesystem": {
      "enabled": true,
      "type": "npm",
      "package": "@modelcontextprotocol/server-filesystem",
      "description": "File operations server",
      "environment": {
        "WORKSPACE_PATH": "/workspace"
      },
      "tags": ["files", "official"]
    },
    "my-database": {
      "enabled": true,
      "type": "custom", 
      "image": "my-sqlite-mcp:1.0.0",
      "description": "Custom database server",
      "environment": {
        "DB_PATH": "/data/app.db"
      },
      "tags": ["database", "custom"]
    }
  }
}
```

### 2. 一键部署

```bash
# 部署所有启用的服务器
./deploy-from-config.sh my-config.json

# 使用默认配置
./deploy-from-config.sh
```

### 3. 自动生成VS Code配置

脚本会自动生成VS Code MCP配置：

```json
{
  "servers": {
    "filesystem": {
      "url": "http://localhost:8000/adapters/filesystem/mcp",
      "description": "File operations server"
    },
    "my-database": {
      "url": "http://localhost:8000/adapters/my-database/mcp",
      "description": "Custom database server"
    }
  }
}
```

## 📋 **支持的服务器类型**

### 1. npm类型（官方包）
```json
{
  "type": "npm",
  "package": "@modelcontextprotocol/server-filesystem",
  "version": "latest"
}
```
- 自动构建Dockerfile
- 安装指定的npm包
- 使用npx启动

### 2. custom类型（自定义镜像）
```json
{
  "type": "custom",
  "image": "my-custom-mcp:1.0.0"
}
```
- 使用预构建的镜像
- 支持自定义环境变量
- 灵活的配置选项

### 3. docker类型（外部镜像）
```json
{
  "type": "docker",
  "image": "third-party/mcp-server:latest"
}
```
- 自动拉取外部镜像
- 重新标记到本地registry
- 统一管理

## 🎯 **配置文件结构**

### 完整示例
```json
{
  "version": "1.0.0",
  "description": "Production MCP Servers",
  "registry": {
    "url": "localhost:5003",
    "namespace": "prod-mcp"
  },
  "gateway": {
    "url": "http://localhost:8000",
    "auth": {
      "type": "bearer",
      "token": "your-auth-token"
    }
  },
  "servers": {
    "server-name": {
      "enabled": true,
      "type": "npm|custom|docker",
      "package": "npm-package-name",
      "image": "docker-image-name",
      "version": "1.0.0",
      "description": "Server description",
      "environment": {
        "KEY": "value"
      },
      "resources": {
        "memory": "256Mi",
        "cpu": "200m"
      },
      "volumes": [
        {
          "name": "data",
          "path": "/data",
          "type": "persistentVolume",
          "size": "1Gi"
        }
      ],
      "secrets": [
        {
          "name": "API_KEY",
          "required": true
        }
      ],
      "tags": ["category", "type"]
    }
  },
  "deployment": {
    "strategy": "rolling",
    "timeout": "300s",
    "healthCheck": {
      "enabled": true,
      "path": "/health"
    }
  }
}
```

## 💡 **最佳实践**

### 1. 环境管理
```bash
# 开发环境
./deploy-from-config.sh dev-mcp-servers.json

# 生产环境  
./deploy-from-config.sh prod-mcp-servers.json
```

### 2. 版本控制
```bash
git add mcp-servers.config.json
git commit -m "Add filesystem and database MCP servers"
```

### 3. 团队共享
```bash
# 团队成员只需要
git pull
./deploy-from-config.sh
```

## 🔄 **与Cursor配置的对比**

### Cursor MCP配置 (.vscode/mcp.json)
```json
{
  "servers": {
    "filesystem": {
      "command": "npx",
      "args": ["@modelcontextprotocol/server-filesystem", "/workspace"]
    }
  }
}
```

### MCP Gateway配置 (mcp-servers.config.json)
```json
{
  "servers": {
    "filesystem": {
      "enabled": true,
      "type": "npm",
      "package": "@modelcontextprotocol/server-filesystem",
      "environment": {"WORKSPACE_PATH": "/workspace"}
    }
  }
}
```

**相同的理念，但支持企业级部署和管理！**

## 🎉 **优势总结**

### 🚀 **开发体验**
- ✅ **声明式** - 描述想要的状态，不是步骤
- ✅ **版本控制** - 配置文件可以git管理
- ✅ **团队共享** - 一个配置，全团队使用
- ✅ **环境一致** - dev/staging/prod使用相同配置

### 🏢 **企业就绪**
- ✅ **批量管理** - 一次部署多个服务器
- ✅ **资源控制** - CPU、内存、存储配置
- ✅ **监控集成** - 健康检查、指标收集
- ✅ **安全性** - 密钥管理、访问控制

### 🔧 **运维友好**
- ✅ **Infrastructure as Code** - 配置即文档
- ✅ **回滚能力** - 配置版本控制
- ✅ **扩展性** - 轻松添加新服务器
- ✅ **标准化** - 统一的部署模式

## 🎯 **结论**

**这就是MCP服务器管理的未来！**

```bash
# 从复杂的手动部署
docker build ...
docker push ...
curl -X POST ...

# 到简单的配置驱动
./deploy-from-config.sh
```

**一个配置文件，管理整个MCP服务器生态系统！** 🚀
