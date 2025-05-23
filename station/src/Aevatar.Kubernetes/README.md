# Aevatar.Kubernetes

This project provides Kubernetes integration for the Aevatar platform, enabling deployment and management of Aevatar services in Kubernetes environments.

## Overview

`Aevatar.Kubernetes` provides abstractions and utilities for interacting with Kubernetes clusters, simplifying deployment, scaling, and management of Aevatar services. It's built as a reusable library that can be referenced by deployment tools and CI/CD pipelines.

## Key Features

- Kubernetes resource management (pods, deployments, services)
- Deployment orchestration
- Configuration injection
- Service discovery
- Health monitoring
- Resource scaling
- Template-based configuration

## Dependencies

The project leverages the following key dependencies:

- `KubernetesClient` - .NET client for Kubernetes API
- `Aevatar.WebHook.Deploy` - WebHook deployment utilities
- `Aevatar.Domain.Shared` - Shared domain models and constants

## Templates

The project includes template files for:

- Application settings (`AppConfigTemplate/appsettings-template.json`)
- Logging configuration (`AppConfigTemplate/filebeat-template.yml`)

## Project Structure

- **Clients/**: Kubernetes API client implementations
- **Models/**: Kubernetes resource models
- **Services/**: Kubernetes operation services
- **Utilities/**: Helper functions for Kubernetes interactions
- **AppConfigTemplate/**: Configuration templates

## Usage

This library can be used to:

1. Deploy Aevatar services to Kubernetes clusters
2. Update existing deployments
3. Scale services based on demand
4. Monitor service health
5. Manage configuration across environments

## Configuration

When using this library, configure Kubernetes connection settings:

```csharp
var kubernetesOptions = new KubernetesOptions
{
    ApiServer = "https://kubernetes.default.svc",
    Namespace = "aevatar",
    // Additional configuration
};

services.Configure<KubernetesOptions>(options => 
{
    options.ApiServer = kubernetesOptions.ApiServer;
    options.Namespace = kubernetesOptions.Namespace;
    // Copy other properties
});
```

## Logging

The library logs important operations and errors at appropriate checkpoints to facilitate debugging and monitoring.

## Best Practices

- Use namespaces to isolate different environments (dev, staging, prod)
- Leverage Kubernetes secrets for sensitive configuration
- Implement health checks for all services
- Configure appropriate resource limits and requests 