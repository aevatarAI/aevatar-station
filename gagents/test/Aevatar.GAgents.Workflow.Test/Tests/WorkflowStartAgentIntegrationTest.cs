using Aevatar.GAgents.Workflow;
using Orleans;
using Shouldly;
using Xunit;

namespace Aevatar.GAgents.Workflow.Test.Tests;

/// <summary>
/// Integration tests for WorkflowStartAgent focusing only on agent-specific functionality.
/// Does NOT test base class methods from BusinessAgentBase.
/// </summary>
public class WorkflowStartAgentIntegrationTest : AevatarWorkflowTestBase
{
    private readonly IGrainFactory _grainFactory;

    public WorkflowStartAgentIntegrationTest()
    {
        _grainFactory = GetRequiredService<IGrainFactory>();
    }

    [Fact]
    public async Task GetDescriptionAsync_Should_ReturnCorrectDescription()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowStartAgent>(agentId);

        // Act
        var description = await agent.GetDescriptionAsync();

        // Assert
        description.ShouldContain("Workflow Start Agent");
        description.ShouldContain("initiates workflows");
        description.ShouldContain("forwarding events");
    }

    [Fact]
    public async Task ConfigAsync_Should_SetConfigurationCorrectly()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowStartAgent>(agentId);
        
        var config = new WorkflowStartConfigDto
        {
            AgentName = "TestWorkflowStartAgent"
        };

        // Act
        await agent.ConfigAsync(config);

        // Assert
        var state = await agent.GetStateAsync();
        state.ShouldNotBeNull();
        state.MemberName.ShouldNotBeNull(); // BusinessAgentState has MemberName property
    }

    [Fact]
    public async Task GetStateAsync_Should_ReturnWorkflowStartState()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowStartAgent>(agentId);

        // Act
        var state = await agent.GetStateAsync();

        // Assert
        state.ShouldNotBeNull();
        state.ShouldBeOfType<WorkflowStartState>();
        state.MemberName.ShouldNotBeNull(); // BusinessAgentState has MemberName property
    }
}
