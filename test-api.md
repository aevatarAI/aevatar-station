# Cross-URL管理和服务重启接口测试指南

## 1. 环境准备

### 1.1 检查项目编译状态
```bash
# 在项目根目录执行
cd /Users/tangchen/Project/github/aevatar-station
dotnet build
```

### 1.2 本地K8s集群准备
确保Docker Desktop已启用Kubernetes：
1. 打开Docker Desktop设置
2. 启用Kubernetes选项
3. 验证连接：`kubectl cluster-info`

### 1.3 检查kubeconfig文件
```bash
# 检查配置文件是否存在
ls -la ~/.kube/config

# 验证kubectl可以连接集群
kubectl get nodes
kubectl get pods --all-namespaces
```

## 2. 启动服务

### 2.1 启动Developer Host
```bash
cd src/Aevatar.Developer.Host
dotnet run
```

服务将在 `http://localhost:8308` 启动

### 2.2 验证服务启动
```bash
# 检查服务健康状态
curl -X GET "http://localhost:8308/health" || echo "Health endpoint not available"

# 检查端口是否监听
netstat -an | grep 8308
```

## 3. API接口测试

### 3.1 Cross-URL管理接口测试

#### 获取Cross-URL列表（使用Mock数据）
```bash
curl -X GET "http://localhost:8308/api/developer/cross-urls?clientId=test-client" \
  -H "Accept: application/json"
```

**预期响应：**
```json
{
  "items": [
    {
      "id": "guid",
      "url": "https://api.example.com",
      "created": "2024-01-01T00:00:00Z",
      "createdBy": "admin"
    },
    {
      "id": "guid", 
      "url": "https://webhook.test.com",
      "created": "2024-01-02T00:00:00Z",
      "createdBy": "developer"
    }
  ],
  "totalCount": 2
}
```

#### 创建新的Cross-URL
```bash
curl -X POST "http://localhost:8308/api/developer/cross-urls?clientId=test-client" \
  -H "Content-Type: application/json" \
  -d '{"url": "https://new-api.example.com"}'
```

**预期响应：**
```json
{
  "id": "new-guid",
  "url": "https://new-api.example.com", 
  "created": "2024-01-03T00:00:00Z",
  "createdBy": "anonymous"
}
```

#### 删除Cross-URL
```bash
# 先获取一个ID，然后删除
URL_ID="从上面获取的ID"
curl -X DELETE "http://localhost:8308/api/developer/cross-urls/${URL_ID}"
```

### 3.2 服务重启接口测试

#### 获取服务状态
```bash
curl -X GET "http://localhost:8308/api/developer/status?clientId=test-client" \
  -H "Accept: application/json"
```

**可能的响应：**
```json
{
  "status": 0,  // Unknown=0, Starting=1, Running=2, Stopping=3, Stopped=4, Error=5
  "lastRestartTime": null,
  "uptime": null,
  "statusDescription": "No deployment found for the specified client",
  "readyReplicas": 0,
  "totalReplicas": 0
}
```

#### 触发服务重启
```bash
curl -X POST "http://localhost:8308/api/developer/restart?clientId=test-client" \
  -H "Accept: application/json"
```

**预期响应：**
```json
{
  "isSuccess": true,
  "message": "Service is restarting to apply configuration changes"
}
```

## 4. K8s集群连接测试

### 4.1 验证K8s配置
检查appsettings.json中的Kubernetes配置：
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

### 4.2 测试K8s连接
```bash
# 检查默认命名空间中的deployments
kubectl get deployments -n default

# 检查是否有test-client相关的deployment
kubectl get deployments -n default | grep test-client

# 如果没有，创建一个测试deployment用于测试
kubectl create deployment test-client-silo --image=nginx -n default
kubectl create deployment test-client-client --image=nginx -n default
```

### 4.3 测试重启功能（有真实deployment时）
```bash
# 触发重启
curl -X POST "http://localhost:8308/api/developer/restart?clientId=test-client"

# 检查deployment重启状态
kubectl get deployments -n default
kubectl describe deployment test-client-silo -n default
kubectl describe deployment test-client-client -n default

# 检查Pod重启
kubectl get pods -n default | grep test-client
```

## 5. 测试场景

