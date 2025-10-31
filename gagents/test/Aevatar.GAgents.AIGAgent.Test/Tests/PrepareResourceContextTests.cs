using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Test;
using Aevatar.GAgents.AIGAgent.Test.GAgents;
using Aevatar.GAgents.AIGAgent.Test.TestAgents;
using Shouldly;
using Aevatar.GAgents.TestBase;

namespace Aevatar.GAgents.AIGAgent.Tests;

[Collection(ClusterCollection.Name)]
public class PrepareResourceContextTests : AevatarAIGAgentTestBase
{
    private readonly IGAgentFactory _gAgentFactory;

    public PrepareResourceContextTests()
    {
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact(Skip = "Timeout issue - PrepareResourceContextAsync with complex AIGAgent initialization takes too long. " +
                "This test requires full Brain + Semantic Kernel setup which can be unstable in test environment. " +
                "The functionality is covered by MCPWithAIGAgentIntegrationTests.")]
    public async Task PrepareResourceContextAsync_Should_Register_MCP_And_ToolGAgent_Functions()
    {
        // Arrange: create resources (MCP agent and Tool GAgent)
        // Use "filesystem" which is a predefined mock server in MockMcpClientProvider
        var mcpConfig = new MCP.Options.MCPGAgentConfig
        {
            ServerConfig = new MCP.Options.MCPServerConfig
            {
                ServerName = "filesystem",  // Use mock filesystem server
                Command = "npx",  // Mock command (will be intercepted by MockMcpClientProvider)
                Description = "Mock filesystem server for testing"
            }
        };

        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<Aevatar.GAgents.MCP.Core.IMCPGAgent>(mcpConfig);
        var toolGAgent = await _gAgentFactory.GetGAgentAsync<ITestToolGAgent>(Guid.NewGuid());

        var mcpId = mcpGAgent.GetGrainId();
        var toolId = toolGAgent.GetGrainId();

        // AIGent under test
        var aiAgent = await _gAgentFactory.GetGAgentAsync<ITestGAgentToolsAIGAgent>(Guid.NewGuid());
        await aiAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "for resource context test",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        // Build ResourceContext with both resources
        var resourceContext = ResourceContext.Create(new List<GrainId> { mcpId, toolId },
            $"test:{Guid.NewGuid():N}")
            .WithMetadata("case", "prepare-resource-context");

        // Act: prepare context
        await aiAgent.PrepareResourceContextAsync(resourceContext);

        // Assert: state reflects registrations
        var state = await aiAgent.GetStateAsync();
        state.EnableMCPTools.ShouldBeTrue();
        state.MCPAgents.ShouldNotBeEmpty();

        // Tool GAgents selected and registered functions tracked
        state.EnableGAgentTools.ShouldBeTrue();
        state.ToolGAgents.ShouldNotBeEmpty();

        // RegisteredGAgentFunctions may be empty if no event handlers exist on TestToolGAgent,
        // but the selection list should include our toolId.
        state.ToolGAgents.Any(x => x.Equals(toolId)).ShouldBeTrue();
    }
    
    /// <summary>
    /// Simplified test that validates MCP configuration without complex AIGAgent initialization
    /// This avoids the timeout issue by testing MCP setup independently
    /// </summary>
    [Fact]
    public async Task MCPGAgent_Configuration_Should_Be_Valid_For_ResourceContext()
    {
        // Arrange - Create MCP GAgent with filesystem config
        var mcpConfig = new MCP.Options.MCPGAgentConfig
        {
            ServerConfig = new MCP.Options.MCPServerConfig
            {
                ServerName = "filesystem",
                Command = "npx",
                Description = "Mock filesystem server for testing"
            }
        };

        // Act - Get MCP GAgent
        var mcpGAgent = await _gAgentFactory.GetGAgentAsync<Aevatar.GAgents.MCP.Core.IMCPGAgent>(mcpConfig);
        
        // Wait for initialization
        await Task.Delay(500);

        // Assert - Verify MCP GAgent is configured correctly
        var state = await mcpGAgent.GetStateAsync();
        state.ShouldNotBeNull();
        state.MCPServerConfig.ShouldNotBeNull();
        state.MCPServerConfig.ServerName.ShouldBe("filesystem");
        
        // Verify GrainId can be used in ResourceContext
        var grainId = mcpGAgent.GetGrainId();
        grainId.ToString().ShouldNotBeNullOrEmpty();
    }
    
    /// <summary>
    /// Test that Tool GAgent can be configured for ResourceContext
    /// </summary>
    [Fact]
    public async Task ToolGAgent_Configuration_Should_Be_Valid_For_ResourceContext()
    {
        // Arrange & Act - Create Tool GAgent
        var toolGAgent = await _gAgentFactory.GetGAgentAsync<ITestToolGAgent>(Guid.NewGuid());
        
        // Wait for initialization
        await Task.Delay(500);

        // Assert - Verify Tool GAgent is accessible
        var state = await toolGAgent.GetStateAsync();
        state.ShouldNotBeNull();
        
        // Verify GrainId can be used in ResourceContext
        var grainId = toolGAgent.GetGrainId();
        grainId.ToString().ShouldNotBeNullOrEmpty();
    }
}

