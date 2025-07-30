using System.Collections.Concurrent;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Communication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.EventSourcing;
using Orleans.Providers;
using Orleans.Serialization;
using Orleans.Streams;
using System.Diagnostics;

namespace Aevatar.Core;

[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public abstract class
    GAgentBase<TState, TStateLogEvent>
    : GAgentBase<TState, TStateLogEvent, EventBase, ConfigurationBase>
    where TState : StateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>;

[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public abstract class
    GAgentBase<TState, TStateLogEvent, TEvent>
    : GAgentBase<TState, TStateLogEvent, TEvent, ConfigurationBase>
    where TState : StateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase;

[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public abstract partial class
    GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
    : CoreGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>, IStateGAgent<TState>, IExtGAgent
    where TState : StateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ConfigurationBase
{
    // Note: StateDispatcher is no longer needed since state publishing is handled by CoreGAgentBase with StatePublisher

    #region IGAgent Implementation (Layered Communication)

    public async Task RegisterAsync(IGAgent gAgent)
    {
        if (gAgent.GetGrainId() == this.GetGrainId())
        {
            Logger.LogError($"Cannot register GAgent with same GrainId.");
            return;
        }

        await AddChildAsync(gAgent.GetGrainId());
        await gAgent.SubscribeToAsync(this);
        await OnRegisterAgentAsync(gAgent.GetGrainId());
    }

    public async Task RegisterManyAsync(List<IGAgent> gAgents)
    {
        if (gAgents.IsNullOrEmpty())
        {
            return;
        }

        gAgents.RemoveAll(g => g.GetGrainId() == this.GetGrainId());
        if (gAgents.IsNullOrEmpty())
        {
            return;
        }

        var grainIds = gAgents.Select(g => g.GetGrainId()).ToList();
        var tasks = new List<Task>();
        foreach (var gAgent in gAgents)
        {
            tasks.Add(gAgent.SubscribeToAsync(this));
        }
        tasks.Add(AddChildManyAsync(grainIds));
        tasks.Add(OnRegisterAgentManyAsync(grainIds));
        await Task.WhenAll(tasks);
    }

    public async Task SubscribeToAsync(IGAgent gAgent)
    {
        await SetParentAsync(gAgent.GetGrainId());
    }

    public Task UnsubscribeFromAsync(IGAgent gAgent)
    {
        return ClearParentAsync(gAgent.GetGrainId());
    }

    public async Task UnregisterAsync(IGAgent gAgent)
    {
        await RemoveChildAsync(gAgent.GetGrainId());
        await gAgent.UnsubscribeFromAsync(this);
        await OnUnregisterAgentAsync(gAgent.GetGrainId());
    }

    public Task<List<GrainId>> GetChildrenAsync()
    {
        return Task.FromResult(State.Children);
    }

    public Task<GrainId> GetParentAsync()
    {
        return Task.FromResult(State.Parent ?? default);
    }

    #endregion

    #region Layered Communication Event Handling

    [EventHandler]
    // ReSharper disable once UnusedMember.Global
    public async Task<SubscribedEventListEvent> HandleRequestAllSubscriptionsEventAsync(
        RequestAllSubscriptionsEvent request)
    {
        return await GetGroupSubscribedEventListEvent();
    }

    private async Task<SubscribedEventListEvent> GetGroupSubscribedEventListEvent()
    {
        var gAgentList = State.Children
            .Distinct()
            .Select(grainId => GrainFactory.GetGrain<IGAgent>(grainId))
            .ToList();

        if (gAgentList.IsNullOrEmpty())
        {
            return new SubscribedEventListEvent
            {
                Value = new Dictionary<Type, List<Type>>(),
                GAgentType = GetType()
            };
        }

        if (gAgentList.Any(grain => grain == null))
        {
            throw new InvalidOperationException($"Null grains detected in GAgent List. Count: {gAgentList.Count}");
        }

        var subscriptionMap = new Dictionary<Type, List<Type>>();

        foreach (var gAgent in gAgentList)
        {
            var events = await gAgent.GetAllSubscribedEventsAsync() ?? [];
            subscriptionMap[gAgent.GetType()] = events;
        }

        return new SubscribedEventListEvent
        {
            Value = subscriptionMap,
            GAgentType = GetType()
        };
    }

    [AllEventHandler(allowSelfHandling: true)]
    protected virtual async Task ForwardEventAsync(EventWrapperBase eventWrapper)
    {
        if (eventWrapper is not EventWrapper<TEvent> typedWrapper)
        {
            Logger.LogWarning("Invalid event type received: {EventType}", eventWrapper.GetType());
            return;
        }

        using (Logger.BeginScope(new Dictionary<string, object>
               {
                   ["GrainId"] = typedWrapper.GrainId,
                   ["CorrelationId"] = typedWrapper.CorrelationId!,
                   ["PublisherGrainId"] = typedWrapper.PublisherGrainId!,
                   ["EventType"] = typeof(TEvent).Name
               }))
        {
            Logger.LogDebug("Forwarding event to children: {Event}", JsonConvert.SerializeObject(typedWrapper));
            await SendEventDownwardsAsync(typedWrapper);
        }
    }

    #endregion

    #region Layered Communication Exception Handling

    /// <summary>
    /// Sends exception events using broadcast communication in layered communication.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="evevnt"></param>
    /// <param name="targetGrainId"></param>
    /// <returns></returns>
    public override async Task SendExceptionAsync<T>(T @event, GrainId targetGrainId)
    {
        await PublishAsync(@event);
    }
     
    #endregion

    #region Layered Communication Response Handling

    /// <summary>
    /// Sends response events using broadcast communication.
    /// GAgentBase uses PublishAsync for broadcast responses in layered communication.
    /// </summary>
    /// <param name="responseEvent">The response event to send</param>
    /// <param name="targetGrainId">The grain ID to send the response to (ignored in broadcast)</param>
    /// <returns>Task representing the async operation</returns>
    public override async Task SendResponseAsync<T>(EventWrapper<T> responseEvent, GrainId targetGrainId)
    {
        await PublishAsync(responseEvent);
    }

    #endregion

    #region Orleans Grain Lifecycle Overrides

    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // Note: No GAgentBase-specific dependencies to initialize
        // State management is handled by CoreGAgentBase with StatePublisher
        
        // Call base method for any derived class customization
        await base.OnGAgentActivateAsync(cancellationToken);
    }

    #endregion

    #region State Management Overrides

    // Note: State change handling is now handled by CoreGAgentBase with StatePublisher
    // GAgentBase no longer needs to override state management methods since CoreGAgentBase handles it

    #endregion

    #region Layered Communication Helper Methods

    protected virtual Task OnRegisterAgentAsync(GrainId agentGuid)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnRegisterAgentManyAsync(List<GrainId> agentGuids)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnUnregisterAgentAsync(GrainId agentGuid)
    {
        return Task.CompletedTask;
    }

    #endregion

    // Note: The following methods will be implemented when layered communication managers are integrated:
    // - AddChildAsync, AddChildManyAsync, RemoveChildAsync
    // - SetParentAsync, ClearParentAsync  
    // - SendEventDownwardsAsync
    // - PublishAsync
    // These are currently in partial class files that will be refactored in later tasks
}