### 5.1 Mock数据测试场景
1. **基础CRUD测试**：创建、读取、删除Cross-URL
2. **数据持久性测试**：重启服务后检查Mock数据是否保持
3. **错误处理测试**：删除不存在的ID、创建无效URL

### 5.2 K8s集群测试场景  
1. **无deployment场景**：测试当集群中没有对应deployment时的行为
2. **有deployment场景**：测试真实的重启和状态查询功能
3. **权限测试**：验证kubeconfig权限是否足够

### 5.3 集成测试场景
1. **完整流程测试**：创建Cross-URL → 触发重启 → 验证配置应用
2. **并发测试**：同时调用多个接口
3. **错误恢复测试**：K8s连接失败时的降级处理

## 6. 常见问题排查

### 6.1 服务启动失败
```bash
# 检查端口占用
lsof -i :8308

# 检查日志
tail -f src/Aevatar.Developer.Host/Logs/log-*.log

# 检查依赖服务
# MongoDB: 默认端口27017
# Redis: 默认端口6379
```

### 6.2 K8s连接问题
```bash
# 验证kubeconfig
kubectl config view
kubectl config current-context

# 测试基本权限
kubectl auth can-i get deployments
kubectl auth can-i update deployments
```

### 6.3 API调用失败
```bash
# 检查服务是否运行
ps aux | grep Aevatar.Developer.Host

# 检查网络连接
curl -v http://localhost:8308/api/developer/cross-urls

# 检查防火墙设置
```

## 7. 性能测试

### 7.1 基础性能测试
```bash
# 使用ab进行压力测试
ab -n 100 -c 10 http://localhost:8308/api/developer/cross-urls?clientId=test-client

# 使用curl测试响应时间
time curl -X GET "http://localhost:8308/api/developer/cross-urls?clientId=test-client"
```

### 7.2 K8s操作性能
```bash
# 测试重启操作时间
time curl -X POST "http://localhost:8308/api/developer/restart?clientId=test-client"

# 监控K8s API调用
kubectl get events --sort-by=.metadata.creationTimestamp
```

## 8. 自动化测试脚本

### 8.1 创建测试脚本
```bash
#!/bin/bash
# test-apis.sh

BASE_URL="http://localhost:8308/api/developer"
CLIENT_ID="test-client"

echo "=== Testing Cross-URL APIs ==="

# Test GET
echo "1. Getting Cross-URLs..."
curl -s "$BASE_URL/cross-urls?clientId=$CLIENT_ID" | jq .

# Test POST  
echo "2. Creating Cross-URL..."
RESPONSE=$(curl -s -X POST "$BASE_URL/cross-urls?clientId=$CLIENT_ID" \
  -H "Content-Type: application/json" \
  -d '{"url": "https://test-api.example.com"}')
echo $RESPONSE | jq .

# Extract ID for deletion
URL_ID=$(echo $RESPONSE | jq -r .id)

# Test DELETE
echo "3. Deleting Cross-URL..."
curl -s -X DELETE "$BASE_URL/cross-urls/$URL_ID"

echo "=== Testing Service APIs ==="

# Test Status
echo "4. Getting Service Status..."
curl -s "$BASE_URL/status?clientId=$CLIENT_ID" | jq .

# Test Restart
echo "5. Triggering Service Restart..."
curl -s -X POST "$BASE_URL/restart?clientId=$CLIENT_ID" | jq .

echo "=== Test Complete ==="
```

### 8.2 运行测试
```bash
chmod +x test-apis.sh
./test-apis.sh
```

## 9. 监控和日志

### 9.1 应用日志
```bash
# 实时查看应用日志
tail -f src/Aevatar.Developer.Host/Logs/log-*.log

# 过滤特定日志
grep "Cross-URL" src/Aevatar.Developer.Host/Logs/log-*.log
grep "restart" src/Aevatar.Developer.Host/Logs/log-*.log
```

### 9.2 K8s事件监控
```bash
# 监控K8s事件
kubectl get events --watch

# 查看特定deployment事件
kubectl describe deployment test-client-silo -n default
```

这个测试指南涵盖了从基础的Mock数据测试到完整的K8s集群集成测试，帮助验证所有功能是否正常工作。 