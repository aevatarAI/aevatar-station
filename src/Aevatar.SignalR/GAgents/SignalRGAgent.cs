using System.Threading.Channels;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.SignalR.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Aevatar.SignalR.GAgents;

[GenerateSerializer]
public class SignalRGAgentState : StateBase
{
    [Id(1)] public Dictionary<string, bool> ConnectionIds { get; set; } = new();
    [Id(2)] public Dictionary<Guid, string> ConnectionIdMap { get; set; } = new();
}

[GenerateSerializer]
public class SignalRStateLogEvent : StateLogEventBase<SignalRStateLogEvent>;

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
    private readonly HubContext<AevatarSignalRHub> _hubContext;

    private Channel<string> _signalRMessageChannel;

    public SignalRGAgent(IGrainFactory grainFactory, IGAgentFactory gAgentFactory)
    {
        _hubContext = new HubContext<AevatarSignalRHub>(grainFactory);
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("SignalR Publisher.");
    }

    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        _signalRMessageChannel = Channel.CreateUnbounded<string>();
        StartProcessingQueue();
    }

    private void StartProcessingQueue()
    {
        var reader = _signalRMessageChannel.Reader;
        Task.Run(async () =>
        {
            while (await reader.WaitToReadAsync())
            {
                while (reader.TryRead(out var msg))
                {
                    await SendWithRetryAsync(msg);
                }
            }
        });
    }

    private async Task SendWithRetryAsync(string message)
    {
        const int maxRetries = 3;
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                var connectionIdList = State.ConnectionIds;
                foreach (var (connectionId, fireAndForget) in connectionIdList)
                {
                    Logger.LogDebug("Sending message to connectionId: {ConnectionId}", connectionId);
                    await _hubContext.Client(connectionId)
                        .Send(SignalROrleansConstants.MethodName, JsonConvert.SerializeObject(message));
                    if (fireAndForget)
                    {
                        Logger.LogDebug("Cleaning up connectionId: {ConnectionId}", connectionId);
                        RaiseEvent(new RemoveConnectionIdStateLogEvent
                        {
                            ConnectionId = connectionId
                        });
                        await ConfirmEvents();
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                if (i >= maxRetries - 1)
                    Logger.LogError(ex, $"Message failed after {maxRetries} retries.");
                else
                    await Task.Delay(1000 * (i + 1));
            }
        }
    }

    private async Task EnqueueMessageAsync(string message)
    {
        await _signalRMessageChannel.Writer.WriteAsync(message);
    }

    public async Task PublishEventAsync<T>(T @event, string connectionId) where T : EventBase
    {
        await PublishAsync(@event);
        
        Logger.LogDebug("Mapping correlationId to connectionId: {@CorrelationId} {ConnectionId}", @event.CorrelationId!.Value, connectionId);
        
        RaiseEvent(new MapCorrelationIdToConnectionIdStateLogEvent
        {
            CorrelationId = @event.CorrelationId!.Value,
            ConnectionId = connectionId
        });
        await ConfirmEvents();
    }

    public async Task AddConnectionIdAsync(string connectionId, bool fireAndForget)
    {
        RaiseEvent(new AddConnectionIdStateLogEvent
        {
            ConnectionId = connectionId,
            FireAndForget = fireAndForget
        });
        await ConfirmEvents();
    }

    public async Task RemoveConnectionIdAsync(string connectionId)
    {
        RaiseEvent(new RemoveConnectionIdStateLogEvent
        {
            ConnectionId = connectionId
        });
        await ConfirmEvents();
    }

    [AllEventHandler]
    public async Task ResponseToSignalRAsync(EventWrapperBase eventWrapperBase)
    {
        Logger.LogInformation($"ResponseToSignalRAsync: {eventWrapperBase}");
        var eventWrapper = (EventWrapper<EventBase>)eventWrapperBase;
        if (!eventWrapper.Event.GetType().IsSubclassOf(typeof(ResponseToPublisherEventBase)))
        {
            Logger.LogDebug("Event is not a ResponseToPublisherEventBase");
            return;
        }

        var @event = (ResponseToPublisherEventBase)eventWrapper.Event;

        if (State.ConnectionIdMap.TryGetValue(@event.CorrelationId!.Value, out var connectionId))
        {
            @event.ConnectionId = connectionId;
        }
        else
        {
            Logger.LogInformation("Cannot find corresponding connectionId for correlationId: {@CorrelationId}", @event.CorrelationId);
        }

        await EnqueueMessageAsync(JsonConvert.SerializeObject(@event));
    }

    protected override void GAgentTransitionState(SignalRGAgentState state,
        StateLogEventBase<SignalRStateLogEvent> @event)
    {
        switch (@event)
        {
            case AddConnectionIdStateLogEvent addConnectionIdStateLogEvent:
                State.ConnectionIds[addConnectionIdStateLogEvent.ConnectionId] =
                    addConnectionIdStateLogEvent.FireAndForget;
                break;
            case RemoveConnectionIdStateLogEvent removeConnectionIdStateLogEvent:
                State.ConnectionIds.Remove(removeConnectionIdStateLogEvent.ConnectionId);
                break;
            case MapCorrelationIdToConnectionIdStateLogEvent mapCorrelationIdToConnectionIdStateLogEvent:
                State.ConnectionIdMap[mapCorrelationIdToConnectionIdStateLogEvent.CorrelationId] =
                    mapCorrelationIdToConnectionIdStateLogEvent.ConnectionId;
                break;
        }
    }

    [GenerateSerializer]
    public class AddConnectionIdStateLogEvent : SignalRStateLogEvent
    {
        [Id(0)] public string ConnectionId { get; set; } = string.Empty;
        [Id(1)] public bool FireAndForget { get; set; } = true;
    }

    [GenerateSerializer]
    public class RemoveConnectionIdStateLogEvent : SignalRStateLogEvent
    {
        [Id(0)] public string ConnectionId { get; set; } = string.Empty;
    }

    [GenerateSerializer]
    public class MapCorrelationIdToConnectionIdStateLogEvent : SignalRStateLogEvent
    {
        [Id(0)] public Guid CorrelationId { get; set; }
        [Id(1)] public string ConnectionId { get; set; }
    }
}