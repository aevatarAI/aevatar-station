# Host API Documentation

## Overview

The Host API provides endpoints for managing and monitoring hosts in the Aevatar platform. These endpoints require authentication.

## Base URL

```
/api/host
```

## Authentication

All endpoints require Bearer token authentication.

## Endpoints

### Get Latest Real-Time Logs

Retrieve the latest real-time logs for a specific host.

```http
GET /api/host/log
```

#### Query Parameters

- `appId` (string): The application identifier
- `hostType` (HostTypeEnum): The type of host
- `offset` (integer): The offset for pagination

#### Response

Returns a list of `HostLogIndex` objects containing the latest logs.

```json
[
  {
    // HostLogIndex properties
  }
]
```

#### Status Codes

- 200: Successful operation
- 400: Invalid request parameters
- 401: Unauthorized
- 500: Server error

## Error Handling

The API uses standard HTTP status codes for error responses. Detailed error messages are included in the response body when applicable.

## Rate Limiting

API endpoints may be subject to rate limiting based on your subscription tier. Please refer to the platform documentation for specific limits.