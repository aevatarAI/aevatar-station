# Aevatar.Neo4JStore

This project provides Neo4j graph database integration for the Aevatar platform.

## Overview

`Aevatar.Neo4JStore` enables graph-based data storage and querying for Aevatar, leveraging Neo4j's capabilities for handling complex relationships and network structures. It's particularly useful for social networks, recommendation systems, and other relationship-rich data models.

## Key Features

- Neo4j driver integration
- Graph data modeling
- Cypher query abstractions
- Transaction management
- Entity-to-graph mapping
- Relationship traversal utilities
- Performance optimizations for graph queries

## Dependencies

The project leverages the following dependencies:

- `Neo4j.Driver` - Official Neo4j .NET driver
- `Volo.Abp.AutoMapper` - Object mapping utilities

## Data Model

The Neo4j data model focuses on entities (nodes) and relationships (edges) between them, with nodes representing domain entities and edges representing various relationships between entities. The model is optimized for traversing relationships and finding patterns in the data.

## Connection Configuration

The Neo4j connection is configured in the host project's `appsettings.json`:

```json
{
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "password"
  }
}
```

## Project Structure

- **Clients/**: Neo4j client implementations
- **Repositories/**: Graph-based repository implementations
- **Mapping/**: Entity-to-graph mapping configurations
- **Queries/**: Cypher query builders and executors
- **Extensions/**: Neo4j-specific extension methods

## Usage

This library can be used to:

1. Store and retrieve graph-based data
2. Query complex relationships efficiently
3. Perform graph traversals and pattern matching
4. Execute graph algorithms for analytics

Example usage:

```csharp
// Inject the Neo4j graph client
private readonly INeo4jGraphClient _graphClient;

public UserRelationshipService(INeo4jGraphClient graphClient)
{
    _graphClient = graphClient;
}

// Use the client to execute graph operations
public async Task<IEnumerable<UserDto>> GetUserConnectionsAsync(Guid userId, int depth = 2)
{
    var cypher = _graphClient.CreateQuery()
        .Match("(user:User)-[*1..{depth}]-(connection:User)")
        .Where("user.Id = $userId")
        .Return("DISTINCT connection")
        .WithParams(new { userId = userId.ToString(), depth });
        
    return await _graphClient.ExecuteQueryAsync<UserDto>(cypher);
}
```

## Performance Considerations

- Indexes are created for node properties that are frequently queried
- Relationships are modeled to optimize common traversal patterns
- Query planning is considered for complex graph operations
- Batching is used for bulk operations

## Logging

Graph operations log important database operations and errors at appropriate checkpoints to facilitate debugging and monitoring. 