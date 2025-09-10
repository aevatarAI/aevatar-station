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
        if (@event.CoordinatorMessages?.Count > 0)
        {
            var lastMessage = @event.CoordinatorMessages.Last();
            sourceAgentId = lastMessage.MemberId.ToString();
        }
        
        RaiseEvent(new StartExecuteWorkUnitLogEvent
        {
            WorkUnitGrainId = @event.WorkUnitGrainId,
            SourceAgentId = sourceAgentId,
            InputData = System.Text.Json.JsonSerializer.Serialize(@event.CoordinatorMessages)
        });
        
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(ChatResponseEvent @event)
    {
        RaiseEvent(new FinishExecuteWorkUnitLogEvent
        {
            WorkUnitGrainId = @event.PublisherGrainId.ToString(),
            OutputData = System.Text.Json.JsonSerializer.Serialize(@event.ChatResponse?.Content)
        });
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
                // Do NOT create records here - they will be created dynamically based on message flow
                break;
            case FinishExecuteWorkflowLogEvent finishExecuteWorkflowLogEvent:
                state.EndTime = DateTime.UtcNow;
                state.Status = WorkflowExecutionStatus.Completed;
                break;
            case StartExecuteWorkUnitLogEvent startExecuteWorkUnitLogEvent:
                // Create or find execution record for the message flow: source -> target
                var recordKey = $"{startExecuteWorkUnitLogEvent.SourceAgentId ?? "START"}|{startExecuteWorkUnitLogEvent.WorkUnitGrainId}";
                var existingRecord = state.WorkUnitRecords.FirstOrDefault(r => 
                    r.SourceAgentId == startExecuteWorkUnitLogEvent.SourceAgentId && 
                    r.WorkUnitGrainId == startExecuteWorkUnitLogEvent.WorkUnitGrainId);
                
                if (existingRecord == null)
                {
                    // Create new record for this message flow
                    var newRecord = new WorkUnitExecutionRecord
                    {
                        WorkUnitGrainId = startExecuteWorkUnitLogEvent.WorkUnitGrainId,
                        SourceAgentId = startExecuteWorkUnitLogEvent.SourceAgentId,
                        StartTime = DateTime.UtcNow,
                        Status = WorkflowExecutionStatus.Running,
                        InputData = startExecuteWorkUnitLogEvent.InputData
                    };
                    state.WorkUnitRecords.Add(newRecord);
                }
                else
                {
                    // Update existing record
                    existingRecord.StartTime = DateTime.UtcNow;
                    existingRecord.Status = WorkflowExecutionStatus.Running;
                    existingRecord.InputData = startExecuteWorkUnitLogEvent.InputData;
                }
                break;
            case FinishExecuteWorkUnitLogEvent finishExecuteWorkUnitLogEvent:
                // Finish ALL running records for this target agent
                // An agent's response completes all input flows to that agent
                var runningRecords = state.WorkUnitRecords
                    .Where(r => r.WorkUnitGrainId == finishExecuteWorkUnitLogEvent.WorkUnitGrainId)
                    .Where(r => r.Status == WorkflowExecutionStatus.Running)
                    .ToList();
                    
                foreach (var record in runningRecords)
                {
                    record.EndTime = DateTime.UtcNow;
                    record.Status = WorkflowExecutionStatus.Completed;
                    record.OutputData = finishExecuteWorkUnitLogEvent.OutputData;
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
    public string WorkUnitGrainId { get; set; }
    [Id(1)]
    public string InputData { get; set; }
    [Id(2)]
    public string? SourceAgentId { get; set; }
}

[GenerateSerializer]
public class FinishExecuteWorkUnitLogEvent : WorkflowExecutionRecordLogEvent
{
    [Id(0)]
    public string WorkUnitGrainId { get; set; }
    [Id(1)]
    public string OutputData { get; set; }
}