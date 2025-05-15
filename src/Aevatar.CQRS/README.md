# Aevatar.CQRS

## Overview

The Aevatar.CQRS module provides command and query responsibility segregation functionality for the Aevatar platform. It serves as a bridge between application state and persistence, primarily leveraging Elasticsearch for storing and querying state data. This module implements the CQRS pattern to separate the read and write operations, allowing for optimized querying capabilities and efficient state persistence.

## Core Features

- **Command Processing**: Handles state changes through commands, batching updates for efficient persistence
- **Query Operations**: Provides flexible query capabilities against the Elasticsearch indexes
- **State Projection**: Projects application state to Elasticsearch indexes for efficient querying
- **Batch Processing**: Optimizes updates by batching commands to reduce I/O operations
- **Versioning**: Maintains state versioning to ensure data consistency

## Architecture

### Module Structure

The `AevatarCQRSModule` is an ABP module that configures the necessary services:

- Registers command and query handlers
- Configures Elasticsearch client
- Sets up the state projector
- Registers providers and services
- Configures metrics for monitoring

### Key Components

1. **Command DTOs**
   - `SaveStateCommand`: Represents a state update with versioning
   - `SaveStateBatchCommand`: Groups multiple state commands for batch processing

2. **Query DTOs**
   - `GetStateQuery`: Defines query parameters for retrieving state data

3. **Service Interfaces**
   - `IIndexingService`: Manages Elasticsearch indexes and document operations
   - `ICQRSProvider`: Provides high-level query operations

4. **Implementations**
   - `ElasticIndexingService`: Handles Elasticsearch operations for state persistence
   - `CQRSProvider`: Implements query operations through MediatR
   - `AevatarStateProjector`: Manages state projection to Elasticsearch

5. **Handlers**
   - `SaveStateBatchCommandHandler`: Processes batched state changes
   - `GetStateQueryHandler`: Processes query requests

6. **Monitoring**
   - `MetricsElasticIndexingService`: Provides metrics and monitoring for Elasticsearch operations

## Technical Details

### Dependencies

- **Aevatar.Core**: Core abstractions and domain primitives
- **Aevatar.Core.Abstractions**: Shared interfaces and abstractions
- **Aevatar.EventSourcing.Core**: Event sourcing functionality
- **Elastic.Clients.Elasticsearch**: Elasticsearch client for .NET
- **Volo.Abp.AutoMapper**: Object mapping capabilities
- **MediatR**: Mediator pattern implementation for command and query handling

### Elasticsearch Integration

The module uses Elasticsearch for state storage with the following features:

- Dynamic index creation based on state types
- Versioned document updates to maintain data consistency
- Bulk operations for efficient updates
- Query capabilities using the Elasticsearch Query DSL

### State Projection Process

1. State changes are captured through `ProjectAsync<T>()` method
2. Changes are collected in a concurrent dictionary
3. Periodically or when thresholds are met, changes are flushed to Elasticsearch
4. Batch processing optimizes the update operations

### Query Execution Flow

1. Queries are created using the Elasticsearch Query DSL
2. Queries are processed through MediatR to the appropriate handler
3. The handler uses the indexing service to execute the query against Elasticsearch
4. Results are returned as JSON or mapped to DTOs

## Configuration

The module uses the following configuration sections:

- **ElasticUris**: Configures Elasticsearch connection URIs
- **ProjectorBatch**: Controls batch processing parameters
  - BatchSize: Maximum number of commands per batch
  - BatchTimeoutSeconds: Maximum time before flushing
  - FlushMinPeriodInMs: Minimum period between flush operations

## Usage Examples

### Querying State Data

```csharp
// Example: Query agent state by ID
var agentState = await _cqrsProvider.QueryAgentStateAsync("agentState", agentId);

// Example: Custom query
var customQuery = new Action<QueryDescriptor<dynamic>>(q =>
    q.Term(t => t.Field("property").Value("value")));
var results = await _cqrsProvider.QueryStateAsync("indexName", customQuery, 0, 10);
```

### State Projection

```csharp
// State changes are automatically projected when using Orleans grains
// The state projector handles the persistence to Elasticsearch

// Manual flushing if needed
await _stateProjector.FlushAsync();
```

## Performance Considerations

- State updates are batched to minimize I/O operations
- Versioning ensures consistency without unnecessary updates
- Elasticsearch indexes are optimized for query performance
- Metrics provide visibility into system performance

## Error Handling

The module implements comprehensive error handling with:

- Logging of failures at appropriate levels
- Retry mechanisms for transient failures
- Monitoring through metrics
- Exception propagation to calling code when necessary 