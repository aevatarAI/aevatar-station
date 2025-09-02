using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGEvent;
using Aevatar.GAgents.MCP.Core;
using Aevatar.GAgents.MCP.Core.Extensions;
using Aevatar.GAgents.MCP.Options;
using Shouldly;
using Xunit;

namespace Aevatar.GAgents.MCP.Test;

/// <summary>
/// Unit tests for GAgentFactoryExtensions
/// Tests MCP server configuration management and GAgent retrieval functionality
/// </summary>
public sealed class ExtensionMethodsTests : AevatarMCPTestBase
{
    private readonly IGAgentFactory _gAgentFactory;

    public ExtensionMethodsTests()
    {
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    #region GetMCPServerConfigGAgent Tests

    [Fact]
    public async Task GetMCPServerConfigGAgent_MultipleCallsShouldReturnSameInstance()
    {
        // Act
        var configManager1 = await _gAgentFactory.GetMCPServerConfigGAgent();
        var configManager2 = await _gAgentFactory.GetMCPServerConfigGAgent();

        // Assert
        configManager1.ShouldNotBeNull();
        configManager2.ShouldNotBeNull();
        // Note: Orleans grains are stateful, so they should represent the same logical entity
        // but may be different object instances
    }

    #endregion

    #region ConfigMCPWhitelistAsync Tests

    [Fact]
    public async Task ConfigMCPWhitelistAsync_ValidConfig_ShouldReturnTure()
    {
        // Arrange
        var servers = new Dictionary<string, MCPServerConfig>
        {
            ["filesystem"] = CreateFileSystemServerConfig(),
            ["sqlite"] = CreateSQLiteServerConfig()
        };

        var configJson = JsonSerializer.Serialize(servers);

        // Act
        var mcpServerConfigGAgent = await _gAgentFactory.GetMCPServerConfigGAgent();
        var result = await mcpServerConfigGAgent.ConfigMCPWhitelistAsync(configJson);

        // Assert
        result.ShouldBeTrue();

        // Verify configuration was actually stored
        var configManager = await _gAgentFactory.GetMCPServerConfigGAgent();
        var requestEvent = new ConfigRequestEvent
        {
            ConfigType = MCPServerConfigManagerGAgentExtensions.MCPWhitelistConfigTypeFullName
        };

        var response = await configManager.RequestConfigAsync(requestEvent);
        response.Success.ShouldBeTrue();
        response.ConfigJson.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ConfigMCPWhitelistAsync_EmptyConfig_ShouldReturnFalse()
    {
        // Act
        var mcpServerConfigGAgent = await _gAgentFactory.GetMCPServerConfigGAgent();
        var result = await mcpServerConfigGAgent.ConfigMCPWhitelistAsync("");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ConfigMCPWhitelistAsync_NullConfig_ShouldReturnFalse()
    {
        // Act
        var mcpServerConfigGAgent = await _gAgentFactory.GetMCPServerConfigGAgent();
        var result = await mcpServerConfigGAgent.ConfigMCPWhitelistAsync([]);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ConfigMCPWhitelistAsync_InvalidJson_ShouldThrowException()
    {
        // Arrange
        var invalidJson = "{invalid json format";

        var mcpServerConfigGAgent = await _gAgentFactory.GetMCPServerConfigGAgent();
        // Act & Assert
        await Should.ThrowAsync<JsonException>(async () =>
        {
            await mcpServerConfigGAgent.ConfigMCPWhitelistAsync(invalidJson);
        });
    }

    [Fact]
    public async Task ConfigMCPWhitelistAsync_ComplexConfiguration_ShouldHandleCorrectly()
    {
        // Arrange
        var servers = new Dictionary<string, MCPServerConfig>
        {
            ["filesystem"] = new MCPServerConfig
            {
                ServerName = "filesystem",
                Command = "npx",
                Args = ["@modelcontextprotocol/server-filesystem", "/workspace"],
                Description = "File system server for workspace access",
                Type = MCPServerType.Stdio,
                Env = new Dictionary<string, string>
                {
                    ["WORKSPACE_PATH"] = "/workspace",
                    ["DEBUG"] = "true"
                }
            },
            ["web-search"] = new MCPServerConfig
            {
                ServerName = "web-search",
                Command = "npx",
                Args = ["@modelcontextprotocol/server-web-search"],
                Description = "Web search capabilities",
                Type = MCPServerType.Stdio
            },
            ["sqlite"] = new MCPServerConfig
            {
                ServerName = "sqlite",
                Command = "npx",
                Args = ["@modelcontextprotocol/server-sqlite", "--db-path", "/data/app.db"],
                Description = "SQLite database access",
                Type = MCPServerType.Stdio
            }
        };

        var configJson = JsonSerializer.Serialize(servers);

        // Act
        var mcpServerConfigGAgent = await _gAgentFactory.GetMCPServerConfigGAgent();
        var result = await mcpServerConfigGAgent.ConfigMCPWhitelistAsync(configJson);

        // Assert
        result.ShouldBeTrue();

        // Verify complex configuration was stored correctly
        var retrievedServers = await mcpServerConfigGAgent.GetMCPWhiteListAsync();
        retrievedServers.Count.ShouldBe(3);
        retrievedServers.ShouldContainKey("filesystem");
        retrievedServers.ShouldContainKey("web-search");
        retrievedServers.ShouldContainKey("sqlite");

        var fsServer = retrievedServers["filesystem"];
        fsServer.Args.Count.ShouldBe(2);
        fsServer.Env.Count.ShouldBe(2);
        fsServer.Type.ShouldBe(MCPServerType.Stdio);
    }

    #endregion

    #region GetMCPWhiteListAsync Tests

    [Fact]
    public async Task GetMCPWhiteListAsync_WithValidConfig_ShouldReturnServers()
    {
        // Arrange
        var servers = new Dictionary<string, MCPServerConfig>
        {
            ["filesystem"] = CreateFileSystemServerConfig(),
            ["sqlite"] = CreateSQLiteServerConfig()
        };

        var mcpServerConfigGAgent = await _gAgentFactory.GetMCPServerConfigGAgent();
        var configJson = JsonSerializer.Serialize(servers);
        await mcpServerConfigGAgent.ConfigMCPWhitelistAsync(configJson);

        // Act
        var result = await mcpServerConfigGAgent.GetMCPWhiteListAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldContainKey("filesystem");
        result.ShouldContainKey("sqlite");

        var fsServer = result["filesystem"];
        fsServer.ServerName.ShouldBe("filesystem");
        fsServer.Command.ShouldBe("mock-filesystem");
        fsServer.Args.ShouldBe(["/tmp"]);
    }

    [Fact]
    public async Task GetMCPWhiteListAsync_NoConfigStored_ShouldReturnEmptyDictionary()
    {
        // Arrange - Use a new factory instance to ensure clean state
        var freshFactory = GetRequiredService<IGAgentFactory>();

        // Act
        var mcpServerConfigGAgent = await freshFactory.GetMCPServerConfigGAgent();
        var result = await mcpServerConfigGAgent.GetMCPWhiteListAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetMCPWhiteListAsync_InvalidJsonInConfig_ShouldReturnEmptyDictionary()
    {
        // Arrange - Setup config manager with invalid JSON
        var configManager = await _gAgentFactory.GetMCPServerConfigGAgent();
        await configManager.UpdateConfigAsync(new ConfigUpdateEvent
        {
            ConfigType = MCPServerConfigManagerGAgentExtensions.MCPWhitelistConfigTypeFullName,
            ConfigJson = "invalid json"
        });

        // Act
        var mcpServerConfigGAgent = await _gAgentFactory.GetMCPServerConfigGAgent();
        var result = await mcpServerConfigGAgent.GetMCPWhiteListAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetMCPWhiteListAsync_EmptyConfig_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var emptyServers = new Dictionary<string, MCPServerConfig>();
        var configJson = JsonSerializer.Serialize(emptyServers);
        var mcpServerConfigGAgent = await _gAgentFactory.GetMCPServerConfigGAgent();
        await mcpServerConfigGAgent.ConfigMCPWhitelistAsync(configJson);

        // Act
        var result = await mcpServerConfigGAgent.GetMCPWhiteListAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    #endregion

    #region GetMCPGAgentAsync Tests

    [Fact]
    public async Task GetMCPGAgentAsync_ValidServerName_ShouldReturnMCPGAgent()
    {
        // Arrange
        var servers = new Dictionary<string, MCPServerConfig>
        {
            ["filesystem"] = CreateFileSystemServerConfig(),
            ["sqlite"] = CreateSQLiteServerConfig()
        };

        var mcpServerConfigGAgent = await _gAgentFactory.GetMCPServerConfigGAgent();
        var configJson = JsonSerializer.Serialize(servers);
        await mcpServerConfigGAgent.ConfigMCPWhitelistAsync(configJson);

        // Act
        var mcpGAgent = await _gAgentFactory.GetMCPGAgentAsync("filesystem");

        // Assert
        mcpGAgent.ShouldNotBeNull();
        mcpGAgent.ShouldBeAssignableTo<IMCPGAgent>();
    }

    [Fact]
    public async Task GetMCPGAgentAsync_NonExistentServerName_ShouldReturnNull()
    {
        // Arrange
        var servers = new Dictionary<string, MCPServerConfig>
        {
            ["filesystem"] = CreateFileSystemServerConfig()
        };

        var configJson = JsonSerializer.Serialize(servers);
        var mcpServerConfigGAgent = await _gAgentFactory.GetMCPServerConfigGAgent();
        await mcpServerConfigGAgent.ConfigMCPWhitelistAsync(configJson);

        // Act
        var mcpGAgent = await _gAgentFactory.GetMCPGAgentAsync("non-existent-server");

        // Assert
        mcpGAgent.ShouldBeNull();
    }

    [Fact]
    public async Task GetMCPGAgentAsync_NoConfigStored_ShouldReturnNull()
    {
        // Act
        var mcpGAgent = await _gAgentFactory.GetMCPGAgentAsync("filesystem");

        // Assert
        mcpGAgent.ShouldBeNull();
    }

    [Fact]
    public async Task GetMCPGAgentAsync_EmptyServerName_ShouldReturnNull()
    {
        // Arrange
        var servers = new Dictionary<string, MCPServerConfig>
        {
            ["filesystem"] = CreateFileSystemServerConfig()
        };

        var mcpServerConfigGAgent = await _gAgentFactory.GetMCPServerConfigGAgent();
        var configJson = JsonSerializer.Serialize(servers);
        await mcpServerConfigGAgent.ConfigMCPWhitelistAsync(configJson);

        // Act
        var mcpGAgent = await _gAgentFactory.GetMCPGAgentAsync("");

        // Assert
        mcpGAgent.ShouldBeNull();
    }

    [Fact]
    public async Task GetMCPGAgentAsync_WhitespaceServerName_ShouldReturnNull()
    {
        // Arrange
        var servers = new Dictionary<string, MCPServerConfig>
        {
            ["filesystem"] = CreateFileSystemServerConfig()
        };

        var mcpServerConfigGAgent = await _gAgentFactory.GetMCPServerConfigGAgent();
        var configJson = JsonSerializer.Serialize(servers);
        await mcpServerConfigGAgent.ConfigMCPWhitelistAsync(configJson);

        // Act
        var mcpGAgent = await _gAgentFactory.GetMCPGAgentAsync("   ");

        // Assert
        mcpGAgent.ShouldBeNull();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task IntegrationTest_CompleteWorkflow_ShouldWorkCorrectly()
    {
        // Arrange - Setup multiple server configurations
        var servers = new Dictionary<string, MCPServerConfig>
        {
            ["filesystem"] = CreateFileSystemServerConfig(),
            ["sqlite"] = CreateSQLiteServerConfig(),
            ["error-server"] = CreateErrorServerConfig()
        };

        var mcpServerConfigGAgent = await _gAgentFactory.GetMCPServerConfigGAgent();
        // Act 1 - Configure MCP whitelist
        var configJson = JsonSerializer.Serialize(servers);
        var configResult = await mcpServerConfigGAgent.ConfigMCPWhitelistAsync(configJson);

        // Assert 1 - Configuration succeeded
        configResult.ShouldBeTrue();

        // Act 2 - Retrieve whitelist
        var whitelist = await mcpServerConfigGAgent.GetMCPWhiteListAsync();

        // Assert 2 - Whitelist contains all servers
        whitelist.Count.ShouldBe(3);
        whitelist.Keys.ShouldContain("filesystem");
        whitelist.Keys.ShouldContain("sqlite");
        whitelist.Keys.ShouldContain("error-server");

        // Act 3 - Get specific MCP GAgents
        var filesystemAgent = await _gAgentFactory.GetMCPGAgentAsync("filesystem");
        var sqliteAgent = await _gAgentFactory.GetMCPGAgentAsync("sqlite");
        var errorAgent = await _gAgentFactory.GetMCPGAgentAsync("error-server");
        var nonExistentAgent = await _gAgentFactory.GetMCPGAgentAsync("non-existent");

        // Assert 3 - GAgent retrieval works correctly
        filesystemAgent.ShouldNotBeNull();
        sqliteAgent.ShouldNotBeNull();
        errorAgent.ShouldNotBeNull();
        nonExistentAgent.ShouldBeNull();

        // Act 4 - Verify config manager consistency
        var configManager = await _gAgentFactory.GetMCPServerConfigGAgent();
        var state = await configManager.GetStateAsync();

        // Assert 4 - State is consistent
        state.ConfigType.ShouldBe(MCPServerConfigManagerGAgentExtensions.MCPWhitelistConfigTypeFullName);
        state.ConfigJson.ShouldNotBeEmpty();
        state.TotalUpdates.ShouldBe(1);
    }

    [Fact]
    public async Task IntegrationTest_UpdateConfiguration_ShouldReflectChanges()
    {
        // Arrange - Initial configuration
        var initialServers = new Dictionary<string, MCPServerConfig>
        {
            ["filesystem"] = CreateFileSystemServerConfig()
        };

        var mcpServerConfigGAgent = await _gAgentFactory.GetMCPServerConfigGAgent();
        var initialConfigJson = JsonSerializer.Serialize(initialServers);
        await mcpServerConfigGAgent.ConfigMCPWhitelistAsync(initialConfigJson);

        // Act 1 - Add more servers
        var updatedServers = new Dictionary<string, MCPServerConfig>
        {
            ["filesystem"] = CreateFileSystemServerConfig(),
            ["sqlite"] = CreateSQLiteServerConfig(),
            ["new-server"] = new MCPServerConfig
            {
                ServerName = "new-server",
                Command = "mock-new",
                Args = ["--config", "test"],
                Description = "New server for testing updates"
            }
        };

        var updatedConfigJson = JsonSerializer.Serialize(updatedServers);
        await mcpServerConfigGAgent.ConfigMCPWhitelistAsync(updatedConfigJson);

        // Act 2 - Retrieve updated whitelist
        var whitelist = await mcpServerConfigGAgent.GetMCPWhiteListAsync();

        // Assert - Updated configuration is reflected
        whitelist.Count.ShouldBe(3);
        whitelist.ShouldContainKey("filesystem");
        whitelist.ShouldContainKey("sqlite");
        whitelist.ShouldContainKey("new-server");

        // Verify new server configuration
        var newServer = whitelist["new-server"];
        newServer.ServerName.ShouldBe("new-server");
        newServer.Command.ShouldBe("mock-new");
        newServer.Args.ShouldContain("--config");
        newServer.Args.ShouldContain("test");
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task GetMCPGAgentAsync_InvalidConfigJson_ShouldReturnNull()
    {
        // Arrange - Setup config manager with invalid JSON for specific key request
        var configManager = await _gAgentFactory.GetMCPServerConfigGAgent();
        await configManager.UpdateConfigAsync(new ConfigUpdateEvent
        {
            ConfigType = MCPServerConfigManagerGAgentExtensions.MCPWhitelistConfigTypeFullName,
            ConfigJson = "{\"invalidStructure\": \"notAServerConfig\"}"
        });

        // Act & Assert
        Should.Throw<ArgumentException>(async () =>
        {
            await _gAgentFactory.GetMCPGAgentAsync("invalidStructure");
        });
    }

    [Fact]
    public async Task ConfigMCPWhitelistAsync_SpecialCharactersInServerName_ShouldHandleCorrectly()
    {
        // Arrange
        var servers = new Dictionary<string, MCPServerConfig>
        {
            ["server-with-dashes"] = new MCPServerConfig
            {
                ServerName = "server-with-dashes",
                Command = "mock-cmd",
                Description = "Server with special characters"
            },
            ["server_with_underscores"] = new MCPServerConfig
            {
                ServerName = "server_with_underscores",
                Command = "mock-cmd",
                Description = "Server with underscores"
            },
            ["server.with.dots"] = new MCPServerConfig
            {
                ServerName = "server.with.dots",
                Command = "mock-cmd",
                Description = "Server with dots"
            }
        };

        var configJson = JsonSerializer.Serialize(servers);

        // Act
        var configManager = await _gAgentFactory.GetMCPServerConfigGAgent();
        await configManager.ConfigMCPWhitelistAsync(configJson);
        var whitelist = await configManager.GetMCPWhiteListAsync();

        // Assert
        whitelist.Count.ShouldBe(3);
        whitelist.ShouldContainKey("server-with-dashes");
        whitelist.ShouldContainKey("server_with_underscores");
        whitelist.ShouldContainKey("server.with.dots");
    }

    [Fact]
    public async Task GetMCPGAgentAsync_CaseSensitivity_ShouldBeExact()
    {
        // Arrange
        var servers = new Dictionary<string, MCPServerConfig>
        {
            ["FileSystem"] = CreateFileSystemServerConfig("FileSystem")
        };

        var configManager = await _gAgentFactory.GetMCPServerConfigGAgent();
        var configJson = JsonSerializer.Serialize(servers);
        await configManager.ConfigMCPWhitelistAsync(configJson);

        // Act
        var exactMatchAgent = await _gAgentFactory.GetMCPGAgentAsync("FileSystem");
        var lowercaseAgent = await _gAgentFactory.GetMCPGAgentAsync("filesystem");
        var uppercaseAgent = await _gAgentFactory.GetMCPGAgentAsync("FILESYSTEM");

        // Assert
        exactMatchAgent.ShouldNotBeNull();
        lowercaseAgent.ShouldBeNull();
        uppercaseAgent.ShouldBeNull();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Create test MCP server configuration with custom arguments
    /// </summary>
    private static MCPServerConfig CreateCustomServerConfig(string serverName, string command, params string[] args)
    {
        return new MCPServerConfig
        {
            ServerName = serverName,
            Command = command,
            Args = args.ToList(),
            Description = $"Custom test server: {serverName}",
            Type = MCPServerType.Stdio
        };
    }

    /// <summary>
    /// Create test configuration with multiple server types
    /// </summary>
    private static Dictionary<string, MCPServerConfig> CreateMultiServerConfig()
    {
        return new Dictionary<string, MCPServerConfig>
        {
            ["stdio-server"] = new MCPServerConfig
            {
                ServerName = "stdio-server",
                Command = "mock-stdio",
                Type = MCPServerType.Stdio,
                Args = ["--stdio"],
                Description = "STDIO test server"
            },
            ["http-server"] = new MCPServerConfig
            {
                ServerName = "http-server",
                Url = "http://localhost:8080/mcp",
                Type = MCPServerType.StreamableHttp,
                Description = "HTTP test server"
            }
        };
    }

    #endregion
}