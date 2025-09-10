using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.Core.Dto;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.GEvent;

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

    public Task<WorkerState> GetState()
    {
        return Task.FromResult(State);
    }

    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        var random = new Random();
        
        return Task.FromResult(random.Next(1, 90));
    }

    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
    {
        if (State.DelaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(State.DelaySeconds));
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


public interface IWorkerGAgent : IStateGAgent<WorkerState>
{
    Task SetDelayWorkAsync(int delaySeconds);
}

[GenerateSerializer]
public class WorkerState : GroupMemberState
{
    [Id(0)] public List<string> PreWorkUnits = new List<string>();
    [Id(1)] public int DelaySeconds { get; set; } = 0;
}