# 🔧 MCP Gateway Aspire Integration - 修正版

## ⚠️ 重要更正

之前的集成方案中使用了**错误的Docker镜像地址**。经过重新调研官方文档，现已修正。

### 🎯 **正确的镜像信息**

#### **官方镜像**（推荐）
```bash
ghcr.io/mcp-ecosystem/mcp-gateway/allinone:latest
```

#### **备选镜像**（国内用户）
```bash
# Docker Hub
docker.io/ifuryst/unla-allinone:latest

# 阿里云镜像（推荐国内用户）
registry.ap-southeast-1.aliyuncs.com/amoylab/unla-allinone:latest
```

### 🔌 **正确的端口配置**

MCP Gateway使用多个端口，不是单一的8080端口：

```yaml
端口映射:
  - 7004:80     # 主API端口
  - 5234:5234   # WebSocket端口  
  - 5235:5235   # gRPC端口
  - 5335:5335   # 管理端口
  - 5236:5236   # 指标端口
```

### 🚀 **修正后的启动流程**

```bash
cd station/src/Aevatar.Aspire
dotnet run

# 等待35秒后，访问：
# 🌐 MCP Gateway API: http://localhost:7004
# 📊 MCP Gateway Admin: http://localhost:5335  
# 📈 MCP Gateway Metrics: http://localhost:5236
# 📚 API文档: http://localhost:7004/swagger (如果支持)
```

### 🧪 **验证集成**

```bash
# 测试MCP Gateway是否正常启动
curl -s http://localhost:7004/api/health

# 测试管理接口
curl -s http://localhost:5335/admin/status

# 运行完整测试脚本
./test-mcp-integration.sh
```

### 📋 **环境变量配置**

MCP Gateway需要以下必需环境变量：

```bash
ENV=development
TZ=UTC
APISERVER_JWT_SECRET_KEY=aevatar-dev-jwt-secret-key-12345
SUPER_ADMIN_USERNAME=admin
SUPER_ADMIN_PASSWORD=admin123
```

### 🔄 **API能力保持**

✅ **完全保持MCP Gateway的原生API能力**：
- 动态创建/删除MCP服务器适配器
- 会话感知路由和负载均衡  
- 实时监控和指标收集
- 通过REST API管理适配器生命周期

### 🎯 **集成架构**

```
Aspire编排层:
├── MCP Gateway容器 (ghcr.io/mcp-ecosystem/mcp-gateway/allinone:latest)
├── 基础设施服务 (MongoDB, Redis, ES等)
└── Aevatar服务 (集成MCP Gateway客户端)

运行时管理层:
├── MCP Gateway原生API (端口7004) ← 完整动态管理能力
├── Aevatar MCP API (端口7002) ← 集成权限控制
└── 容器编排 (Docker/K8s) ← 基础设施管理
```

### ✅ **结论**

**Aspire集成完全可行且不会限制MCP Gateway的API能力！**

- ✅ 使用正确的官方镜像
- ✅ 正确的端口配置  
- ✅ 保持完整的动态管理能力
- ✅ 与Aevatar生态系统完美集成

修正后的方案既利用了Aspire的编排便利性，又完全保留了MCP Gateway的核心价值。
