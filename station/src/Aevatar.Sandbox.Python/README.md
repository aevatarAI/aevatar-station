# Aevatar.Sandbox.Python

Python execution environment for the Aevatar Sandbox Platform.

## Overview

This package provides a secure and scalable Python code execution environment built on top of Aevatar.Sandbox.Core. It enables safe execution of untrusted Python code in isolated containers with resource limits and security boundaries.

## Features

- Python 3.11+ runtime environment
- Pre-installed common data science packages
- Resource usage monitoring
- Execution isolation
- Code validation and security checks

## Installation

```bash
dotnet add package Aevatar.Sandbox.Python
```

## Usage

### Basic Execution

```csharp
var service = new PythonSandboxService();
var result = await service.ExecuteAsync(new SandboxExecutionRequest
{
    Code = @"
import numpy as np
arr = np.array([1, 2, 3])
print(arr.mean())
    ",
    Timeout = TimeSpan.FromSeconds(30)
});
```

### With Resource Limits

```csharp
var request = new SandboxExecutionRequest
{
    Code = pythonCode,
    Resources = new SandboxResources
    {
        CpuLimit = "100m",
        MemoryLimit = "256Mi",
        DiskLimit = "1Gi"
    }
};
```

### Async Execution with Progress

```csharp
var executionId = await service.StartAsync(request);
while (true)
{
    var status = await service.GetStatusAsync(executionId);
    if (status.IsCompleted)
        break;
    await Task.Delay(1000);
}
var result = await service.GetResultAsync(executionId);
```

## Container Environment

### Pre-installed Packages

- numpy
- pandas
- scipy
- matplotlib
- scikit-learn
- requests
- beautifulsoup4

### Security Restrictions

- No network access
- No file system persistence
- Limited CPU and memory
- Restricted module imports

## Configuration

```json
{
  "PythonSandbox": {
    "Image": "aevatar/python-sandbox:3.11",
    "DefaultTimeout": 30,
    "MaxCodeLength": 1000000,
    "AllowedModules": [
      "numpy",
      "pandas",
      "scipy"
    ]
  }
}
```

## Local Development

### Prerequisites

1. Install Docker
2. Install Kubernetes (or K3d/Kind)
3. Install .NET 8.0+

### Setup Local Environment

1. Build sandbox image:
```bash
docker build -t aevatar/python-sandbox:local .
```

2. Deploy to local Kubernetes:
```bash
kubectl apply -f k8s/
```

3. Run tests:
```bash
dotnet test
```

### Testing Python Code

1. Start local sandbox:
```bash
dotnet run --project samples/LocalSandbox
```

2. Execute test code:
```bash
curl -X POST http://localhost:5000/api/python/execute \
  -H "Content-Type: application/json" \
  -d '{"code": "print(\"Hello, World!\")"}'
```

## Architecture

### Components

- **PythonSandboxService**: Main service implementation
- **PythonCodeValidator**: Code analysis and security checks
- **PythonEnvironmentManager**: Container environment management
- **PythonExecutionMonitor**: Resource usage tracking

### Execution Flow

1. Code validation and security checks
2. Container creation with resource limits
3. Code execution in isolated environment
4. Result collection and resource cleanup

## Best Practices

1. **Resource Management**
   - Set appropriate memory limits
   - Monitor CPU usage
   - Clean up resources promptly

2. **Security**
   - Validate all input code
   - Restrict module imports
   - Monitor execution time

3. **Error Handling**
   - Provide clear error messages
   - Handle timeouts gracefully
   - Clean up on failures

## Contributing

1. Fork the repository
2. Create feature branch
3. Commit changes
4. Create pull request

## License

Copyright (c) Aevatar. All rights reserved.