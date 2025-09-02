# MCP Server Test Interface

This is a web-based testing interface for Model Context Protocol (MCP) servers configured in the DefaultMCPServers class.

## 🚀 Features

- **Server Discovery**: Lists all available MCP servers from DefaultMCPServers configuration
- **Tool Exploration**: Browse available tools for each MCP server
- **Interactive Testing**: Execute MCP tools with custom parameters through a web interface
- **Environment Configuration**: Support for environment variable configuration via appsettings.json
- **Real-time Results**: View tool execution results with success/error indication

## 🏗️ Project Structure

```
MCPTest.WebHost/
├── Controllers/
│   └── MCPController.cs          # API endpoints for MCP operations
├── Services/
│   ├── MCPTestService.cs         # Core MCP testing logic
│   └── EnvironmentVariableService.cs # Environment variable resolution
├── wwwroot/
│   ├── index.html                # Frontend interface
│   └── app.js                    # Frontend logic
├── appsettings.json              # Configuration including MCP servers
└── Program.cs                    # Application startup
```

## 🛠️ Configuration

### MCP Servers

Configure MCP servers in `appsettings.json`:

```json
{
  "MCPServerOptions": {
    "MCPServers": {
      "filesystem": {
        "ServerName": "filesystem",
        "Command": "npx",
        "Args": ["-y", "@modelcontextprotocol/server-filesystem", "/tmp"],
        "Description": "File system operations",
        "Type": "Stdio"
      },
      "github": {
        "ServerName": "github",
        "Command": "npx",
        "Args": ["-y", "@modelcontextprotocol/server-github"],
        "Description": "GitHub integration",
        "Type": "Stdio",
        "Env": {
          "GITHUB_PERSONAL_ACCESS_TOKEN": "${GITHUB_PERSONAL_ACCESS_TOKEN}"
        }
      }
    }
  }
}
```

### Environment Variables

Set environment variables in the `EnvironmentVariables` section:

```json
{
  "EnvironmentVariables": {
    "GITHUB_PERSONAL_ACCESS_TOKEN": "your_github_token_here",
    "TWITTER_API_KEY": "your_twitter_api_key_here"
  }
}
```

Environment variables support the following patterns:
- `${VAR_NAME}` - Will be resolved from config or system environment
- Variables are first looked up in the `EnvironmentVariables` config section
- If not found, system environment variables are checked

## 🚦 Running the Project

1. **Install Dependencies**:
   ```bash
   cd simples/MCPTest/MCPTest.WebHost
   dotnet restore
   ```

2. **Configure Environment**:
   - Update `appsettings.json` with your MCP server configurations
   - Set any required environment variables

3. **Run the Application**:
   ```bash
   dotnet run
   ```

4. **Open Browser**:
   - Navigate to `https://localhost:5001` or `http://localhost:5000`
   - The web interface will be available for testing MCP tools

## 🎯 How to Use

1. **Select a Server**: Click on any available MCP server card
2. **Browse Tools**: View the available tools for the selected server
3. **Select a Tool**: Click on a tool to see its parameters
4. **Set Parameters**: Fill in the required and optional parameters
5. **Execute Tool**: Click "Execute Tool" to run the tool
6. **View Results**: See the execution results with success/error indication

## 🔧 Available MCP Servers

The test interface supports all servers defined in `DefaultMCPServers`:

### Servers that don't require environment variables:
- **filesystem**: File system operations (read, write, list directories)
- **memory**: Knowledge graph memory system
- **sequential-thinking**: Structured reasoning framework
- **fetch**: Network resource acquisition
- **time**: Time and date management
- **git**: Git version control operations
- **context7**: Context-aware document management

### Servers that require environment variables:
- **github**: GitHub integration (requires `GITHUB_PERSONAL_ACCESS_TOKEN`)
- **enescinar-twitter**: Twitter integration (requires Twitter API credentials)

## 🐛 Debugging

### API Health Check
Visit `/api/mcp/health` to verify the API is running.

### Environment Variables
Visit `/api/mcp/debug/environment` to see available environment variables (for debugging).

### Console Logs
Check the browser developer console for detailed execution logs.

## 📝 API Endpoints

- `GET /api/mcp/servers` - List available MCP servers
- `GET /api/mcp/servers/{serverName}/tools` - Get tools for a server
- `POST /api/mcp/servers/{serverName}/tools/{toolName}/call` - Execute a tool
- `GET /api/mcp/health` - Health check
- `GET /api/mcp/debug/environment` - Environment variables (debug)

## 🔒 Security Notes

- This is a testing interface and should not be used in production
- Environment variables are logged for debugging purposes
- Some MCP servers may require external dependencies (Node.js, Python, etc.)

## 📦 Dependencies

- .NET 8.0
- ASP.NET Core
- Aevatar.GAgents.MCP
- MockMcpClient (for testing)

## 🤝 Contributing

This is a simple testing interface. Feel free to extend it with additional features:
- Real MCP server connections (instead of mocks)
- Tool result visualization
- Server status monitoring
- Configuration management UI
