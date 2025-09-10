using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.Core.Dto;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.GEvent;

namespace GroupChat.Grain;

public class Worker : GroupMemberGAgentBase<GroupMemberState, WorkerEventLog, EventBase, GroupMemberConfigDto>, IWorker
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("you are worker");
    }

    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        var random = new Random();

        return Task.FromResult(random.Next(1, 90));
    }

    protected override Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? messages)
    {
        var response = new ChatResponse();
        response.Content = $"{State.MemberName} Send the message";

        Console.WriteLine($"{State.MemberName} Can Speak, receive:{messages!.Select(s => s.Content).ToList().JoinAsString(" ")}");
        return Task.FromResult(response);
    }

    protected override Task GroupChatFinishAsync(Guid blackboardId)
    {
        Console.WriteLine($"{State.MemberName} receive finish message");
        return Task.CompletedTask;
    }
}

public interface IWorker : IGAgent
{
}

[GenerateSerializer]
public class WorkerEventLog : StateLogEventBase<WorkerEventLog>
{
}