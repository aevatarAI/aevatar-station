# Aevatar.Domain.Grains

## Overview

The `Aevatar.Domain.Grains` project is a core component of the Aevatar platform, responsible for implementing domain-specific Orleans grain interfaces and classes. It provides Orleans-based distributed computing capabilities for the Aevatar domain model.

## Purpose

This project implements domain logic as Orleans grains, which are distributed virtual actors that enable:
- Scalable state management
- Fault tolerance
- Concurrency control
- Event sourcing patterns

## Dependencies

### NuGet Packages
- **Microsoft.Orleans.Sdk**: Core SDK for Orleans distributed virtual actor framework
- **AElf.OpenTelemetry**: Provides distributed tracing and metrics for the Orleans grains
- **Volo.Abp.Ddd.Domain**: Domain-driven design components from the ABP Framework

### Project References
- **Aevatar.Domain.Shared**: Contains shared domain models, interfaces, and events

## Components

### Module Interface
- **IDomainGrainsModule**: Interface for the domain grains module

### Subscription
- **SubscribeEventInputDto**: Data transfer object for event subscription
  - Contains agent ID, event types, callback URL, and user ID
  - Uses Orleans' `[GenerateSerializer]` attribute for efficient serialization

## Orleans Features Used

- **Serialization**: Uses Orleans' built-in serialization with `[GenerateSerializer]` and `[Id]` attributes
- **Grain Interfaces**: Defines grain interfaces for domain objects
- **State Management**: Handles persistent state for domain entities

## Development Guidelines

### Adding New Grains

1. Define grain interfaces in the appropriate domain subdirectory
2. Implement the grain state class with proper serialization attributes
3. Implement the grain class with appropriate state management

### Serialization

All DTOs that need to be serialized for Orleans should:
1. Include the `[GenerateSerializer]` attribute on the class
2. Mark each property with `[Id(n)]` where `n` is a sequential number

### Event Sourcing

When implementing event sourcing in grains:
1. Define events in the Domain.Shared project
2. Apply events to update grain state
3. Use appropriate event storage providers

## Usage Example

```csharp
// Subscription example
var subscriptionInput = new SubscribeEventInputDto
{
    AgentId = agentId,
    EventTypes = new List<string> { "AgentCreated", "AgentUpdated" },
    CallbackUrl = "https://example.com/api/callback",
    UserId = userId
};
```

## Integration

This project is designed to be used with the Orleans hosting environment and integrates with other Aevatar platform components through:
- Domain events
- Shared interfaces
- ABP Framework modules 