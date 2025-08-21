# Python沙箱本地测试指南

本指南提供了在本地设置和测试Python沙箱环境的分步说明。

## 前置要求

1. 安装所需工具：
   ```bash
   # 安装Docker
   brew install docker

   # 安装K3d（轻量级本地Kubernetes）
   brew install k3d

   # 安装kubectl
   brew install kubectl

   # 安装.NET 8.0 SDK
   brew install dotnet-sdk
   ```

2. 克隆代码库：
   ```bash
   git clone <your-repository-url>
   cd aevatar-station
   ```

## 设置本地环境

### 1. 创建本地Kubernetes集群

```bash
# 创建一个具有1个服务器和2个代理的新集群
k3d cluster create sandbox-dev \
  --servers 1 \
  --agents 2 \
  --port "8080:80@loadbalancer" \
  --port "8443:443@loadbalancer"

# 验证集群是否运行
kubectl cluster-info
```

### 2. 构建Python沙箱镜像

```bash
# 导航到Python沙箱目录
cd src/Aevatar.Sandbox.Python/Docker

# 构建镜像
docker build -t aevatar/python-sandbox:local .

# 将镜像导入K3d集群
k3d image import aevatar/python-sandbox:local -c sandbox-dev
```

### 3. 部署沙箱组件

```bash
# 应用Kubernetes配置
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/rbac.yaml
kubectl apply -f k8s/network-policy.yaml
kubectl apply -f k8s/resource-quota.yaml

# 部署沙箱服务
kubectl apply -f k8s/sandbox-deployment.yaml
kubectl apply -f k8s/sandbox-service.yaml
```

### 4. 启动本地开发环境

```bash
# 启动API主机
cd src/Aevatar.Sandbox.HttpApi.Host
dotnet run
```

## 测试Python代码执行

### 1. 基本测试

```bash
# 测试简单的Python代码
curl -X POST http://localhost:5000/api/sandbox/execute \
  -H "Content-Type: application/json" \
  -d '{
    "language": "python",
    "code": "print(\"Hello, World!\")",
    "timeout": 30
  }'
```

### 2. 测试依赖项

```bash
# 测试使用numpy的代码
curl -X POST http://localhost:5000/api/sandbox/execute \
  -H "Content-Type: application/json" \
  -d '{
    "language": "python",
    "code": "import numpy as np\nprint(np.array([1, 2, 3]).mean())",
    "timeout": 30
  }'
```

### 3. 测试资源限制

```bash
# 测试资源限制
curl -X POST http://localhost:5000/api/sandbox/execute \
  -H "Content-Type: application/json" \
  -d '{
    "language": "python",
    "code": "import numpy as np\na = np.random.rand(1000, 1000)\nprint(a.mean())",
    "timeout": 30,
    "resources": {
      "cpu": "100m",
      "memory": "256Mi"
    }
  }'
```

### 4. 测试长时间运行的代码

```bash
# 启动异步执行
curl -X POST http://localhost:5000/api/sandbox/execute-async \
  -H "Content-Type: application/json" \
  -d '{
    "language": "python",
    "code": "import time\nfor i in range(5):\n    time.sleep(1)\n    print(f\"Step {i}\")",
    "timeout": 30
  }'

# 从响应中获取执行ID，然后检查状态
curl http://localhost:5000/api/sandbox/status/{executionId}

# 获取日志
curl http://localhost:5000/api/sandbox/logs/{executionId}
```

## 监控和调试

### 1. 查看Kubernetes资源

```bash
# 检查pods
kubectl get pods -n sandbox

# 检查日志
kubectl logs -n sandbox deployment/python-sandbox

# 检查事件
kubectl get events -n sandbox
```

### 2. 监控资源使用

```bash
# 获取pod指标
kubectl top pod -n sandbox

# 获取节点指标
kubectl top node
```

### 3. 调试容器

```bash
# 获取沙箱pod的shell访问权限
kubectl exec -it -n sandbox deployment/python-sandbox -- /bin/bash

# 检查沙箱环境
python3 --version
pip list
```

## 常见问题和解决方案

### 1. Pod启动问题

如果pods无法启动：
```bash
# 检查pod状态
kubectl describe pod -n sandbox

# 检查事件
kubectl get events -n sandbox
```

### 2. 资源限制

如果看到OOMKilled：
```bash
# 在部署中增加内存限制
kubectl edit deployment -n sandbox python-sandbox
```

### 3. 网络问题

如果网络策略过于严格：
```bash
# 检查网络策略
kubectl get networkpolicies -n sandbox

# 应用较宽松的策略
kubectl apply -f k8s/network-policy-dev.yaml
```

## 测试不同场景

### 1. 安全测试

```python
# 测试文件系统访问
code = """
try:
    with open('/etc/passwd', 'r') as f:
        print(f.read())
except Exception as e:
    print(f"Error: {e}")
"""

# 测试网络访问
code = """
import requests
try:
    r = requests.get('https://api.github.com')
    print(r.status_code)
except Exception as e:
    print(f"Error: {e}")
"""
```

### 2. 资源测试

```python
# 测试内存限制
code = """
import numpy as np
try:
    # 尝试分配大数组
    a = np.zeros((1000000, 1000000))
except Exception as e:
    print(f"Error: {e}")
"""

# 测试CPU限制
code = """
def cpu_intensive():
    return sum(i * i for i in range(10**7))
print(cpu_intensive())
"""
```

### 3. 超时测试

```python
# 测试超时处理
code = """
import time
print("Starting")
time.sleep(60)
print("Should not reach here")
"""
```

## 清理

```bash
# 删除沙箱资源
kubectl delete namespace sandbox

# 删除本地集群
k3d cluster delete sandbox-dev

# 删除本地镜像
docker rmi aevatar/python-sandbox:local
```

## 下一步

1. 探索高级功能：
   - 自定义Python包
   - 多文件执行
   - 交互式会话

2. 集成测试：
   - Orleans集成
   - 事件溯源
   - 指标收集

3. 性能测试：
   - 负载测试
   - 并发执行
   - 资源优化

## 许可证

版权所有 (c) Aevatar。保留所有权利。