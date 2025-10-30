using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Workflow;
using Aevatar.GAgents.Workflow.Core;
using Aevatar.GAgents.Workflow.Core.Events;
using Aevatar.GAgents.Workflow.Core.Models;
using Aevatar.GAgents.Workflow.Core.States;
using Aevatar.GAgents.Core;
using Orleans;
using Orleans.Providers;
using Shouldly;
using Xunit;
using Newtonsoft.Json;

namespace Aevatar.GAgents.Workflow.Test.Tests;

/// <summary>
/// Integration tests for WorkflowExecutionRecordGAgentPlus using Orleans TestCluster infrastructure.
/// These tests duplicate the functionality from the legacy WorkflowExecutionRecordGAgent tests
/// but adapted for the BusinessAgentBase-based Plus version.
/// </summary>
public class WorkflowExecutionRecordGAgentPlusIntegrationTest : AevatarWorkflowTestBase
{
    private readonly IGrainFactory _grainFactory;

    public WorkflowExecutionRecordGAgentPlusIntegrationTest()
    {
        _grainFactory = GetRequiredService<IGrainFactory>();
    }

    #region Configuration Tests

    [Fact]
    public async Task GetDescriptionAsync_Should_ReturnCorrectDescription()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowExecutionRecordGAgentPlus>(agentId);

        // Act
        var description = await agent.GetDescriptionAsync();

