# MCP Server Controller API Examples

## Overview
The MCP Server Controller provides complete CRUD operations for managing MCP (Model Context Protocol) servers. All endpoints require appropriate permissions.

## Base URL
```
/api/mcp/servers
```

## Authentication & Authorization
All endpoints require authentication and specific MCP server permissions:
- `AevatarPermissions.McpServers.Default` - View servers
- `AevatarPermissions.McpServers.Create` - Create servers
- `AevatarPermissions.McpServers.Edit` - Update servers
- `AevatarPermissions.McpServers.Delete` - Delete servers

## API Examples

### 1. Create a new MCP server

```http
POST /api/mcp/servers
Content-Type: application/json
Authorization: Bearer {token}

{
  "serverName": "example-server",
  "command": "python",
  "args": ["-m", "mcp_server", "--port", "3000"],
  "env": {
    "NODE_ENV": "production",
    "MCP_PORT": "3000"
  },
  "description": "Example MCP server for demonstration",
  "url": "http://localhost:3000"
}
```

**For StdIO servers (no URL needed):**
```http
POST /api/mcp/servers
Content-Type: application/json
Authorization: Bearer {token}

{
  "serverName": "stdio-server",
  "command": "node",
  "args": ["mcp-server.js"],
  "env": {
    "NODE_ENV": "production"
  },
  "description": "Example StdIO MCP server"
}
```

### 2. Get all servers with pagination and filtering

**Basic pagination request:**
```http
GET /api/mcp/servers?pageNumber=1&maxResultCount=10&sorting=serverName asc
Authorization: Bearer {token}
```

**Advanced filtering and pagination:**
```http
GET /api/mcp/servers?searchTerm=example&serverType=StreamableHttp&pageNumber=2&maxResultCount=20&sorting=command desc
Authorization: Bearer {token}
```

**Filter by Stdio servers:**
```http
GET /api/mcp/servers?serverType=Stdio&pageNumber=1&maxResultCount=10&sorting=createdAt desc
Authorization: Bearer {token}
```

**Using traditional pagination parameters:**
```http
GET /api/mcp/servers?skipCount=20&maxResultCount=10&sorting=description asc
Authorization: Bearer {token}
```

### 3. Get a specific server

```http
GET /api/mcp/servers/example-server
Authorization: Bearer {token}
```

### 4. Update an existing server

```http
PUT /api/mcp/servers/example-server
Content-Type: application/json
Authorization: Bearer {token}

{
  "command": "node",
  "args": ["server.js"],
  "description": "Updated MCP server description",
  "env": {
    "NODE_ENV": "development"
  }
}
```

### 5. Delete a server

```http
DELETE /api/mcp/servers/example-server
Authorization: Bearer {token}
```

### 6. Get all server names

```http
GET /api/mcp/servers/names
Authorization: Bearer {token}
```

### 7. Get raw configurations (for backward compatibility)

```http
GET /api/mcp/servers/raw
Authorization: Bearer {token}
```

## Data Types

### Server Type Detection
Server type is automatically determined based on the URL field:
- **Stdio**: When `url` is null or empty - communication through standard input/output
- **StreamableHttp**: When `url` is provided - communication through network protocols (SSE/WebSocket/HTTP)

### Server Configuration Structure
```json
{
  "serverName": "string",           // Unique identifier for the server (max 50 chars)
  "command": "string",              // Executable command (max 20 chars, e.g., "python", "node")
  "args": ["string[]"],             // Command line arguments
  "env": {"key": "value"},          // Environment variables as key-value pairs
  "description": "string",          // Human-readable description (max 1000 chars)
  "url": "string (optional)",       // Server URL (null/empty = Stdio, present = StreamableHttp)
  "serverType": "string",           // Auto-calculated: "Stdio" or "StreamableHttp" (read-only)
  "createdAt": "datetime",          // Creation timestamp
  "modifiedAt": "datetime"          // Last modification timestamp (optional)
}
```

### Field Mapping to MCPServerConfig
This API directly maps to the underlying `MCPServerConfig` structure:
- ✅ `ServerName` → `serverName`
- ✅ `Command` → `command` 
- ✅ `Args` → `args`
- ✅ `Env` → `env`
- ✅ `Description` → `description`
- ✅ `Url` → `url`
- ❌ `Type` field removed - server type is determined by URL presence

## Pagination and Sorting Features

### Pagination Parameters
- **pageNumber**: Page number (1-based, automatically calculates skipCount)
- **maxResultCount**: Page size (1-100, default 10)
- **skipCount**: Number of records to skip (traditional pagination method)

### Sorting Parameters
- **sorting**: Sort field and direction, format: "fieldName direction"
- **Supported sort fields**:
  - `serverName` - Server name
  - `command` - Command
  - `description` - Description
  - `serverType` - Server type
  - `createdAt` - Creation date
  - `modifiedAt` - Modification date
- **Sort directions**: `asc` (ascending, default) or `desc` (descending)

### Pagination Response Structure
```json
{
  "totalCount": 100,           // Total number of records
  "items": [                   // Current page data
    {
      "serverName": "example",
      "command": "python",
      // ... other fields
    }
  ]
}
```

### Sorting Examples
```http
# Sort by server name ascending
GET /api/mcp/servers?sorting=serverName asc

# Sort by creation date descending
GET /api/mcp/servers?sorting=createdAt desc

# Sort by command ascending (default direction)
GET /api/mcp/servers?sorting=command
```

## Error Responses

The API returns standard HTTP status codes and user-friendly error messages:

- `400 Bad Request` - Invalid input data or validation errors
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Server not found
- `409 Conflict` - Server name already exists
- `500 Internal Server Error` - Server processing error

Example error response:
```json
{
  "error": {
    "message": "MCP server 'example-server' already exists",
    "details": null
  }
}
```

## Validation Rules

### Field Validation
- **Server Name**: Required, 1-50 characters
- **Command**: Required, 1-20 characters
- **Description**: Optional, max 1000 characters
- **URL**: Optional, must be valid URL format
- **Args**: Optional string array
- **Env**: Optional key-value pairs dictionary

### Pagination Validation
- **Page Size**: Must be between 1 and 100
- **Skip Count**: Cannot be negative
- **Page Number**: Must be greater than 0

### Business Rules
- Server names must be unique within the system
- Command path must be valid and accessible
- URL format must comply with standard URL specifications

## Best Practices

1. **Unique Server Names**: Each server must have a unique name within the system.

2. **Command Validation**: Ensure the command path is valid and accessible.

3. **Environment Variables**: Use environment variables for configuration that might change between environments.

4. **Server Types**: The system automatically determines server type:
   - **Stdio**: For process-based servers (no URL required)
   - **StreamableHttp**: For network-based servers (URL required) supporting SSE, WebSocket, or HTTP protocols

5. **Pagination**: Use reasonable page sizes (10-50) for optimal performance. Consider using sorting for consistent results.

6. **Error Handling**: Always handle potential errors gracefully in your client applications.

7. **Security**: Ensure proper authentication and authorization for all API calls.