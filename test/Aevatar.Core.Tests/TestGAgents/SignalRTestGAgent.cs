using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestEvents;
using Aevatar.SignalR;
using Microsoft.Extensions.Logging;

namespace Aevatar.Core.Tests.TestGAgents;

[GenerateSerializer]

public class SignalRTestGAgentState : StateBase
{

}

[GenerateSerializer]
public class SignalRTestStateLogEvent : StateLogEventBase<SignalRTestStateLogEvent>
{

}

[GAgent("signalR", "test")]
public class SignalRTestGAgent : GAgentBase<SignalRTestGAgentState, SignalRTestStateLogEvent>
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