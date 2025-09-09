using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.Core;
using Aevatar.GAgents.GroupChat.Core.States;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Newtonsoft.Json;

namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator;

/// <summary>
/// Workflow Execution Record GAgent (DEPRECATED)
/// This class is obsolete and will be removed in a future version. 
/// Use WorkflowRunRecordGAgent instead for enhanced workflow execution tracking.
/// </summary>
[Obsolete("This GAgent is deprecated. Use WorkflowRunRecordGAgent instead for enhanced workflow execution tracking with real-time state and data lineage.", false)]
[GAgent]
public class WorkflowExecutionRecordGAgent :
    GAgentBase<WorkflowExecutionRecordState, WorkflowExecutionRecordLogEvent, EventBase>, IWorkflowExecutionRecordGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("[DEPRECATED] Workflow Execution Record GAgent - Use WorkflowRunRecordGAgent instead");
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
        RaiseEvent(new StartExecuteWorkUnitLogEvent
        {
            WorkUnitGrainId = @event.WorkUnitGrainId,
            InputData = JsonConvert.SerializeObject(@event.CoordinatorMessages)
        });
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(ChatResponseEvent @event)
    {
        RaiseEvent(new FinishExecuteWorkUnitLogEvent
        {
            WorkUnitGrainId = @event.PublisherGrainId.ToString(),
            OutputData = JsonConvert.SerializeObject(@event.ChatResponse?.Content)
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
            case StartExecuteWorkflowLogEvent startEvent:
                state.WorkflowId = startEvent.WorkflowId;
                state.RoundId = startEvent.RoundId;
                state.WorkUnitInfos = startEvent.WorkUnitInfos;
                state.InitContent = startEvent.Content;
                state.StartTime = DateTime.UtcNow;
                state.Status = Core.States.WorkflowExecutionStatus.Running;
                
                foreach (var workUnitInfo in startEvent.WorkUnitInfos)
                {
                    var existingRecord = state.WorkUnitRecords.FirstOrDefault(r => r.WorkUnitGrainId == workUnitInfo.GrainId);
                    if (existingRecord == null)
                    {
                        state.WorkUnitRecords.Add(new WorkUnitExecutionRecord
                        {
                            WorkUnitGrainId = workUnitInfo.GrainId,
                            Status = Core.States.WorkflowExecutionStatus.Pending,
                            StartTime = DateTime.UtcNow,
                            InputData = "",
                            OutputData = ""
                        });
                    }
                    else
                    {
                        existingRecord.Status = Core.States.WorkflowExecutionStatus.Running;
                    }
                }
                break;

            case FinishExecuteWorkflowLogEvent:
                state.Status = Core.States.WorkflowExecutionStatus.Completed;
                state.EndTime = DateTime.UtcNow;
                break;

            case StartExecuteWorkUnitLogEvent startUnitEvent:
                var record = state.WorkUnitRecords.FirstOrDefault(r => r.WorkUnitGrainId == startUnitEvent.WorkUnitGrainId);
                if (record == null)
                {
                    state.WorkUnitRecords.Add(new WorkUnitExecutionRecord
                    {
                        WorkUnitGrainId = startUnitEvent.WorkUnitGrainId,
                        Status = Core.States.WorkflowExecutionStatus.Running,
                        StartTime = DateTime.UtcNow,
                        InputData = startUnitEvent.InputData,
                        OutputData = ""
                    });
                }
                else
                {
                    if (record.Status != Core.States.WorkflowExecutionStatus.Completed)
                    {
                        record.Status = Core.States.WorkflowExecutionStatus.Running;
                        record.StartTime = DateTime.UtcNow;
                        record.InputData = startUnitEvent.InputData;
                    }
                }
                break;

            case FinishExecuteWorkUnitLogEvent finishUnitEvent:
                var finishedRecord = state.WorkUnitRecords.FirstOrDefault(r => r.WorkUnitGrainId == finishUnitEvent.WorkUnitGrainId);
                if (finishedRecord != null)
                {
                    finishedRecord.Status = Core.States.WorkflowExecutionStatus.Completed;
                    finishedRecord.EndTime = DateTime.UtcNow;
                    finishedRecord.OutputData = finishUnitEvent.OutputData;
                }
                break;
        }
    }
}

/// <summary>
/// Base log event for WorkflowExecutionRecordGAgent (DEPRECATED)
/// </summary>
[Obsolete("This log event is deprecated. Use WorkflowRunRecordLogEvent instead.", false)]
[GenerateSerializer]
public class WorkflowExecutionRecordLogEvent : StateLogEventBase<WorkflowExecutionRecordLogEvent>
{
}

/// <summary>
/// DEPRECATED: Use WorkflowRunRecordLogEvent equivalents instead
/// </summary>
[Obsolete("Use WorkflowRunRecordLogEvent equivalents instead.", false)]
[GenerateSerializer]
public class StartExecuteWorkflowLogEvent : WorkflowExecutionRecordLogEvent
{
    [Id(0)] public Guid WorkflowId { get; set; }
    [Id(1)] public long RoundId { get; set; }
    [Id(2)] public List<WorkUnitInfo> WorkUnitInfos { get; set; } = new();
    [Id(3)] public string? Content { get; set; }
}

/// <summary>
/// DEPRECATED: Use WorkflowRunRecordLogEvent equivalents instead
/// </summary>
[Obsolete("Use WorkflowRunRecordLogEvent equivalents instead.", false)]
[GenerateSerializer]
public class StartExecuteWorkUnitLogEvent : WorkflowExecutionRecordLogEvent
{
    [Id(0)] public string WorkUnitGrainId { get; set; } = string.Empty;
    [Id(1)] public string InputData { get; set; } = string.Empty;
}

/// <summary>
/// DEPRECATED: Use WorkflowRunRecordLogEvent equivalents instead
/// </summary>
[Obsolete("Use WorkflowRunRecordLogEvent equivalents instead.", false)]
[GenerateSerializer]
public class FinishExecuteWorkUnitLogEvent : WorkflowExecutionRecordLogEvent
{
    [Id(0)] public string WorkUnitGrainId { get; set; } = string.Empty;
    [Id(1)] public string OutputData { get; set; } = string.Empty;
}

/// <summary>
/// DEPRECATED: Use WorkflowRunRecordLogEvent equivalents instead
/// </summary>
[Obsolete("Use WorkflowRunRecordLogEvent equivalents instead.", false)]
[GenerateSerializer]
public class FinishExecuteWorkflowLogEvent : WorkflowExecutionRecordLogEvent
{
}
