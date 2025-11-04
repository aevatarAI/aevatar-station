# MCP Client Providers Architecture

## Current Implementation Status

### Existing Providers

1. **StdioMCPClientProvider**
    - Purpose: Handles stdio-based MCP servers (process-based communication)
    - Transport: Uses Process.Start to launch servers and communicate via stdin/stdout
    - Examples: filesystem, sqlite, brave-search

2. **RealMCPClientProvider**
    - Purpose: Currently handles HTTP-based MCP servers
    - Transport: Uses HttpClient for JSON-RPC over HTTP
    - Current Issues: Also contains temporary SSE workaround code
    - Examples: HTTP-based MCP servers

3. **MockMCPClientProvider**
    - Purpose: Testing and development
    - Transport: In-memory mock implementation

4. **MCPClientProviderSelector**
    - Purpose: Selects appropriate provider based on configuration
    - Logic: Routes to StdioMCPClientProvider or RealMCPClientProvider

## Proposed Architecture

### Clear Separation by Transport Type

```
IMCPClientProvider
├── StdioMCPClientProvider    (Process-based communication)
├── HttpMCPClientProvider     (HTTP JSON-RPC)
├── SSEMCPClientProvider      (Server-Sent Events)
└── MockMCPClientProvider     (Testing)
```

### 1. **StdioMCPClientProvider** ✅ (Already Implemented)
- **Transport**: Process stdin/stdout
- **Use Cases**: Local MCP servers launched as processes
- **Configuration**: Command + Args
- **Examples**:
    - filesystem
    - sqlite
    - brave-search
    - everything (Windows search)

### 2. **HttpMCPClientProvider** (Rename from RealMCPClientProvider)
- **Transport**: HTTP POST with JSON-RPC
- **Use Cases**: Remote MCP servers with REST-like endpoints
- **Configuration**: URL endpoint
- **Authentication**: Headers (Bearer token, API keys)
- **Examples**:
    - REST API based MCP servers
    - Cloud-hosted MCP services

### 3. **SSEMCPClientProvider** 🚧 (To Be Implemented)
- **Transport**: Server-Sent Events (HTTP GET with event stream)
- **Use Cases**: Real-time streaming MCP servers
- **Configuration**: SSE endpoint URL
- **Authentication**: Query parameters or headers
- **Examples**:
    - zhipu-web-search-sse
    - Real-time data feeds
    - Streaming search results

## Implementation Plan

### Phase 1: Rename and Refactor
1. Rename `RealMCPClientProvider` → `HttpMCPClientProvider`
2. Remove SSE workaround code from HttpMCPClientProvider
3. Update MCPClientProviderSelector to use new names

### Phase 2: Implement SSEMCPClientProvider
1. Create new SSEMCPClientProvider class
2. Implement SSE event stream handling
3. Support authorization via headers
4. Handle reconnection logic
5. Process SSE events to MCP responses

### Phase 3: Update Selector Logic
```csharp
// MCPClientProviderSelector logic
if (config.TransportType == "stdio" || IsExecutableCommand(config.Command))
    → StdioMCPClientProvider

else if (config.TransportType == "sse" || config.Url?.Contains("/sse"))
    → SSEMCPClientProvider

else if (config.TransportType == "http" || IsHttpUrl(config.Command))
    → HttpMCPClientProvider

else
    → Default to StdioMCPClientProvider
```

## Configuration Examples

### Stdio Configuration
```json
{
  "filesystem": {
    "command": "npx",
    "args": ["-y", "@modelcontextprotocol/server-filesystem"]
  }
}
```

### HTTP Configuration
```json
{
  "api-server": {
    "url": "https://api.example.com/mcp",
    "transportType": "http",
    "env": {
      "API_KEY": "secret-key"
    }
  }
}
```

### SSE Configuration
```json
{
  "zhipu-web-search-sse": {
    "url": "https://open.bigmodel.cn/api/mcp/web_search/sse?Authorization=token",
    "transportType": "sse"
  }
}
```

## Benefits of This Architecture

1. **Clear Separation of Concerns**: Each provider handles one transport type
2. **Maintainability**: Easier to debug and extend specific transport implementations
3. **Type Safety**: Transport-specific configurations and behaviors
4. **Scalability**: Easy to add new transport types (WebSocket, gRPC, etc.)
5. **Testing**: Each provider can be tested independently

## Current Workarounds to Remove

1. SSE detection logic in RealMCPClientProvider
2. Hardcoded tool definitions for SSE servers
3. Mixed transport handling in single provider

## Future Extensions

- **WebSocketMCPClientProvider**: For bidirectional real-time communication
- **GrpcMCPClientProvider**: For high-performance binary protocol
- **GraphQLMCPClientProvider**: For GraphQL-based MCP servers