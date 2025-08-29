# SSE MCP Tool Calling Implementation

## Overview

The SSE MCP Client Provider implements the full Model Context Protocol specification with Server-Sent Events transport. This provides a standards-compliant implementation for SSE-based MCP servers with automatic server type detection.

## Key Features

1. **Full MCP Protocol Support**: Complete implementation of the MCP specification
2. **Auto-Detection**: Automatically detects MCP vs simple SSE servers
3. **JSON-RPC 2.0**: Standard message format for MCP communication
4. **Asynchronous Messaging**: Request/response matching with concurrent support
5. **Timeout Protection**: 30-second timeout for all operations
6. **Authorization Support**: Bearer token from URL query parameters

## Auto-Detection Mechanism

The client intelligently detects the server type during connection:

### Detection Flow

1. **Initial Connection Attempt**:
   ```csharp
   // Try MCP initialization with 10-second timeout
   var initSuccessful = await TryMcpInitializationAsync(baseUrl, authToken);
   ```

2. **Server Classification**:
   - **Success**: Server supports full MCP protocol
   - **Failure**: Server is a simple SSE API

3. **Adaptive Behavior**:
   - **MCP Servers**: Use JSON-RPC 2.0 format with method calls
   - **Simple SSE APIs**: Send arguments directly as JSON

### Benefits

- No configuration needed to specify server type
- Graceful degradation for non-MCP servers
- Maintains compatibility with both server types
- Clear logging of detected server type

## Implementation Details

### Protocol Implementation

The SSE transport follows the MCP specification exactly:

1. **Connection Initialization**:
   - Send `initialize` request via HTTP POST with JSON-RPC 2.0
   - Receive `initialize` response with server capabilities
   - Establish SSE connection for server-to-client messages
   - Send `initialized` notification to complete handshake

2. **Message Flow**:
   - Client-to-server: HTTP POST requests with JSON-RPC messages
   - Server-to-client: SSE event stream with `event: message` format
   - Asynchronous response matching using request IDs

3. **Tool Operations**:
   - Tool discovery: `tools/list` method returns available tools
   - Tool execution: `tools/call` method with `name` and `arguments` parameters
   - Standard MCP result format with content array

### SSE Event Format

The implementation processes SSE events in the standard format:
```
event: message
data: {"jsonrpc":"2.0","id":"1","result":{...}}
```

### JSON-RPC Message Format

All messages follow the JSON-RPC 2.0 specification:

**Request (Client to Server via HTTP POST):**
```json
{
    "jsonrpc": "2.0",
    "id": "1",
    "method": "tools/call",
    "params": {
        "name": "tool_name",
        "arguments": { "param1": "value1" }
    }
}
```

**Response (Server to Client via SSE):**
```json
{
    "jsonrpc": "2.0",
    "id": "1",
    "result": {
        "content": [
            {
                "type": "text",
                "text": "Tool execution result"
            }
        ]
    }
}
```

## Configuration

### SSE MCP Server Example

```json
{
  "ServerName": "example-sse-server",
  "TransportType": "sse",
  "Url": "https://example.com/api/mcp/sse?Authorization=<your-token>",
  "AutoReconnect": true,
  "ReconnectDelay": "00:00:05"
}
```

## Testing

### 1. Basic Connection Test
```csharp
// The SSE client should connect successfully
var isConnected = await mcpAgent.IsConnectedAsync("example-sse-server");
Assert.IsTrue(isConnected);
```

### 2. Tool Discovery Test
```csharp
// Should discover available tools
var tools = await mcpAgent.DiscoverToolsAsync("example-sse-server");
Assert.IsTrue(tools.Any());
```

### 3. Tool Execution Test
```csharp
// Execute a tool
var result = await mcpAgent.CallToolAsync("example-sse-server.tool_name", 
    new Dictionary<string, object> { ["param"] = "value" });
Assert.IsTrue(result.Success);
```

## Error Handling

The implementation handles various error scenarios:

1. **Connection Errors**: Returns appropriate error messages if not connected
2. **HTTP Errors**: Captures HTTP status codes and error responses
3. **Timeout Errors**: Cancels requests after 30 seconds
4. **Parsing Errors**: Falls back to text response if JSON parsing fails
5. **SSE Format Errors**: Handles malformed SSE events gracefully

## Limitations

1. **One-way Communication**: Current implementation only supports request-response pattern
2. **No Continuous Streaming**: Does not maintain persistent SSE connections for real-time events
3. **Limited Event Types**: Only processes "result" events, other event types are logged but not processed

## Future Improvements

1. **Persistent Connections**: Maintain SSE connections for real-time updates
2. **Event Handlers**: Support for different event types (error, progress, etc.)
3. **Streaming Results**: Support for partial/progressive results
4. **Better Error Recovery**: Automatic retry with exponential backoff
5. **Configuration Validation**: Validate SSE endpoint URLs and formats

## Debugging

Enable debug logging to see detailed SSE communication:

```csharp
services.AddLogging(builder => 
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

This will log:
- Request JSON payloads
- Authorization header additions
- SSE event types and data
- Parsing attempts and errors 