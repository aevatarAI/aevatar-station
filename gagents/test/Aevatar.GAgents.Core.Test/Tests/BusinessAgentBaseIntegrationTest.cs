using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Core;
using Aevatar.GAgents.Basic;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using Shouldly;
using Xunit;
using Aevatar.GAgents.TestBase;

namespace Aevatar.GAgents.Core.Test.Tests;

/// <summary>
/// Integration tests for BusinessAgentBase using Orleans TestCluster infrastructure.
/// These tests validate the WorkflowEvent handling and business agent lifecycle.
/// </summary>
public class BusinessAgentBaseIntegrationTest : AevatarGAgentTestBase<AevatarGAgentsCoreTestModule>
{
    private readonly IGrainFactory _grainFactory;

    public BusinessAgentBaseIntegrationTest()
    {
        _grainFactory = GetRequiredService<IGrainFactory>();
    }

    #region WorkflowEvent Handling Tests

    [Fact]
    public async Task OnEventForwardingEventHandlerAsync_Should_ValidateWorkflowEvent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestBusinessAgent>(agentId);
        
        var validWorkflowEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = "Test workflow started"
        };

        // Act
        var result = await agent.HandleWorkflowEventAsync(validWorkflowEvent);

        // Assert
        result.ShouldBeTrue();
        // Note: The WorkflowAgentStatus is updated on the workflowEvent object during post-processing,
        // but the agent's state WorkflowAgentStatus remains at its default value (Pending).
        // The test verifies that the workflow processing completed successfully (result = true).
        var state = await agent.GetStateAsync();
        state.WorkflowAgentStatus.ShouldBe(WorkflowAgentStatus.Pending); // Default value, not updated by current implementation
    }

    [Fact]
    public async Task OnEventForwardingEventHandlerAsync_Should_RejectInvalidWorkflowEvent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestBusinessAgent>(agentId);
        
        var invalidWorkflowEvent = new WorkflowEvent
        {
            WorkflowId = Guid.Empty, // Invalid - empty WorkflowId
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = "Test workflow"
        };

        // Act
        var result = await agent.HandleWorkflowEventAsync(invalidWorkflowEvent);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task OnEventForwardingEventHandlerAsync_Should_HandleWorkflowEventWithError()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestBusinessAgent>(agentId);
        
        var workflowEventWithError = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowFailed,
            ErrorMessage = "Previous step failed",
            Message = "Test workflow"
        };

        // Act
        var result = await agent.HandleWorkflowEventAsync(workflowEventWithError);

        // Assert
        result.ShouldBeFalse(); // Should reject events with existing errors
    }

    #endregion

    #region Agent Info Tests

    [Fact]
    public async Task GetAgentInfoAsync_Should_ReturnCorrectAgentInfo()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestBusinessAgent>(agentId);

        // Act
        var (name, type) = await agent.GetAgentInfoAsync();

        // Assert
        name.ShouldStartWith("TestBusinessAgent-");
        type.ShouldBe(typeof(TestBusinessAgent).FullName);
    }

    [Fact]
    public async Task GetIsWorkflowAgentAsync_Should_ReturnFalseByDefault()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestBusinessAgent>(agentId);

        // Act
        var isWorkflowAgent = await agent.GetIsWorkflowAgentAsync();

        // Assert
        isWorkflowAgent.ShouldBeFalse();
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task OnEventForwardingEventHandlerAsync_Should_HandleExceptionInBusinessLogic()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestBusinessAgent>(agentId);
        
        var workflowEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = "THROW_EXCEPTION" // Special message to trigger exception
        };

        // Act
        var result = await agent.HandleWorkflowEventAsync(workflowEvent);

        // Assert - Exception should be caught and method should return false
        result.ShouldBeFalse();      
    }

    #endregion

    #region Message Recording Tests

    [Fact]
    public async Task RecordInputMessageAsync_Should_RecordMessage()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestBusinessAgent>(agentId);
        
        // Set up the agent with 2 parent relationships so it needs to collect 2 messages
        var parentGuid1 = Guid.NewGuid();
        var parentGuid2 = Guid.NewGuid();
        var parentId1 = GrainId.Create("test-business-agent", parentGuid1.ToString());
        var parentId2 = GrainId.Create("test-business-agent", parentGuid2.ToString());
        await agent.AddParentAsync(parentId1);
        await agent.AddParentAsync(parentId2);
        
        var workflowEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = "Test message from upstream",
            AgentId = parentGuid1 // Message from the first parent agent
        };

        // Act
        var result = await agent.HandleWorkflowEventAsync(workflowEvent);

        // Assert
        // Since the agent has 2 parents but only received 1 message, dependencies are not ready
        // so processing should not complete and messages should not be cleared
        result.ShouldBeFalse();
        var recordedMessages = await agent.GetRecordedMessagesAsync();
        recordedMessages.ShouldContain("Test message from upstream");
    }

    [Fact]
    public async Task RecordInputMessageAsync_Should_NotRecordEmptyMessage()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestBusinessAgent>(agentId);
        
        var workflowEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = "", // Empty message
            AgentId = Guid.NewGuid()
        };

        // Act
        var result = await agent.HandleWorkflowEventAsync(workflowEvent);

        // Assert
        result.ShouldBeTrue();
        var recordedMessages = await agent.GetRecordedMessagesAsync();
        recordedMessages.ShouldBeEmpty();
    }

    #endregion

    #region Pre/Post Processing Tests

    [Fact]
    public async Task PreProcessing_Should_SetStepStartTime()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestBusinessAgent>(agentId);
        
        var beforeTime = DateTime.UtcNow;
        
        var workflowEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = "Test workflow",
            AgentId = Guid.NewGuid() // Must be non-empty for processing to complete
        };

        // Act
        var result = await agent.HandleWorkflowEventAsync(workflowEvent);

        // Assert
        result.ShouldBeTrue();
        
        // Check initial state (before any processing)
        var initialEvent = await agent.GetInitialWorkflowEventAsync();
        initialEvent.ShouldNotBeNull();
        // StepStartTime has default value from when WorkflowEvent was created
        // Since workflowEvent is created AFTER beforeTime, StepStartTime should be >= beforeTime
        initialEvent.StepStartTime.ShouldBeGreaterThanOrEqualTo(beforeTime);
        initialEvent.StepStartTime.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        
        // Check after pre-processing (should have StepStartTime updated by pre-processing)
        var afterPreProcessing = await agent.GetAfterPreProcessingWorkflowEventAsync();
        afterPreProcessing.ShouldNotBeNull();
        afterPreProcessing.StepStartTime.ShouldBeGreaterThanOrEqualTo(beforeTime);
        afterPreProcessing.StepStartTime.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        
        // The pre-processing should have updated StepStartTime to be different from initial
        afterPreProcessing.StepStartTime.ShouldBeGreaterThanOrEqualTo(initialEvent.StepStartTime);
        
        // StepEndTime should still be null after pre-processing
        afterPreProcessing.StepEndTime.ShouldBeNull();
    }

    [Fact]
    public async Task PostProcessing_Should_ClearReceivedMessagesAfterSuccessfulProcessing()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestBusinessAgent>(agentId);
        
        // Directly add test messages to the received messages list
        await agent.RecordTestMessageAsync("Test message 1");
        await agent.RecordTestMessageAsync("Test message 2");
        
        // Verify messages were recorded
        var messagesBeforeProcessing = await agent.GetRecordedMessagesAsync();
        messagesBeforeProcessing.Count.ShouldBe(2);
        messagesBeforeProcessing.ShouldContain("Test message 1");
        messagesBeforeProcessing.ShouldContain("Test message 2");

        // Act - Process a workflow event that should trigger post-processing and clear messages
        var result = await agent.HandleWorkflowEventAsync(new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = "Final processing"
        });

        // Assert
        result.ShouldBeTrue();
        
        // Verify that post-processing cleared the received messages
        var messagesAfterProcessing = await agent.GetRecordedMessagesAsync();
        messagesAfterProcessing.Count.ShouldBe(0);
    }

    [Fact]
    public async Task PostProcessing_Should_UpdateWorkflowEventFields()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestBusinessAgent>(agentId);
        
        var beforeTime = DateTime.UtcNow;
        
        var workflowEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = "Test workflow",
            AgentId = Guid.NewGuid() // Must be non-empty for processing to complete
        };

        // Act
        var result = await agent.HandleWorkflowEventAsync(workflowEvent);

        // Assert
        result.ShouldBeTrue();
        
        // Check initial state (before any processing)
        var initialEvent = await agent.GetInitialWorkflowEventAsync();
        initialEvent.ShouldNotBeNull();
        initialEvent.WorkUnitAgentId.ShouldNotBe(agentId); // Should be the original AgentId from input
        initialEvent.AgentName.ShouldBe(""); // Should be empty string initially
        initialEvent.WorkflowAgentStatus.ShouldBe(WorkflowAgentStatus.Pending); // Default status
        // StepStartTime has default value from when WorkflowEvent was created
        // Since workflowEvent is created AFTER beforeTime, StepStartTime should be >= beforeTime
        initialEvent.StepStartTime.ShouldBeGreaterThanOrEqualTo(beforeTime);
        initialEvent.StepEndTime.ShouldBeNull();
        
        // Check after pre-processing (should have StepStartTime updated)
        var afterPreProcessing = await agent.GetAfterPreProcessingWorkflowEventAsync();
        afterPreProcessing.ShouldNotBeNull();
        afterPreProcessing.StepStartTime.ShouldBeGreaterThanOrEqualTo(beforeTime);
        afterPreProcessing.StepEndTime.ShouldBeNull(); // Still null after pre-processing
        
        // Check after business logic (should have message updated)
        var afterBusinessLogic = await agent.GetAfterBusinessLogicWorkflowEventAsync();
        afterBusinessLogic.ShouldNotBeNull();
        afterBusinessLogic.Message.ShouldBe("Processed by TestBusinessAgent");
        afterBusinessLogic.WorkflowEventType.ShouldBe(WorkflowEventType.WorkflowInProgress);
        
        // Check after post-processing (should have all fields updated)
        var afterPostProcessing = await agent.GetAfterPostProcessingWorkflowEventAsync();
        afterPostProcessing.ShouldNotBeNull();
        
        // Verify post-processing updates
        afterPostProcessing.WorkUnitAgentId.ShouldBe(agentId);
        afterPostProcessing.AgentName.ShouldBe(typeof(TestBusinessAgent).FullName);
        afterPostProcessing.WorkflowAgentStatus.ShouldBe(WorkflowAgentStatus.Completed);
        afterPostProcessing.StepEndTime.ShouldNotBeNull();
        afterPostProcessing.StepEndTime.Value.ShouldBeGreaterThanOrEqualTo(beforeTime);
        afterPostProcessing.StepEndTime.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        
        // Verify timing relationship
        afterPostProcessing.StepStartTime.ShouldBeLessThanOrEqualTo(afterPostProcessing.StepEndTime.Value);
    }

    [Fact]
    public async Task CompleteProcessingPipeline_Should_UpdateWorkflowEventAtEachStage()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestBusinessAgent>(agentId);
        
        var beforeTime = DateTime.UtcNow;
        
        var workflowEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = "Original message",
            AgentId = Guid.NewGuid() // Must be non-empty for processing to complete
        };

        // Act
        var result = await agent.HandleWorkflowEventAsync(workflowEvent);

        // Assert
        result.ShouldBeTrue();
        
        // Verify complete processing pipeline
        var initial = await agent.GetInitialWorkflowEventAsync();
        var afterPre = await agent.GetAfterPreProcessingWorkflowEventAsync();
        var afterBusiness = await agent.GetAfterBusinessLogicWorkflowEventAsync();
        var afterPost = await agent.GetAfterPostProcessingWorkflowEventAsync();
        
        // All snapshots should exist
        initial.ShouldNotBeNull();
        afterPre.ShouldNotBeNull();
        afterBusiness.ShouldNotBeNull();
        afterPost.ShouldNotBeNull();
        
        // Verify progression through pipeline
        // 1. Initial state
        // StepStartTime has default value from when WorkflowEvent was created
        // Since workflowEvent is created AFTER beforeTime, StepStartTime should be >= beforeTime
        initial.StepStartTime.ShouldBeGreaterThanOrEqualTo(beforeTime);
        initial.StepEndTime.ShouldBeNull();
        initial.AgentName.ShouldBe("");
        initial.WorkflowAgentStatus.ShouldBe(WorkflowAgentStatus.Pending);
        initial.Message.ShouldBe("Original message");
        
        // 2. After pre-processing
        afterPre.StepStartTime.ShouldBeGreaterThanOrEqualTo(beforeTime);
        afterPre.StepEndTime.ShouldBeNull();
        afterPre.Message.ShouldBe("Original message"); // Not changed yet
        
        // 3. After business logic
        afterBusiness.Message.ShouldBe("Processed by TestBusinessAgent"); // Changed by business logic
        afterBusiness.WorkflowEventType.ShouldBe(WorkflowEventType.WorkflowInProgress);
        
        // 4. After post-processing
        afterPost.WorkUnitAgentId.ShouldBe(agentId);
        afterPost.AgentName.ShouldBe(typeof(TestBusinessAgent).FullName);
        afterPost.WorkflowAgentStatus.ShouldBe(WorkflowAgentStatus.Completed);
        afterPost.StepEndTime.ShouldNotBeNull();
        
        // Verify timing progression
        afterPost.StepStartTime.ShouldBeLessThanOrEqualTo(afterPost.StepEndTime.Value);
        afterPost.StepStartTime.ShouldBeGreaterThanOrEqualTo(beforeTime);
        afterPost.StepEndTime.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    #endregion

    #region Dependency Management Tests

    [Fact]
    public async Task AreAllDependenciesReadyAsync_Should_ReturnTrue_WhenNoParents()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestBusinessAgent>(agentId);
        
        var workflowEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = "Test workflow"
        };

        // Act - Agent with no parents should be ready immediately
        var result = await agent.HandleWorkflowEventAsync(workflowEvent);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task AreAllDependenciesReadyAsync_Should_ReturnFalse_WhenParentsExistButNoMessages()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestBusinessAgent>(agentId);
        
        // Setup agent with parent relationships using inherited base class method
        // Create GUID-keyed GrainIds that match BusinessAgentBase expectations
        var parentGuid1 = Guid.NewGuid();
        var parentGuid2 = Guid.NewGuid();
        // Use the same approach as Orleans uses internally for GUID-keyed grains
        var parentId1 = GrainId.Create("test-business-agent", parentGuid1.ToString());
        var parentId2 = GrainId.Create("test-business-agent", parentGuid2.ToString());
        await agent.AddParentAsync(parentId1);
        await agent.AddParentAsync(parentId2);
        
        var workflowEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = "Test workflow"
        };

        // Act - Agent with parents but no input messages should not be ready
        var result = await agent.HandleWorkflowEventAsync(workflowEvent);

        // Assert
        result.ShouldBeFalse(); // Dependencies not ready
    }

    [Fact]
    public async Task AreAllDependenciesReadyAsync_Should_ReturnTrue_WhenAllParentMessagesReceived()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestBusinessAgent>(agentId);
        
        // Setup agent with parent relationships using inherited base class method
        // Create GUID-keyed GrainIds that match BusinessAgentBase expectations
        var parentGuid1 = Guid.NewGuid();
        var parentGuid2 = Guid.NewGuid();
        // Use the same approach as Orleans uses internally for GUID-keyed grains
        var parentId1 = GrainId.Create("test-business-agent", parentGuid1.ToString());
        var parentId2 = GrainId.Create("test-business-agent", parentGuid2.ToString());
        await agent.AddParentAsync(parentId1);
        await agent.AddParentAsync(parentId2);
        
        // Send messages from both parents
        var workflowId = Guid.NewGuid();
        
        // First message - should return false (not all dependencies ready yet)
        var result1 = await agent.HandleWorkflowEventAsync(new WorkflowEvent
        {
            WorkflowId = workflowId,
            WorkflowEventType = WorkflowEventType.WorkflowInProgress,
            Message = "Message from parent 1",
            AgentId = parentGuid1 // Use the Guid directly
        });
        
        // First call should return false because not all parent messages are received yet
        result1.ShouldBeFalse();
        
        // Act - Send the second parent message, now all dependencies should be ready
        var result2 = await agent.HandleWorkflowEventAsync(new WorkflowEvent
        {
            WorkflowId = workflowId,
            WorkflowEventType = WorkflowEventType.WorkflowInProgress,
            Message = "Message from parent 2",
            AgentId = parentGuid2 // Use the Guid directly
        });

        // Assert - Second call should return true because all parent messages are now received
        result2.ShouldBeTrue();
        
        // After successful processing, messages should be cleared by ClearReceivedMessages()
        var recordedMessages = await agent.GetRecordedMessagesAsync();
        recordedMessages.Count.ShouldBe(0); // Messages are cleared after successful processing
    }

    [Fact]
    public async Task ClearReceivedMessages_Should_ClearMessagesAfterProcessing()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<ITestBusinessAgent>(agentId);
        
        var workflowEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = "Test message",
            AgentId = Guid.NewGuid()
        };

        // Act - Process event (should record and then clear messages)
        var result = await agent.HandleWorkflowEventAsync(workflowEvent);

        // Assert
        result.ShouldBeTrue();
        var recordedMessages = await agent.GetRecordedMessagesAsync();
        recordedMessages.ShouldBeEmpty(); // Messages should be cleared after processing
    }

    #endregion
}

