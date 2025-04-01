using System.Reflection;
using Aevatar.Application.Grains.Agents.TestAgent;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.SignalR;
using Microsoft.Extensions.Logging;

namespace SignalRSample.GAgents;

[GenerateSerializer]
public class SignalRTestGAgentState : StateBase;

[GenerateSerializer]
public class SignalRTestStateLogEvent : StateLogEventBase<SignalRTestStateLogEvent>;

public interface ISignalRTestGAgent : IStateGAgent<SignalRTestGAgentState>;

[GAgent]
public class SignalRTestGAgent : GAgentBase<SignalRTestGAgentState, SignalRTestStateLogEvent>, ISignalRTestGAgent
{
    private readonly ILogger<SignalRTestGAgent> _logger;
    
    public SignalRTestGAgent(ILogger<SignalRTestGAgent> logger)
    {
        _logger = logger;
    }
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent for testing SignalRGAgent");
    }

    [EventHandler]
    public async Task HandleEventAsync(NaiveTestEvent eventData)
    {
        //throw new Exception("Hey, something wrong here.");
        _logger.LogInformation("SignalRTestGAgent NaiveTestEvent publish begin ");
        await PublishAsync(new SignalRResponseEvent<string>
        {
            Message = eventData.Greeting,
            Data = "test"
        });
        _logger.LogInformation("SignalRTestGAgent NaiveTestEvent publish end ");
    }

    // [EventHandler]
    // public async Task HandleWithExceptionAsync(NaiveTestEvent eventData)
    // {
    //     throw new Exception("Hey, something wrong here123.");
    // }
}

[GenerateSerializer]
public class SignalRResponseEvent<T> : ResponseToPublisherEventBase
{
    [Id(0)] public string Message { get; set; }
    [Id(1)] public T Data { get; set; }
}