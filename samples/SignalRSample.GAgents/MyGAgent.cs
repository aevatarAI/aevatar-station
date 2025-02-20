using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.SignalR;

namespace SignalRSample.GAgents;

[GenerateSerializer]
public class MyGAgentState : StateBase
{
    // Define properties.
}

[GenerateSerializer]
public class MyStateLogEvent : StateLogEventBase<MyStateLogEvent>
{
    // Define properties.
}

[GAgent]
public class MyGAgent : GAgentBase<MyGAgentState, MyStateLogEvent>
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent for demo.");
    }
    
    [EventHandler]
    public async Task HandleEventAsync(UserLoginEvent eventData)
    {
        // Some logic.

        await PublishAsync(new LoginResponse
        {
            Success = true,
            SessionId = Guid.NewGuid()
        });
    }
}

[GenerateSerializer]
public class UserLoginEvent : EventBase
{
    [Id(0)] public Guid UserId { get; set; }
    [Id(1)] public DateTimeOffset LoginTime { get; set; }
}

[GenerateSerializer]
public class LoginResponse : ResponseToPublisherEventBase
{
    [Id(0)] public bool Success { get; set; }
    [Id(1)] public Guid SessionId { get; set; }
}