using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.Core;
using Aevatar.GAgents.GroupChat.Core.States;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using GroupChat.GAgent.Feature.Coordinator.GEvent;

namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator;

[GAgent]
public class WorkflowExecutionRecordGAgent :
    GAgentBase<WorkflowExecutionRecordState, WorkflowExecutionRecordLogEvent, EventBase>, IWorkflowExecutionRecordGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Workflow Execution Record GAgent");
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
        string? sourceAgentName = null;
        if (@event.CoordinatorMessages?.Count > 0)
        {
            var lastMessage = @event.CoordinatorMessages.Last();
            sourceAgentId = lastMessage.MemberId.ToString();
            sourceAgentName = lastMessage.AgentName;
        }
        
        // Extract target agent info from GrainId
        var targetAgentId = @event.TargetAgentId;
        var targetAgentName = ExtractAgentNameFromGrainId(targetAgentId);
        
        RaiseEvent(new StartExecuteWorkUnitLogEvent
        {
            TargetAgentId = targetAgentId,
            SourceAgentId = sourceAgentId,
            SourceAgentName = sourceAgentName,
            TargetAgentName = targetAgentName,
            InputData = System.Text.Json.JsonSerializer.Serialize(@event.CoordinatorMessages)
        });
        
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(ChatResponseEvent @event)
    {
        var targetAgentId = @event.PublisherGrainId.ToString();
        // Find records that are Running or Pending (for incorrect sequence scenarios)
        var eligibleRecords = State.WorkUnitRecords
            .Where(r => r.TargetAgentId == targetAgentId)
            .Where(r => r.Status == WorkflowExecutionStatus.Running || r.Status == WorkflowExecutionStatus.Pending)
            .ToList();
            
        foreach (var record in eligibleRecords)
        {
            RaiseEvent(new FinishExecuteWorkUnitLogEvent
            {
                TargetAgentId = targetAgentId,
                SourceAgentId = record.SourceAgentId,
                SourceAgentName = record.SourceAgentName,
                TargetAgentName = record.TargetAgentName,
                OutputData = System.Text.Json.JsonSerializer.Serialize(@event.ChatResponse?.Content)
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
                // Create initial skeleton records - they will be populated with SourceAgent info when StartExecuteWorkUnitEvent is received
                state.WorkUnitRecords = startExecuteWorkflowLogEvent.WorkUnitInfos.Select(o =>
                    new WorkUnitExecutionRecord
                    {
                        TargetAgentId = o.GrainId,
                        TargetAgentName = ExtractAgentNameFromGrainId(o.GrainId),
                        SourceAgentId = null, // Will be set when StartExecuteWorkUnitEvent is received
                        SourceAgentName = null,
                        StartTime = DateTime.MinValue, // Will be updated when StartExecuteWorkUnitEvent is received
                        Status = WorkflowExecutionStatus.Pending,
                        InputData = null,
                        OutputData = null
                    }).ToList();
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
                            existingRecord.SourceAgentName = startExecuteWorkUnitLogEvent.SourceAgentName;
                            existingRecord.TargetAgentName = startExecuteWorkUnitLogEvent.TargetAgentName;
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
                                existingRecord.SourceAgentName = startExecuteWorkUnitLogEvent.SourceAgentName;
                                existingRecord.TargetAgentName = startExecuteWorkUnitLogEvent.TargetAgentName;
                            }
                        }
                    }
                else
                {
                    // Create new record if none exists (fallback case)
                    var newRecord = new WorkUnitExecutionRecord
                    {
                        TargetAgentId = startExecuteWorkUnitLogEvent.TargetAgentId,
                        TargetAgentName = startExecuteWorkUnitLogEvent.TargetAgentName,
                        SourceAgentId = startExecuteWorkUnitLogEvent.SourceAgentId,
                        SourceAgentName = startExecuteWorkUnitLogEvent.SourceAgentName,
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
    
    /// <summary>
    /// Extract agent name from GrainId string
    /// Example: "Aevatar.GAgents.InputGAgent.GAgent.InputGAgent/guid" -> "InputGAgent"
    /// </summary>
    private string? ExtractAgentNameFromGrainId(string grainId)
    {
        if (string.IsNullOrEmpty(grainId))
            return null;
            
        try
        {
            // Split by '/' to get the type part
            var typePart = grainId.Split('/')[0];
            
            // Split by '.' to get namespaces
            var namespaces = typePart.Split('.');
            
            // Look for the agent name - usually the last non-duplicate part
            // Example: ["Aevatar", "GAgents", "InputGAgent", "GAgent", "InputGAgent"]
            for (int i = namespaces.Length - 1; i >= 0; i--)
            {
                var part = namespaces[i];
                if (!string.IsNullOrEmpty(part) && 
                    part != "GAgent" && 
                    part != "GAgents" && 
                    part != "Aevatar")
                {
                    return part;
                }
            }
            
            return null;
        }
        catch
        {
            return null;
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
    public string? SourceAgentName { get; set; }
    [Id(4)]
    public string? TargetAgentName { get; set; }
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
    public string? SourceAgentName { get; set; }
    [Id(4)]
    public string? TargetAgentName { get; set; }
}