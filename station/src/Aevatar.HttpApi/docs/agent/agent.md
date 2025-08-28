# Agent API Documentation

## Overview

The Agent API provides endpoints for managing agents in the Aevatar platform. These endpoints require authentication.

## Base URL

```
/api/agent
```

## Authentication

All endpoints require Bearer token authentication.

## Endpoints

### List Agent Types

Retrieve all available agent types.

```http
GET /api/agent/agent-type-info-list
```

#### Response

Returns a list of `AgentTypeDto` objects.

### List Agent Instances

Retrieve all agent instances based on query parameters.

```http
GET /api/agent/agent-list
```

#### Query Parameters

- Query parameters defined in `GetAllAgentInstancesQueryDto`

#### Response

Returns a list of `AgentInstanceDto` objects.

### Create Agent

Create a new agent.

```http
POST /api/agent
```

#### Request Body

```json
{
  // CreateAgentInputDto properties
}
```

#### Response

Returns an `AgentDto` object.

### Get Agent

Retrieve a specific agent by GUID.

```http
GET /api/agent/{guid}
```

#### Parameters

- `guid` (path parameter): The unique identifier of the agent

#### Response

Returns an `AgentDto` object.

### Get Agent Relationship

Retrieve relationship information for a specific agent.

```http
GET /api/agent/{guid}/relationship
```

#### Parameters

- `guid` (path parameter): The unique identifier of the agent

#### Response

Returns an `AgentRelationshipDto` object.

### Add Sub-Agent

Add a sub-agent to an existing agent.

```http
POST /api/agent/{guid}/add-subagent
```

#### Parameters

- `guid` (path parameter): The unique identifier of the parent agent

#### Request Body

```json
{
  // AddSubAgentDto properties
}
```

#### Response

Returns a `SubAgentDto` object.

### Remove Sub-Agent

Remove a sub-agent from an existing agent.

```http
POST /api/agent/{guid}/remove-subagent
```

#### Parameters

- `guid` (path parameter): The unique identifier of the parent agent

#### Request Body

```json
{
  // RemoveSubAgentDto properties
}
```

#### Response

Returns a `SubAgentDto` object.

### Remove All Sub-Agents

Remove all sub-agents from an existing agent.

```http
POST /api/agent/{guid}/remove-all-subagent
```

#### Parameters

- `guid` (path parameter): The unique identifier of the parent agent

### Update Agent

Update an existing agent.

```http
PUT /api/agent/{guid}
```

#### Parameters

- `guid` (path parameter): The unique identifier of the agent

#### Request Body

```json
{
  // UpdateAgentInputDto properties
}
```

#### Response

Returns an `AgentDto` object.

### Delete Agent

Delete an existing agent.

```http
DELETE /api/agent/{guid}
```

#### Parameters

- `guid` (path parameter): The unique identifier of the agent

### Publish Event

Publish an event through the agent system.

```http
POST /api/agent/publishEvent
```

#### Request Body

```json
{
  // PublishEventDto properties
}
```

## Status Codes

- 200: Successful operation
- 400: Invalid request
- 401: Unauthorized
- 403: Forbidden
- 404: Agent not found
- 500: Server error

## Error Handling

The API uses standard HTTP status codes for error responses. Detailed error messages are included in the response body when applicable.

## Rate Limiting

API endpoints may be subject to rate limiting based on your subscription tier. Please refer to the platform documentation for specific limits.