# SSE MCP Auto-Detection Implementation

This document describes the auto-detection mechanism implemented in the SSE MCP client provider that automatically differentiates between full MCP-compliant servers and simple SSE API services.

## Overview

The SSE MCP client implementation now includes intelligent auto-detection that determines whether a server follows the full Model Context Protocol (MCP) specification or is a simple SSE API. This allows seamless support for both types of services without requiring special configuration.

## How It Works

### Connection Phase

When connecting to an SSE server, the client:

1. **Attempts MCP Initialization** - Sends a standard MCP `initialize` request with a 10-second timeout
2. **Evaluates Response**:
   - **Success**: Server responds with valid MCP initialization → Treat as full MCP server
   - **Timeout/Error**: No response or error → Treat as simple SSE API
3. **Sets Mode**: Updates internal `_isSimpleSSEApi` flag based on detection result

### Key Implementation Details

```csharp
// Attempt MCP initialization with timeout
try 
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    var initResponse = await SendRequestAsync<InitializeResult>(initRequest, cts.Token);
    
    if (initResponse != null)
    {
        // Full MCP server - continue with standard flow
        _isSimpleSSEApi = false;
        // Send initialized notification...
    }
}
catch (Exception ex)
{
    // Simple SSE API - no MCP initialization needed
    _isSimpleSSEApi = true;
    _logger.LogInformation("Server does not support MCP initialization, treating as simple SSE API");
}
```

### Communication Differences

Based on the detection result, the client adapts its behavior:

#### Full MCP Servers
- Uses JSON-RPC 2.0 protocol
- Maintains request/response correlation with IDs
- Supports full MCP method calls (tools/list, tools/call, etc.)
- Handles structured responses with result/error fields

#### Simple SSE APIs  
- Sends direct JSON payloads without JSON-RPC wrapper
- Does not expect correlated responses
- Receives results as SSE events
- No method routing - direct tool invocation

### Tool Discovery

For simple SSE APIs, the client uses a simplified tool discovery approach:

1. **URL Pattern Extraction**: Attempts to extract tool name from the URL path
   - Pattern: `/api/mcp/{tool_name}/sse` → tool name is extracted
   - Example: `https://open.bigmodel.cn/api/mcp/web_search/sse` → tool is `web_search`

2. **Fallback Options**:
   - Check for tools discovered from SSE event stream
   - Use predefined tools from configuration
   - Return empty tool list if no discovery method succeeds

### Benefits

1. **Zero Configuration** - No need to specify server type in configuration
2. **Backward Compatible** - Existing MCP servers continue to work without changes  
3. **Vendor Agnostic** - No hardcoded vendor-specific logic
4. **Transparent Operation** - Applications don't need to know the server type
5. **Graceful Degradation** - Falls back appropriately based on server capabilities

### Example Usage

Both server types use the same configuration and API:

```csharp
// Configuration - same for both types
var config = new MCPServerConfig
{
    ServerName = "my-server",
    ServerType = "sse",
    Uri = "https://example.com/sse" // or MCP-compliant server
};

// Usage - identical regardless of server type
var provider = new SSEMCPClientProvider(config, httpClient, logger);
await provider.ConnectAsync();

// Auto-detection happens during connection
// Client automatically adapts to server type
```

## Technical Details

### Detection Criteria

The detection is based on the server's response to the MCP initialization request:
- Valid MCP `initialize` response → Full MCP server
- Timeout (10 seconds) → Simple SSE API
- Connection error → Simple SSE API
- Invalid/unexpected response → Simple SSE API

### State Management

The `_isSimpleSSEApi` flag is set during connection and remains constant for the session. This ensures consistent behavior throughout the client's lifetime.

### Error Handling

Both paths include appropriate error handling:
- MCP servers: Standard JSON-RPC error responses
- Simple SSE APIs: Connection errors and event parsing errors

This auto-detection mechanism provides maximum flexibility while maintaining standards compliance, allowing the SSE MCP client to work with any SSE-based service transparently. 