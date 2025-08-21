# Aevatar Sandbox Architecture

## Overview

The Aevatar Sandbox Platform is a secure, scalable, and extensible system for executing untrusted code in isolated environments. It supports multiple programming languages through a unified abstraction layer and provides comprehensive monitoring, resource management, and security controls.

## Architecture Diagrams

### Component Architecture

```mermaid
graph TB
    subgraph "API Layer"
        A["Aevatar.Sandbox.HttpApi.Host"]
        A1["execute/result/cancel/logs"]
    end

    subgraph "Core Layer"
        B["SandboxServiceBase"]
        B1["ISandboxService"]
        B2["Resource/Network Policies"]
    end

    subgraph "Language Adapters"
        C1["PythonSandboxService"]
        C2["CSharpSandboxService"]
        C3["RustSandboxService"]
        C4["GoSandboxService"]
    end

    subgraph "Execution Layer"
        D["SandboxExecutionGAgent"]
        D1["ISandboxExecutionClientGrain"]
        D2["State Machine & Events"]
    end

    subgraph "Infrastructure"
        E["KubernetesHostManager"]
        E1["Kubernetes Cluster"]
        E2["Orleans Streaming"]
    end

    A --> B1
    B1 --> B
    B --> B2
    B --> C1
    B --> C2
    B --> C3
    B --> C4
    A --> D
    D --> D1
    D --> D2
    D1 --> E
    C1 --> E
    C2 --> E
    C3 --> E
    C4 --> E
    E --> E1
    E --> E2
```

### Execution Flow

```mermaid
sequenceDiagram
    participant Client
    participant API as Sandbox.HttpApi.Host
    participant Agent as SandboxExecutionGAgent
    participant Service as SandboxService
    participant K8s as KubernetesHostManager
    participant Pod as Sandbox Pod

    Client->>API: Execute Code Request
    API->>Agent: Forward Request
    Agent->>Agent: Validate & Queue
    Agent->>Service: Execute Code
    Service->>K8s: Create Job/Pod
    K8s->>Pod: Run Code
    Pod-->>K8s: Execution Result
    K8s-->>Service: Job Status
    Service-->>Agent: Execution Complete
    Agent-->>API: Return Result
    API-->>Client: Response
```

## Core Components

### 1. API Layer (Aevatar.Sandbox.HttpApi.Host)

The API layer provides a unified HTTP interface for all sandbox operations:

- Code execution
- Status monitoring
- Log retrieval
- Execution cancellation

### 2. Core Layer (Aevatar.Sandbox.Core)

The core layer defines the fundamental abstractions and implementations:

- ISandboxService interface
- SandboxServiceBase abstract class
- Resource management policies
- Security boundaries

### 3. Language Adapters

Language-specific implementations extending SandboxServiceBase:

- PythonSandboxService
- CSharpSandboxService (planned)
- RustSandboxService (planned)
- GoSandboxService (planned)

### 4. Execution Layer

Orleans-based execution coordination:

- SandboxExecutionGAgent
- State machine management
- Event sourcing
- Concurrency control

### 5. Infrastructure Layer

Kubernetes-based container orchestration:

- KubernetesHostManager
- Resource quotas
- Network policies
- Container lifecycle

## Security Architecture

### 1. Container Isolation

- Dedicated namespace per execution
- Resource limits and quotas
- Network policy enforcement
- Read-only root filesystem

### 2. Code Validation

- Static analysis
- Banned import detection
- Resource usage estimation
- Security policy validation

### 3. Runtime Controls

- CPU limits
- Memory constraints
- Disk quotas
- Network restrictions
- Execution timeouts

### 4. Access Control

- API authentication
- Rate limiting
- Audit logging
- Resource authorization

## Resource Management

### 1. Compute Resources

```yaml
resources:
  limits:
    cpu: "100m"
    memory: "256Mi"
  requests:
    cpu: "50m"
    memory: "128Mi"
```

### 2. Network Policies

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: sandbox-isolation
spec:
  podSelector:
    matchLabels:
      role: sandbox
  policyTypes:
  - Ingress
  - Egress
  ingress: []
  egress: []
```

### 3. Storage

```yaml
spec:
  volumes:
  - name: tmp
    emptyDir:
      medium: Memory
      sizeLimit: "64Mi"
```

## Monitoring and Observability

### 1. Metrics

- Execution duration
- Resource utilization
- Error rates
- Queue length
- Latency distribution

### 2. Logging

- Execution logs
- System events
- Security alerts
- Audit trail

### 3. Tracing

- Request flow
- Component interactions
- Performance bottlenecks
- Error propagation

## Local Development Setup

### Prerequisites

1. Install required tools:
   - Docker
   - Kubernetes (or K3d/Kind)
   - .NET 8.0+
   - Orleans tools

2. Clone repositories:
   ```bash
   git clone https://github.com/aevatar/sandbox.git
   ```

3. Build projects:
   ```bash
   dotnet restore
   dotnet build
   ```

### Local Testing

1. Start local Kubernetes cluster:
   ```bash
   k3d cluster create sandbox-dev
   ```

2. Deploy sandbox components:
   ```bash
   kubectl apply -f k8s/
   ```

3. Run integration tests:
   ```bash
   dotnet test
   ```

## Deployment Architecture

### 1. Production Environment

- Multiple Kubernetes clusters
- Load balancing
- Auto-scaling
- High availability

### 2. Scaling Strategy

- Horizontal pod autoscaling
- Cluster autoscaling
- Queue-based load management
- Resource optimization

### 3. Disaster Recovery

- Multi-region deployment
- Backup and restore
- Failover procedures
- Data retention

## Future Enhancements

1. **Language Support**
   - Add support for more languages
   - Improve language-specific optimizations
   - Enhanced runtime controls

2. **Security**
   - Advanced code analysis
   - Enhanced isolation
   - Vulnerability scanning
   - Runtime protection

3. **Performance**
   - Warm pool management
   - Resource prediction
   - Cache optimization
   - Request batching

4. **Monitoring**
   - Advanced analytics
   - Predictive alerts
   - Cost optimization
   - Performance insights

## License

Copyright (c) Aevatar. All rights reserved.