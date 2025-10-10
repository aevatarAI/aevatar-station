using System.Collections.Concurrent;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Communication;
using Aevatar.Core.Abstractions.Exceptions;
using Aevatar.Core.Abstractions.SyncWorker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.EventSourcing;
using Orleans.Providers;
using Orleans.Serialization;
using Orleans.Streams;
using Orleans.SyncWork;
using System.Diagnostics;

namespace Aevatar.Core;

[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public abstract class
    GAgentBasePlus<TState, TStateLogEvent>
    : GAgentBasePlus<TState, TStateLogEvent, EventBase, ConfigurationBase>
    where TState : StateBasePlus, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>;

[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public abstract class
    GAgentBasePlus<TState, TStateLogEvent, TEvent>
    : GAgentBasePlus<TState, TStateLogEvent, TEvent, ConfigurationBase>
    where TState : StateBasePlus, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase;

[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public abstract partial class
    GAgentBasePlus<TState, TStateLogEvent, TEvent, TConfiguration>
    : BroadcastGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>, IStateGAgentPlus<TState>, IExtGAgentPlus
    where TState : StateBasePlus, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ConfigurationBase
{

    private Guid? _correlationId;
    private GrainId GrainId => this.GetGrainId();

    /// <summary>
    /// Register a GAgent as a child of the current GAgent.
    /// </summary>
    /// <param name="gAgent">The child GAgent to register</param>
    /// <returns></returns>
    public async Task RegisterAsync(IGAgentPlus gAgent)
    {
        if (gAgent.GetGrainId() == this.GetGrainId())
        {
            Logger.LogError($"Cannot register GAgent with same GrainId.");
            return;
        }

        await AddChildAsync(gAgent.GetGrainId());

        // Set up bidirectional communication
        await gAgent.SubscribeToParentAsync(this);     // Child subscribes to parent's downward streams
        await this.SubscribeToChildAsync(gAgent);       // Parent subscribes to child's upward streams

        await OnRegisterAgentAsync(gAgent.GetGrainId());
    }

    /// <summary>
    /// Register multiple GAgents as children of the current GAgent.
    /// </summary>
    /// <param name="gAgents">The child GAgents to register</param>
    /// <returns></returns>
    public async Task RegisterManyAsync(List<IGAgentPlus> gAgents)
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

        // Set up bidirectional communication
        foreach (var gAgent in gAgents)
        {
            tasks.Add(gAgent.SubscribeToParentAsync(this));     // Each child subscribes to parent's downward streams
        }

        // Parent subscribes to all children's upward streams in batch
        tasks.Add(this.SubscribeToManyChildAsync(gAgents));

        tasks.Add(AddChildManyAsync(grainIds));
        tasks.Add(OnRegisterAgentManyAsync(grainIds));
        await Task.WhenAll(tasks);
    }


    /// <summary>
    /// Subscribe to a parent GAgent to receive downward events from the parent.
    /// </summary>
    /// <param name="gAgent">The parent GAgent to subscribe to</param>
    /// <returns></returns>
    public async Task SubscribeToParentAsync(IGAgentPlus gAgent)
    {
        await AddParentAsync(gAgent.GetGrainId());

        // Subscribe to downward events from parent (parent is stream owner, direction 1)
        var parentStreamId = GetStreamIdStringPrefix(gAgent.GetGrainId().ToString(), 1);
        await this.SubscribeBroadcastEventAsync<TEvent>(parentStreamId, EventForwardingEventHandlerAsync);
        // await this.SubscribeBroadcastEventAsync<TConfiguration>(parentStreamId, ConfigurationForwardingEventHandlerAsync);
    }
    
    /// <summary>
    /// Subscribe to many parents' downward streams using batch pattern to receive downward events from the parents.
    /// </summary>
    /// <param name="parentAgents">The parent agents to subscribe to</param>
    /// <returns></returns>
    public async Task SubscribeToManyParentAsync(List<IGAgentPlus> parentAgents)
    {
        if (parentAgents.IsNullOrEmpty())
        {
            return;
        }

        // Use batch subscription pattern for multiple parents
        await StartBatchSubscriptionAsync();
        
        foreach (var parentAgent in parentAgents)
        {
            // Subscribe to downward events from each parent (parent is stream owner, direction 1)
            var parentStreamId = GetStreamIdStringPrefix(parentAgent.GetGrainId().ToString(), 1);
            await AddSubscriptionAsync<TEvent>(parentStreamId, EventForwardingEventHandlerAsync);
            // await AddSubscriptionAsync<TConfiguration>(parentStreamId, ConfigurationForwardingEventHandlerAsync);
        }
        
        // Save batch subscriptions
        await SaveBatchSubscriptionsAsync();
    }
    
    /// <summary>
    /// Subscribe to many parents' downward streams using batch pattern to receive downward events from the parents.
    /// </summary>
    /// <param name="parentGrainIds">The parent grain IDs to subscribe to</param>
    /// <returns></returns>
    private async Task SubscribeToManyParentByIdAsync(List<GrainId> parentGrainIds)
    {
        if (parentGrainIds.IsNullOrEmpty())
        {
            return;
        }

        // Use batch subscription pattern for multiple parents
        await StartBatchSubscriptionAsync();
        
        foreach (var parentGrainId in parentGrainIds)
        {
            // Subscribe to downward events from each parent (parent is stream owner, direction 1)
            var parentStreamId = GetStreamIdStringPrefix(parentGrainId.ToString(), 1);
            await AddSubscriptionAsync<TEvent>(parentStreamId, EventForwardingEventHandlerAsync);
            // await AddSubscriptionAsync<TConfiguration>(parentStreamId, ConfigurationForwardingEventHandlerAsync);
        }
        
        // Save batch subscriptions
        await SaveBatchSubscriptionsAsync();
    }
    
    /// <summary>
    /// Unsubscribe from a parent GAgent to stop receiving downward events from the parent.
    /// </summary>
    /// <param name="gAgent">The parent GAgent to unsubscribe from</param>
    /// <returns></returns>
    public async Task UnsubscribeFromParentAsync(IGAgentPlus gAgent)
    {
        await RemoveParentAsync(gAgent.GetGrainId());
        
        // Unsubscribe from parent's streams (parent is stream owner, direction 1)
        var parentStreamId = GetStreamIdStringPrefix(gAgent.GetGrainId().ToString(), 1);
        await this.UnSubscribeBroadcastAsync<TEvent>(parentStreamId);
        // await this.UnSubscribeBroadcastAsync<TConfiguration>(parentStreamId);
    }
    
    /// <summary>
    /// Subscribe to child's upward streams for bidirectional communication
    /// </summary>
    /// <param name="childAgent">The child agent to subscribe to</param>
    public async Task SubscribeToChildAsync(IGAgentPlus childAgent)
    {
        // Subscribe to upward events from child (child is stream owner, direction 0)
        var childStreamId = GetStreamIdStringPrefix(childAgent.GetGrainId().ToString(), 0);
        await this.SubscribeBroadcastEventAsync<TEvent>(childStreamId, EventForwardingEventHandlerAsync);
        // await this.SubscribeBroadcastEventAsync<TConfiguration>(childStreamId, ConfigurationForwardingEventHandlerAsync);
    }
    
    /// <summary>
    /// Subscribe to many children's upward streams using batch pattern to receive upward events from the children.
    /// </summary>
    /// <param name="childGrainIds">The child grain IDs to subscribe to</param>
    private async Task SubscribeToManyChildByIdAsync(List<GrainId> childGrainIds)
    {
        if (childGrainIds.IsNullOrEmpty())
        {
            return;
        }

        // Use batch subscription pattern for multiple children
        await StartBatchSubscriptionAsync();
        
        foreach (var childGrainId in childGrainIds)
        {
            // Subscribe to upward events from each child (child is stream owner, direction 0)
            var childStreamId = GetStreamIdStringPrefix(childGrainId.ToString(), 0);
            await AddSubscriptionAsync<TEvent>(childStreamId, EventForwardingEventHandlerAsync);
            // await AddSubscriptionAsync<TConfiguration>(childStreamId, ConfigurationForwardingEventHandlerAsync);
        }
        
        // Save batch subscriptions
        await SaveBatchSubscriptionsAsync();
    }
    
    /// <summary>
    /// Subscribe to many children's upward streams using batch pattern
    /// </summary>
    /// <param name="childAgents">The child agents to subscribe to</param>
    public async Task SubscribeToManyChildAsync(List<IGAgentPlus> childAgents)
    {
        if (childAgents.IsNullOrEmpty())
        {
            return;
        }

        // Use batch subscription pattern for multiple children
        await StartBatchSubscriptionAsync();

        foreach (var childAgent in childAgents)
        {
            // Subscribe to upward events from each child (child is stream owner, direction 0)
            var childStreamId = GetStreamIdStringPrefix(childAgent.GetGrainId().ToString(), 0);
            await AddSubscriptionAsync<TEvent>(childStreamId, EventForwardingEventHandlerAsync);
            // await AddSubscriptionAsync<TConfiguration>(childStreamId, ConfigurationForwardingEventHandlerAsync);
        }

        // Save batch subscriptions
        await SaveBatchSubscriptionsAsync();
    }
    
    /// <summary>
    /// Unsubscribe from child's upward streams
    /// </summary>
    /// <param name="childAgent">The child agent to unsubscribe from</param>
    public async Task UnsubscribeFromChildAsync(IGAgentPlus childAgent)
    {
        // Unsubscribe from child's upward streams (child is stream owner, direction 0)
        var childStreamId = GetStreamIdStringPrefix(childAgent.GetGrainId().ToString(), 0);
        await this.UnSubscribeBroadcastAsync<TEvent>(childStreamId);
        // await this.UnSubscribeBroadcastAsync<TConfiguration>(childStreamId);
    }

    /// <summary>
    /// Unregister a GAgent from the current GAgent's children.
    /// </summary>
    /// <param name="gAgent"></param>
    /// <returns></returns>
    public async Task UnregisterAsync(IGAgentPlus gAgent)
    {
        await RemoveChildAsync(gAgent.GetGrainId());

        // Clean up bidirectional state: child should remove this agent from its parents list
        await gAgent.RemoveSpecificParentAsync(this.GetGrainId());

        // Clean up bidirectional communication
        await gAgent.UnsubscribeFromParentAsync(this);     // Child unsubscribes from parent's downward streams
        await this.UnsubscribeFromChildAsync(gAgent);       // Parent unsubscribes from child's upward streams

        await OnUnregisterAgentAsync(gAgent.GetGrainId());
    }

    /// <summary>
    /// Unregister the current GAgent from one of its parents.
    /// </summary>
    /// <param name="parentAgent">The parent agent to unregister from</param>
    /// <returns></returns>
    public async Task UnregisterParentAsync(IGAgentPlus parentAgent)
    {
        await RemoveParentAsync(parentAgent.GetGrainId());

        // Clean up bidirectional state: parent should remove this agent from its children list
        await parentAgent.RemoveSpecificChildAsync(this.GetGrainId());

        // Clean up bidirectional communication
        await this.UnsubscribeFromParentAsync(parentAgent);    // Current agent unsubscribes from parent's downward streams
        await parentAgent.UnsubscribeFromChildAsync(this);     // Parent unsubscribes from current agent's upward streams

        await OnUnregisterParentAsync(parentAgent.GetGrainId());
    }

    /// <summary>
    /// Get the children of the current GAgent.
    /// </summary>
    /// <returns></returns>
    public Task<List<GrainId>> GetChildrenAsync()
    {
        return Task.FromResult(State.Children);
    }

    /// <summary>
    /// Get the parent of the current GAgent.
    /// </summary>
    /// <returns></returns>
    [Obsolete("Use GetParentsAsync instead")]
    public Task<GrainId> GetParentAsync()
    {
        return Task.FromResult(State.Parent ?? default);
    }
    
    /// <summary>
    /// Get the parents of the current GAgent.
    /// </summary>
    /// <returns></returns>
    public Task<List<GrainId>> GetParentsAsync()
    {
        return Task.FromResult(State.Parents);
    }

    /// <summary>
    /// Remove a specific parent from the current GAgent's parents list.
    /// This method is called by parent agents during unregistration.
    /// </summary>
    /// <param name="parentGrainId">The grain ID of the parent to remove</param>
    /// <returns></returns>
    public async Task RemoveSpecificParentAsync(GrainId parentGrainId)
    {
        await RemoveParentAsync(parentGrainId);
    }

    /// <summary>
    /// Remove a specific child from the current GAgent's children list.
    /// This method is called by child agents during unregistration.
    /// </summary>
    /// <param name="childGrainId">The grain ID of the child to remove</param>
    /// <returns></returns>
    public async Task RemoveSpecificChildAsync(GrainId childGrainId)
    {
        await RemoveChildAsync(childGrainId);
    }

    #region Layered Communication Event Handling

    public virtual async Task PrepareResourceContextAsync(ResourceContext context)
    {
        Logger.LogDebug("Preparing resource context for GAgent {GrainId} with {ResourceCount} resources",
            this.GetGrainId(), context.AvailableResources.Count);

        await OnPrepareResourceContextAsync(context);
    }

    /// <summary>
    /// Override this method in derived classes to handle resource context preparation
    /// </summary>
    /// <param name="context">The resource context containing available resources</param>
    protected virtual Task OnPrepareResourceContextAsync(ResourceContext context)
    {
        // Default implementation does nothing - derived classes can override this
        return Task.CompletedTask;
    }

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
            .Select(grainId => GrainFactory.GetGrain<IGAgentPlus>(grainId))
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

    [EventHandler]
    public async Task EventForwardingEventHandlerAsync(TEvent @event)
    {
        await EventForwardingHandlerCore(@event, OnEventForwardingEventHandlerAsync);
    }

    // [EventHandler]
    // public async Task ConfigurationForwardingEventHandlerAsync(TConfiguration configuration)
    // {
    //     await EventForwardingHandlerCore(configuration, OnConfigurationForwardingEventHandlerAsync);
    // }

    private async Task EventForwardingHandlerCore<T>(T @event, Func<T, Task<bool>> onEventHandler) where T : EventBase
    {
        // Create a deep copy of the event to avoid shared state mutations across multiple agents
        // Orleans streams broadcast the same event object reference to all subscribers
        // Each agent must work on its own copy to prevent accumulated mutations
        var copier = ServiceProvider.GetService<DeepCopier>();
        var eventCopy = copier?.Copy(@event) ?? @event;

        if (eventCopy.Publishers.Contains(this.GetGrainId()))
        {
            Logger.LogDebug("[StreamConstraint] Event already published by this grain: {Event}", JsonConvert.SerializeObject(eventCopy));
            return;
        }
        eventCopy.Publishers.Add(this.GetGrainId());
        if (!(await onEventHandler(eventCopy)))
            return;

        if (eventCopy.MaxHopCount != -1)
        {
            eventCopy.CurrentHopCount++;
            Logger.LogDebug("[StreamConstraint] Event current hop count incremented to {CurrentHopCount}: {Event}", eventCopy.CurrentHopCount, JsonConvert.SerializeObject(eventCopy));
        }

        if (eventCopy.ShouldStopPropagation || (eventCopy.MaxHopCount != -1 && eventCopy.CurrentHopCount >= eventCopy.MaxHopCount))
        {
            Logger.LogDebug("[StreamConstraint] Event should stop propagation or reached max hop count ({CurrentHopCount}/{MaxHopCount}): {Event}", eventCopy.CurrentHopCount, eventCopy.MaxHopCount, JsonConvert.SerializeObject(eventCopy));
            return;
        }

        if (eventCopy.Direction == EventDirection.Bidirectional)
        {
            Logger.LogDebug("[StreamConstraint] Event is bidirectional: {Event}", JsonConvert.SerializeObject(eventCopy));
            await SendEventUpwardsAsync(eventCopy);
            await SendEventDownwardsAsync(eventCopy);
        }
        else if (eventCopy.Direction == EventDirection.Up)
        {
            Logger.LogDebug("[StreamConstraint] Event is upward: {Event}", JsonConvert.SerializeObject(eventCopy));
            await SendEventUpwardsAsync(eventCopy);
        }
        else if (eventCopy.Direction == EventDirection.Down)
        {
            Logger.LogDebug("[StreamConstraint] Event is downward: {Event}", JsonConvert.SerializeObject(eventCopy));
            await SendEventDownwardsAsync(eventCopy);
        }
        else if (eventCopy.Direction == EventDirection.UpThenDown)
        {
            Logger.LogDebug("[StreamConstraint] Event is up then down: {Event}", JsonConvert.SerializeObject(eventCopy));
            eventCopy.Direction = EventDirection.Down;
            await SendEventUpwardsAsync(eventCopy);
        }
        else
        {
            Logger.LogError("[StreamConstraint] Event has unknown direction: {Event}", JsonConvert.SerializeObject(eventCopy));
        }
    }

    protected virtual Task<bool> OnEventForwardingEventHandlerAsync(TEvent @event)
    {
        return Task.FromResult(true);
    }

    // protected virtual Task<bool> OnConfigurationForwardingEventHandlerAsync(TConfiguration configuration)
    // {
    //     return Task.FromResult(true);
    // }

    /// <summary>
    /// Publishes an event directly based on its direction property using stream-based broadcasting
    /// </summary>
    /// <typeparam name="T">The event type</typeparam>
    /// <param name="event">The event to publish</param>
    public async Task PublishEventByDirectionAsync<T>(T @event) where T : EventBase
    {
        try
        {
            @event.PublisherGrainId = this.GetGrainId();
            @event.Publishers.Add(this.GetGrainId());
            switch (@event.Direction)
            {
                case EventDirection.Up:
                    // Upward stream (direction 0) - child owns the stream
                    var upwardStreamId = GetStreamIdStringPrefix(this.GetGrainId().ToString(), 0);
                    await this.BroadcastEventAsync(upwardStreamId, @event);
                    Logger.LogDebug("Published upward event to stream: {StreamId}", upwardStreamId);
                    break;
                    
                case EventDirection.Down:
                    // Downward stream (direction 1) - parent owns the stream
                    var downwardStreamId = GetStreamIdStringPrefix(this.GetGrainId().ToString(), 1);
                    await this.BroadcastEventAsync(downwardStreamId, @event);
                    Logger.LogDebug("Published downward event to stream: {StreamId}", downwardStreamId);
                    break;
                
                case EventDirection.UpThenDown:
                    // First goes up to parent, then parent broadcasts down to siblings
                    var upThenDownStreamId = GetStreamIdStringPrefix(this.GetGrainId().ToString(), 0);
                    @event.Direction = EventDirection.Down;
                    await this.BroadcastEventAsync(upThenDownStreamId, @event);
                    Logger.LogDebug("Published UpThenDown event to stream: {StreamId}", upThenDownStreamId);
                    break;
                    
                case EventDirection.Bidirectional:
                    // Bidirectional - publish to both streams
                    var upStreamId = GetStreamIdStringPrefix(this.GetGrainId().ToString(), 0);
                    var downStreamId = GetStreamIdStringPrefix(this.GetGrainId().ToString(), 1);
                    
                    await this.BroadcastEventAsync(upStreamId, @event);
                    await this.BroadcastEventAsync(downStreamId, @event);
                    Logger.LogDebug("Published bidirectional event to streams: {UpStreamId}, {DownStreamId}", upStreamId, downStreamId);
                    break;
                    
                default:
                    Logger.LogWarning("Unknown event direction: {Direction}", @event.Direction);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to publish event {EventType} with direction {Direction}", 
                typeof(T).Name, @event.Direction);
            throw;
        }
    }


    protected async Task DownwardEventHandlerAsync(TEvent @event)
    {
        await SendEventDownwardsAsync(@event);
    }

    protected async Task UpwardEventHandlerAsync(TEvent @event)
    {
        await SendEventUpwardsAsync(@event);
    }

    #endregion

    #region Orleans Grain Lifecycle Overrides

    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // Note: No GAgentBase-specific dependencies to initialize
        // State management is handled by CoreGAgentBase with StatePublisher
        
        // Call base method for any derived class customization
        await base.OnGAgentActivateAsync(cancellationToken);
        
        // Re-establish subscriptions that were lost during deactivation
        await ResumeForwardingSubscriptionsAsync();
    }
    
    /// <summary>
    /// Restores all parent and child subscriptions during grain activation
    /// </summary>
    private async Task ResumeForwardingSubscriptionsAsync()
    {
        try
        {
            Logger.LogDebug("Restoring subscriptions for grain {GrainId}. Parents: {ParentCount}, Children: {ChildCount}", 
                this.GetGrainId(), State.Parents?.Count ?? 0, State.Children?.Count ?? 0);

            var tasks = new List<Task>();
            
            // Restore subscriptions to parents' downward streams using batch method
            if (!State.Parents.IsNullOrEmpty())
            {
                tasks.Add(SubscribeToManyParentByIdAsync(State.Parents));
            }
            
            // Restore subscriptions to children's upward streams using batch method
            if (!State.Children.IsNullOrEmpty())
            {
                tasks.Add(SubscribeToManyChildByIdAsync(State.Children));
            }
            
            // Execute all batch subscription tasks in parallel
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
                // var totalSubscriptions = (State.Parents?.Count ?? 0) * 2 + (State.Children?.Count ?? 0) * 2; // 2 = TEvent + TConfiguration
                var totalSubscriptions = (State.Parents?.Count ?? 0) + (State.Children?.Count ?? 0);
                Logger.LogDebug("Successfully restored {SubscriptionCount} stream subscriptions for grain {GrainId} using batch methods", 
                    totalSubscriptions, this.GetGrainId());
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore subscriptions for grain {GrainId}", this.GetGrainId());
            throw;
        }
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

    protected virtual Task OnUnregisterParentAsync(GrainId parentGuid)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Publishing and Event Communication


    /// <summary>
    /// Sends an event upwards to the parent using broadcast communication.
    /// </summary>
    /// <typeparam name="T">The event type</typeparam>
    /// <param name="event">The event to send</param>
    /// <returns></returns>
    /// <exception cref="EventPublishingException"></exception>
    private async Task SendEventUpwardsAsync<T>(T @event) where T : EventBase
    {
        if (State.Parents.IsNullOrEmpty())
        {
            return;
        }

        try
        {
            var streamId = GetStreamIdStringPrefix(this.GetGrainId().ToString(), 0);
            await this.BroadcastEventAsync(streamId, @event);
        }
        catch (Exception ex)
        {
            Logger.LogError("{GrainId} failed to send event {EventWrapper} upwards", GrainId.ToString(),
                @event);
            throw new EventPublishingException($"{GrainId.ToString()} failed to send event upwards", ex);
        }
    }

    /// <summary>
    /// Get the stream ID string prefix for the given grain and direction
    /// The broadcast agent will append the event type name to create the final stream ID
    /// </summary>
    /// <param name="grainIdString">The grain ID string</param>
    /// <param name="direction">0: upwards, 1: downwards</param>
    /// <returns></returns>
    private string GetStreamIdStringPrefix(string grainIdString, int direction)
    {
        return grainIdString + "." + direction;
    }

    /// <summary>
    /// Sends an event downwards to the children using broadcast communication.
    /// </summary>
    /// <typeparam name="T">The event type</typeparam>
    /// <param name="event">The event to send</param>
    /// <returns></returns>
    /// <exception cref="EventPublishingException"></exception>
    private async Task SendEventDownwardsAsync<T>(T @event) where T : EventBase
    {
        if (State.Children.IsNullOrEmpty())
        {
            return;
        }

        Logger.LogInformation($"{GrainId.ToString()} has {State.Children.Count} children.");

        try
        {
            var streamId = GetStreamIdStringPrefix(this.GetGrainId().ToString(), 1);
            await this.BroadcastEventAsync(streamId, @event);
        }
        catch (Exception ex)
        {
            Logger.LogError("{GrainId} failed to send event {EventWrapper} downwards", GrainId.ToString(),
                @event);
            throw new EventPublishingException($"{GrainId.ToString()} failed to send event downwards", ex);
        }
    }

    #endregion

    #region State Management and Subscriptions

    protected override void GAgentTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
        switch (@event)
        {
            case AddChildStateLogEvent addChildEvent:
                Logger.LogDebug("GrainId {GrainId}: Adding child {Child}", this.GetGrainId().ToString(), addChildEvent.Child);
                State.Children.Add(addChildEvent.Child);
                break;
            case AddChildManyStateLogEvent addChildManyEvent:
                Logger.LogDebug("GrainId {GrainId}: Adding children {Child}", this.GetGrainId().ToString(), addChildManyEvent.Children);
                State.Children.AddRange(addChildManyEvent.Children);
                break;
            case RemoveChildStateLogEvent removeChildEvent:
                Logger.LogDebug("GrainId {GrainId}: Removing child {Child}", this.GetGrainId().ToString(), removeChildEvent.Child);
                State.Children.Remove(removeChildEvent.Child);
                break;
            case SetParentStateLogEvent setParentEvent:
                Logger.LogDebug("GrainId {GrainId}: Setting parent to {Parent}", this.GetGrainId().ToString(), setParentEvent.Parent);
                State.Parent = setParentEvent.Parent;
                break;
            case AddParentStateLogEvent addParentEvent:
                Logger.LogDebug("GrainId {GrainId}: Adding parent {Parent}", this.GetGrainId().ToString(), addParentEvent.Parent);
                if (!State.Parents.Contains(addParentEvent.Parent))
                    State.Parents.Add(addParentEvent.Parent);
                break;
            case RemoveParentStateLogEvent removeParentEvent:
                Logger.LogDebug("GrainId {GrainId}: Removing parent {Parent}", this.GetGrainId().ToString(), removeParentEvent.Parent);
                State.Parents.Remove(removeParentEvent.Parent);
                break;
            case ClearParentStateLogEvent clearParentEvent:
                Logger.LogDebug("GrainId {GrainId}: Clearing parent {Parent}", this.GetGrainId().ToString(), clearParentEvent.Parent);
                if (State.Parent == clearParentEvent.Parent)
                    State.Parent = default;
                break;
        }
        
        // Call base implementation for any additional logic in derived classes
        base.GAgentTransitionState(state, @event);
        
    }

    private async Task AddChildAsync(GrainId grainId)
    {
        if (State.Children.Contains(grainId))
        {
            Logger.LogError($"Cannot add duplicate child {grainId}.");
            return;
        }
        
        Logger.LogDebug("GrainId [{GrainId}] Adding child to {Parent}", this.GetGrainId().ToString(), grainId);

        base.RaiseEvent(new AddChildStateLogEvent
        {
            Child = grainId
        });
        await ConfirmEvents();
    }

    private async Task AddChildManyAsync(List<GrainId> grainIds)
    {    
        base.RaiseEvent(new AddChildManyStateLogEvent
        {
            Children = grainIds
        });
        await ConfirmEvents();
    }

    [GenerateSerializer]
    public class AddChildStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public GrainId Child { get; set; }
    }

    [GenerateSerializer]
    public class AddChildManyStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public required List<GrainId> Children { get; set; }
    }

    private async Task RemoveChildAsync(GrainId grainId)
    {
        Logger.LogDebug("GrainId [{GrainId}] Removing child to {Parent}", this.GetGrainId().ToString(), grainId);
        if (!State.Children.IsNullOrEmpty())
        {
            base.RaiseEvent(new RemoveChildStateLogEvent
            {
                Child = grainId
            });
            await ConfirmEvents();
        }
    }

    [GenerateSerializer]
    public class RemoveChildStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public GrainId Child { get; set; }
    }

    [GenerateSerializer]
    public class SetParentStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public GrainId Parent { get; set; }
    }

    private async Task SetParentAsync(GrainId grainId)
    {
        Logger.LogDebug("GrainId [{GrainId}] Setting parent to {Parent}", this.GetGrainId().ToString(), grainId);
        base.RaiseEvent(new SetParentStateLogEvent
        {
            Parent = grainId
        });
        await ConfirmEvents();
    }

    private async Task AddParentAsync(GrainId grainId)
    {
        Logger.LogDebug("Agent [{GrainId}] is adding {Parent} as one of the parent", this.GetGrainId().ToString(), grainId);
        base.RaiseEvent(new AddParentStateLogEvent
        {
            Parent = grainId
        });
        await ConfirmEvents();
        Logger.LogDebug("Agent [{GrainId}] has added {Parent} as one of the parent", this.GetGrainId().ToString(), grainId);
    }

    [GenerateSerializer]
    public class AddParentStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public GrainId Parent { get; set; }
    }

    [GenerateSerializer]
    public class ClearParentStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public GrainId Parent { get; set; }
    }
    
    [GenerateSerializer]
    public class RemoveParentStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public GrainId Parent { get; set; }
    }
    
    /// <summary>
    /// Clear the parent relationship in the internal state (low-level operation).
    /// </summary>
    /// <param name="grainId">The grain ID of the parent to clear</param>
    /// <returns>Task representing the async operation</returns>
    private async Task ClearParentAsync(GrainId grainId)
    {
        Logger.LogDebug("GrainId [{GrainId}] Removing parent to {Parent}", this.GetGrainId().ToString(), grainId);
        base.RaiseEvent(new ClearParentStateLogEvent
        {
            Parent = grainId
        });
        await ConfirmEvents();
    }
    
    private async Task RemoveParentAsync(GrainId grainId)
    {
        Logger.LogDebug("Agent [{GrainId}] is removing {Parent} from parents list", this.GetGrainId().ToString(), grainId);
        base.RaiseEvent(new RemoveParentStateLogEvent
        {
            Parent = grainId
        });
        await ConfirmEvents();
        Logger.LogDebug("Agent [{GrainId}] has removed {Parent} from parents list", this.GetGrainId().ToString(), grainId);
    }

    #endregion

    #region Sync Worker Operations

    protected async Task CreateLongRunTaskAsync<TRequest, TResponse>(TRequest request)
        where TRequest : EventBase
        where TResponse : EventBase
    {
        try
        {
            var grainId = $"{typeof(TRequest).Name}_to_{typeof(TResponse).Name}/{Guid.NewGuid()}";
            var syncWorker = GrainFactory.GetGrain<IAevatarSyncWorker<TRequest, TResponse>>(grainId);
            await syncWorker.SetLongRunTaskAsync(GetEventBaseStream(GrainId));
            await syncWorker.Start(request);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error creating long run task: {ex.Message}");
            throw;
        }
    }

    #endregion
}