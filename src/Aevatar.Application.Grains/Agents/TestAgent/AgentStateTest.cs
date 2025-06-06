using System.ComponentModel;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.PermissionManagement;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.TestAgent;

[Description("AgentStateTest")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class AgentStateTest : GAgentBase<StateTestState, StateTestEvent>,IAgentStateTest
{
    private readonly ILogger<AgentStateTest> _logger;

    public AgentStateTest(ILogger<AgentStateTest> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a sate test agent");
    }
    
    [EventHandler]
    public async Task HandleEventAsync(SetStateEvent @event)
    {
        _logger.LogInformation("SetStateEvent: {Value}", @event.Value);
        RaiseEvent(new StateTestEvent
        {
            BoolValue = @event.Value
        });
        await ConfirmEvents();
    }
    
    protected override void GAgentTransitionState(StateTestState state, StateLogEventBase<StateTestEvent> @event)
    {
        switch (@event)
        {
            case StateTestEvent testEvent:
                State.BoolValue = testEvent.BoolValue;
                break;
        }
    }
}

[GenerateSerializer]
public class StateTestState : StateBase
{
    [Id(0)] public bool BoolValue { get; set; } = true;
}

[GenerateSerializer]
public class StateTestEvent : StateLogEventBase<StateTestEvent>
{
    [Id(0)]
    public bool BoolValue { get; set; }
}

[GenerateSerializer]
public class SetStateEvent : EventBase
{
    [Id(0)]
    public bool Value { get; set; }
}

public interface IAgentStateTest : IStateGAgent<StateTestState>
{
}