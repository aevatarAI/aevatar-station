using System.Reflection;
using Microsoft.CSharp.RuntimeBinder;
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
    /// Uses the new GetStateSnapshotAsync method from IGAgent interface
    /// </summary>
    private async Task<string?> CaptureAgentStateAsync(string targetAgentId)
    {
        try
        {
            // Parse the GrainId (assumed valid)
            var grainId = GrainId.Parse(targetAgentId);

            // Get the target GAgent using GAgentFactory
            var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
            var targetGAgent = await gAgentFactory.GetGAgentAsync(grainId);
            
            Logger.LogInformation("🔍 Attempting to get state snapshot for: {TargetAgentId}", targetAgentId);

            // Call the new GetStateSnapshotAsync method
            var stateSnapshot = await targetGAgent.GetStateSnapshotAsync();
            
            if (!string.IsNullOrEmpty(stateSnapshot))
            {
                Logger.LogInformation("✅ Successfully retrieved state snapshot");
                return stateSnapshot;
            }
            else
            {
                Logger.LogInformation("ℹ️ GAgent returned null state snapshot (likely not stateful)");
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error during agent state capture for {TargetAgentId}", targetAgentId);
            return null;
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
        // Capture current state snapshot of target agent
        var stateSnapshot = await CaptureAgentStateAsync(@event.WorkUnitGrainId);
        
        RaiseEvent(new StartExecuteWorkUnitLogEvent
        {
            WorkUnitGrainId = @event.WorkUnitGrainId,
            InputData = System.Text.Json.JsonSerializer.Serialize(@event.CoordinatorMessages),
            CurrentStateSnapshot = stateSnapshot
        });
        
        Logger.LogInformation("✅ StartExecuteWorkUnitEvent处理完成");
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(ChatResponseEvent @event)
    {
        var targetAgentId = @event.PublisherGrainId.ToString();
        
        // Capture current state snapshot
        var stateSnapshot = await CaptureAgentStateAsync(targetAgentId);
        
        if (@event.FailureSummary.IsNullOrEmpty())
        {
            RaiseEvent(new FinishExecuteWorkUnitLogEvent
            {
                WorkUnitGrainId = targetAgentId,
                OutputData = System.Text.Json.JsonSerializer.Serialize(@event.ChatResponse?.Content),
                CurrentStateSnapshot = stateSnapshot
            });
        }
        else
        {
            RaiseEvent(new FailExecuteWorkflowLogEvent()
            {
                WorkUnitGrainId = targetAgentId,
                FailureSummary = @event.FailureSummary
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
                state.WorkUnitRecords = startExecuteWorkflowLogEvent.WorkUnitInfos.Select(o =>
                    new WorkUnitExecutionRecord
                    {
                        WorkUnitGrainId = o.GrainId,
                        Status = WorkflowExecutionStatus.Pending
                    }).ToList();
                break;
            case FinishExecuteWorkflowLogEvent finishExecuteWorkflowLogEvent:
                state.EndTime = DateTime.UtcNow;
                state.Status = WorkflowExecutionStatus.Completed;
                break;
            case StartExecuteWorkUnitLogEvent startExecuteWorkUnitLogEvent:
                var startUnit = state.WorkUnitRecords.First(o =>
                    o.WorkUnitGrainId == startExecuteWorkUnitLogEvent.WorkUnitGrainId);
                startUnit.WorkUnitGrainId = startExecuteWorkUnitLogEvent.WorkUnitGrainId;
                startUnit.StartTime = DateTime.UtcNow;
                if (startUnit.Status == WorkflowExecutionStatus.Pending)
                {
                    startUnit.Status = WorkflowExecutionStatus.Running;
                }
                startUnit.InputData = startExecuteWorkUnitLogEvent.InputData;
                break;
            case FinishExecuteWorkUnitLogEvent finishExecuteWorkUnitLogEvent:
                var workUnit = state.WorkUnitRecords.First(o =>
                    o.WorkUnitGrainId == finishExecuteWorkUnitLogEvent.WorkUnitGrainId);
                workUnit.EndTime = DateTime.UtcNow;
                workUnit.Status = WorkflowExecutionStatus.Completed;
                workUnit.OutputData = finishExecuteWorkUnitLogEvent.OutputData;
                break;
            case FailExecuteWorkflowLogEvent failExecuteWorkflowLogEvent:
                var failWorkUnit = state.WorkUnitRecords.First(o =>
                    o.WorkUnitGrainId == failExecuteWorkflowLogEvent.WorkUnitGrainId);
                failWorkUnit.EndTime = DateTime.UtcNow;
                failWorkUnit.Status = WorkflowExecutionStatus.Failed;
                failWorkUnit.FailureSummary = failExecuteWorkflowLogEvent.FailureSummary;

                state.Status = WorkflowExecutionStatus.Failed;
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
    public string? CurrentStateSnapshot { get; set; }
}

[GenerateSerializer]
public class FinishExecuteWorkUnitLogEvent : WorkflowExecutionRecordLogEvent
{
    [Id(0)]
    public string WorkUnitGrainId { get; set; }
    [Id(1)]
    public string OutputData { get; set; }
    [Id(2)]
    public string? CurrentStateSnapshot { get; set; }
}

[GenerateSerializer]
public class FailExecuteWorkflowLogEvent : WorkflowExecutionRecordLogEvent
{
    [Id(0)]
    public string WorkUnitGrainId { get; set; }
    [Id(1)]
    public string FailureSummary { get; set; }
}