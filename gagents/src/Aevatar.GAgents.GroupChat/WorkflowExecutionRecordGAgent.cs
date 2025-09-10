using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.Core;
using Aevatar.GAgents.GroupChat.Core.States;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator;

[GAgent]
public class WorkflowExecutionRecordGAgent :
    GAgentBase<WorkflowExecutionRecordState, WorkflowExecutionRecordLogEvent, EventBase>, IWorkflowExecutionRecordGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Workflow Execution Record GAgent");
    }
    
    /// <summary>
    /// Capture the current state snapshot of a target GAgent
    /// </summary>
    private Task<string?> CaptureAgentStateAsync(string targetAgentId)
    {
        try
        {
            // For now, we'll create a simple snapshot with basic info
            // In the future, this could be enhanced to call specific GAgent methods
            var snapshot = new
            {
                targetAgentId = targetAgentId,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                note = "Basic state snapshot - enhanced state capture can be implemented later"
            };
            
            return Task.FromResult<string?>(System.Text.Json.JsonSerializer.Serialize(snapshot));
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Failed to capture agent state for {TargetAgentId}: {Error}", targetAgentId, ex.Message);
            return Task.FromResult<string?>(null);
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(StartExecuteWorkflowEvent @event)
    {
        RaiseEvent(new StartExecuteWorkflowLogEvent
        {
            WorkflowId = @event.WorkflowId,
            RoundId = @event.RoundId,
            WorkUnitInfos = @event.WorkUnitInfos,
            Content = @event.Content,
        });
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(StartExecuteWorkUnitEvent @event)
    {
        // Extract upstream agent from the last message in CoordinatorMessages
        string? sourceAgentId = null;
        if (@event.CoordinatorMessages?.Count > 0)
        {
            var lastMessage = @event.CoordinatorMessages.Last();
            sourceAgentId = lastMessage.MemberId.ToString();
        }
        
        // Extract target agent info from GrainId
        var targetAgentId = @event.TargetAgentId;
        
        // Capture current state snapshot of target agent
        var stateSnapshot = await CaptureAgentStateAsync(targetAgentId);
        
        RaiseEvent(new StartExecuteWorkUnitLogEvent
        {
            TargetAgentId = targetAgentId,
            SourceAgentId = sourceAgentId,
            InputData = System.Text.Json.JsonSerializer.Serialize(@event.CoordinatorMessages),
            CurrentStateSnapshot = stateSnapshot
        });
        
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(ChatResponseEvent @event)
    {
        var targetAgentId = @event.PublisherGrainId.ToString();
        
        // Capture current state snapshot of target agent after completion
        var stateSnapshot = await CaptureAgentStateAsync(targetAgentId);
        
        // Find records that are Running or Pending (for incorrect sequence scenarios)
        var eligibleRecords = State.WorkUnitRecords
            .Where(r => r.TargetAgentId == targetAgentId)
            .Where(r => r.Status == WorkflowExecutionStatus.Running || r.Status == WorkflowExecutionStatus.Pending)
            .ToList();
            
        if (eligibleRecords.Any())
        {
            foreach (var record in eligibleRecords)
            {
                RaiseEvent(new FinishExecuteWorkUnitLogEvent
                {
                    TargetAgentId = targetAgentId,
                    SourceAgentId = record.SourceAgentId,
                    OutputData = System.Text.Json.JsonSerializer.Serialize(@event.ChatResponse?.Content),
                    CurrentStateSnapshot = stateSnapshot
                });
            }
        }
        else
        {
            // Handle incorrect sequence: ChatResponseEvent arrives before StartExecuteWorkUnitEvent
            // Create a completed record to handle this out-of-order scenario
            RaiseEvent(new StartExecuteWorkUnitLogEvent
            {
                TargetAgentId = targetAgentId,
                SourceAgentId = null, // Will be updated when StartExecuteWorkUnitEvent arrives
                InputData = null,
                CurrentStateSnapshot = stateSnapshot
            });
            
            RaiseEvent(new FinishExecuteWorkUnitLogEvent
            {
                TargetAgentId = targetAgentId,
                SourceAgentId = null,
                OutputData = System.Text.Json.JsonSerializer.Serialize(@event.ChatResponse?.Content),
                CurrentStateSnapshot = stateSnapshot
            });
        }
        
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(GroupChatFinishEvent @event)
    {
        RaiseEvent(new FinishExecuteWorkflowLogEvent
        {
        });
        await ConfirmEvents();
    }

    protected override void GAgentTransitionState(WorkflowExecutionRecordState state,
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
                // Initialize empty records list - records will be created dynamically when StartExecuteWorkUnitEvent is received
                state.WorkUnitRecords = new List<WorkUnitExecutionRecord>();
                break;
            case FinishExecuteWorkflowLogEvent finishExecuteWorkflowLogEvent:
                state.EndTime = DateTime.UtcNow;
                state.Status = WorkflowExecutionStatus.Completed;
                break;
            case StartExecuteWorkUnitLogEvent startExecuteWorkUnitLogEvent:
                // Find existing record for this TargetAgentId and update it
                var existingRecord = state.WorkUnitRecords.FirstOrDefault(r => 
                    r.TargetAgentId == startExecuteWorkUnitLogEvent.TargetAgentId);
                
                    if (existingRecord != null)
                    {
                        // Only update if the record is not already completed
                        if (existingRecord.Status != WorkflowExecutionStatus.Completed)
                        {
                            // Update existing record with source agent and mark as running
                            existingRecord.SourceAgentId = startExecuteWorkUnitLogEvent.SourceAgentId;
                            existingRecord.StartTime = DateTime.UtcNow;
                            existingRecord.Status = WorkflowExecutionStatus.Running;
                            existingRecord.InputData = startExecuteWorkUnitLogEvent.InputData;
                        }
                        else
                        {
                            // Even if completed, update InputData if it's null (for incorrect sequence scenarios)
                            if (string.IsNullOrEmpty(existingRecord.InputData))
                            {
                                existingRecord.InputData = startExecuteWorkUnitLogEvent.InputData;
                                existingRecord.SourceAgentId = startExecuteWorkUnitLogEvent.SourceAgentId;
                            }
                        }
                    }
                else
                {
                    // Create new record if none exists (fallback case)
                    var newRecord = new WorkUnitExecutionRecord
                    {
                        TargetAgentId = startExecuteWorkUnitLogEvent.TargetAgentId,
                        SourceAgentId = startExecuteWorkUnitLogEvent.SourceAgentId,
                        StartTime = DateTime.UtcNow,
                        Status = WorkflowExecutionStatus.Running,
                        InputData = startExecuteWorkUnitLogEvent.InputData
                    };
                    state.WorkUnitRecords.Add(newRecord);
                }
                break;
            case FinishExecuteWorkUnitLogEvent finishExecuteWorkUnitLogEvent:
                // If SourceAgentId is specified, use precise matching
                if (finishExecuteWorkUnitLogEvent.SourceAgentId != null)
                {
                    var targetRecord = state.WorkUnitRecords.FirstOrDefault(r => 
                        r.TargetAgentId == finishExecuteWorkUnitLogEvent.TargetAgentId &&
                        r.SourceAgentId == finishExecuteWorkUnitLogEvent.SourceAgentId);
                        
                    if (targetRecord != null)
                    {
                        targetRecord.EndTime = DateTime.UtcNow;
                        targetRecord.Status = WorkflowExecutionStatus.Completed;
                        targetRecord.OutputData = finishExecuteWorkUnitLogEvent.OutputData;
                    }
                }
                else
                {
                    // Fallback: Complete any running or pending record for this TargetAgentId
                    var targetRecord = state.WorkUnitRecords.FirstOrDefault(r => 
                        r.TargetAgentId == finishExecuteWorkUnitLogEvent.TargetAgentId &&
                        (r.Status == WorkflowExecutionStatus.Running || r.Status == WorkflowExecutionStatus.Pending));
                        
                    if (targetRecord != null)
                    {
                        targetRecord.EndTime = DateTime.UtcNow;
                        targetRecord.Status = WorkflowExecutionStatus.Completed;
                        targetRecord.OutputData = finishExecuteWorkUnitLogEvent.OutputData;
                    }
                }
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
    public string TargetAgentId { get; set; }
    [Id(1)]
    public string InputData { get; set; }
    [Id(2)]
    public string? SourceAgentId { get; set; }
    [Id(3)]
    public string? CurrentStateSnapshot { get; set; }
}

[GenerateSerializer]
public class FinishExecuteWorkUnitLogEvent : WorkflowExecutionRecordLogEvent
{
    [Id(0)]
    public string TargetAgentId { get; set; }
    [Id(1)]
    public string OutputData { get; set; }
    [Id(2)]
    public string? SourceAgentId { get; set; }
    [Id(3)]
    public string? CurrentStateSnapshot { get; set; }
}