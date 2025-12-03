using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Workflow.Core;
using Newtonsoft.Json;
using Aevatar.Core.Placement;
using Microsoft.Extensions.Logging;
using Aevatar.GAgents.Workflow.Core.States;
using Aevatar.GAgents.Core;
using Aevatar.GAgents.Workflow.Core.Models;

namespace Aevatar.GAgents.Workflow;

[GAgent]
[SiloNamePatternPlacement("Projector")]
public class WorkflowExecutionRecordGAgentPlus :
    BusinessAgentBase<WorkflowExecutionRecordStatePlus, WorkflowExecutionRecordLogEvent, ConfigurationBase>, Core.IWorkflowExecutionRecordGAgentPlus
{
    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // Mark this agent as a workflow agent to exclude from topology discovery
        this._isWorkflowAgent = true;
        
        await base.OnGAgentActivateAsync(cancellationToken);
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "WorkflowExecutionRecordGAgent - Child of WorkflowCoordinatorGAgent for audit trail. " +
            "Receives workflow lifecycle events via automatic TEvent forwarding from coordinator parent. " +
            "Enhanced architecture: Coordinator registers ExecutionRecord as child for efficient event flow."
        );
    }
    
    protected override bool AreAllDependenciesReadyAsync(WorkflowEvent workflowEvent)
    {
        return true;
    }

    /// <summary>
    /// Override OnBusinessAgentEventForwardingEventHandlerAsync for workflow execution tracking
    /// </summary>
    protected override async Task OnBusinessAgentEventForwardingEventHandlerAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogDebug("ExecutionRecordGAgent received WorkflowEvent: {WorkflowEventType} for workflow {WorkflowId}",
            workflowEvent.WorkflowEventType, workflowEvent.WorkflowId);

        try
        {
            // Handle different workflow event types for execution record tracking
            switch (workflowEvent.WorkflowEventType)
            {
                case WorkflowEventType.WorkflowStarted:
                    await HandleWorkflowStartedAsync(workflowEvent);
                    break;

                case WorkflowEventType.WorkflowInProgress:
                    await HandleWorkflowAsync(workflowEvent);
                    break;

                case WorkflowEventType.WorkflowCompleted:
                    Logger.LogInformation("[ExecutionRecordGAgent] Received WorkflowCompleted event for WorkflowId: {WorkflowId}, WorkflowAgentStatus: {AgentStatus}", 
                        workflowEvent.WorkflowId, workflowEvent.WorkflowAgentStatus);
                    await HandleWorkflowCompletedAsync(workflowEvent);
                    break;

                case WorkflowEventType.WorkflowFailed:
                    await HandleWorkflowFailedAsync(workflowEvent);
                    break;

                default:
                    Logger.LogDebug("ExecutionRecordGAgent ignoring WorkflowEventType: {EventType}",
                        workflowEvent.WorkflowEventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ExecutionRecordGAgent error processing WorkflowEvent: {WorkflowEventType}",
                workflowEvent.WorkflowEventType);
        }
    }

    private async Task HandleWorkflowStartedAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogInformation("[ExecutionRecordGAgent] HandleWorkflowStartedAsync for WorkflowId: {WorkflowId}, WorkUnitAgentId: {WorkUnitAgentId}", 
            workflowEvent.WorkflowId, workflowEvent.WorkUnitAgentId);
        
        // Extract workflow information from metadata
        var roundId = workflowEvent.Metadata.TryGetValue("RoundId", out var roundIdObj) && roundIdObj is long rid 
            ? rid : 0L;
        var content = workflowEvent.Metadata.TryGetValue("Content", out var contentObj) 
            ? contentObj?.ToString() : null;
        var workUnitInfos = workflowEvent.Metadata.TryGetValue("WorkUnitInfos", out var workUnitInfosObj) && 
                           workUnitInfosObj is List<WorkUnitInfo> workUnits
            ? workUnits : new List<WorkUnitInfo>();

        RaiseEvent(new StartExecuteWorkflowLogEvent
        {
            WorkflowId = workflowEvent.WorkflowId,
            RoundId = roundId,
            WorkUnitInfos = workUnitInfos,
            Content = content,
        });
        
        // Track WorkflowStartAgent as a completed work unit
        if (workflowEvent.WorkUnitAgentId != Guid.Empty)
        {
            Logger.LogInformation("[ExecutionRecordGAgent] Marking WorkflowStartAgent work unit as completed: {WorkUnitAgentId}", workflowEvent.WorkUnitAgentId);
            RaiseEvent(new FinishExecuteWorkUnitLogEvent
            {
                WorkUnitGrainId = workflowEvent.WorkUnitAgentId.ToString()
            });
        }
        
        await ConfirmEvents();
    }


    private async Task HandleWorkflowAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogDebug("ExecutionRecordGAgent handling WorkflowTaskCompleted for work unit agent {WorkUnitAgentId}", workflowEvent.WorkUnitAgentId);
        
        if (workflowEvent.ErrorMessage.IsNullOrEmpty())
        {
            // Track successful completion using WorkUnitAgentId to find the current work unit
            RaiseEvent(new FinishExecuteWorkUnitLogEvent
            {
                WorkUnitGrainId = workflowEvent.WorkUnitAgentId.ToString()
            });
        }
        else
        {
            RaiseEvent(new FailExecuteWorkflowLogEvent()
            {
                WorkUnitGrainId = workflowEvent.WorkUnitAgentId.ToString(),
                FailureSummary = workflowEvent.ErrorMessage
            });
        }

        await ConfirmEvents();
    }

    private async Task HandleWorkflowCompletedAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogInformation("[ExecutionRecordGAgent] HandleWorkflowCompletedAsync called for WorkflowId: {WorkflowId}, WorkUnitAgentId: {WorkUnitAgentId}, Current Status: {CurrentStatus}", 
            workflowEvent.WorkflowId, workflowEvent.WorkUnitAgentId, State.Status);
        
        // Track WorkflowEndAgent as a completed work unit before completing overall workflow
        if (workflowEvent.WorkUnitAgentId != Guid.Empty)
        {
            Logger.LogInformation("[ExecutionRecordGAgent] Marking WorkflowEndAgent work unit as completed: {WorkUnitAgentId}", workflowEvent.WorkUnitAgentId);
            RaiseEvent(new FinishExecuteWorkUnitLogEvent
            {
                WorkUnitGrainId = workflowEvent.WorkUnitAgentId.ToString()
            });
        }
        
        RaiseEvent(new FinishExecuteWorkflowLogEvent());
        await ConfirmEvents();
        
        Logger.LogInformation("[ExecutionRecordGAgent] FinishExecuteWorkflowLogEvent raised and confirmed, Status should now be: Completed");
        
        // Unregister from parent after processing completion event
        try
        {
            // Use WorkflowId from the event as the coordinator's ID (as pointed out by user)
            var coordinatorId = workflowEvent.WorkflowId;
            if (coordinatorId != Guid.Empty)
            {
                var parentCoordinator = GrainFactory.GetGrain<IWorkflowCoordinatorGAgentPlus>(coordinatorId);
                await UnregisterParentAsync(parentCoordinator);
                Logger.LogInformation("🔌 [ExecutionRecordGAgent] Successfully unregistered from parent coordinator {CoordinatorId} after workflow completion", coordinatorId);
            }
            else
            {
                Logger.LogWarning("[ExecutionRecordGAgent] No coordinator ID found in workflow event to unregister from");
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[ExecutionRecordGAgent] Failed to unregister from parent coordinator, but workflow completion is still valid");
        }
    }

    private async Task HandleWorkflowFailedAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogDebug("ExecutionRecordGAgent handling WorkflowFailed for work unit agent {WorkUnitAgentId}", workflowEvent.WorkUnitAgentId);
        
        RaiseEvent(new FailExecuteWorkflowLogEvent()
        {
            WorkUnitGrainId = workflowEvent.WorkUnitAgentId.ToString(),
            FailureSummary = workflowEvent.ErrorMessage ?? "Task failed without specific error message"
        });
        await ConfirmEvents();
        
        // Unregister from parent after processing failure event
        try
        {
            // Use WorkflowId from the event as the coordinator's ID (as pointed out by user)
            var coordinatorId = workflowEvent.WorkflowId;
            if (coordinatorId != Guid.Empty)
            {
                var parentCoordinator = GrainFactory.GetGrain<IWorkflowCoordinatorGAgentPlus>(coordinatorId);
                await UnregisterParentAsync(parentCoordinator);
                Logger.LogInformation("🔌 [ExecutionRecordGAgent] Successfully unregistered from parent coordinator {CoordinatorId} after workflow failure", coordinatorId);
            }
            else
            {
                Logger.LogWarning("[ExecutionRecordGAgent] No coordinator ID found in workflow event to unregister from");
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[ExecutionRecordGAgent] Failed to unregister from parent coordinator, but workflow failure is still recorded");
        }
    }

    protected override void GAgentTransitionState(WorkflowExecutionRecordStatePlus state,
        StateLogEventBase<WorkflowExecutionRecordLogEvent> @event)
    {
        switch (@event)
        {
            case StartExecuteWorkflowLogEvent startExecuteWorkflowLogEvent:
                state.WorkflowId = startExecuteWorkflowLogEvent.WorkflowId;
                state.RoundId = startExecuteWorkflowLogEvent.RoundId;
                state.WorkUnitInfos = startExecuteWorkflowLogEvent.WorkUnitInfos;
                state.InitContent = startExecuteWorkflowLogEvent.Content;
                state.StartTime = DateTime.UtcNow;
                state.Status = WorkflowExecutionStatus.Running;
                state.WorkUnitRecords = startExecuteWorkflowLogEvent.WorkUnitInfos.Select(o =>
                    new WorkUnitExecutionRecord
                    {
                        WorkUnitGrainId = o.AgentId.ToString(),
                        Status = WorkflowExecutionStatus.Pending
                    }).ToList();
                break;
            case FinishExecuteWorkflowLogEvent finishExecuteWorkflowLogEvent:
                Logger.LogInformation("[ExecutionRecordGAgent] Processing FinishExecuteWorkflowLogEvent - Setting status from {OldStatus} to Completed", state.Status);
                state.EndTime = DateTime.UtcNow;
                state.Status = WorkflowExecutionStatus.Completed;
                Logger.LogInformation("[ExecutionRecordGAgent] Status successfully set to: {NewStatus} at {EndTime}", state.Status, state.EndTime);
                break;
            case StartExecuteWorkUnitLogEvent startExecuteWorkUnitLogEvent:
                var startingWorkUnits = state.WorkUnitRecords.Where(o =>
                    o.WorkUnitGrainId == startExecuteWorkUnitLogEvent.WorkUnitGrainId).ToList();
                
                if (startingWorkUnits.Any())
                {
                    Logger.LogInformation("[ExecutionRecordGAgent] Processing StartExecuteWorkUnitLogEvent for WorkUnit {WorkUnitGrainId} - Found {Count} matching records to start", 
                        startExecuteWorkUnitLogEvent.WorkUnitGrainId, startingWorkUnits.Count);
                    
                    var startTime = DateTime.UtcNow;
                    foreach (var workUnit in startingWorkUnits)
                    {
                        Logger.LogInformation("[ExecutionRecordGAgent] Starting WorkUnit record - Setting status from {OldStatus} to Running", workUnit.Status);
                        workUnit.WorkUnitGrainId = startExecuteWorkUnitLogEvent.WorkUnitGrainId;
                        workUnit.StartTime = startTime;
                        if (workUnit.Status == WorkflowExecutionStatus.Pending)
                        {
                            workUnit.Status = WorkflowExecutionStatus.Running;
                        }
                    }
                    
                    Logger.LogInformation("[ExecutionRecordGAgent] Successfully updated {Count} WorkUnit records for {WorkUnitGrainId} to status: Running at {StartTime}", 
                        startingWorkUnits.Count, startExecuteWorkUnitLogEvent.WorkUnitGrainId, startTime);
                }
                else
                {
                    Logger.LogWarning("[ExecutionRecordGAgent] No WorkUnit records found for {WorkUnitGrainId} in WorkUnitRecords for start event", startExecuteWorkUnitLogEvent.WorkUnitGrainId);
                }
                break;

            case FinishExecuteWorkUnitLogEvent finishExecuteWorkUnitLogEvent:
                var matchingWorkUnits = state.WorkUnitRecords.Where(o =>
                    o.WorkUnitGrainId == finishExecuteWorkUnitLogEvent.WorkUnitGrainId).ToList();
                
                if (matchingWorkUnits.Any())
                {
                    Logger.LogInformation("[ExecutionRecordGAgent] Processing FinishExecuteWorkUnitLogEvent for WorkUnit {WorkUnitGrainId} - Found {Count} matching records to update", 
                        finishExecuteWorkUnitLogEvent.WorkUnitGrainId, matchingWorkUnits.Count);
                    
                    var endTime = DateTime.UtcNow;
                    foreach (var workUnit in matchingWorkUnits)
                    {
                        Logger.LogInformation("[ExecutionRecordGAgent] Updating WorkUnit record - Setting status from {OldStatus} to Completed", workUnit.Status);
                        workUnit.EndTime = endTime;
                        workUnit.Status = WorkflowExecutionStatus.Completed;
                    }
                    
                    Logger.LogInformation("[ExecutionRecordGAgent] Successfully updated {Count} WorkUnit records for {WorkUnitGrainId} to status: Completed at {EndTime}", 
                        matchingWorkUnits.Count, finishExecuteWorkUnitLogEvent.WorkUnitGrainId, endTime);
                }
                else
                {
                    Logger.LogWarning("[ExecutionRecordGAgent] No WorkUnit records found for {WorkUnitGrainId} in WorkUnitRecords", finishExecuteWorkUnitLogEvent.WorkUnitGrainId);
                }
                break;
            case FailExecuteWorkflowLogEvent failExecuteWorkflowLogEvent:
                var failingWorkUnits = state.WorkUnitRecords.Where(o =>
                    o.WorkUnitGrainId == failExecuteWorkflowLogEvent.WorkUnitGrainId).ToList();
                
                if (failingWorkUnits.Any())
                {
                    Logger.LogInformation("[ExecutionRecordGAgent] Processing FailExecuteWorkflowLogEvent for WorkUnit {WorkUnitGrainId} - Found {Count} matching records to mark as failed", 
                        failExecuteWorkflowLogEvent.WorkUnitGrainId, failingWorkUnits.Count);
                    
                    var endTime = DateTime.UtcNow;
                    foreach (var workUnit in failingWorkUnits)
                    {
                        Logger.LogInformation("[ExecutionRecordGAgent] Marking WorkUnit record as failed - Setting status from {OldStatus} to Failed", workUnit.Status);
                        workUnit.EndTime = endTime;
                        workUnit.Status = WorkflowExecutionStatus.Failed;
                        workUnit.FailureSummary = failExecuteWorkflowLogEvent.FailureSummary;
                    }
                    
                    Logger.LogInformation("[ExecutionRecordGAgent] Successfully updated {Count} WorkUnit records for {WorkUnitGrainId} to status: Failed at {EndTime}", 
                        failingWorkUnits.Count, failExecuteWorkflowLogEvent.WorkUnitGrainId, endTime);
                }
                else
                {
                    Logger.LogWarning("[ExecutionRecordGAgent] No WorkUnit records found for {WorkUnitGrainId} in WorkUnitRecords for failure event", failExecuteWorkflowLogEvent.WorkUnitGrainId);
                }

                state.Status = WorkflowExecutionStatus.Failed;
                state.EndTime = DateTime.UtcNow;
                break;
        }
    }
}

