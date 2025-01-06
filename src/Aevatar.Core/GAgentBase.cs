using System.Collections.Concurrent;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.EventSourcing;
using Orleans.Providers;
using Orleans.Storage;
using Orleans.Streams;

namespace Aevatar.Core;

[GAgent("base")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public abstract partial class GAgentBase<TState, TEvent> : JournaledGrain<TState, TEvent>, IStateGAgent<TState>
    where TState : StateBase, new()
    where TEvent : GEventBase
{
    protected IStreamProvider StreamProvider => this.GetStreamProvider(AevatarCoreConstants.StreamProvider);

    protected readonly ILogger Logger;
    protected readonly IGrainStorage GrainStorage;

    /// <summary>
    /// Observer -> StreamId -> HandleId
    /// </summary>
    private readonly Dictionary<EventWrapperBaseAsyncObserver, Dictionary<StreamId, Guid>> Observers = new();

    private IEventDispatcher? EventDispatcher { get; set; }

    protected GAgentBase(ILogger logger)
    {
        Logger = logger;
        GrainStorage = ServiceProvider.GetRequiredService<IGrainStorage>();
        EventDispatcher = ServiceProvider.GetService<IEventDispatcher>();
    }

    public async Task ActivateAsync()
    {
    }

    public async Task RegisterAsync(IGAgent gAgent)
    {
        var guid = gAgent.GetPrimaryKey();
        if (gAgent.GetGrainId() == this.GetGrainId())
        {
            Logger.LogError($"Cannot register GAgent with same GrainId.");
            return;
        }
        await AddSubscriberAsync(gAgent.GetGrainId());
        await gAgent.SubscribeToAsync(this);
        await OnRegisterAgentAsync(guid);
    }

    public Task SubscribeToAsync(IGAgent gAgent)
    {
        return SetSubscriptionAsync(gAgent.GetGrainId());
    }

    public async Task UnregisterAsync(IGAgent gAgent)
    {
        await RemoveSubscriberAsync(gAgent.GetGrainId());
        await OnUnregisterAgentAsync(gAgent.GetPrimaryKey());
    }

    public async Task<List<Type>?> GetAllSubscribedEventsAsync(bool includeBaseHandlers = false)
    {
        var eventHandlerMethods = GetEventHandlerMethods();
        eventHandlerMethods = eventHandlerMethods.Where(m => m.Name != nameof(ForwardEventAsync));
        var handlingTypes = eventHandlerMethods
            .Select(m => m.GetParameters().First().ParameterType);
        if (!includeBaseHandlers)
        {
            handlingTypes = handlingTypes.Where(t => t != typeof(RequestAllSubscriptionsEvent));
        }

        return handlingTypes.ToList();
    }

    public async Task<List<GrainId>> GetSubscribersAsync()
    {
        await LoadSubscribersAsync();
        return _subscribers.State;
    }

    public async Task<GrainId> GetSubscriptionAsync()
    {
        await LoadSubscriptionAsync();
        return _subscription.State;
    }

    [EventHandler]
    public async Task<SubscribedEventListEvent> HandleRequestAllSubscriptionsEventAsync(
        RequestAllSubscriptionsEvent request)
    {
        return await GetGroupSubscribedEventListEvent();
    }

    private async Task<SubscribedEventListEvent> GetGroupSubscribedEventListEvent()
    {
        await LoadSubscribersAsync();

        var gAgentList = _subscribers.State.Select(grainId => GrainFactory.GetGrain<IGAgent>(grainId)).ToList();

        if (gAgentList.IsNullOrEmpty())
        {
            return new SubscribedEventListEvent
            {
                GAgentType = GetType()
            };
        }

        if (gAgentList.Any(grain => grain == null))
        {
            // Only happened on test environment.
            throw new InvalidOperationException("One or more grains in gAgentList are null.");
        }

        var dict = new ConcurrentDictionary<Type, List<Type>>();
        foreach (var gAgent in gAgentList.AsParallel())
        {
            var eventList = await gAgent.GetAllSubscribedEventsAsync();
            dict[gAgent.GetType()] = eventList ?? [];
        }

        return new SubscribedEventListEvent
        {
            Value = dict.ToDictionary(),
            GAgentType = GetType()
        };
    }

    [AllEventHandler]
    internal async Task ForwardEventAsync(EventWrapperBase eventWrapper)
    {
        Logger.LogInformation(
            $"{this.GetGrainId().ToString()} is forwarding event downwards: {JsonConvert.SerializeObject((EventWrapper<EventBase>)eventWrapper)}");
        await SendEventDownwardsAsync((EventWrapper<EventBase>)eventWrapper);
    }

    protected virtual async Task OnRegisterAgentAsync(Guid agentGuid)
    {
    }

    protected virtual async Task OnUnregisterAgentAsync(Guid agentGuid)
    {
    }

    public abstract Task<string> GetDescriptionAsync();

    public Task<TState> GetStateAsync()
    {
        return Task.FromResult(State);
    }

    public sealed override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await BaseOnActivateAsync(cancellationToken);
        await OnGAgentActivateAsync(cancellationToken);
    }

    protected virtual async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // Derived classes can override this method.
    }

    private async Task BaseOnActivateAsync(CancellationToken cancellationToken)
    {
        // This must be called first to initialize Observers field.
        await UpdateObserverList();

        await InitializeStreamOfThisGAgentAsync();

        //_stateSaveTimer =
        // this.RegisterGrainTimer(SaveSubscriberAsync, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private async Task InitializeStreamOfThisGAgentAsync()
    {
        var streamOfThisGAgent = GetStream(this.GetGrainId().ToString());
        var handles = await streamOfThisGAgent.GetAllSubscriptionHandles();
        if (handles.Count != 0)
        {
            foreach (var handle in handles)
            {
                await handle.UnsubscribeAsync();
            }
        }

        foreach (var observer in Observers.Keys)
        {
            await streamOfThisGAgent.SubscribeAsync(observer);
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _stateSaveTimer?.Dispose();
        await SaveSubscriberAsync(cancellationToken);
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    protected virtual async Task HandleStateChangedAsync()
    {
    }

    protected sealed override void OnStateChanged()
    {
        InternalOnStateChangedAsync().ContinueWith(task =>
        {
            if (task.Exception != null)
            {
                Logger.LogError(task.Exception, "InternalOnStateChangedAsync operation failed");
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task InternalOnStateChangedAsync()
    {
        await HandleStateChangedAsync();
        //TODO:  need optimize use kafka,ensure Es written successfully
        if(EventDispatcher != null)
        {
            await EventDispatcher.PublishAsync(State, this.GetGrainId().ToString());
        }
    }
    
    protected sealed override async void RaiseEvent<TEvent>(TEvent @event)
    {
        Logger.LogInformation("base raiseEvent info:{info}", JsonConvert.SerializeObject(@event));
        base.RaiseEvent(@event);
        InternalRaiseEventAsync(@event).ContinueWith(task =>
        {
            if (task.Exception != null)
            {
                Logger.LogError(task.Exception, "InternalRaiseEventAsync operation failed");
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
    private async Task InternalRaiseEventAsync(TEvent @event)
    {
        await HandleRaiseEventAsync();
        //TODO:  need optimize use kafka,ensure Es written successfully
        if (EventDispatcher != null)
        {
            await EventDispatcher.PublishAsync(@event, @event.Id.ToString());
        }
    }
    protected virtual async Task HandleRaiseEventAsync()
    {
        
    }

    private IAsyncStream<EventWrapperBase> GetStream(string grainIdString)
    {
        var streamId = StreamId.Create(AevatarCoreConstants.StreamNamespace, grainIdString);
        return StreamProvider.GetStream<EventWrapperBase>(streamId);
    }
}