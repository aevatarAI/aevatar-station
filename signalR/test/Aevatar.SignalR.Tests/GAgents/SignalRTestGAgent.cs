using Aevatar.Core;
using Aevatar.Core.Abstractions;

namespace Aevatar.SignalR.Tests.GAgents;

[GenerateSerializer]
public class SignalRTestGAgentState : StateBase;

[GenerateSerializer]
public class SignalRTestStateLogEvent : StateLogEventBase<SignalRTestStateLogEvent>;

public interface ISignalRTestGAgent : IStateGAgent<SignalRTestGAgentState>;

[GAgent("signalR")]
public class SignalRTestGAgent : GAgentBase<SignalRTestGAgentState, SignalRTestStateLogEvent>, ISignalRTestGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent for testing SignalRGAgent");
    }

    [EventHandler]
    public async Task HandleEventAsync(NaiveTestEvent eventData)
    {
        await PublishAsync(new SignalRResponseEvent
        {
            Message = eventData.Greeting
        });
    }
}

[GenerateSerializer]
public class SignalRResponseEvent : ResponseToPublisherEventBase
{
    [Id(0)] public string Message { get; set; }
}