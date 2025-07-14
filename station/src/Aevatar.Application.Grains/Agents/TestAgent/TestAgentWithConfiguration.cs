using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.TestAgent;

[Description("TestAgentWithConfiguration")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class TestAgentWithConfiguration : GAgentBase<TestAgentState, TestAgentEvent, EventBase, AgentConfiguration>, ITestAgentWithConfiguration
{
    private readonly ILogger<TestAgentWithConfiguration> _logger;

    public TestAgentWithConfiguration(ILogger<TestAgentWithConfiguration> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("this is used for front test");
    }
    
    protected override async Task PerformConfigAsync(AgentConfiguration configuration)
    {
        RaiseEvent(new InitializationSEvent
        {
            Id = Guid.NewGuid(),
            Name = configuration.Name
        });
        await ConfirmEvents();
    }
    

    [EventHandler]
    public async Task HandleAddDataEvent(SetNumberGEvent @event)
    {
        RaiseEvent(new SetNumberSEvent()
        {
            Id = Guid.NewGuid(),
            Number = @event.Number
        });
        await ConfirmEvents();
    }

    protected override void GAgentTransitionState(TestAgentState state, StateLogEventBase<TestAgentEvent> @event)
    {
        switch (@event)
        {
            case InitializationSEvent initializationSEvent:
                state.Id = initializationSEvent.Id;
                state.Name = initializationSEvent.Name;
                break;
            case SetNumberSEvent setDataSEvent:
                state.Number = setDataSEvent.Number;
                break;
        }
    }
}

public interface ITestAgentWithConfiguration : IGAgent
{
    // Task PublishEventAsync(FrontTestCreateEvent frontTestCreateEvent);
}

[GenerateSerializer]
public class TestAgentState : StateBase
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] [Required] public string Name { get; set; }
    [Id(2)] public int Number { get; set; }
}

[GenerateSerializer]
public class TestAgentEvent : StateLogEventBase<TestAgentEvent>
{
}

[GenerateSerializer]
public class SetNumberSEvent : TestAgentEvent
{
    [Id(0)] public int Number { get; set; }
}

[GenerateSerializer]
public class InitializationSEvent : TestAgentEvent
{
    [Id(0)] public string Name { get; set; }
}

[GenerateSerializer]
public class SetNumberGEvent : EventBase
{
    [Id(0)] [Required] public int Number { get; set; }
}


[GenerateSerializer]
public class AgentConfiguration : ConfigurationBase
{
    [Id(0)] public string Name { get; set; }
}