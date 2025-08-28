# Workflow View API Documentation

## Overview

The Workflow View API provides endpoints for managing and interacting with workflow views in the Aevatar platform. These endpoints require authentication.

## Base URL

```
/api/workflow-view
```

## Authentication

All endpoints require Bearer token authentication.

## Endpoints

### Publish Workflow

Publish a workflow by its GUID.

```http
POST /api/workflow-view/{guid}/publish-workflow
```

#### Parameters

- `guid` (path parameter): The unique identifier of the workflow (GUID format)

#### Response

Returns an `AgentDto` object containing the published workflow agent information.

```json
{
  // Agent properties and configuration
}
```

#### Status Codes

- 200: Successful operation
- 400: Invalid GUID format
- 401: Unauthorized
- 404: Workflow not found
- 500: Server error

## Error Handling

The API uses standard HTTP status codes for error responses. Detailed error messages are included in the response body when applicable.

## Rate Limiting

API endpoints may be subject to rate limiting based on your subscription tier. Please refer to the platform documentation for specific limits.