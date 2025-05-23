using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.TestAgent;

[Description("AgentParentTest")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class AgentParentTest : GAgentBase<ParentAgentState, FrontParentTestEvent, EventBase>, IFrontAgentParentTest
{
    private readonly ILogger<AgentParentTest> _logger;
    private readonly Random _random;

    public AgentParentTest(ILogger<AgentParentTest> logger)
    {
        _logger = logger;
        _random = new Random();
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("this is used for front parent test");
    }

    [EventHandler]
    public async Task HandleFrontParentTestCreateEvent(FrontParentTestCreateEvent @event)
    {
        _logger.LogInformation("FrontParentTestCreateEvent: {name}", @event.Name);
        RaiseEvent(new FrontParentTestCreateSEvent
        {
            Id = Guid.NewGuid(),
            Name = @event.Name
        });
        await ConfirmEvents();
    }

    protected override void GAgentTransitionState(ParentAgentState state,
        StateLogEventBase<FrontParentTestEvent> @event)
    {
        switch (@event)
        {
            case FrontParentTestCreateSEvent frontParentTestCreateSEvent:
                state.Name = frontParentTestCreateSEvent.Name;
                state.Id = Guid.NewGuid();
                state.Count++;
                break;
        }
    }
}

public interface IFrontAgentParentTest : IGAgent
{
}

[GenerateSerializer]
public class ParentAgentState : StateBase
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] [Required] public string Name { get; set; }
    [Id(2)] public int Count { get; set; }
}

[GenerateSerializer]
public class FrontParentTestEvent : StateLogEventBase<FrontParentTestEvent>
{
}

[GenerateSerializer]
public class FrontParentTestCreateSEvent : FrontParentTestEvent
{
    [Id(0)] public string Name { get; set; }
}

[GenerateSerializer]
public class FrontParentTestCreateEvent : EventBase
{
    [Id(0)] [Required] public string Name { get; set; }
}