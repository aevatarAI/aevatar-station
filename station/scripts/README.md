# Aevatar Station 测试脚本

用于本地开发和端到端测试的脚本集合。

## 前置条件

1. **Kubernetes 配置** - 需要能访问 K8S 集群（用于 MongoDB 端口转发）
   ```bash
   # 确保 kubectl 已配置正确
   kubectl cluster-info
   ```

2. **本地依赖**
   ```bash
   # Redis (本地缓存)
   brew services start redis
   
   # 或使用 Docker
   docker run -d -p 6379:6379 --name redis redis:latest
   ```

3. **依赖工具**
   ```bash
   # 安装 jq (用于解析 JSON)
   brew install jq
   ```

## 脚本说明

### 1. 启动服务 (`start-services.sh`)

启动所有必要的服务：MongoDB 端口转发、Silo (Orleans)、AuthServer、HttpApi

```bash
# 完整启动（构建 + 启动所有服务）
./scripts/start-services.sh

# 跳过构建（已经构建过）
./scripts/start-services.sh --no-build

# 跳过 MongoDB 端口转发（使用本地 MongoDB）
./scripts/start-services.sh --no-mongo
```

服务启动后：
- **Silo**: 运行在端口 11111 (Orleans)
- **AuthServer**: http://localhost:8082
- **HttpApi**: http://localhost:8001
- **MongoDB**: localhost:27018 (通过 kubectl 端口转发)

日志文件保存在 `scripts/.pids/` 目录下。

### 2. 停止服务 (`stop-services.sh`)

停止所有运行中的服务：

```bash
# 正常停止
./scripts/stop-services.sh

# 停止并清理日志
./scripts/stop-services.sh --clean

# 强制停止所有进程
./scripts/stop-services.sh --force
```

## 测试流程

### 完整的端到端测试

```bash
# 1. 启动服务
./scripts/start-services.sh

# 2. 等待服务就绪（脚本会自动等待）

# 3. 测试 State Export API
curl http://localhost:8001/api/admin/export/types
curl http://localhost:8001/api/admin/export/summary

# 4. 停止服务
./scripts/stop-services.sh
```

### 快速重启

```bash
# 停止后重新启动（跳过构建）
./scripts/stop-services.sh && ./scripts/start-services.sh --no-build
```

## API 端点

### State Export API - `/api/admin/export`

| 方法 | 端点 | 说明 |
|------|------|------|
| GET | `/types` | 获取可导出的 State 类型列表 |
| GET | `/summary` | 获取导出概览（每个类型的数据量） |
| GET | `/page?collection=xxx&skip=0&limit=1000` | 分页获取 State 数据 |

### 使用示例

```bash
# 获取授权 Token
TOKEN=$(curl -s -X POST "http://localhost:8082/connect/token" \
  -d "grant_type=password&client_id=BusinessServer_App&client_secret=1q2w3e*&username=admin&password=1q2w3E*&scope=BusinessServer openid profile" \
  | jq -r '.access_token')

# 获取可用类型
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:8001/api/admin/export/types

# 获取导出概览
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:8001/api/admin/export/summary

# 分页导出数据
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:8001/api/admin/export/page?collection=StreamgodgptAevatar.Application.Grains.UserStatistics.UserStatisticsGAgent&skip=0&limit=100"
```

## 故障排除

### 服务启动失败

1. 检查 kubectl 端口转发：
   ```bash
   lsof -i :27018
   ```

2. 检查端口是否被占用：
   ```bash
   lsof -i :8001  # HttpApi
   lsof -i :8082  # AuthServer
   lsof -i :11111 # Silo
   ```

3. 查看日志：
   ```bash
   tail -f scripts/.pids/silo.log
   tail -f scripts/.pids/auth.log
   tail -f scripts/.pids/httpapi.log
   ```

### Redis 连接问题

确保 Redis 正在运行：
```bash
redis-cli ping
# 应返回 PONG
```

### MongoDB 连接问题

1. 检查 kubectl 端口转发：
   ```bash
   kubectl -n dapp-factory-shared get pods | grep mongo
   ```

2. 测试 MongoDB 连接：
   ```bash
   mongosh "mongodb://localhost:27018"
   ```

### 权限问题

确保脚本有执行权限：
```bash
chmod +x scripts/*.sh
```

## 目录结构

```
scripts/
├── .pids/              # PID 文件和日志目录
│   ├── silo.pid
│   ├── silo.log
│   ├── auth.pid
│   ├── auth.log
│   ├── httpapi.pid
│   ├── httpapi.log
│   ├── portforward.pid
│   └── portforward.log
├── start-services.sh   # 启动脚本
├── stop-services.sh    # 停止脚本
└── README.md           # 本文档
```

## 注意事项

1. **数据安全**: State Export API 可导出敏感数据，仅供管理员使用
2. **资源消耗**: 启动完整服务栈需要较多内存（建议 8GB+）
3. **端口冲突**: 确保端口 8001, 8082, 11111, 27018 未被占用
