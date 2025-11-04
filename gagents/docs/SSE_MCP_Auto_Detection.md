# SSE MCP Auto-Detection Mechanism

## Overview

The SSE MCP Client Provider implements an intelligent auto-detection mechanism that automatically determines whether an SSE server follows the full Model Context Protocol (MCP) specification or is a simple SSE API.

## How It Works

### 1. Connection Process

When connecting to an SSE server, the client follows this process:

1. **Attempt MCP Initialization** (10-second timeout):
   ```csharp
   var initSuccessful = await TryMcpInitializationAsync(baseUrl, authToken);
   ```

2. **Server Classification**:
   - **Success**: Server supports full MCP protocol → Use JSON-RPC 2.0 format
   - **Failure**: Server is a simple SSE API → Use simplified communication

3. **Establish SSE Connection**:
   - Always establish SSE connection for server-to-client messages
   - For MCP servers, also send `initialized` notification

### 2. Tool Discovery

The tool discovery process adapts based on server type:

#### For MCP Servers:
```csharp
// Use standard tools/list method
var request = new JsonRpcRequest
{
    JsonRpc = "2.0",
    Id = NextRequestId(),
    Method = "tools/list",
    Params = new { }
};
```

#### For Simple SSE APIs:
```csharp
// Attempt to discover tools via GET request
var toolsUrl = baseUrl.TrimEnd('/') + "/tools";
var httpRequest = new HttpRequestMessage(HttpMethod.Get, toolsUrl);
```

The client tries common endpoints like `/tools` to discover available tools. If this fails, it returns an empty tool list.

### 3. Tool Calling

Tool calling also adapts to server type:

#### MCP Servers:
- Use JSON-RPC 2.0 format with `tools/call` method
- Include request ID for response matching
- Handle asynchronous responses via SSE

#### Simple SSE APIs:
- Send arguments directly as JSON via HTTP POST
- Process SSE response stream for results
- No JSON-RPC wrapper needed

## Benefits

1. **Zero Configuration**: No need to specify server type
2. **Graceful Degradation**: Automatically falls back to simple mode
3. **Wide Compatibility**: Works with both MCP and non-MCP SSE servers
4. **Clear Logging**: Logs detected server type for debugging

## Example Usage

```csharp
// Configuration is the same for both server types
var config = new MCPServerConfig
{
    ServerName = "my-sse-server",
    TransportType = "sse",
    Url = "https://api.example.com/sse?Authorization=token"
};

// Client automatically detects server type
var mcpAgent = serviceProvider.GetRequiredService<IMCPGAgent>();
await mcpAgent.ConnectAsync("my-sse-server");

// Works regardless of server type
var tools = await mcpAgent.DiscoverToolsAsync("my-sse-server");
var result = await mcpAgent.CallToolAsync("my-sse-server.tool_name", arguments);
```

## Error Handling

Common errors and their meanings:

1. **"The response ended prematurely"**: 
   - Server doesn't support MCP initialization
   - Client automatically falls back to simple SSE mode

2. **"Could not discover tools for simple SSE API"**:
   - Simple SSE API doesn't expose tools at expected endpoints
   - Client returns empty tool list

## Implementation Details

The auto-detection is implemented in `SSEMCPClientProvider.cs` with these key components:

1. **TryMcpInitializationAsync**: Attempts MCP handshake with timeout
2. **_isSimpleSSEApi**: Flag tracking detected server type
3. **Adaptive behavior**: Different code paths based on server type

This design ensures maximum compatibility while maintaining standards compliance for full MCP servers. 