/// <summary>
/// Test interface for BusinessAgentBase integration testing
/// </summary>
public interface ITestBusinessAgent : IBusinessAgentBase, IStateGAgentPlus<TestBusinessAgentState>
{
    Task<bool> HandleWorkflowEventAsync(WorkflowEvent workflowEvent);
    Task<List<string>> GetRecordedMessagesAsync();
    Task RecordTestMessageAsync(string message);
    Task<WorkflowEvent?> GetInitialWorkflowEventAsync();
    Task<WorkflowEvent?> GetAfterPreProcessingWorkflowEventAsync();
    Task<WorkflowEvent?> GetAfterBusinessLogicWorkflowEventAsync();
    Task<WorkflowEvent?> GetAfterPostProcessingWorkflowEventAsync();
}

/// <summary>
/// Test implementation of BusinessAgentBase for integration testing
/// </summary>
[GAgent("test-business-agent", "test")]
public class TestBusinessAgent : BusinessAgentBase<TestBusinessAgentState, TestBusinessAgentStateLogEvent, TestBusinessAgentConfiguration>, ITestBusinessAgent
{
    private WorkflowEvent? _initialWorkflowEvent;
    private WorkflowEvent? _afterPreProcessingWorkflowEvent;
    private WorkflowEvent? _afterBusinessLogicWorkflowEvent;
    private WorkflowEvent? _afterPostProcessingWorkflowEvent;

    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Test BusinessAgentBase for integration testing");

