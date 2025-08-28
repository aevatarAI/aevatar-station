# Workflow API Documentation

## Overview

The Workflow API provides endpoints for generating and managing workflows in the Aevatar platform. These endpoints require authentication.

## Base URL

```
/api/workflow
```

## Authentication

All endpoints require Bearer token authentication.

## Endpoints

### Generate Workflow

Generate a new workflow based on user goals.

```http
POST /api/workflow/generate
```

#### Request Body

```json
{
  "userGoal": "string"  // User's goal description
}
```

#### Response

Returns an `AiWorkflowViewConfigDto` object containing the generated workflow configuration.

```json
{
  "properties": {
    "workflowNodeList": [
      // Array of workflow nodes
    ]
  }
  // Additional workflow configuration properties
}
```

#### Status Codes

- 200: Successful operation
- 400: Invalid request
- 401: Unauthorized
- 500: Server error

### Generate Text Completions

Generate multiple text completion options based on user input.

```http
POST /api/workflow/text-completion/generate
```

#### Request Body

```json
{
  "userGoal": "string"  // User's input text
}
```

#### Response

Returns a `TextCompletionResponseDto` containing multiple completion options.

```json
{
  "completions": [
    // Array of completion options
  ]
}
```

#### Status Codes

- 200: Successful operation
- 400: Invalid request
- 401: Unauthorized
- 500: Server error

## Error Handling

The API uses standard HTTP status codes for error responses. Detailed error messages are included in the response body when applicable.

## Rate Limiting

API endpoints may be subject to rate limiting based on your subscription tier. Please refer to the platform documentation for specific limits.