using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Test;
using Aevatar.GAgents.AIGAgent.Test.GAgents;
using Aevatar.GAgents.AIGAgent.Test.TestAgents;
using Shouldly;

namespace Aevatar.GAgents.AIGAgent.Tests;

public class PrepareResourceContextTests : AevatarAIGAgentTestBase
{
    private readonly IGAgentFactory _gAgentFactory;

    public PrepareResourceContextTests()
    {
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task PrepareResourceContextAsync_Should_Register_MCP_And_ToolGAgent_Functions()
    {
        // Arrange: create resources (MCP agent and Tool GAgent)
        var mcpConfig = new MCP.Options.MCPGAgentConfig
        {
            ServerConfig = new MCP.Options.MCPServerConfig
            {
                ServerName = "test-mcp-server",
                Command = "mock-cmd"
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
}

