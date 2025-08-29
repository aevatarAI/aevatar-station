using Aevatar.GAgents.MCP.Options;
using Aevatar.GAgents.MCP.Test.Mocks;
using Aevatar.GAgents.TestBase;
using Microsoft.Extensions.DependencyInjection;

namespace Aevatar.GAgents.MCP.Test;

public abstract class AevatarMCPTestBase : AevatarGAgentTestBase<AevatarMCPTestModule>
{
    /// <summary>
    /// Get Mock MCP client provider for special configuration during testing
    /// </summary>
    protected MockMcpClientProvider GetMockMcpClientProvider()
    {
        return GetRequiredService<MockMcpClientProvider>();
    }
    
    /// <summary>
    /// Create standard file system server configuration for testing
    /// </summary>
    protected static MCPServerConfig CreateFileSystemServerConfig(string serverName = "filesystem")
    {
        return new MCPServerConfig
        {
            ServerName = serverName,
            Command = "mock-filesystem", // Mock implementation
            Args = ["/tmp"],
            Description = "Mock filesystem server for testing"
        };
    }
    
    /// <summary>
    /// Create standard SQLite server configuration for testing
    /// </summary>
    protected static MCPServerConfig CreateSQLiteServerConfig(string serverName = "sqlite")
    {
        return new MCPServerConfig
        {
            ServerName = serverName,
            Command = "mock-sqlite", // Mock implementation
            Args = ["memory:"],
            Description = "Mock SQLite server for testing"
        };
    }
    
    /// <summary>
    /// Create server configuration for error testing
    /// </summary>
    protected static MCPServerConfig CreateErrorServerConfig(string serverName = "error-server")
    {
        return new MCPServerConfig
        {
            ServerName = serverName,
            Command = "mock-error", // Mock implementation that always fails
            Description = "Mock error server for testing error scenarios"
        };
    }
}