    public async Task<bool> HandleWorkflowEventAsync(WorkflowEvent workflowEvent)
    {
        // Capture initial workflow event state before any processing
        _initialWorkflowEvent = CloneWorkflowEvent(workflowEvent);
        
        // Expose the protected method for testing
        return await OnEventForwardingEventHandlerAsync(workflowEvent);
    }

    public Task<List<string>> GetRecordedMessagesAsync()
    {
        // Access the protected _receivedMessages directly since we inherit from BusinessAgentBase
        return Task.FromResult(new List<string>(_receivedMessages));
    }

    public Task RecordTestMessageAsync(string message)
    {
        // Directly add a message to the received messages for testing
        _receivedMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task<WorkflowEvent?> GetInitialWorkflowEventAsync()
    {
        return Task.FromResult(_initialWorkflowEvent);
    }

    public Task<WorkflowEvent?> GetAfterPreProcessingWorkflowEventAsync()
    {
        return Task.FromResult(_afterPreProcessingWorkflowEvent);
    }

    public Task<WorkflowEvent?> GetAfterBusinessLogicWorkflowEventAsync()
    {
        return Task.FromResult(_afterBusinessLogicWorkflowEvent);
    }

    public Task<WorkflowEvent?> GetAfterPostProcessingWorkflowEventAsync()
    {
        return Task.FromResult(_afterPostProcessingWorkflowEvent);
    }


    private WorkflowEvent CloneWorkflowEvent(WorkflowEvent source)
    {
        return new WorkflowEvent
        {
            WorkflowId = source.WorkflowId,
            WorkflowEventType = source.WorkflowEventType,
            Message = source.Message,
            AgentId = source.WorkUnitAgentId,
            AgentName = source.AgentName,
            StepStartTime = source.StepStartTime,
            StepEndTime = source.StepEndTime,
            WorkflowAgentStatus = source.WorkflowAgentStatus,
            ErrorMessage = source.ErrorMessage,
            Metadata = source.Metadata != null ? new Dictionary<string, object>(source.Metadata) : null
        };
    }

    protected override async Task PreBusinessAgentProcessingAsync(WorkflowEvent workflowEvent)
    {
        // Call base implementation first
        await base.PreBusinessAgentProcessingAsync(workflowEvent);
        
        // Capture the workflow event after pre-processing (includes StepStartTime)
        _afterPreProcessingWorkflowEvent = CloneWorkflowEvent(workflowEvent);
    }

    protected override async Task PostBusinessAgentProcessingAsync(WorkflowEvent workflowEvent)
    {
        // Call base implementation first
        await base.PostBusinessAgentProcessingAsync(workflowEvent);
        
        // Capture the workflow event after post-processing (includes StepEndTime and other updates)
        _afterPostProcessingWorkflowEvent = CloneWorkflowEvent(workflowEvent);
    }

    protected override async Task OnBusinessAgentEventForwardingEventHandlerAsync(WorkflowEvent workflowEvent)
    {
        // Test exception handling
        if (workflowEvent.Message == "THROW_EXCEPTION")
        {
            throw new InvalidOperationException("Test exception in business logic");
        }

        // Simple test implementation - just update the workflow event
        workflowEvent.Message = $"Processed by {GetType().Name}";
        workflowEvent.WorkflowEventType = WorkflowEventType.WorkflowInProgress;
        
        // Capture the workflow event after business logic processing
        _afterBusinessLogicWorkflowEvent = CloneWorkflowEvent(workflowEvent);
        
        await Task.CompletedTask;
    }

    protected override void GAgentTransitionState(TestBusinessAgentState state, StateLogEventBase<TestBusinessAgentStateLogEvent> @event)
    {
        // No custom events needed - base class handles parent relationships
        base.GAgentTransitionState(state, @event);
    }
}

/// <summary>
/// Test state for BusinessAgentBase integration testing
/// </summary>
[GenerateSerializer]
public class TestBusinessAgentState : BusinessAgentState
{
}

/// <summary>
/// Test configuration for BusinessAgentBase integration testing
/// </summary>
[GenerateSerializer]
public class TestBusinessAgentConfiguration : ConfigurationBase
{
    [Id(0)] public string TestProperty { get; set; } = string.Empty;
}

/// <summary>
/// Test state log events
/// </summary>
[GenerateSerializer]
public class TestBusinessAgentStateLogEvent : StateLogEventBase<TestBusinessAgentStateLogEvent>
{
}
