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
    
    protected override bool AreAllDependenciesReadyAsync(WorkflowEvent workflowEvent)
    {
        return true;
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
        
        // ✅ StartAgent is a workflow system agent - no need to track its execution in business records
        // WorkUnitInfos already filtered to exclude StartAgent/EndAgent in WorkflowCoordinator
        
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
            var outputData = System.Text.Json.JsonSerializer.Serialize(workflowEvent.Message);
            
            RaiseEvent(new FinishExecuteWorkUnitLogEvent
            {
                WorkUnitGrainId = workflowEvent.WorkUnitAgentId,
                InputData = inputData,
                OutputData = outputData,
                CurrentStateSnapshot = stateSnapshot,
                StartTime = workflowEvent.StepStartTime != default ? workflowEvent.StepStartTime : null
            });
        }
        else
        {
            // ✅ FAILURE: Track work unit failure with all context
            // Only trigger FailExecuteWorkflowLogEvent (not FinishExecuteWorkUnitLogEvent)
            Logger.LogWarning("❌ [ExecutionRecordGAgent] Recording failed work unit for {WorkUnitAgentId}, Error: {ErrorMessage}", 
                workflowEvent.WorkUnitAgentId, workflowEvent.ErrorMessage);
            
            RaiseEvent(new FailExecuteWorkflowLogEvent
            {
                WorkUnitGrainId = workflowEvent.WorkUnitAgentId,
                FailureSummary = workflowEvent.ErrorMessage,
                InputData = inputData,  // Preserve original input
                CurrentStateSnapshot = stateSnapshot,  // Capture state at failure
                StartTime = workflowEvent.StepStartTime != default ? workflowEvent.StepStartTime : null
            });
        }

        await ConfirmEvents();
    }

    private async Task HandleWorkflowCompletedAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogInformation("🎯 [ExecutionRecordGAgent] HandleWorkflowCompletedAsync called for WorkflowId: {WorkflowId}, WorkflowAgentStatus: {AgentStatus}, Current Status: {CurrentStatus}", 
            workflowEvent.WorkflowId, workflowEvent.WorkflowAgentStatus, State.Status);
        
        // ✅ EndAgent is a workflow system agent - no need to track its execution in business records
        // WorkUnitInfos already filtered to exclude StartAgent/EndAgent in WorkflowCoordinator
        RaiseEvent(new FinishExecuteWorkflowLogEvent());
        await ConfirmEvents();
        
        Logger.LogInformation("✅ [ExecutionRecordGAgent] FinishExecuteWorkflowLogEvent raised and confirmed, Final Status: {FinalStatus}", State.Status);
        
        // ✅ UNIFIED: Unregister from parent coordinator after workflow completion
        await UnregisterFromCoordinatorAsync(workflowEvent.WorkflowId, "completion");
    }

    private async Task HandleWorkflowFailedAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogWarning("❌ [ExecutionRecordGAgent] HandleWorkflowFailedAsync for work unit agent {WorkUnitAgentId}, Error: {ErrorMessage}", 
            workflowEvent.WorkUnitAgentId, workflowEvent.ErrorMessage);
        
        // ✅ Capture state snapshot before recording failure
        var stateSnapshot = await CaptureAgentStateAsync(workflowEvent.WorkUnitAgentId);
        
        // ✅ Extract InputData from Metadata or Message (same pattern as HandleWorkflowAsync)
        var inputData = workflowEvent.Metadata.TryGetValue("inputData", out var savedInput) 
            ? savedInput?.ToString() ?? string.Empty
            : (workflowEvent.Message ?? string.Empty);
        
        // ✅ Record workflow failure with complete context
        RaiseEvent(new FailExecuteWorkflowLogEvent()
        {
            WorkUnitGrainId = workflowEvent.WorkUnitAgentId,
            FailureSummary = workflowEvent.ErrorMessage ?? "Task failed without specific error message",
            InputData = inputData,
            CurrentStateSnapshot = stateSnapshot,
            StartTime = workflowEvent.StepStartTime != default ? workflowEvent.StepStartTime : null
        });
        await ConfirmEvents();
        
        Logger.LogInformation("❌ [ExecutionRecordGAgent] Workflow failure recorded, Final Status: {FinalStatus}", State.Status);
        
        // ✅ UNIFIED: Unregister from parent coordinator after workflow failure
        // This ensures proper cleanup when workflow fails via P2P failure events
        await UnregisterFromCoordinatorAsync(workflowEvent.WorkflowId, "failure");
    }

    /// <summary>
    /// ✅ UNIFIED: Unregister from parent coordinator after workflow ends (completion or failure)
    /// Encapsulates the unregister logic in a reusable method
    /// </summary>
    /// <param name="coordinatorId">The WorkflowId (same as coordinator's GrainId)</param>
    /// <param name="reason">Reason for unregistering (for logging: "completion" or "failure")</param>
    private async Task UnregisterFromCoordinatorAsync(Guid coordinatorId, string reason)
    {
        try
        {
            if (coordinatorId == Guid.Empty)
            {
                Logger.LogWarning("⚠️ [ExecutionRecordGAgent] No coordinator ID provided for unregister on {Reason}", reason);
                return;
            }
            var parentCoordinator = GrainFactory.GetGrain<IWorkflowCoordinatorGAgentPlus>(coordinatorId);
            await UnregisterParentAsync(parentCoordinator);

            Logger.LogInformation("✅ [ExecutionRecordGAgent] Successfully unregistered from parent coordinator {CoordinatorId} on workflow {Reason}", 
                coordinatorId, reason);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "⚠️ [ExecutionRecordGAgent] Failed to unregister from parent coordinator {CoordinatorId} on workflow {Reason}, but workflow {Reason} is still valid", 
                coordinatorId, reason, reason);
            // Don't rethrow - unregister failure shouldn't prevent workflow completion/failure recording
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
                        Status = WorkflowExecutionStatus.Pending,
                        AgentName = o.Name
                    }).ToList();
                break;
            case FinishExecuteWorkflowLogEvent finishExecuteWorkflowLogEvent:
                Logger.LogInformation("[ExecutionRecordGAgent] Processing FinishExecuteWorkflowLogEvent - Setting status from {OldStatus} to Completed", state.Status);
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
                        
                        // Set StartTime from event, fallback to default if needed
                        if (workUnit.StartTime == default || workUnit.StartTime == DateTime.MinValue)
                        {
                            workUnit.StartTime = (finishExecuteWorkUnitLogEvent.StartTime.HasValue && finishExecuteWorkUnitLogEvent.StartTime.Value != default)
                                ? finishExecuteWorkUnitLogEvent.StartTime.Value
                                : endTime.AddMilliseconds(-100);
                        }
                        
                        if (string.IsNullOrEmpty(workUnit.InputData) && !string.IsNullOrEmpty(finishExecuteWorkUnitLogEvent.InputData))
                        {
                            workUnit.InputData = finishExecuteWorkUnitLogEvent.InputData;
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
                        
                        // Set StartTime from event, fallback to default if needed
                        if (workUnit.StartTime == default || workUnit.StartTime == DateTime.MinValue)
                        {
                            workUnit.StartTime = (failExecuteWorkflowLogEvent.StartTime.HasValue && failExecuteWorkflowLogEvent.StartTime.Value != default)
                                ? failExecuteWorkflowLogEvent.StartTime.Value
                                : endTime.AddMilliseconds(-100);
                        }
                        
                        workUnit.EndTime = endTime;
                        workUnit.Status = WorkflowExecutionStatus.Failed;
                        workUnit.FailureSummary = failExecuteWorkflowLogEvent.FailureSummary;
                        
                        // ✅ Apply InputData and CurrentStateSnapshot from failure event
                        if (!string.IsNullOrEmpty(failExecuteWorkflowLogEvent.InputData))
                        {
                            workUnit.InputData = failExecuteWorkflowLogEvent.InputData;
                        }
                        if (failExecuteWorkflowLogEvent.CurrentStateSnapshot != null)
                        {
                            workUnit.CurrentStateSnapshot = failExecuteWorkflowLogEvent.CurrentStateSnapshot;
                        }
                    }
                    
                    Logger.LogInformation("✅ [ExecutionRecordGAgent] Successfully updated {Count} WorkUnit records for {WorkUnitGrainId} to status: Failed at {EndTime}", 
                        failingWorkUnits.Count, failExecuteWorkflowLogEvent.WorkUnitGrainId, endTime);
                }
                else
                {
                    Logger.LogWarning("⚠️ [ExecutionRecordGAgent] No WorkUnit records found for {WorkUnitGrainId} in WorkUnitRecords for failure event", failExecuteWorkflowLogEvent.WorkUnitGrainId);
                }

                state.Status = WorkflowExecutionStatus.Failed;
                state.EndTime = DateTime.UtcNow;
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
    [Id(4)]
    public DateTime? StartTime { get; set; }
}

[GenerateSerializer]
public class FailExecuteWorkflowLogEvent : WorkflowExecutionRecordLogEvent
{
    [Id(0)]
    public string WorkUnitGrainId { get; set; } = string.Empty;
    [Id(1)]
    public string FailureSummary { get; set; } = string.Empty;
    [Id(2)]
    public string InputData { get; set; } = string.Empty;
    [Id(3)]
    public string? CurrentStateSnapshot { get; set; }
    [Id(4)]
    public DateTime? StartTime { get; set; }
}