[GenerateSerializer]
public class WorkflowExecutionRecordLogEvent : StateLogEventBase<WorkflowExecutionRecordLogEvent>
{
    
}

[GenerateSerializer]
public class StartExecuteWorkflowLogEvent : WorkflowExecutionRecordLogEvent
{
    [Id(0)] public Guid WorkflowId { get; set; }
    [Id(1)] public long RoundId { get; set; }
    [Id(2)] public List<WorkUnitInfo> WorkUnitInfos { get; set; } = new();
    [Id(3)] public string? Content { get; set; } = null;
}

[GenerateSerializer]
public class FinishExecuteWorkflowLogEvent : WorkflowExecutionRecordLogEvent
{

}

[GenerateSerializer]
public class StartExecuteWorkUnitLogEvent : WorkflowExecutionRecordLogEvent
{
    [Id(0)]
    public string WorkUnitGrainId { get; set; }
}

[GenerateSerializer]
public class FinishExecuteWorkUnitLogEvent : WorkflowExecutionRecordLogEvent
{
    [Id(0)]
    public string WorkUnitGrainId { get; set; }
}

[GenerateSerializer]
public class FailExecuteWorkflowLogEvent : WorkflowExecutionRecordLogEvent
{
    [Id(0)]
    public string WorkUnitGrainId { get; set; }
    [Id(1)]
    public string FailureSummary { get; set; }
}