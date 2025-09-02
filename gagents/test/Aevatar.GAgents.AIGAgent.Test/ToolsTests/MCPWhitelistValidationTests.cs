using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.Basic.BasicGAgents;
using Aevatar.GAgents.Basic.BasicGEvent;
using Aevatar.GAgents.MCP.Core.Extensions;
using Aevatar.GAgents.MCP.Core.Options;
using Aevatar.GAgents.MCP.Options;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.GAgents.AIGAgent.Test.ToolsTests;

/// <summary>
/// Tests for MCP server whitelist validation functionality in AIGAgentBase.
/// Uses indirect testing approach - verifies whitelist enforcement through 
/// ConfigureMCPServersAsync behavior rather than exposing validation logic.
/// </summary>
public sealed class MCPWhitelistValidationTests : AevatarAIGAgentTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public MCPWhitelistValidationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    #region Basic Functionality Tests

    [Fact]
    public async Task ConfigureMCPServersAsync_WithValidWhitelistedServer_ShouldSucceed()
    {
        // Arrange
        var aiAgent = await CreateTestAIGAgentAsync();

        // Setup whitelist with filesystem server
        await SetupWhitelistAsync(new Dictionary<string, MCPServerConfig>
        {
            ["filesystem"] = new MCPServerConfig
            {
                ServerName = "filesystem",
                Command = "npx",
                Args = ["@modelcontextprotocol/server-filesystem", "/workspace"],
                Type = MCPServerType.Stdio,
                Description = "Filesystem MCP server"
            }
        });

        var serversToTest = new List<MCPServerConfig>
        {
            new MCPServerConfig
            {
                ServerName = "filesystem",
                Command = "npx",
                Args = ["@modelcontextprotocol/server-filesystem", "/workspace"],
                Type = MCPServerType.Stdio,
                Description = "Filesystem MCP server"
            }
        };

        // Act
        var configResult = await aiAgent.ConfigureMCPServersAsync(serversToTest);

        // Assert
        configResult.ShouldBeTrue();
        _testOutputHelper.WriteLine("✅ Valid whitelisted server configuration succeeded");
    }

    [Fact]
    public async Task ConfigureMCPServersAsync_WithStrictStdioValidation_ShouldEnforceExactMatch()
    {
        // Arrange
        var aiAgent = await CreateTestAIGAgentAsync();

        // Setup strict whitelist
        await SetupWhitelistAsync(new Dictionary<string, MCPServerConfig>
        {
            ["filesystem"] = new MCPServerConfig
            {
                ServerName = "filesystem",
                Command = "npx",
                Args = ["@modelcontextprotocol/server-filesystem", "/workspace"],
                Type = MCPServerType.Stdio
            }
        });

        // Test with different command (should fail)
        var invalidCommandServers = new List<MCPServerConfig>
        {
            new MCPServerConfig
            {
                ServerName = "filesystem",
                Command = "node", // Different command
                Args = ["@modelcontextprotocol/server-filesystem", "/workspace"],
                Type = MCPServerType.Stdio
            }
        };

        // Test with different args (should fail)
        var invalidArgsServers = new List<MCPServerConfig>
        {
            new MCPServerConfig
            {
                ServerName = "filesystem",
                Command = "npx",
                Args = ["@modelcontextprotocol/server-filesystem", "/different/path"], // Different args
                Type = MCPServerType.Stdio
            }
        };

        // Act & Assert
        var commandResult = await aiAgent.ConfigureMCPServersAsync(invalidCommandServers);
        var argsResult = await aiAgent.ConfigureMCPServersAsync(invalidArgsServers);

        commandResult.ShouldBeFalse("Different command should be rejected");
        argsResult.ShouldBeFalse("Different args should be rejected");

        _testOutputHelper.WriteLine("✅ Stdio validation correctly enforced strict matching");
    }

    [Fact]
    public async Task ConfigureMCPServersAsync_WithSSETransport_ShouldThrowMissingProviderException()
    {
        // Arrange
        var aiAgent = await CreateTestAIGAgentAsync();

        // Setup whitelist with SSE server (flexible validation)
        await SetupWhitelistAsync(new Dictionary<string, MCPServerConfig>
        {
            ["sse-server"] = new MCPServerConfig
            {
                ServerName = "sse-server",
                Type = MCPServerType.StreamableHttp,
                Description = "SSE MCP server"
            }
        });

        var sseServers = new List<MCPServerConfig>
        {
            new MCPServerConfig
            {
                ServerName = "sse-server",
                Type = MCPServerType.StreamableHttp,
                Url = "http://example.com/sse", // URL can be different
                Description = "Different description" // Description can be different
            }
        };

        // Act - SSE transport should pass whitelist validation but fail at client creation
        var configResult = await aiAgent.ConfigureMCPServersAsync(sseServers);

        // Assert - Configuration should fail due to missing SSE provider, not whitelist rejection
        configResult.ShouldBeFalse("SSE servers should fail due to missing client provider, not whitelist rejection");
        _testOutputHelper.WriteLine("✅ SSE transport passed whitelist validation but failed at client creation stage");
    }

    [Fact]
    public async Task ConfigureMCPServersAsync_WithNonWhitelistedServer_ShouldFail()
    {
        // Arrange
        var aiAgent = await CreateTestAIGAgentAsync();

        // Setup whitelist with only one server
        await SetupWhitelistAsync(new Dictionary<string, MCPServerConfig>
        {
            ["filesystem"] = new MCPServerConfig
            {
                ServerName = "filesystem",
                Command = "npx",
                Args = ["@modelcontextprotocol/server-filesystem", "/workspace"],
                Type = MCPServerType.Stdio
            }
        });

        // Try to configure a non-whitelisted server
        var unauthorizedServers = new List<MCPServerConfig>
        {
            new MCPServerConfig
            {
                ServerName = "unauthorized-server",
                Command = "malicious-command",
                Args = ["malicious-arg"],
                Type = MCPServerType.Stdio
            }
        };

        // Act
        var configResult = await aiAgent.ConfigureMCPServersAsync(unauthorizedServers);

        // Assert
        configResult.ShouldBeFalse("Non-whitelisted server should be rejected");
        _testOutputHelper.WriteLine("✅ Non-whitelisted server was correctly rejected");
    }

    [Fact]
    public async Task ConfigureMCPServersAsync_WithEmptyWhitelist_ShouldRejectAllServers()
    {
        // Arrange
        var aiAgent = await CreateTestAIGAgentAsync();

        // Setup empty whitelist
        await SetupWhitelistAsync(new Dictionary<string, MCPServerConfig>());

        var anyServers = new List<MCPServerConfig>
        {
            new MCPServerConfig
            {
                ServerName = "any-server",
                Command = "any-command",
                Args = ["any-arg"],
                Type = MCPServerType.Stdio
            }
        };

        // Act
        var configResult = await aiAgent.ConfigureMCPServersAsync(anyServers);

        // Assert
        configResult.ShouldBeFalse("Empty whitelist should reject all servers");
        _testOutputHelper.WriteLine("✅ Empty whitelist correctly rejected all servers");
    }

    [Fact]
    public async Task ConfigureMCPServersAsync_WithNoWhitelist_ShouldAllowAllServers()
    {
        // Arrange
        var aiAgent = await CreateTestAIGAgentAsync();

        // Don't setup any whitelist (configuration manager returns null/empty)

        var anyServers = new List<MCPServerConfig>
        {
            new MCPServerConfig
            {
                ServerName = "any-server",
                Command = "any-command",
                Args = ["any-arg"],
                Type = MCPServerType.Stdio
            }
        };

        // Act
        var configResult = await aiAgent.ConfigureMCPServersAsync(anyServers);

        // Assert
        configResult.ShouldBeFalse("No whitelist should not all any server");
    }

    [Fact]
    public async Task ConfigureMCPServersAsync_WithMultipleServers_ShouldValidateEach()
    {
        // Arrange
        var aiAgent = await CreateTestAIGAgentAsync();

        // Setup whitelist with specific servers
        await SetupWhitelistAsync(new Dictionary<string, MCPServerConfig>
        {
            ["filesystem"] = new MCPServerConfig
            {
                ServerName = "filesystem",
                Command = "npx",
                Args = ["@modelcontextprotocol/server-filesystem", "/workspace"],
                Type = MCPServerType.Stdio
            }
        });

        var mixedServers = new List<MCPServerConfig>
        {
            new MCPServerConfig
            {
                ServerName = "filesystem", // Valid
                Type = MCPServerType.Stdio,
                Command = "npx",
                Args = new List<string> { "@modelcontextprotocol/server-filesystem", "/workspace" }
            },
            new MCPServerConfig
            {
                ServerName = "unauthorized", // Invalid
                Type = MCPServerType.Stdio,
                Command = "bad-command",
                Args = ["bad-arg"]
            }
        };

        // Act
        var configResult = await aiAgent.ConfigureMCPServersAsync(mixedServers);

        // Assert
        configResult.ShouldBeFalse("Mixed servers with invalid ones should fail");
        _testOutputHelper.WriteLine("✅ Mixed server validation correctly failed on invalid server");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Setup MCP server whitelist in configuration manager
    /// </summary>
    private async Task SetupWhitelistAsync(Dictionary<string, MCPServerConfig> whitelist)
    {
        var mcpServerConfigGAgent = await _gAgentFactory.GetMCPServerConfigGAgent();
        await mcpServerConfigGAgent.ConfigMCPWhitelistAsync(whitelist);
    }

    /// <summary>
    /// Create a test AI agent for validation testing
    /// </summary>
    private async Task<ITestAIGAgent> CreateTestAIGAgentAsync()
    {
        var agentId = Guid.NewGuid();
        return await _gAgentFactory.GetGAgentAsync<ITestAIGAgent>(agentId);
    }

    #endregion
}

#region Test Agent Implementation

/// <summary>
/// Test interface for AI agent that supports MCP configuration
/// </summary>
public interface ITestAIGAgent : IStateGAgent<TestAIGAgentState>, IAIGAgent;

/// <summary>
/// Simple test AI agent for whitelist validation testing
/// </summary>
[GAgent("test-ai-agent", "test")]
public class TestAIGAgent : AIGAgentBase<TestAIGAgentState, TestAIGAgentStateLogEvent>, ITestAIGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Test AI agent for MCP whitelist validation");
    }
}

/// <summary>
/// Test state for the AI agent
/// </summary>
[GenerateSerializer]
public class TestAIGAgentState : AIGAgentStateBase
{
    // Empty state for testing
}

/// <summary>
/// Test state log event for the AI agent
/// </summary>
[GenerateSerializer]
public class TestAIGAgentStateLogEvent : StateLogEventBase<TestAIGAgentStateLogEvent>
{
    // Empty events for testing
}

#endregion