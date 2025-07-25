# 本地 Kubernetes 集群配置指南

## 1. 环境准备

### 1.1 安装 Docker Desktop (推荐)
1. 下载并安装 [Docker Desktop](https://www.docker.com/products/docker-desktop/)
2. 启动 Docker Desktop
3. 在设置中启用 Kubernetes
   - 打开 Docker Desktop 设置
   - 点击 "Kubernetes" 选项卡
   - 勾选 "Enable Kubernetes"
   - 点击 "Apply & Restart"

### 1.2 验证安装
```bash
# 检查 kubectl 是否可用
kubectl version --client

# 检查集群状态
kubectl cluster-info

# 检查节点状态
kubectl get nodes
```

## 2. 项目配置

### 2.1 Kubernetes 配置文件位置
项目默认使用以下配置文件路径：
- **macOS/Linux**: `~/.kube/config`
- **Windows**: `%USERPROFILE%\.kube\config`

### 2.2 appsettings.json 配置
在 `src/Aevatar.Developer.Host/appsettings.json` 中已添加：

```json
{
  "Kubernetes": {
    "KubeConfigPath": "~/.kube/config",
    "AppNameSpace": "default",
    "AppPodReplicas": 1,
    "WebhookHostName": "aevatar-webhook",
    "DeveloperHostName": "aevatar-developer",
    "RequestCpuCore": "1",
    "RequestMemory": "2Gi"
  }
}
```

## 3. 测试接口

### 3.1 启动服务
```bash
cd src/Aevatar.Developer.Host
dotnet run
```

服务将在 `http://localhost:8308` 启动

### 3.2 测试 Cross-URL 管理接口

#### 获取 Cross-URL 列表
```bash
curl -X GET "http://localhost:8308/api/developer/cross-urls?clientId=test-client"
```

#### 创建 Cross-URL
```bash
curl -X POST "http://localhost:8308/api/developer/cross-urls?clientId=test-client" \
  -H "Content-Type: application/json" \
  -d '{"url": "https://example.com"}'
```

#### 删除 Cross-URL
```bash
curl -X DELETE "http://localhost:8308/api/developer/cross-urls/{id}"
```

### 3.3 测试服务重启接口

#### 获取服务状态
```bash
curl -X GET "http://localhost:8308/api/developer/status?clientId=test-client"
```

#### 重启服务应用配置
```bash
curl -X POST "http://localhost:8308/api/developer/restart?clientId=test-client"
```

## 4. Mock 数据说明

当前实现使用内存中的 Mock 数据进行测试：

```csharp
private static readonly List<CrossUrlDto> _mockCrossUrls = new()
{
    new CrossUrlDto
    {
        Id = Guid.NewGuid(),
        Url = "https://api.example.com",
        Created = DateTime.UtcNow.AddDays(-2),
        CreatedBy = "admin"
    },
    new CrossUrlDto
    {
        Id = Guid.NewGuid(),
        Url = "https://webhook.test.com",
        Created = DateTime.UtcNow.AddDays(-1),
        CreatedBy = "developer"
    }
};
```

## 5. 常见问题排查

### 5.1 Kubernetes 连接问题
```bash
# 检查 kubeconfig 文件是否存在
ls -la ~/.kube/config

# 检查当前上下文
kubectl config current-context

# 检查集群连接
kubectl get pods --all-namespaces
```

### 5.2 服务启动问题
1. 确保端口 8308 未被占用
2. 检查 MongoDB 和 Redis 是否正常运行
3. 查看应用日志了解详细错误信息

### 5.3 权限问题
如果遇到 Kubernetes API 权限问题，确保：
1. Docker Desktop 的 Kubernetes 已正确启用
2. kubectl 可以正常访问集群
3. 当前用户有足够的权限访问 default 命名空间

## 6. 开发建议

### 6.1 日志查看
```bash
# 查看应用日志
tail -f src/Aevatar.Developer.Host/Logs/log-*.log

# 查看 Kubernetes 事件
kubectl get events --sort-by=.metadata.creationTimestamp
```

### 6.2 调试技巧
1. 使用 Postman 或类似工具测试 API
2. 在 IDE 中设置断点调试
3. 查看 Kubernetes Dashboard（如果安装了）
4. 使用 `kubectl describe` 命令查看资源详情

### 6.3 下一步开发
1. 集成真实的数据库存储（替换 Mock 数据）
2. 添加用户认证和授权
3. 实现更复杂的 Kubernetes 操作
4. 添加监控和告警功能 