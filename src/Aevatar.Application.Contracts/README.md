# Aevatar.Application.Contracts - Technical Documentation

## Overview

Aevatar.Application.Contracts is a crucial infrastructure module that defines the contract interfaces, DTOs (Data Transfer Objects), and permissions for the Aevatar platform. It serves as the bridge between the presentation layer and the application layer, establishing a clear API contract that different components of the system can rely on.

## Purpose & Role

This module:
- Defines interface contracts for application services
- Contains DTOs that transfer data between the presentation and application layers
- Establishes permission definitions and authorization rules
- Provides SignalR communication contracts
- Contains CQRS query definitions
- Defines Dapr integration points

## Project Structure

```
src/Aevatar.Application.Contracts/
├── Account/ - Account-related DTOs and interfaces
├── ApiKeys/ - API key management contracts
├── ApiRequests/ - API request DTOs
├── Contract/ - Core contract definitions
├── Dapr/ - Dapr integration interfaces
├── Exceptions/ - Exception definitions
├── Notification/ - Notification service contracts
├── Organizations/ - Organization management DTOs and interfaces
├── Permissions/ - Permission definitions and providers
├── Projects/ - Project management DTOs and interfaces
├── Query/ - CQRS query DTOs and interfaces
├── Schema/ - Schema definition contracts
├── Sender/ - Message sender interfaces
├── SignalR/ - SignalR message contracts
└── User/ - User-related DTOs and interfaces
```

## Key Components

### Application Contracts Module

- **AevatarApplicationContractsModule.cs**: The entry point for the module that configures all dependencies and extension points. It depends on domain shared modules and ABP framework contract modules.

### Data Transfer Objects

The module contains numerous DTOs that facilitate data exchange between layers:
- **Projects/ProjectDto**: Project information transfer
- **Organizations/OrganizationDto**: Organization data structure
- **Query/AgentEventLogsDto**: Agent event log data structure
- **Query/AgentStateDto**: Agent state information

### Service Interfaces

Defines contracts for application services that implement business logic:
- **Projects/IProjectService**: Project management operations
- **Organizations/IOrganizationPermissionService**: Organization permission management
- **SignalR/IHubService**: SignalR hub communication
- **Dapr/IDaprProvider**: Dapr integration services

### Permissions

- **AevatarPermissions.cs**: Defines permission constants for different functional areas
- **AevatarPermissionDefinitionProvider.cs**: Registers and organizes permissions in the permission system

### SignalR Communication

- **ISignalRMessage.cs**: Base interface for all SignalR messages
- **SignalRMessage/ namespace**: Contains specific message type implementations

## Dependencies

The project relies on several key libraries and modules:

### Project References
- **Aevatar.Domain.Grains**: Orleans grain interfaces
- **Aevatar.Domain.Shared**: Shared domain models and constants

### External Dependencies
- **Microsoft.Orleans.Streaming**: Orleans stream processing
- **NJsonSchema**: JSON Schema generation and validation
- **Volo.Abp.Dapr**: Dapr integration for ABP
- **Volo.Abp.AspNetCore.Mvc.Dapr.EventBus**: Dapr event bus integration
- **Volo.Abp.ObjectExtending**: Object extension capabilities
- **Volo.Abp.Account/Identity/PermissionManagement.Application.Contracts**: ABP framework modules
- **AElf.Client/Types/Sdk.CSharp**: AElf blockchain integration
- **Elastic.Clients.Elasticsearch**: Elasticsearch client for search capabilities

## Integration Points

- **ABP Module System**: Integrates with the ABP modular architecture
- **Orleans Grains**: Defines contracts for the distributed Orleans system
- **CQRS Pattern**: Contains query definitions for the CQRS implementation
- **SignalR**: Defines message contracts for real-time communication
- **Dapr**: Contains interfaces for Dapr service invocation and pub/sub

## Extension Points

The module provides several extension points:
- **DTO Extensions**: Through ABP's object extending system
- **Permission Definitions**: Through ABP's permission management system
- **Contract Interfaces**: Can be implemented by different service providers

## Usage in the Aevatar Platform

The Application.Contracts module is referenced by:
1. **API Layer**: To define endpoints based on the contracts
2. **Client SDK**: To facilitate remote API calls
3. **Application Layer**: To implement the defined interfaces
4. **HttpApi.Client**: To generate client proxies

## Best Practices

When working with this module:
1. Keep interfaces focused and cohesive, following ISP (Interface Segregation Principle)
2. Design DTOs to be serialization-friendly
3. Organize permissions logically by functional areas
4. Version contracts appropriately when making breaking changes
5. Use meaningful namespaces for better organization

## Testing Approach

This module primarily contains interfaces and DTOs that are typically not tested directly. Instead, their implementations in the Application layer and their consumption in the API layer are tested. 