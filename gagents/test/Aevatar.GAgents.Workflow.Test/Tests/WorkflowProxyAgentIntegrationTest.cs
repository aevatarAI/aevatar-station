using Aevatar.GAgents.Workflow;
using Orleans;
using Shouldly;
using Xunit;

namespace Aevatar.GAgents.Workflow.Test.Tests;

/// <summary>
/// Integration tests for WorkflowProxyAgent focusing only on agent-specific functionality.
/// Does NOT test base class methods from CoreGAgentBase.
/// </summary>
[Collection(ClusterCollection.Name)]
public class WorkflowProxyAgentIntegrationTest : AevatarWorkflowTestBase
{
    private readonly IGrainFactory _grainFactory;

    public WorkflowProxyAgentIntegrationTest()
    {
        _grainFactory = GetRequiredService<IGrainFactory>();
    }

    [Fact]
    public async Task GetDescriptionAsync_Should_ReturnCorrectDescription()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowProxyAgent>(agentId);

        // Act
        var description = await agent.GetDescriptionAsync();

        // Assert
        description.ShouldContain("Proxy agent");
        description.ShouldContain("CoreGAgentBase");
        description.ShouldContain("SendEventToAgentAsync");
        description.ShouldContain("P2P workflow event communication");
    }

    [Fact]
    public async Task GetStateAsync_Should_ReturnWorkflowProxyAgentState()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowProxyAgent>(agentId);

        // Act
        var state = await agent.GetStateAsync();

        // Assert
        state.ShouldNotBeNull();
        state.ShouldBeOfType<WorkflowProxyAgentState>();
    }

    [Fact]
    public async Task Agent_Should_BeInstantiable()
    {
        // Arrange & Act
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowProxyAgent>(agentId);

        // Assert - Just verify the agent can be created and accessed
        agent.ShouldNotBeNull();
        
        // Verify basic Orleans grain functionality
        var grainReference = agent.AsReference<IWorkflowProxyAgent>();
        grainReference.ShouldNotBeNull();
    }
}
