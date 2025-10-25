using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Workflow.Core;
using Newtonsoft.Json;
using Aevatar.Core.Placement;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.GAgents.Workflow.Core.States;
using Aevatar.GAgents.Core;
using Aevatar.GAgents.Workflow.Core.Models;
using Orleans;

namespace Aevatar.GAgents.Workflow;

[GAgent]
[SiloNamePatternPlacement("Projector")]
public class WorkflowExecutionRecordGAgentPlus :
    GAgentBasePlus<WorkflowExecutionRecordStatePlus, WorkflowExecutionRecordLogEvent, WorkflowEvent, ConfigurationBase>, Core.IWorkflowExecutionRecordGAgentPlus
{
    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // WorkflowExecutionRecordGAgent is a system agent (recorder), not a business agent
        await base.OnGAgentActivateAsync(cancellationToken);
    }
    
    /// <summary>
    /// Returns true to indicate this is a workflow system agent
    /// (Not a business processing agent)
    /// </summary>
    public Task<bool> GetIsWorkflowAgentAsync()
    {
        return Task.FromResult(true);
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "✅ REFACTORED WorkflowExecutionRecordGAgent - Child of WorkflowCoordinatorGAgent for audit trail. " +
            "Receives workflow lifecycle events via automatic TEvent forwarding from coordinator parent. " +
            "Now directly inherits from GAgentBasePlus - not a business processor, purely a system recorder."
        );
    }

    /// <summary>
    /// Capture the current state snapshot of a target GAgent
    /// Uses the new GetStateSnapshotAsync method from IGAgent interface
    /// </summary>
    private async Task<string?> CaptureAgentStateAsync(string targetAgentGrainId)
    {
        try
        {
            if (string.IsNullOrEmpty(targetAgentGrainId))
            {
                Logger.LogDebug("Skipping state capture for empty agent ID");
                return null;
            }

            // ✅ FIX: Use GrainFactory directly to get ICoreGAgent instead of IGAgent
            // This supports both old (IGAgent) and new (IGAgentPlus) agents
            var grainId = GrainId.Parse(targetAgentGrainId);
            var targetGAgent = GrainFactory.GetGrain<ICoreGAgent>(grainId);
            
            Logger.LogDebug("🔍 Attempting to get state snapshot for agent: {TargetAgentId}", targetAgentGrainId);

            // Call GetStateSnapshotAsync method
            var stateSnapshot = await targetGAgent.GetStateSnapshotAsync();
            
            if (!string.IsNullOrEmpty(stateSnapshot))
            {
                Logger.LogDebug("✅ Successfully retrieved state snapshot for agent: {TargetAgentId}", targetAgentGrainId);
                return stateSnapshot;
            }
            else
            {
                Logger.LogDebug("ℹ️ GAgent returned null state snapshot (likely not stateful): {TargetAgentId}", targetAgentGrainId);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error during agent state capture for {TargetAgentId}", targetAgentGrainId);
            return null;
        }
    }

    /// <summary>
    /// ✅ REFACTORED: Direct implementation of WorkflowEvent handling
    /// No longer inherits from BusinessAgentBase - this is a pure recorder, not a processor
    /// Child of WorkflowCoordinatorGAgent for targeted audit logging and execution tracking
    /// </summary>
    protected override async Task<bool> OnEventForwardingEventHandlerAsync(WorkflowEvent workflowEvent)
    {
        // ✅ VALIDATION: Basic checks (recorder accepts all workflow events, including failed ones)
        if (workflowEvent == null)
        {
            Logger.LogWarning("ExecutionRecordGAgent received null WorkflowEvent");
            return false;
        }

        if (workflowEvent.WorkflowId == Guid.Empty)
        {
            Logger.LogWarning("ExecutionRecordGAgent received WorkflowEvent with empty WorkflowId");
            return false;
        }

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
                    Logger.LogInformation("🎯 [ExecutionRecordGAgent] Received WorkflowCompleted event for WorkflowId: {WorkflowId}, WorkflowAgentStatus: {AgentStatus}", 
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
            return false;
        }
        
        // ✅ Event recorded successfully - no further forwarding needed (this is a leaf node)
        return true;
    }

    private async Task HandleWorkflowStartedAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogInformation("🎯 [ExecutionRecordGAgent] HandleWorkflowStartedAsync for WorkflowId: {WorkflowId}, WorkUnitAgentId: {WorkUnitAgentId}", 
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
        
        // ✅ FIX: Also track WorkflowStartAgent as a completed work unit
        if (!string.IsNullOrEmpty(workflowEvent.WorkUnitAgentId))
        {
            Logger.LogInformation("🚀 [ExecutionRecordGAgent] Marking WorkflowStartAgent work unit as completed: {WorkUnitAgentId}", workflowEvent.WorkUnitAgentId);
            
            // Capture state snapshot
            var stateSnapshot = await CaptureAgentStateAsync(workflowEvent.WorkUnitAgentId);
            
            // ✅ SIMPLIFIED: InputData = Message, OutputData = TaskResult
            var inputData = workflowEvent.Message ?? string.Empty;
            var outputData = workflowEvent.TaskResult ?? string.Empty;
            
            // Only trigger Finish event - StartTime and InputData will be set in state transition
            RaiseEvent(new FinishExecuteWorkUnitLogEvent
            {
                WorkUnitGrainId = workflowEvent.WorkUnitAgentId,
                InputData = inputData,
                OutputData = outputData,
                CurrentStateSnapshot = stateSnapshot
            });
        }
        
        await ConfirmEvents();
    }


    private async Task HandleWorkflowAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogDebug("ExecutionRecordGAgent handling WorkflowInProgress for work unit agent {WorkUnitAgentId}", workflowEvent.WorkUnitAgentId);
        
        // Capture current state snapshot of the work unit agent
        var stateSnapshot = await CaptureAgentStateAsync(workflowEvent.WorkUnitAgentId);
        
        // ✅ DESIGN: InputData = original input saved in Metadata["inputData"] before processing
        // Fallback to Message if inputData is not in Metadata
        var inputData = workflowEvent.Metadata.TryGetValue("inputData", out var savedInput) 
            ? savedInput?.ToString() ?? string.Empty
            : (workflowEvent.Message ?? string.Empty);
        
        if (workflowEvent.ErrorMessage.IsNullOrEmpty())
        {
            // ✅ SUCCESS: Track work unit completion with both input and output data
            Logger.LogInformation("✅ [ExecutionRecordGAgent] Recording successful work unit completion for {WorkUnitAgentId}", 
                workflowEvent.WorkUnitAgentId);
            
            // OutputData = TaskResult (what the agent PRODUCED)
            var outputData = System.Text.Json.JsonSerializer.Serialize(workflowEvent.TaskResult);
            
            RaiseEvent(new FinishExecuteWorkUnitLogEvent
            {
                WorkUnitGrainId = workflowEvent.WorkUnitAgentId,
                InputData = inputData,
                OutputData = outputData,
                CurrentStateSnapshot = stateSnapshot
            });
        }
        else
        {
            // ✅ FAILURE: Track work unit failure
            // - InputData = original input (preserved in Message)
            // - OutputData = empty (no output on failure)
            // - FailureSummary = ErrorMessage (exception details)
            Logger.LogWarning("❌ [ExecutionRecordGAgent] Recording failed work unit for {WorkUnitAgentId}, Error: {ErrorMessage}", 
                workflowEvent.WorkUnitAgentId, workflowEvent.ErrorMessage);
            
            RaiseEvent(new FinishExecuteWorkUnitLogEvent
            {
                WorkUnitGrainId = workflowEvent.WorkUnitAgentId,
                InputData = inputData,  // Preserve original input from Message
                OutputData = string.Empty,  // No output on failure
                CurrentStateSnapshot = stateSnapshot
            });
            
            // Trigger workflow-level failure event with error details
            RaiseEvent(new FailExecuteWorkflowLogEvent
            {
                WorkUnitGrainId = workflowEvent.WorkUnitAgentId,
                FailureSummary = workflowEvent.ErrorMessage
            });
        }

        await ConfirmEvents();
    }

    private async Task HandleWorkflowCompletedAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogInformation("🎯 [ExecutionRecordGAgent] HandleWorkflowCompletedAsync called for WorkflowId: {WorkflowId}, WorkUnitAgentId: {WorkUnitAgentId}, WorkflowAgentStatus: {AgentStatus}, Current Status: {CurrentStatus}", 
            workflowEvent.WorkflowId, workflowEvent.WorkUnitAgentId, workflowEvent.WorkflowAgentStatus, State.Status);
        
        // ✅ Track WorkflowEndAgent as a completed work unit BEFORE completing overall workflow
        if (!string.IsNullOrEmpty(workflowEvent.WorkUnitAgentId))
        {
            Logger.LogInformation("🏁 [ExecutionRecordGAgent] Recording WorkflowEndAgent work unit: {WorkUnitAgentId}", workflowEvent.WorkUnitAgentId);
            
            // Capture state snapshot
            var stateSnapshot = await CaptureAgentStateAsync(workflowEvent.WorkUnitAgentId);
            
            // ✅ DESIGN: InputData = original input saved in Metadata["inputData"]
            var inputData = workflowEvent.Metadata.TryGetValue("inputData", out var savedInput) 
                ? savedInput?.ToString() ?? string.Empty
                : (workflowEvent.Message ?? string.Empty);
            var outputData = workflowEvent.TaskResult ?? string.Empty;
            
            RaiseEvent(new FinishExecuteWorkUnitLogEvent
            {
                WorkUnitGrainId = workflowEvent.WorkUnitAgentId,
                InputData = inputData,
                OutputData = outputData,
                CurrentStateSnapshot = stateSnapshot
            });
        }
        
        // ✅ UNIFIED: Always raise FinishExecuteWorkflowLogEvent when workflow ends
        // Status will be preserved (Failed) or set to Completed based on previous events
        RaiseEvent(new FinishExecuteWorkflowLogEvent());
        await ConfirmEvents();
        
        Logger.LogInformation("✅ [ExecutionRecordGAgent] FinishExecuteWorkflowLogEvent raised and confirmed, Final Status: {FinalStatus}", State.Status);
        
        // ✅ UNIFIED: Unregister from parent coordinator AFTER workflow completion (regardless of success or failure)
        // This ensures all workflow events are received before breaking parent-child relationship
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
                Logger.LogWarning("⚠️ [ExecutionRecordGAgent] No coordinator ID found in workflow event to unregister from");
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "⚠️ [ExecutionRecordGAgent] Failed to unregister from parent coordinator, but workflow completion is still valid");
        }
    }

    private async Task HandleWorkflowFailedAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogWarning("ExecutionRecordGAgent handling WorkflowFailed for work unit agent {WorkUnitAgentId}, Error: {ErrorMessage}", 
            workflowEvent.WorkUnitAgentId, workflowEvent.ErrorMessage);
        
        // ✅ Record workflow failure
        RaiseEvent(new FailExecuteWorkflowLogEvent()
        {
            WorkUnitGrainId = workflowEvent.WorkUnitAgentId,
            FailureSummary = workflowEvent.ErrorMessage ?? "Task failed without specific error message"
        });
        await ConfirmEvents();
        
        // ✅ DO NOT unregister from parent coordinator here
        // Wait for WorkflowCompleted event from EndAgent to signal workflow end
        // This ensures ExecutionRecord receives all workflow events (including EndAgent's completion)
        Logger.LogInformation("ExecutionRecordGAgent recorded failure, waiting for WorkflowCompleted to finalize");
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
                        Status = WorkflowExecutionStatus.Pending,
                        AgentName = o.Name
                    }).ToList();
                break;
            case FinishExecuteWorkflowLogEvent finishExecuteWorkflowLogEvent:
                state.EndTime = DateTime.UtcNow;
                
                // ✅ Only set status to Completed if not already Failed
                // If workflow already failed (FailExecuteWorkflowLogEvent was raised), preserve Failed status
                if (state.Status != WorkflowExecutionStatus.Failed)
                {
                    state.Status = WorkflowExecutionStatus.Completed;
                }
                break;
            case StartExecuteWorkUnitLogEvent startExecuteWorkUnitLogEvent:
                var startingWorkUnits = state.WorkUnitRecords.Where(o =>
                    o.WorkUnitGrainId == startExecuteWorkUnitLogEvent.WorkUnitGrainId).ToList();
                
                if (startingWorkUnits.Any())
                {
                    Logger.LogInformation("🚀 [ExecutionRecordGAgent] Processing StartExecuteWorkUnitLogEvent for WorkUnit {WorkUnitGrainId} - Found {Count} matching records to start", 
                        startExecuteWorkUnitLogEvent.WorkUnitGrainId, startingWorkUnits.Count);
                    
                    var startTime = DateTime.UtcNow;
                    foreach (var workUnit in startingWorkUnits)
                    {
                        Logger.LogInformation("🔄 [ExecutionRecordGAgent] Starting WorkUnit record - Setting status from {OldStatus} to Running", workUnit.Status);
                        workUnit.WorkUnitGrainId = startExecuteWorkUnitLogEvent.WorkUnitGrainId;
                        workUnit.StartTime = startTime;
                        if (workUnit.Status == WorkflowExecutionStatus.Pending)
                        {
                            workUnit.Status = WorkflowExecutionStatus.Running;
                        }
                        workUnit.InputData = startExecuteWorkUnitLogEvent.InputData;
                        workUnit.CurrentStateSnapshot = startExecuteWorkUnitLogEvent.CurrentStateSnapshot;
                    }
                    
                    Logger.LogInformation("✅ [ExecutionRecordGAgent] Successfully updated {Count} WorkUnit records for {WorkUnitGrainId} to status: Running at {StartTime}", 
                        startingWorkUnits.Count, startExecuteWorkUnitLogEvent.WorkUnitGrainId, startTime);
                }
                else
                {
                    Logger.LogWarning("⚠️ [ExecutionRecordGAgent] No WorkUnit records found for {WorkUnitGrainId} in WorkUnitRecords for start event", startExecuteWorkUnitLogEvent.WorkUnitGrainId);
                }
                break;

            case FinishExecuteWorkUnitLogEvent finishExecuteWorkUnitLogEvent:
                var matchingWorkUnits = state.WorkUnitRecords.Where(o =>
                    o.WorkUnitGrainId == finishExecuteWorkUnitLogEvent.WorkUnitGrainId).ToList();
                
                if (matchingWorkUnits.Any())
                {
                    Logger.LogInformation("🏁 [ExecutionRecordGAgent] Processing FinishExecuteWorkUnitLogEvent for WorkUnit {WorkUnitGrainId} - Found {Count} matching records to update", 
                        finishExecuteWorkUnitLogEvent.WorkUnitGrainId, matchingWorkUnits.Count);
                    
                    var endTime = DateTime.UtcNow;
                    foreach (var workUnit in matchingWorkUnits)
                    {
                        Logger.LogInformation("🔄 [ExecutionRecordGAgent] Updating WorkUnit record - Setting status from {OldStatus} to Completed", workUnit.Status);
                        
                        // ✅ FIX: Set StartTime and InputData if not already set (first time processing)
                        if (workUnit.StartTime == default || workUnit.StartTime == DateTime.MinValue)
                        {
                            workUnit.StartTime = endTime.AddMilliseconds(-100); // Slight offset before end
                            Logger.LogDebug("🕐 [ExecutionRecordGAgent] Setting StartTime for {WorkUnitGrainId}", finishExecuteWorkUnitLogEvent.WorkUnitGrainId);
                        }
                        
                        if (string.IsNullOrEmpty(workUnit.InputData) && !string.IsNullOrEmpty(finishExecuteWorkUnitLogEvent.InputData))
                        {
                            workUnit.InputData = finishExecuteWorkUnitLogEvent.InputData;
                            Logger.LogDebug("📥 [ExecutionRecordGAgent] Setting InputData for {WorkUnitGrainId}: {InputData}", 
                                finishExecuteWorkUnitLogEvent.WorkUnitGrainId, 
                                finishExecuteWorkUnitLogEvent.InputData.Length > 50 ? finishExecuteWorkUnitLogEvent.InputData.Substring(0, 50) + "..." : finishExecuteWorkUnitLogEvent.InputData);
                        }
                        
                        workUnit.EndTime = endTime;
                        workUnit.Status = WorkflowExecutionStatus.Completed;
                        workUnit.OutputData = finishExecuteWorkUnitLogEvent.OutputData;
                        workUnit.CurrentStateSnapshot = finishExecuteWorkUnitLogEvent.CurrentStateSnapshot;
                    }
                    
                    Logger.LogInformation("✅ [ExecutionRecordGAgent] Successfully updated {Count} WorkUnit records for {WorkUnitGrainId} to status: Completed at {EndTime}", 
                        matchingWorkUnits.Count, finishExecuteWorkUnitLogEvent.WorkUnitGrainId, endTime);
                }
                else
                {
                    Logger.LogWarning("⚠️ [ExecutionRecordGAgent] No WorkUnit records found for {WorkUnitGrainId} in WorkUnitRecords", finishExecuteWorkUnitLogEvent.WorkUnitGrainId);
                }
                break;
            case FailExecuteWorkflowLogEvent failExecuteWorkflowLogEvent:
                var failingWorkUnits = state.WorkUnitRecords.Where(o =>
                    o.WorkUnitGrainId == failExecuteWorkflowLogEvent.WorkUnitGrainId).ToList();
                
                if (failingWorkUnits.Any())
                {
                    Logger.LogInformation("❌ [ExecutionRecordGAgent] Processing FailExecuteWorkflowLogEvent for WorkUnit {WorkUnitGrainId} - Found {Count} matching records to mark as failed", 
                        failExecuteWorkflowLogEvent.WorkUnitGrainId, failingWorkUnits.Count);
                    
                    var endTime = DateTime.UtcNow;
                    foreach (var workUnit in failingWorkUnits)
                    {
                        Logger.LogInformation("🔄 [ExecutionRecordGAgent] Marking WorkUnit record as failed - Setting status from {OldStatus} to Failed", workUnit.Status);
                        workUnit.EndTime = endTime;
                        workUnit.Status = WorkflowExecutionStatus.Failed;
                        workUnit.FailureSummary = failExecuteWorkflowLogEvent.FailureSummary;
                    }
                    
                    Logger.LogInformation("✅ [ExecutionRecordGAgent] Successfully updated {Count} WorkUnit records for {WorkUnitGrainId} to status: Failed at {EndTime}", 
                        failingWorkUnits.Count, failExecuteWorkflowLogEvent.WorkUnitGrainId, endTime);
                }
                else
                {
                    Logger.LogWarning("⚠️ [ExecutionRecordGAgent] No WorkUnit records found for {WorkUnitGrainId} in WorkUnitRecords for failure event", failExecuteWorkflowLogEvent.WorkUnitGrainId);
                }

                state.Status = WorkflowExecutionStatus.Failed;
                break;
        }
        base.GAgentTransitionState(state, @event);
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
    public string WorkUnitGrainId { get; set; } = string.Empty;
    [Id(1)]
    public string InputData { get; set; } = string.Empty;
    [Id(2)]
    public string? CurrentStateSnapshot { get; set; }
}

[GenerateSerializer]
public class FinishExecuteWorkUnitLogEvent : WorkflowExecutionRecordLogEvent
{
    [Id(0)]
    public string WorkUnitGrainId { get; set; } = string.Empty;
    [Id(1)]
    public string OutputData { get; set; } = string.Empty;
    [Id(2)]
    public string? CurrentStateSnapshot { get; set; }
    [Id(3)]
    public string InputData { get; set; } = string.Empty;
}

[GenerateSerializer]
public class FailExecuteWorkflowLogEvent : WorkflowExecutionRecordLogEvent
{
    [Id(0)]
    public string WorkUnitGrainId { get; set; } = string.Empty;
    [Id(1)]
    public string FailureSummary { get; set; } = string.Empty;
}