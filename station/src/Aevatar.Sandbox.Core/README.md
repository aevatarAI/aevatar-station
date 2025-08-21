# Aevatar.Sandbox.Core

Core abstractions and base implementations for the Aevatar Sandbox Execution Platform.

## Overview

This library provides the foundational components for building secure, scalable, and extensible code execution environments. It defines the core interfaces and base classes that power Aevatar's multi-language sandbox execution platform.

## Key Components

### ISandboxService

The primary interface defining sandbox operations:

```csharp
public interface ISandboxService
{
    Task<string> ExecuteAsync(SandboxExecutionRequest request);
    Task<SandboxExecutionStatus> GetStatusAsync(string executionId);
    Task<string> GetLogsAsync(string executionId);
    Task CancelAsync(string executionId);
}
```

### SandboxServiceBase

Abstract base class implementing common sandbox functionality:

- Resource management
- Network policies
- Security boundaries
- Kubernetes integration
- Execution lifecycle

### Core Models

- `SandboxExecutionRequest`
- `SandboxExecutionStatus`
- `SandboxResources`
- `SandboxSecurityPolicy`

## Features

### Resource Management

- CPU limits and requests
- Memory constraints
- Execution timeouts
- Disk space quotas

### Security

- Process isolation
- Network policies
- File system restrictions
- Resource boundaries

### Extensibility

- Language-specific implementations
- Custom resource policies
- Security policy extensions
- Execution environment customization

## Usage

1. Implement language-specific service:

```csharp
public class PythonSandboxService : SandboxServiceBase
{
    protected override Task<string> ExecuteInternalAsync(
        SandboxExecutionRequest request)
    {
        // Python-specific implementation
    }
}
```

2. Configure service:

```csharp
services.AddSandboxCore(options =>
{
    options.DefaultTimeout = TimeSpan.FromSeconds(30);
    options.MaxConcurrentExecutions = 100;
});
```

## Integration

### Kubernetes

```csharp
public class KubernetesSandboxService : SandboxServiceBase
{
    private readonly IKubernetesHostManager _k8s;

    protected override async Task<string> ExecuteInternalAsync(
        SandboxExecutionRequest request)
    {
        var job = await _k8s.CreateJobAsync(
            new JobSpecification
            {
                Image = request.Image,
                Command = request.Command,
                Resources = request.Resources
            });

        return await WaitForCompletionAsync(job);
    }
}
```

### Orleans Integration

```csharp
public class SandboxExecutionGrain : Grain, ISandboxExecutionGrain
{
    private readonly ISandboxService _sandboxService;

    public async Task<string> ExecuteAsync(
        SandboxExecutionRequest request)
    {
        return await _sandboxService.ExecuteAsync(request);
    }
}
```

## Best Practices

1. **Resource Management**
   - Always set appropriate limits
   - Monitor resource usage
   - Implement graceful degradation

2. **Security**
   - Follow principle of least privilege
   - Validate all inputs
   - Isolate execution environments

3. **Error Handling**
   - Provide detailed error messages
   - Implement proper cleanup
   - Handle timeouts gracefully

## Development

### Prerequisites

- .NET 8.0+
- Kubernetes 1.25+
- Docker

### Building

```bash
dotnet restore
dotnet build
```

### Testing

```bash
dotnet test
```

## License

Copyright (c) Aevatar. All rights reserved.