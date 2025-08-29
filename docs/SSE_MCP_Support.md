# SSE (Server-Sent Events) MCP Support

## Current Status

The SSE MCP Client Provider provides a **complete, standards-compliant implementation** of the Model Context Protocol with Server-Sent Events transport, featuring automatic server type detection.

### What Works

1. **Full Protocol Support**: Complete implementation of MCP specification for SSE transport
2. **Auto-Detection**: Automatically detects whether a server requires MCP initialization
3. **Graceful Fallback**: If MCP initialization fails, treats the server as a simple SSE API
4. **Tool Discovery**: Dynamic tool discovery for MCP servers, predefined tools for simple APIs
5. **Tool Execution**: Full support for tool calling with proper request/response handling
6. **Connection Management**: Robust connection handling with auto-reconnection support
7. **Authorization**: Bearer token extraction from URL query parameters

## Implementation Details

The `SSEMCPClientProvider` implements the complete MCP specification with intelligent auto-detection:

### Auto-Detection Mechanism

When connecting to an SSE server, the client automatically:

1. **Attempts MCP Initialization** (10-second timeout):
   - Sends standard `initialize` request via HTTP POST
   - If successful: treats as full MCP server
   - If failed: treats as simple SSE API

2. **Connection Flow**:
   - Try MCP initialization first
   - On failure, fall back to simple SSE mode
   - Establish SSE connection for server messages
   - For MCP servers, send `initialized` notification

### Transport Architecture

1. **Dual Transport Channels**:
   - Client-to-server: HTTP POST with JSON-RPC 2.0 messages
   - Server-to-client: SSE event stream

2. **Message Format** (MCP servers):
   ```json
   {
       "jsonrpc": "2.0",
       "id": "1",
       "method": "tools/call",
       "params": {
           "name": "tool_name",
           "arguments": { ... }
       }
   }
   ```

3. **Simple SSE API Format**:
   - Direct POST of arguments as JSON
   - SSE response processing for results

## SSE Server Configuration Example

```json
{
  "ServerName": "example-sse-server",
  "TransportType": "sse",
  "Url": "https://example.com/api/mcp/sse?Authorization=<your-token>",
  "AutoReconnect": true,
  "ReconnectDelay": "00:00:05"
}
```

The client will automatically detect whether the server is a full MCP server or a simple SSE API.

## Error Resolution

### "The response ended prematurely" Error

This error typically occurs when:
- The server doesn't support MCP initialization
- The server is a simple SSE API (not full MCP)

**Solution**: The client automatically handles this by falling back to simple SSE mode. No configuration changes needed.

## Usage

```csharp
// Configure the SSE server
var config = new MCPServerConfig
{
    ServerName = "my-sse-server",
    TransportType = "sse",
    Url = "https://api.example.com/sse?Authorization=token"
};

// The client will auto-detect the server type
var mcpAgent = serviceProvider.GetRequiredService<IMCPGAgent>();
var isConnected = await mcpAgent.IsConnectedAsync("my-sse-server");

// Works with both MCP and simple SSE servers
var tools = await mcpAgent.DiscoverToolsAsync("my-sse-server");
var result = await mcpAgent.CallToolAsync("my-sse-server.tool_name", arguments);
```