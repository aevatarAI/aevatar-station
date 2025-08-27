using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Aevatar.SignalR;

namespace Aevatar.Application.Grains.Agents.TestAgent;

[Description("AgentTest")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class AgentTest : GAgentBase<FrontAgentState, FrontTestEvent, EventBase>, IFrontAgentTest
{
    private readonly ILogger<AgentTest> _logger;

    public AgentTest(ILogger<AgentTest> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("this is used for front test");
    }

    public async Task PublishEventAsync(FrontTestCreateEvent @event)
    {
        _logger.LogInformation("FrontTestCreateEvent: {name}", @event.Name);
        RaiseEvent(new FrontTestCreateSEvent
        {
            Id = Guid.NewGuid(),
            Name = @event.Name,
        });
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleFrontTestCreateEvent(FrontTestCreateEvent @event)
    {
        _logger.LogInformation("FrontTestCreateEvent: {name}", @event.Name);
        RaiseEvent(new FrontTestCreateSEvent
        {
            Id = Guid.NewGuid(),
            Name = @event.Name
        });
        await ConfirmEvents();
        await PublishAsync(new FrontTestResponseEvent
        {
            Name = @event.Name
        });
    }

    protected override void GAgentTransitionState(FrontAgentState state, StateLogEventBase<FrontTestEvent> @event)
    {
        switch (@event)
        {
            case FrontTestCreateSEvent frontTestCreateSEvent:
                state.Name = frontTestCreateSEvent.Name;
                state.Id = Guid.NewGuid();
                break;
        }
    }
}

public interface IFrontAgentTest : IGAgent
{
    Task PublishEventAsync(FrontTestCreateEvent frontTestCreateEvent);
}

[GenerateSerializer]
public class FrontAgentState : StateBase
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] [Required] public string Name { get; set; }
}

[GenerateSerializer]
public class FrontTestEvent : StateLogEventBase<FrontTestEvent>
{
}

[GenerateSerializer]
public class FrontTestCreateSEvent : FrontTestEvent
{
    [Id(0)] public string Name { get; set; }
}

[GenerateSerializer]
public class FrontTestCreateEvent : EventBase
{
    [Id(0)] [Required] public string Name { get; set; }
}

[GenerateSerializer]
public class FrontTestResponseEvent : EventBase
{
    [Id(0)] [Required] public string Name { get; set; }
}

[GenerateSerializer]
public class FrontInitConfig : ConfigurationBase
{
    [Id(0)] [Required] public string Name { get; set; }

    [Id(1)] [Required] public List<int> StudentIds { get; set; }

    [Id(2)] [Required] public JobType JobType { get; set; }

    [Required]
    [RegularExpression(@"^https?://.*")]
    [Description("the url of school")]
    [Id(3)]
    public string Url { get; set; }

    [Id(4)] public string Memo { get; set; }
}

public enum JobType
{
    Teacher,
    Professor,
    Dean
}

[GenerateSerializer]
public class SignalRTestGAgentState : StateBase;

[GenerateSerializer]
public class SignalRTestStateLogEvent : StateLogEventBase<SignalRTestStateLogEvent>;

public interface ISignalRTestGAgent : IStateGAgent<SignalRTestGAgentState>;

[GAgent]
public class SignalRTestGAgent : GAgentBase<SignalRTestGAgentState, SignalRTestStateLogEvent>, ISignalRTestGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent for testing SignalRGAgent");
    }

    [EventHandler]
    public async Task HandleEventAsync(NaiveTestEvent eventData)
    {
        //throw new Exception("Hey, something wrong here.");

        await PublishAsync(new SignalRResponseEvent<string>
        {
            Message = eventData.Greeting,
            Data = "test"
        });
    }
}

[GenerateSerializer]
public class NaiveTestEvent : EventBase
{
    [Id(0)] public string Greeting { get; set; }
}

[GenerateSerializer]
public class SignalRResponseEvent<T> : ResponseToPublisherEventBase
{
    [Id(0)] public string Message { get; set; }
    [Id(1)] public T Data { get; set; }
}