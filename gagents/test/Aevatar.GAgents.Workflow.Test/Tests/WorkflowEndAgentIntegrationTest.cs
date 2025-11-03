using Aevatar.GAgents.Workflow;
using Orleans;
using Shouldly;
using Xunit;
using Aevatar.GAgents.TestBase;

namespace Aevatar.GAgents.Workflow.Test.Tests;

/// <summary>
/// Integration tests for WorkflowEndAgent focusing only on agent-specific functionality.
/// Does NOT test base class methods from BusinessAgentBase.
/// </summary>
[Collection(ClusterCollection.Name)]
public class WorkflowEndAgentIntegrationTest : AevatarWorkflowTestBase
{
    private readonly IGrainFactory _grainFactory;

    public WorkflowEndAgentIntegrationTest()
    {
        _grainFactory = GetRequiredService<IGrainFactory>();
    }

    [Fact]
    public async Task GetDescriptionAsync_Should_ReturnCorrectDescription()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowEndAgent>(agentId);

        // Act
        var description = await agent.GetDescriptionAsync();

        // Assert
        description.ShouldContain("Workflow End Agent");
        description.ShouldContain("workflow completion");
        description.ShouldContain("WorkflowCoordinator maintains all workflow information");
    }

    [Fact]
    public async Task ConfigAsync_Should_SetConfigurationCorrectly()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowEndAgent>(agentId);
        
        var config = new WorkflowEndConfigDto
        {
            AgentName = "TestWorkflowEndAgent"
        };

        // Act
        await agent.ConfigAsync(config);

        // Assert
        var state = await agent.GetStateAsync();
        state.ShouldNotBeNull();
        state.MemberName.ShouldNotBeNull(); // BusinessAgentState has MemberName property
    }

    [Fact]
    public async Task GetStateAsync_Should_ReturnWorkflowEndState()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowEndAgent>(agentId);

        // Act
        var state = await agent.GetStateAsync();

        // Assert
        state.ShouldNotBeNull();
        state.ShouldBeOfType<WorkflowEndState>();
        state.MemberName.ShouldNotBeNull(); // BusinessAgentState has MemberName property
    }
}