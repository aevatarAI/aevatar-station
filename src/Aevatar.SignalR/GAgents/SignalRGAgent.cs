using System.Threading.Channels;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.SignalR.Core;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Timers;

namespace Aevatar.SignalR.GAgents;

[GenerateSerializer]
public class SignalRGAgentState : StateBase
{
    [Id(1)] public Dictionary<string, bool> ConnectionIds { get; set; } = new();
    [Id(2)] public Dictionary<Guid, string> ConnectionIdMap { get; set; } = new();
    [Id(3)] public Queue<ResponseToPublisherEventBase> MessageQueue { get; set; } = new();
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
    private IDisposable? _processQueueTimer;
    private readonly TimeSpan _processQueueInterval = TimeSpan.FromSeconds(1);
    private const int MaxMessagesPerBatch = 20;
    private bool _isProcessingQueue;

    public SignalRGAgent(IGrainFactory grainFactory)
    {
        _hubContext = new HubContext<AevatarSignalRHub>(grainFactory);
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("SignalR Publisher.");
    }

    protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        _processQueueTimer = RegisterTimer(
            ProcessQueueTimerCallback,
            null,
            TimeSpan.Zero,
            _processQueueInterval);
            
        return Task.CompletedTask;
    }

    private async Task ProcessQueueTimerCallback(object state)
    {
        await ProcessQueueAsync();
    }

    private async Task ProcessQueueAsync()
    {
        if (_isProcessingQueue) return;
        
        try
        {
            _isProcessingQueue = true;
            
            var messagesToProcess = new List<ResponseToPublisherEventBase>();
            int count = 0;
            
            while (State.MessageQueue.Count > 0 && count < MaxMessagesPerBatch)
            {
                messagesToProcess.Add(State.MessageQueue.Dequeue());
                count++;
            }
            
            await ConfirmEvents();
            
            foreach (var message in messagesToProcess)
            {
                await SendWithRetryAsync(message);
            }
        }
        finally
        {
            _isProcessingQueue = false;
        }
    }

    private async Task SendWithRetryAsync(object message)
    {
        const int maxRetries = 3;
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                var connectionIdList = new Dictionary<string, bool>(State.ConnectionIds);
                foreach (var (connectionId, fireAndForget) in connectionIdList)
                {
                    Logger.LogInformation("Sending message to connectionId: {ConnectionId}, Message {Message}", connectionId,
                        message);
                    await _hubContext.Client(connectionId)
                        .Send(SignalROrleansConstants.ResponseMethodName, message);
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

    private Task EnqueueMessageAsync(ResponseToPublisherEventBase message)
    {
        RaiseEvent(new EnqueueMessageStateLogEvent
        {
            Message = message
        });
        return ConfirmEvents();
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

        await EnqueueMessageAsync(new AevatarSignalRResponse<ResponseToPublisherEventBase>
        {
            IsSuccess = true,
            Response = @event
        });
    }

    [EventHandler]
    public async Task HandleExceptionEventAsync(EventHandlerExceptionEvent @event)
    {
        Logger.LogInformation($"HandleExceptionEventAsync: {@event}");

        if (State.ConnectionIdMap.TryGetValue(@event.CorrelationId!.Value, out var connectionId))
        {
            var response = new AevatarSignalRResponse<ResponseToPublisherEventBase>
            {
                IsSuccess = false,
                ErrorType = ErrorType.EventHandler,
                ErrorMessage = $"GrainId: {@event.GrainId}, ExceptionMessage: {@event.ExceptionMessage}",
                ConnectionId = connectionId
            };
            await EnqueueMessageAsync(response);
        }
    }

    [EventHandler]
    public async Task GAgentBaseExceptionEventAsync(GAgentBaseExceptionEvent @event)
    {
        Logger.LogInformation($"GAgentBaseExceptionEventAsync: {@event}");

        if (State.ConnectionIdMap.TryGetValue(@event.CorrelationId!.Value, out var connectionId))
        {
            var response = new AevatarSignalRResponse<ResponseToPublisherEventBase>
            {
                IsSuccess = false,
                ErrorType = ErrorType.Framework,
                ErrorMessage = $"GrainId: {@event.GrainId}, ExceptionMessage: {@event.ExceptionMessage}",
                ConnectionId = connectionId
            };
            await EnqueueMessageAsync(response);
        }
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
            case EnqueueMessageStateLogEvent enqueueMessageStateLogEvent:
                State.MessageQueue.Enqueue(enqueueMessageStateLogEvent.Message);
                break;
        }
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _processQueueTimer?.Dispose();
        return Task.CompletedTask;
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
    
    [GenerateSerializer]
    public class EnqueueMessageStateLogEvent : SignalRStateLogEvent
    {
        [Id(0)] public ResponseToPublisherEventBase Message { get; set; }
    }
}