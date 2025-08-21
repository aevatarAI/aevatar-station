# Python Sandbox Local Testing Guide

This guide provides step-by-step instructions for setting up and testing the Python sandbox environment locally.

## Prerequisites

1. Install required tools:
   ```bash
   # Install Docker
   brew install docker

   # Install K3d (lightweight local Kubernetes)
   brew install k3d

   # Install kubectl
   brew install kubectl

   # Install .NET 8.0 SDK
   brew install dotnet-sdk
   ```

2. Clone the repository:
   ```bash
   git clone https://github.com/aevatar/sandbox.git
   cd sandbox
   ```

## Setup Local Environment

### 1. Create Local Kubernetes Cluster

```bash
# Create a new cluster with 1 server and 2 agents
k3d cluster create sandbox-dev \
  --servers 1 \
  --agents 2 \
  --port "8080:80@loadbalancer" \
  --port "8443:443@loadbalancer"

# Verify cluster is running
kubectl cluster-info
```

### 2. Build Python Sandbox Image

```bash
# Navigate to Python sandbox directory
cd src/Aevatar.Sandbox.Python/Docker

# Build the image
docker build -t aevatar/python-sandbox:local .

# Import image to K3d cluster
k3d image import aevatar/python-sandbox:local -c sandbox-dev
```

### 3. Deploy Sandbox Components

```bash
# Apply Kubernetes configurations
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/rbac.yaml
kubectl apply -f k8s/network-policy.yaml
kubectl apply -f k8s/resource-quota.yaml

# Deploy sandbox service
kubectl apply -f k8s/sandbox-deployment.yaml
kubectl apply -f k8s/sandbox-service.yaml
```

### 4. Start Local Development Environment

```bash
# Start the API host
cd src/Aevatar.Sandbox.HttpApi.Host
dotnet run
```

## Testing Python Code Execution

### 1. Basic Test

```bash
# Test simple Python code
curl -X POST http://localhost:5000/api/sandbox/execute \
  -H "Content-Type: application/json" \
  -d '{
    "language": "python",
    "code": "print(\"Hello, World!\")",
    "timeout": 30
  }'
```

### 2. Test with Dependencies

```bash
# Test code with numpy
curl -X POST http://localhost:5000/api/sandbox/execute \
  -H "Content-Type: application/json" \
  -d '{
    "language": "python",
    "code": "import numpy as np\nprint(np.array([1, 2, 3]).mean())",
    "timeout": 30
  }'
```

### 3. Test Resource Limits

```bash
# Test with resource limits
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

### 4. Test Long-Running Code

```bash
# Start async execution
curl -X POST http://localhost:5000/api/sandbox/execute-async \
  -H "Content-Type: application/json" \
  -d '{
    "language": "python",
    "code": "import time\nfor i in range(5):\n    time.sleep(1)\n    print(f\"Step {i}\")",
    "timeout": 30
  }'

# Get execution ID from response, then check status
curl http://localhost:5000/api/sandbox/status/{executionId}

# Get logs
curl http://localhost:5000/api/sandbox/logs/{executionId}
```

## Monitoring and Debugging

### 1. View Kubernetes Resources

```bash
# Check pods
kubectl get pods -n sandbox

# Check logs
kubectl logs -n sandbox deployment/python-sandbox

# Check events
kubectl get events -n sandbox
```

### 2. Monitor Resource Usage

```bash
# Get pod metrics
kubectl top pod -n sandbox

# Get node metrics
kubectl top node
```

### 3. Debug Container

```bash
# Get shell access to sandbox pod
kubectl exec -it -n sandbox deployment/python-sandbox -- /bin/bash

# Check sandbox environment
python3 --version
pip list
```

## Common Issues and Solutions

### 1. Pod Startup Issues

If pods are not starting:
```bash
# Check pod status
kubectl describe pod -n sandbox

# Check events
kubectl get events -n sandbox
```

### 2. Resource Limits

If seeing OOMKilled:
```bash
# Increase memory limit in deployment
kubectl edit deployment -n sandbox python-sandbox
```

### 3. Network Issues

If network policies are too restrictive:
```bash
# Check network policies
kubectl get networkpolicies -n sandbox

# Apply less restrictive policy
kubectl apply -f k8s/network-policy-dev.yaml
```

## Testing Different Scenarios

### 1. Security Tests

```python
# Test file system access
code = """
try:
    with open('/etc/passwd', 'r') as f:
        print(f.read())
except Exception as e:
    print(f"Error: {e}")
"""

# Test network access
code = """
import requests
try:
    r = requests.get('https://api.github.com')
    print(r.status_code)
except Exception as e:
    print(f"Error: {e}")
"""
```

### 2. Resource Tests

```python
# Test memory limit
code = """
import numpy as np
try:
    # Try to allocate large array
    a = np.zeros((1000000, 1000000))
except Exception as e:
    print(f"Error: {e}")
"""

# Test CPU limit
code = """
def cpu_intensive():
    return sum(i * i for i in range(10**7))
print(cpu_intensive())
"""
```

### 3. Timeout Tests

```python
# Test timeout handling
code = """
import time
print("Starting")
time.sleep(60)
print("Should not reach here")
"""
```

## Cleanup

```bash
# Delete sandbox resources
kubectl delete namespace sandbox

# Delete local cluster
k3d cluster delete sandbox-dev

# Remove local images
docker rmi aevatar/python-sandbox:local
```

## Next Steps

1. Explore advanced features:
   - Custom Python packages
   - Multi-file execution
   - Interactive sessions

2. Integration testing:
   - Orleans integration
   - Event sourcing
   - Metrics collection

3. Performance testing:
   - Load testing
   - Concurrent executions
   - Resource optimization

## License

Copyright (c) Aevatar. All rights reserved.