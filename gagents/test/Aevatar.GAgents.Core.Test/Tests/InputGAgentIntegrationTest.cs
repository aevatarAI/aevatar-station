using Aevatar.Core.Abstractions;
using Aevatar.GAgents.InputGAgent.GAgent;
using Aevatar.GAgents.InputGAgent.Dto;
using Aevatar.GAgents.InputGAgent.GAgent.SEvent;
using Aevatar.GAgents.Basic;
using Orleans;
using Orleans.Providers;
using Shouldly;
using Xunit;
using GroupChat.GAgent.Feature.Common;
using Aevatar.GAgents.TestBase;

namespace Aevatar.GAgents.Core.Test.Tests;

/// <summary>
/// Integration tests for InputGAgentPlus using Orleans TestCluster infrastructure.
/// Tests the BusinessAgentBase-based input agent functionality with workflow integration.
/// </summary>
public class InputGAgentPlusIntegrationTest : AevatarGAgentTestBase<AevatarGAgentsCoreTestModule>
{
    private readonly IGrainFactory _grainFactory;

    public InputGAgentPlusIntegrationTest()
    {
        _grainFactory = GetRequiredService<IGrainFactory>();
    }

    #region Configuration Tests

    [Fact]
    public async Task PerformConfigAsync_Should_SetInputCorrectly()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IInputGAgentPlus>(agentId);
        var config = new InputConfigDto { Input = "Test input message for Plus version" };

        // Act
        await agent.ConfigAsync(config);

        // Assert
        var state = await agent.GetStateAsync();
        state.Input.ShouldBe("Test input message for Plus version");
    }

    [Fact]
    public async Task GetDescriptionAsync_Should_ReturnCorrectDescription()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IInputGAgentPlus>(agentId);

        // Act
        var description = await agent.GetDescriptionAsync();

        // Assert
        description.ShouldBe("Enhanced input agent that returns configured input text with advanced workflow integration");
    }

    #endregion

    #region InputGAgent-Specific Workflow Event Handling Tests

    [Fact]
    public async Task OnBusinessAgentEventForwardingEventHandlerAsync_Should_ProcessInputWorkflowEvent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = _grainFactory.GetGrain<ITestInputGAgentPlus>(agentId);
        var config = new InputConfigDto { Input = "Workflow input message" };
        
        await testAgent.ConfigAsync(config);

        var workflowEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = "Start workflow"
        };

        // Act - Test InputGAgent-specific workflow event handling
        var result = await testAgent.HandleWorkflowEventAsync(workflowEvent);

        // Assert - Verify InputGAgent-specific behavior
        result.ShouldBeTrue();
        
        // Get the captured workflow event from inside the method (Orleans deep copies prevent direct access)
        var capturedEvent = await testAgent.GetCapturedWorkflowEventAsync();
        capturedEvent.ShouldNotBeNull();
        capturedEvent.Message.ShouldBe("Workflow input message"); // InputGAgent sets message to State.Input
        capturedEvent.WorkflowEventType.ShouldBe(WorkflowEventType.WorkflowInProgress);
        capturedEvent.WorkflowAgentStatus.ShouldBe(WorkflowAgentStatus.Completed);
    }

    #endregion

    #region State Transition Tests

    [Fact]
    public async Task StateTransition_Should_HandleSetInputLogEvent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IInputGAgentPlus>(agentId);
        var initialConfig = new InputConfigDto { Input = "Initial Plus input" };
        
        await agent.ConfigAsync(initialConfig);
        var initialState = await agent.GetStateAsync();
        initialState.Input.ShouldBe("Initial Plus input");

        // Act - Update configuration
        var updatedConfig = new InputConfigDto { Input = "Updated Plus input" };
        await agent.ConfigAsync(updatedConfig);

        // Assert
        var updatedState = await agent.GetStateAsync();
        updatedState.Input.ShouldBe("Updated Plus input");
    }

    #endregion
}

/// <summary>
/// Test extension interface for InputGAgentPlus workflow event handling
/// </summary>
public interface ITestInputGAgentPlus : IInputGAgentPlus
{
    Task<bool> HandleWorkflowEventAsync(WorkflowEvent workflowEvent);
    Task<WorkflowEvent?> GetCapturedWorkflowEventAsync();
}

/// <summary>
/// Test extension for InputGAgentPlus to expose protected methods for testing class-specific behavior
/// </summary>
[GAgent("test-input-gagent-plus", "test")]
public class TestInputGAgentPlus : InputGAgentPlus, ITestInputGAgentPlus
{
    private WorkflowEvent? _capturedWorkflowEvent;

    public async Task<bool> HandleWorkflowEventAsync(WorkflowEvent workflowEvent)
    {
        // Call the full BusinessAgentBase flow which will internally call our overridden method
        return await OnEventForwardingEventHandlerAsync(workflowEvent);
    }

    public Task<WorkflowEvent?> GetCapturedWorkflowEventAsync()
    {
        return Task.FromResult(_capturedWorkflowEvent);
    }

    protected override async Task OnBusinessAgentEventForwardingEventHandlerAsync(WorkflowEvent workflowEvent)
    {
        // Call the base InputGAgent implementation first
        await base.OnBusinessAgentEventForwardingEventHandlerAsync(workflowEvent);
        
        // Capture the workflow event after InputGAgent processing for test verification
        _capturedWorkflowEvent = CloneWorkflowEvent(workflowEvent);
    }

    private WorkflowEvent CloneWorkflowEvent(WorkflowEvent source)
    {
        return new WorkflowEvent
        {
            WorkflowId = source.WorkflowId,
            WorkflowEventType = source.WorkflowEventType,
            Message = source.Message,
            WorkUnitAgentId = source.WorkUnitAgentId,
            AgentName = source.AgentName,
            StepStartTime = source.StepStartTime,
            StepEndTime = source.StepEndTime,
            WorkflowAgentStatus = source.WorkflowAgentStatus,
            ErrorMessage = source.ErrorMessage,
            TaskResult = source.TaskResult,
            Metadata = source.Metadata != null ? new Dictionary<string, object>(source.Metadata) : null
        };
    }
}

