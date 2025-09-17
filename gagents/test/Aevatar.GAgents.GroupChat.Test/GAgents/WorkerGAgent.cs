using Aevatar.Core.Abstractions;
using Aevatar.Core.Interception;
using Aevatar.GAgents.GroupChat.Core.Dto;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.GEvent;
using Volo.Abp;

[module: Interceptor]

namespace Aevatar.GAgents.GroupChat.Test.GAgents;

[GAgent(nameof(WorkerGAgentGAgent))]
public class WorkerGAgentGAgent : GroupMemberGAgentBase<WorkerState, WorkerEventLog, EventBase, GroupMemberConfigDto>, IWorkerGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Group chat Worker GAgent");
    }

    public async Task SetDelayWorkAsync(int delaySeconds)
    {
        RaiseEvent(new WorkerDelayLogEvent(){DelaySeconds = delaySeconds});
        await ConfirmEvents();
    }

    public async Task SetFailureSummary(string failureSummary)
    {
        RaiseEvent(new WorkerFailureLogEvent()
        {
            FailureSummary = failureSummary
        });
        await ConfirmEvents();
    }

    public Task<WorkerState> GetState()
    {
        return Task.FromResult(State);
    }

    [Interceptor(LogCategory = WorkflowLogCategory, ContextProperty = new[] {WorkflowIdProperty})]
    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        var random = new Random();
        
        return Task.FromResult(random.Next(1, 90));
    }

    [Interceptor(LogCategory = WorkflowLogCategory, ContextProperty = new[] {WorkflowIdProperty})]
    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
    {
        if (State.DelaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(State.DelaySeconds));
        }

        if (!State.FailureSummary.IsNullOrEmpty())
        {
            throw new UserFriendlyException(State.FailureSummary);
        }

        var response = new ChatResponse();
        response.Content = $"{State.MemberName} Send the message";
        RaiseEvent(new WorkHandleMessageLogEvent(){ PreWorkUnits = coordinatorMessages!.Select(s=>s.AgentName).ToList()});
        await ConfirmEvents();
        return response;
    }

    protected override void GroupMemberTransitionState(WorkerState state, StateLogEventBase<WorkerEventLog> @event)
    {
        switch (@event)
        {
            case WorkHandleMessageLogEvent workHandleMessageLogEvent:
                state.PreWorkUnits = workHandleMessageLogEvent.PreWorkUnits;
                return;
            case WorkerDelayLogEvent workerDelayLogEvent:
                state.DelaySeconds = workerDelayLogEvent.DelaySeconds;
                return;
            case WorkerFailureLogEvent workerFailureLogEvent:
                state.FailureSummary = workerFailureLogEvent.FailureSummary;
                return;
        }
    }
}

[GenerateSerializer]
public class WorkerEventLog : StateLogEventBase<WorkerEventLog>
{
}


[GenerateSerializer]
public class  WorkHandleMessageLogEvent : WorkerEventLog
{
    [Id(0)] public List<string> PreWorkUnits { get; set; } = new List<string>();
}

[GenerateSerializer]
public class WorkerDelayLogEvent : WorkerEventLog
{
    [Id(0)] public int DelaySeconds;
}

[GenerateSerializer]
public class WorkerFailureLogEvent : WorkerEventLog
{
    [Id(0)] public string FailureSummary;
}


public interface IWorkerGAgent : IStateGAgent<WorkerState>
{
    Task SetDelayWorkAsync(int delaySeconds);
    Task SetFailureSummary(string failureSummary);
}

[GenerateSerializer]
public class WorkerState : GroupMemberState
{
    [Id(0)] public List<string> PreWorkUnits = new List<string>();
    [Id(1)] public int DelaySeconds { get; set; } = 0;
    [Id(2)] public string FailureSummary { get; set; }
}