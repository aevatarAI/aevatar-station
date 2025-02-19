using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.SignalR.Core;
using Microsoft.AspNetCore;

namespace Aevatar.SignalR.GAgents;

[GenerateSerializer]

public class SignalRGAgentState : StateBase
{
    [Id(0)] public string Filter { get; set; } = string.Empty;
    [Id(1)] public string ConnectionId { get; set; } = string.Empty;
    [Id(2)] public Guid? CorrelationId { get; set; }
}

[GenerateSerializer]
public class SignalRStateLogEvent : StateLogEventBase<SignalRStateLogEvent>
{

}

[GenerateSerializer]
public class SignalRGAgentConfiguration : ConfigurationBase
{
    [Id(0)] public string ConnectionId { get; set; } = string.Empty;
}

[GAgent]
public class SignalRGAgent :
    GAgentBase<SignalRGAgentState, SignalRStateLogEvent, EventBase, SignalRGAgentConfiguration>,
    ISignalRGAgent
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly HubContext<AevatarSignalRHub> _hubContext;

    public SignalRGAgent(IGrainFactory grainFactory, IGAgentFactory gAgentFactory)
    {
        _gAgentFactory = gAgentFactory;
        _hubContext = new HubContext<AevatarSignalRHub>(grainFactory);
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("SignalR Publisher.");
    }

    public async Task PublishEventAsync<T>(T @event) where T : EventBase
    {
        await PublishAsync(@event);
        RaiseEvent(new SetCorrelationIdStateLogEvent
        {
            CorrelationId = @event.CorrelationId!.Value
        });
        await ConfirmEvents();
    }

    protected override async Task PerformConfigAsync(SignalRGAgentConfiguration configuration)
    {
        RaiseEvent(new InitializeSignalRStateLogEvent
        {
            ConnectionId = configuration.ConnectionId,
        });
        await ConfirmEvents();
    }

    [AllEventHandler]
    public async Task ResponseToSignalRAsync(EventWrapperBase eventWrapperBase)
    {
        var eventWrapper = (EventWrapper<EventBase>)eventWrapperBase;
        var @event = eventWrapper.Event;
        if (!@event.GetType().IsSubclassOf(typeof(ResponseToPublisherEventBase)))
        {
            return;
        }

        if (@event.CorrelationId != State.CorrelationId)
        {
            return;
        }

        await _hubContext.Client(State.ConnectionId).Send(SignalROrleansConstants.MethodName, @event);
        var parentGAgentGrainId = await GetParentAsync();
        var parentGAgent = await _gAgentFactory.GetGAgentAsync(parentGAgentGrainId);
        await parentGAgent.UnregisterAsync(this);
    }

    protected override void GAgentTransitionState(SignalRGAgentState state,
        StateLogEventBase<SignalRStateLogEvent> @event)
    {
        switch (@event)
        {
            case InitializeSignalRStateLogEvent initializeSignalRStateLogEvent:
                State.ConnectionId = initializeSignalRStateLogEvent.ConnectionId;
                break;
            case SetCorrelationIdStateLogEvent setCorrelationIdStateLogEvent:
                State.CorrelationId = setCorrelationIdStateLogEvent.CorrelationId;
                break;
        }
    }

    [GenerateSerializer]
    public class InitializeSignalRStateLogEvent : SignalRStateLogEvent
    {
        [Id(0)] public string ConnectionId { get; set; } = string.Empty;
    }

    [GenerateSerializer]
    public class SetCorrelationIdStateLogEvent : SignalRStateLogEvent
    {
        [Id(0)] public Guid CorrelationId { get; set; }
    }
}