# Aevatar.Sandbox.HttpApi.Host

A unified HTTP API service for managing and executing code in isolated sandbox environments across multiple programming languages.

## Features

- **Multi-language Support**: Execute code in Python, C#, Rust, Go (extensible)
- **Secure Execution**: Isolated sandbox environments with resource limits
- **RESTful API**: Simple and consistent API for all operations
- **Real-time Monitoring**: Track execution status and logs
- **Kubernetes Integration**: Leverages Kubernetes for container orchestration

## API Endpoints

### Code Execution

```http
POST /api/sandbox/execute
Content-Type: application/json

{
    "language": "python",
    "code": "print('Hello, World!')",
    "timeout": 30,
    "resources": {
        "cpu": "100m",
        "memory": "128Mi"
    }
}
```

### Execution Status

```http
GET /api/sandbox/status/{executionId}
```

### Execution Logs

```http
GET /api/sandbox/logs/{executionId}
```

### Cancel Execution

```http
POST /api/sandbox/cancel/{executionId}
```

## Configuration

```json
{
  "Sandbox": {
    "DefaultTimeout": 30,
    "MaxConcurrentExecutions": 100,
    "Resources": {
      "DefaultCpuLimit": "100m",
      "DefaultMemoryLimit": "128Mi",
      "MaxCpuLimit": "1000m",
      "MaxMemoryLimit": "512Mi"
    }
  }
}
```

## Architecture

The service is built on:
- ASP.NET Core for HTTP API
- Orleans for distributed coordination
- Kubernetes for container orchestration
- Event sourcing for execution tracking

## Security

- Isolated execution environments
- Resource limits and timeouts
- Network restrictions
- Input validation and sanitization

## Monitoring

- Execution metrics (duration, resource usage)
- Error rates and types
- Queue length and latency
- Resource utilization

## Dependencies

- .NET 8.0+
- Orleans 7.0+
- Kubernetes 1.25+
- Docker

## Getting Started

1. Install dependencies:
```bash
dotnet restore
```

2. Configure settings in `appsettings.json`

3. Run the service:
```bash
dotnet run
```

## Development

1. Clone the repository
2. Set up Kubernetes cluster or use local K3d/Kind
3. Configure connection strings
4. Run tests:
```bash
dotnet test
```

## License

Copyright (c) Aevatar. All rights reserved.