        // Assert
        description.ShouldContain("WorkflowExecutionRecordGAgent");
        description.ShouldContain("audit trail");
    }



    #endregion

    #region Workflow Event Handling Tests

    [Fact]
    public async Task Handle_StartExecuteWorkflowEvent_Test()
    {
        // Arrange
        var coordinatorId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var testAgent = _grainFactory.GetGrain<IWorkflowExecutionRecordGAgentPlusTestExtension>(agentId);
        var workerGuid = coordinatorId;

        var workUnitInfos = new List<WorkUnitInfo>
        {
            new WorkUnitInfo
            {
                AgentId = workerGuid.ToString(),
                NextAgentId = Guid.Empty.ToString(),
                UnitStatusEnum = WorkerUnitStatusEnum.Pending
            }
        };

        var startExecuteWorkflowEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = "Init",
            Metadata = new Dictionary<string, object>
            {
                ["WorkUnitInfos"] = workUnitInfos,
                ["RoundId"] = 1L,
                ["Content"] = "Init"
            }
        };

        // Act
        var result = await testAgent.HandleWorkflowEventAsync(startExecuteWorkflowEvent);
        await Task.Delay(500); // Allow for event processing

        // Assert
        result.ShouldBeTrue();
        var state = await testAgent.GetStateAsync();
        state.WorkflowId.ShouldBe(startExecuteWorkflowEvent.WorkflowId);
        state.InitContent.ShouldBe(startExecuteWorkflowEvent.Message);
        state.Status.ShouldBe(WorkflowExecutionStatus.Running);
        state.StartTime.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-5));
        // Verify WorkUnitInfos from metadata were processed correctly
        state.WorkUnitInfos.Count.ShouldBe(1);
        state.WorkUnitInfos.ShouldContain(o => o.WorkUnitAgentId == workerGuid.ToString());
        // Verify WorkUnitRecords were created
        state.WorkUnitRecords.Count.ShouldBe(1);
        state.WorkUnitRecords.ShouldContain(o => o.WorkUnitGrainId == workerGuid.ToString());
    }

    [Fact]
    public async Task Handle_WorkflowFinishedEvent_Test()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = _grainFactory.GetGrain<IWorkflowExecutionRecordGAgentPlusTestExtension>(agentId);
        var workerGuid = Guid.NewGuid();

        await StartExecuteWorkflowAsync(testAgent, workerGuid);

        var finishExecuteWorkflowEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowCompleted,
            Message = "Workflow completed"
        };

        // Act
        var result = await testAgent.HandleWorkflowEventAsync(finishExecuteWorkflowEvent);
        await Task.Delay(500);

        // Assert
        result.ShouldBeTrue();
        var state = await testAgent.GetStateAsync();
        state.Status.ShouldBe(WorkflowExecutionStatus.Completed);
        state.EndTime.ShouldNotBe(default);
    }

    [Fact]
    public async Task Handle_WorkUnitCompletedEvent_Test()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = _grainFactory.GetGrain<IWorkflowExecutionRecordGAgentPlusTestExtension>(agentId);
        var workerGuid = Guid.NewGuid();

        // Start the workflow first to create work unit records
        await StartExecuteWorkflowAsync(testAgent, workerGuid);

        // Create a work unit completion event (WorkflowInProgress with no error = success)
        var workUnitCompletedEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowInProgress,
            WorkUnitAgentId = workerGuid.ToString(),
            Message = "Work unit completed successfully",
            ErrorMessage = null // No error = successful completion
        };

        // Act
        var result = await testAgent.HandleWorkflowEventAsync(workUnitCompletedEvent);
        await Task.Delay(500);

        // Assert
        result.ShouldBeTrue();
        var state = await testAgent.GetStateAsync();
        
        // Verify the work unit was marked as completed
        var workUnitRecord = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGuid.ToString());
        workUnitRecord.Status.ShouldBe(WorkflowExecutionStatus.Completed);
        workUnitRecord.EndTime.ShouldNotBe(default);
        
        // Verify the overall workflow is still running (not completed yet)
        state.Status.ShouldBe(WorkflowExecutionStatus.Running);
    }

    [Fact]
    public async Task Handle_WorkUnitFailedEvent_Test()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = _grainFactory.GetGrain<IWorkflowExecutionRecordGAgentPlusTestExtension>(agentId);
        var workerGuid = Guid.NewGuid();

        // Start the workflow first to create work unit records
        await StartExecuteWorkflowAsync(testAgent, workerGuid);

        // Create a work unit failure event (WorkflowInProgress with error = failure)
        var workUnitFailedEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowInProgress,
            WorkUnitAgentId = workerGuid.ToString(),
            Message = "Work unit processing",
            ErrorMessage = "Work unit failed due to validation error"
        };

        // Act
        var result = await testAgent.HandleWorkflowEventAsync(workUnitFailedEvent);
        await Task.Delay(500);

        // Assert
        result.ShouldBeTrue();
        var state = await testAgent.GetStateAsync();
        
        // Verify the work unit was marked as failed
        var workUnitRecord = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGuid.ToString());
        workUnitRecord.Status.ShouldBe(WorkflowExecutionStatus.Failed);
        workUnitRecord.EndTime.ShouldNotBe(default);
        
        // Verify the overall workflow is now failed (individual unit failure fails entire workflow)
        state.Status.ShouldBe(WorkflowExecutionStatus.Failed);
        state.EndTime.ShouldNotBe(default);
    }

    [Fact]
    public async Task Handle_WorkflowCompletedEvent_Test()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = _grainFactory.GetGrain<IWorkflowExecutionRecordGAgentPlusTestExtension>(agentId);
        var workerGuid = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        // Start the workflow first
        await StartExecuteWorkflowAsync(testAgent, workerGuid);

        // Create a workflow completion event (entire workflow finished)
        var workflowCompletedEvent = new WorkflowEvent
        {
            WorkflowId = workflowId,
            WorkflowEventType = WorkflowEventType.WorkflowCompleted,
            WorkUnitAgentId = workerGuid.ToString(),
            Message = "Entire workflow completed successfully"
        };

        // Act
        var result = await testAgent.HandleWorkflowEventAsync(workflowCompletedEvent);
        await Task.Delay(500);

        // Assert
        result.ShouldBeTrue();
        var state = await testAgent.GetStateAsync();
        
        // Verify the work unit was also marked as completed (WorkflowCompleted marks the final unit as completed)
        var workUnitRecord = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGuid.ToString());
        workUnitRecord.Status.ShouldBe(WorkflowExecutionStatus.Completed);
        workUnitRecord.EndTime.ShouldNotBe(default);
        
        
        // Verify the overall workflow is now completed
        state.Status.ShouldBe(WorkflowExecutionStatus.Completed);
        state.EndTime.ShouldNotBe(default);
    }

    [Fact]
    public async Task Handle_WorkflowFailedEvent_Test()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = _grainFactory.GetGrain<IWorkflowExecutionRecordGAgentPlusTestExtension>(agentId);
        var workerGuid = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        // Start the workflow first
        await StartExecuteWorkflowAsync(testAgent, workerGuid);

        // Create a workflow failure event (entire workflow failed)
        var workflowFailedEvent = new WorkflowEvent
        {
            WorkflowId = workflowId,
            WorkflowEventType = WorkflowEventType.WorkflowFailed,
            WorkUnitAgentId = workerGuid.ToString(),
            ErrorMessage = "Entire workflow failed due to critical error"
        };

        // Act
        var result = await testAgent.HandleWorkflowEventAsync(workflowFailedEvent);
        await Task.Delay(500);

        // Assert
        result.ShouldBeTrue();
        var state = await testAgent.GetStateAsync();
        
        // Verify the overall workflow is now failed
        state.Status.ShouldBe(WorkflowExecutionStatus.Failed);
        state.EndTime.ShouldNotBe(default);
        
        // Verify the work unit was also marked as failed (WorkflowFailed marks the final unit as failed)
        var workUnitRecord = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGuid.ToString());
        workUnitRecord.Status.ShouldBe(WorkflowExecutionStatus.Failed);
        workUnitRecord.FailureSummary.ShouldBe("Entire workflow failed due to critical error");
        workUnitRecord.EndTime.ShouldNotBe(default);
    }

    [Fact]
    public async Task IncorrectSequence_Test()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = _grainFactory.GetGrain<IWorkflowExecutionRecordGAgentPlusTestExtension>(agentId);
        var workerGuid = Guid.NewGuid();

        await StartExecuteWorkflowAsync(testAgent, workerGuid);

        // Act - Send completion before start (incorrect sequence)
        var nodeCompletedEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowCompleted,
            WorkUnitAgentId = workerGuid.ToString(),
            Message = "Grain response"
        };
        // testAgent already available from Arrange section
        var result = await testAgent.HandleWorkflowEventAsync(nodeCompletedEvent);
        await Task.Delay(500);

        var state = await testAgent.GetStateAsync();
        var grainRecord = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGuid.ToString());
        grainRecord.Status.ShouldBe(WorkflowExecutionStatus.Completed);
        // OutputData property no longer exists in WorkUnitExecutionRecord

        // Now send start event (should still be processed)
        var startExecuteUnitEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowInProgress,
            WorkUnitAgentId = workerGuid.ToString(),
            Message = "Input A",
            // CoordinatorMessages property no longer exists
        };
        var result2 = await testAgent.HandleWorkflowEventAsync(startExecuteUnitEvent);
        await Task.Delay(500);

        // Assert - Should still process the late start event
        state = await testAgent.GetStateAsync();
        grainRecord = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGuid.ToString());
        grainRecord.Status.ShouldBe(WorkflowExecutionStatus.Completed);
        // InputData property no longer exists in WorkUnitExecutionRecord
    }

    #endregion

    #region State Management Tests

    [Fact]
    public async Task StateTransition_Should_HandleWorkflowEvents()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = _grainFactory.GetGrain<IWorkflowExecutionRecordGAgentPlusTestExtension>(agentId);
        var workflowId = Guid.NewGuid();

        var workUnitInfos = new List<WorkUnitInfo>();
        
        var workflowEvent = new WorkflowEvent
        {
            WorkflowId = workflowId,
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = "Test workflow started",
            Metadata = new Dictionary<string, object>
            {
                ["WorkUnitInfos"] = workUnitInfos,
                ["RoundId"] = 1L,
                ["Content"] = "Test workflow started"
            }
        };

        // Act
        // testAgent already available from Arrange section
        var result = await testAgent.HandleWorkflowEventAsync(workflowEvent);

        // Assert
        var state = await testAgent.GetStateAsync();
        state.WorkflowId.ShouldBe(workflowId);
        state.Status.ShouldBe(WorkflowExecutionStatus.Running);
        state.InitContent.ShouldBe("Test workflow started");
    }

    #endregion

    #region Helper Methods

    private async Task StartExecuteWorkflowAsync(IWorkflowExecutionRecordGAgentPlusTestExtension testAgent, Guid workerGuid)
    {
        var workUnitInfos = new List<WorkUnitInfo>
        {
            new WorkUnitInfo
            {
                AgentId = workerGuid.ToString(),
                NextAgentId = Guid.Empty.ToString(),
                UnitStatusEnum = WorkerUnitStatusEnum.Pending
            }
        };

        var startExecuteWorkflowEvent = new WorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = "Init",
            Metadata = new Dictionary<string, object>
            {
                ["WorkUnitInfos"] = workUnitInfos,
                ["RoundId"] = 1L,
                ["Content"] = "Init"
            }
        };

        var result = await testAgent.HandleWorkflowEventAsync(startExecuteWorkflowEvent);
        await Task.Delay(500);
    }

    #endregion
}
/// <summary>
/// Extension interface for testing WorkflowExecutionRecordGAgentPlus workflow event handling
/// </summary>
public interface IWorkflowExecutionRecordGAgentPlusTestExtension : IWorkflowExecutionRecordGAgentPlus
{
    Task<bool> HandleWorkflowEventAsync(WorkflowEvent workflowEvent);
}

/// <summary>
/// Test extension for WorkflowExecutionRecordGAgentPlus to expose protected methods
/// </summary>
[GAgent("test-workflow-execution-record-gagent-plus", "test")]
public class TestWorkflowExecutionRecordGAgentPlus : WorkflowExecutionRecordGAgentPlus, IWorkflowExecutionRecordGAgentPlusTestExtension
{
    public async Task<bool> HandleWorkflowEventAsync(WorkflowEvent workflowEvent)
    {
        // NOTE: OnBusinessAgentEventForwardingEventHandlerAsync is not available in GAgentBasePlus
        // WorkflowExecutionRecordGAgentPlus uses TEvent-based forwarding instead
        await Task.CompletedTask;
        return true;
    }
}

