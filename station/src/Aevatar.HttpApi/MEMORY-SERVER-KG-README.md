# Memory Server Knowledge Graph Test Guide

## Overview

The memory-server MCP tool provides a **knowledge graph** implementation, not a simple key-value store. This guide explains how to use the knowledge graph tools with the updated test script.

## Available Tools

The memory-server provides the following knowledge graph operations:

### Entity Management
- **create_entities** - Create new entities with types and observations
- **delete_entities** - Remove entities and their relations from the graph

### Relation Management  
- **create_relations** - Create directed relationships between entities
- **delete_relations** - Remove specific relationships

### Observation Management
- **add_observations** - Add new facts/observations to existing entities
- **delete_observations** - Remove specific observations from entities

### Query Operations
- **read_graph** - Read the entire knowledge graph structure
- **search_nodes** - Search for nodes matching a query
- **open_nodes** - Get detailed information about specific nodes

## Running the Test Script

### Prerequisites

1. Set environment variables:
```bash
source ./set-env.sh
```

2. Ensure the API server is running at:
   - https://station-developer-dev-staging.aevatar.ai/tool-client/api/mcp-demo

### Execute the Test

```bash
./test-tool-calling.sh
```

## Test Workflow

The script demonstrates a complete knowledge graph workflow:

1. **Create Entities** - Creates "John Doe" (person) and "Aevatar Station" (project)
2. **Create Relations** - Establishes "works on" and "is developed by" relationships
3. **Add Observations** - Adds additional facts to entities
4. **Read Graph** - Views the complete graph structure
5. **Search Nodes** - Searches for nodes containing "Orleans"
6. **Open Nodes** - Gets detailed info about specific entities
7. **Delete Observations** - Removes specific facts
8. **Delete Relations** - Removes relationships
9. **Verify Changes** - Reads graph to confirm deletions
10. **Delete Entities** - Removes entities entirely
11. **Final Verification** - Confirms empty graph

## Example Tool Calls

### Creating Entities
```json
{
  "entities": [
    {
      "name": "John Doe",
      "entityType": "person",
      "observations": [
        "Software engineer at Aevatar",
        "Expert in Orleans framework"
      ]
    }
  ]
}
```

### Creating Relations
```json
{
  "relations": [
    {
      "from": "John Doe",
      "to": "Aevatar Station",
      "relationType": "works on"
    }
  ]
}
```

### Searching Nodes
```json
{
  "query": "Orleans"
}
```

## Understanding the Output

The knowledge graph returns structured data showing:
- **Entities** with their types and observations
- **Relations** showing connections between entities
- **Search results** matching your queries

## Important Notes

1. The knowledge graph is **in-memory only** - data is lost when the agent restarts
2. Relations should use **active voice** (e.g., "works on", not "worked on by")
3. Entity names must be unique within the graph
4. All operations are case-sensitive

## Troubleshooting

### Empty Tool List
If no tools appear after initialization, the memory server may have failed to start. Check:
- Docker is running
- Network connectivity to npm registry
- Server startup timeout (increase if needed)

### Tool Call Failures
- Ensure entity names in relations exist before creating relations
- Check JSON syntax carefully
- Verify the tool name matches exactly (e.g., "create_entities" not "createEntities")

### Authentication Issues
If you get 401 errors:
```bash
# Re-source environment variables
source ./set-env.sh

# Script will automatically get a new token
``` 