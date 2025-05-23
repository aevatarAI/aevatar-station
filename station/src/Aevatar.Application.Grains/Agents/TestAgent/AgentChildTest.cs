using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.TestAgent;

[Description("AgentChildTest")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class AgentChildTest : GAgentBase<ChildAgentState, FrontChildTestEvent, EventBase>, IFrontAgentChildTest
{
    private readonly ILogger<AgentChildTest> _logger;
    private readonly Random _random;

    public AgentChildTest(ILogger<AgentChildTest> logger)
    {
        _logger = logger;
        _random = new Random();
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("this is used for front child test");
    }


    [EventHandler]
    public async Task HandleFrontChildTestCreateEvent(FrontChildTestCreateEvent @event)
    {
        _logger.LogInformation("FrontChildTestCreateEvent: {name}", @event.Name);
        RaiseEvent(new FrontChildTestCreateSEvent
        {
            Id = Guid.NewGuid(),
            Name = @event.Name
        });
        await ConfirmEvents();

        await SendRandomMessageAsync();
    }

    protected override void GAgentTransitionState(ChildAgentState state, StateLogEventBase<FrontChildTestEvent> @event)
    {
        switch (@event)
        {
            case FrontChildTestCreateSEvent frontChildTestCreateSEvent:
                state.Name = frontChildTestCreateSEvent.Name;
                state.Id = Guid.NewGuid();
                state.Count++;
                break;
        }
    }

    public async Task SendRandomMessageAsync()
    {
        var randomNumber = _random.Next(1, 10);
        _logger.LogInformation("Sending random message with number {randomNumber}", randomNumber);

        if (randomNumber == 1)
        {
            await PublishAsync(new FrontParentTestCreateEvent
            {
                Name = $"Random Message {randomNumber}"
            });
        }
    }
}

public interface IFrontAgentChildTest : IGAgent
{
}

[GenerateSerializer]
public class ChildAgentState : StateBase
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] [Required] public string Name { get; set; }
    [Id(2)] public int Count { get; set; }
}

[GenerateSerializer]
public class FrontChildTestEvent : StateLogEventBase<FrontChildTestEvent>
{
}

[GenerateSerializer]
public class FrontChildTestCreateSEvent : FrontChildTestEvent
{
    [Id(0)] public string Name { get; set; }
}

[GenerateSerializer]
public class FrontChildTestCreateEvent : EventBase
{
    [Id(0)] [Required] public string Name { get; set; }
}