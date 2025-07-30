using System.Diagnostics;
using System.Reflection;
using System.Buffers;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.EventSourcing;
using Orleans.Providers;
using Orleans.Serialization;
using Orleans.Streams;

using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Exceptions;
using Aevatar.Core.Extensions;
using Aevatar.Core.Abstractions.Communication;
using Aevatar.Core.Abstractions.StateManagement;

namespace Aevatar.Core;

/// <summary>
/// Foundation class for core agent functionality with basic event transmission.
/// Provides Orleans JournaledGrain inheritance, core state management, and basic event communication
/// without layered communication features like parent-child relationships.
/// </summary>
[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public abstract class CoreGAgentBase<TState, TStateLogEvent>
    : CoreGAgentBase<TState, TStateLogEvent, EventBase, ConfigurationBase>
    where TState : CoreStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>;

[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public abstract class CoreGAgentBase<TState, TStateLogEvent, TEvent>
    : CoreGAgentBase<TState, TStateLogEvent, TEvent, ConfigurationBase>
    where TState : CoreStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase;

[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public abstract class CoreGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
    : JournaledGrain<TState, StateLogEventBase<TStateLogEvent>>, ICoreStateGAgent<TState>, IResponseHandler, IExceptionHandler
    where TState : CoreStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ConfigurationBase
{
    // ActivitySource for distributed tracing
    private static readonly ActivitySource ActivitySource = new("Aevatar.Core.CoreGAgent");

    // Core services and dependencies
    private Lazy<IStreamProvider> LazyStreamProvider => new(()
        => this.GetStreamProvider(AevatarCoreConstants.StreamProvider));

    protected IStreamProvider StreamProvider => LazyStreamProvider.Value;
    public ILogger Logger { get; set; } = NullLogger.Instance;

    private readonly List<EventWrapperBaseAsyncObserver> _observers = [];
    private IStatePublisher? StatePublisher { get; set; }
    protected AevatarOptions? AevatarOptions;
    private DeepCopier? _copier;
    private Guid? _correlationId;
    private GrainId GrainId => this.GetGrainId();

    #region ICoreGAgent Implementation

    public async Task ActivateAsync()
    {
        await Task.Yield();
    }

    public virtual Task<List<Type>?> GetAllSubscribedEventsAsync(bool includeBaseHandlers = false)
    {
        var eventHandlerMethods = GetEventHandlerMethods(GetType());
        eventHandlerMethods = eventHandlerMethods.Where(m =>
            m.Name != "ForwardEventAsync" && m.Name != "PerformConfigAsync");
        var handlingTypes = eventHandlerMethods
            .Select(m => m.GetParameters().First().ParameterType);
        if (!includeBaseHandlers)
        {
            handlingTypes = handlingTypes.Where(t => t != typeof(RequestAllSubscriptionsEvent));
        }

        return Task.FromResult(handlingTypes.ToList())!;
    }

    public virtual Task<Type?> GetConfigurationTypeAsync()
    {
        return Task.FromResult<Type?>(typeof(TConfiguration));
    }

    public async Task ConfigAsync(ConfigurationBase configuration)
    {
        if (configuration is TConfiguration config)
        {
            await PerformConfigAsync(config);
        }
    }

    public abstract Task<string> GetDescriptionAsync();

    #endregion

    #region ICoreStateGAgent Implementation

    public Task<TState> GetStateAsync()
    {
        return Task.FromResult(State);
    }

    #endregion

    #region Configuration

    protected virtual Task PerformConfigAsync(TConfiguration configuration)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Orleans Grain Lifecycle

    public sealed override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Initialize dependencies
        _copier = ServiceProvider.GetRequiredService<DeepCopier>();
        StatePublisher = ServiceProvider.GetService<IStatePublisher>();
        AevatarOptions = ServiceProvider.GetRequiredService<IOptions<AevatarOptions>>().Value;

        try
        {
            await base.OnActivateAsync(cancellationToken);
        }
        catch (Exception e)
        {
            Logger.LogError("Error in OnActivateAsync.base.OnActivateAsync: {ExceptionMessage}", e.Message);
            throw;
        }

        try
        {
            await BaseOnActivateAsync(cancellationToken);
        }
        catch (Exception e)
        {
            Logger.LogError("Error in OnActivateAsync.BaseOnActivateAsync: {ExceptionMessage}", e.Message);
            throw;
        }

        try
        {
            await OnGAgentActivateAsync(cancellationToken);
        }
        catch (Exception e)
        {
            Logger.LogError("Error in OnActivateAsync.OnGAgentActivateAsync: {ExceptionMessage}", e.Message);
            throw;
        }
    }

    protected virtual Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // Derived classes can override this method.
        return Task.CompletedTask;
    }

    private async Task BaseOnActivateAsync(CancellationToken cancellationToken)
    {
        try
        {
            // This must be called first to initialize Observers field.
            UpdateObserverListAsync(GetType());
            await InitializeOrResumeEventBaseStreamAsync();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error in BaseOnActivateAsync: {ExceptionMessage}", e.Message);
            throw;
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    #endregion

    #region Response Handling

    /// <summary>
    /// Sends response events using point-to-point communication.
    /// CoreGAgentBase uses SendEventToAgentAsync for direct responses.
    /// </summary>
    /// <param name="responseEvent">The response event to send</param>
    /// <param name="targetGrainId">The grain ID to send the response to</param>
    /// <returns>Task representing the async operation</returns>
    public virtual async Task SendResponseAsync<T>(EventWrapper<T> responseEvent, GrainId targetGrainId) where T : EventBase
    {
        await SendEventToAgentAsync(responseEvent, targetGrainId);
    }

    #endregion

    #region Exception Handling
    /// <summary>
    /// Sends exception events using point-to-point communication.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="event"></param>
    /// <param name="targetGrainId"></param>
    /// <returns></returns>
    public virtual async Task SendExceptionAsync<T>(T @event, GrainId targetGrainId) where T : EventBase
    {
        await SendEventToAgentAsync(@event, targetGrainId);
    }

    #endregion

    #region Basic Event Transmission

    protected async Task<Guid> SendEventToAgentAsync<T>(EventWrapper<T> eventWrapper, GrainId targetGrainId) where T : EventBase
    {
        Logger.LogInformation("Sending event {@Event} to agent {TargetId}", eventWrapper, targetGrainId);

        var eventId = eventWrapper.EventId;
        try
        {
            var stream = GetEventBaseStream(targetGrainId);
            await stream.OnNextAsync(eventWrapper);

            return eventId;
        }
        catch (Exception ex)
        {
            Logger.LogError("{GrainId} failed to send event to {TargetId}: {Event}",
                this.GetGrainId(), targetGrainId, JsonConvert.SerializeObject(eventWrapper));
            throw new InvalidOperationException($"Failed to send event to {targetGrainId}", ex);
        }
    }

    /// <summary>
    /// Sends an event directly to a known agent ID using basic event transmission.
    /// This is for direct agent-to-agent communication without layered hierarchies.
    /// </summary>
    /// <typeparam name="T">Event type</typeparam>
    /// <param name="event">The event to send</param>
    /// <param name="targetGrainId">The target agent's grain ID</param>
    /// <returns>Event ID of the sent event</returns>
    public async Task<Guid> SendEventToAgentAsync<T>(T @event, GrainId targetGrainId) where T : EventBase
    {
        @event.PublisherGrainId = this.GetGrainId();
        Logger.LogInformation("Sending event {@Event} to agent {TargetId}", @event, targetGrainId);

        var eventId = Guid.NewGuid();
        try
        {
            var eventWrapper = new EventWrapper<T>(@event, eventId, this.GetGrainId());
            eventWrapper.PublishedTimestampUtc = DateTime.UtcNow;

            var stream = GetEventBaseStream(targetGrainId);
            await stream.OnNextAsync(eventWrapper);

            return eventId;
        }
        catch (Exception ex)
        {
            Logger.LogError("{GrainId} failed to send event to {TargetId}: {Event}",
                this.GetGrainId(), targetGrainId, JsonConvert.SerializeObject(@event));
            throw new InvalidOperationException($"Failed to send event to {targetGrainId}", ex);
        }
    }

    /// <summary>
    /// Gets the event stream for a specific grain ID.
    /// </summary>
    /// <param name="grainId">The grain ID to get the stream for</param>
    /// <returns>The async stream for the specified grain</returns>
    protected virtual IAsyncStream<EventWrapperBase> GetEventBaseStream(GrainId grainId)
    {
        using var activity = ActivitySource.StartActivity("GetEventBaseStream");
        activity?.SetTag("grain.id", grainId.ToString());
        activity?.SetTag("stream.namespace", AevatarOptions!.StreamNamespace);
        activity?.SetTag("component", "CoreGAgentBase");

        var grainIdString = grainId.ToString();
        var streamId = StreamId.Create(AevatarOptions!.StreamNamespace, grainIdString);

        activity?.SetTag("stream.id", streamId.ToString());
        activity?.SetTag("operation", "GetEventBaseStream");

        return StreamProvider.GetStream<EventWrapperBase>(streamId);
    }

    private async Task InitializeOrResumeEventBaseStreamAsync()
    {
        try
        {
            var streamOfThisAgent = GetEventBaseStream(this.GetGrainId());
            var handles = await streamOfThisAgent.GetAllSubscriptionHandles();
            var asyncObserver = new GAgentAsyncObserver(_observers, this.GetGrainId().ToString());

            if (handles.Count > 0)
            {
                foreach (var handle in handles)
                {
                    await handle.ResumeAsync(asyncObserver);
                }
            }
            else
            {
                await streamOfThisAgent.SubscribeAsync(asyncObserver);
            }
        }
        catch (Exception e)
        {
            Logger.LogError($"Error in InitializeOrResumeEventBaseStreamAsync: {e}");
            throw;
        }
    }

    #endregion

    #region State Management

    protected virtual Task HandleStateChangedAsync()
    {
        // Derived classes can override this method.
        return Task.CompletedTask;
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

        // Publish state changes using StatePublisher with version information
        await StatePublisher.DispatchStateAsync(State, this.GetGrainId(), Version);
    }

    protected sealed override async void RaiseEvent<T>(T @event)
    {
        Logger.LogDebug("Core event raised: {Event}", JsonConvert.SerializeObject(@event));
        base.RaiseEvent(@event);

        AsyncTaskRunner.RunSafely(async () =>
        {
            try
            {
                await InternalRaiseEventAsync(@event);
            }
            catch (TimeoutException ex)
            {
                Logger.LogError(ex, "Event processing timeout occurred");
            }
        }, Logger);
    }

    private async Task InternalRaiseEventAsync<T>(T raisedStateLogEvent) where T : StateLogEventBase<TStateLogEvent>
    {
        await HandleRaiseEventAsync();
    }

    protected virtual Task HandleRaiseEventAsync()
    {
        // Derived classes can override this method.
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sealed override of JournaledGrain's TransitionState to prevent further overriding.
    /// Custom state transition logic should be implemented in GAgentTransitionState instead.
    /// </summary>
    /// <param name="state">The state being transitioned</param>
    /// <param name="event">The state log event causing the transition</param>
    protected sealed override void TransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {

        Logger.LogDebug("GrainId {GrainId}: State before transition: {@State}", this.GetGrainId().ToString(), State);

        // Call the virtual method that derived classes can override for additional logic
        GAgentTransitionState(state, @event);

        // Call the base Orleans JournaledGrain.TransitionState to apply state changes
        base.TransitionState(state, @event);

        // print out the state after transition
        Logger.LogDebug("GrainId {GrainId}: State after transition: {@State}", this.GetGrainId().ToString(), State);
    }

    /// <summary>
    /// Virtual method for derived classes to handle custom state transitions.
    /// Called during state transition processing to allow additional state modification logic.
    /// </summary>
    /// <param name="state">The state being transitioned</param>
    /// <param name="event">The state log event causing the transition</param>
    protected virtual void GAgentTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
        // Derived classes can override this method for custom state transitions.
    }

    #endregion

    #region Observer Management

    private readonly ConcurrentDictionary<Type, MethodInfo[]> _handlerCache = new();

    protected virtual Task UpdateObserverListAsync(Type type)
    {
        var handlerMethods = GetCachedHandlerMethods(type);

        var observers = ArrayPool<EventWrapperBaseAsyncObserver>.Shared.Rent(handlerMethods.Length);
        try
        {
            var count = 0;
            foreach (var method in handlerMethods)
            {
                try
                {
                    var (parameterType, isResponseHandler) = method.AnalysisMethodMetadata();
                    var observer = CreateMethodObserver(method, parameterType, isResponseHandler);
                    observers[count++] = observer;
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Logger.LogCritical(ex, "Metadata analysis failed for method {Method}", method.Name);
                    throw new InvalidOperationException($"Type load error in {method.Name}", ex);
                }
            }

            try
            {
                _observers.AddRange(observers.Take(count));
            }
            catch (ArgumentNullException ex)
            {
                Logger.LogWarning(ex, "Attempted to add null observer for {Type}", type.Name);
            }
        }
        finally
        {
            ArrayPool<EventWrapperBaseAsyncObserver>.Shared.Return(observers);
        }

        return Task.CompletedTask;
    }

    private MethodInfo[] GetCachedHandlerMethods(Type type)
    {
        try
        {
            return _handlerCache.GetOrAdd(type, t =>
            {
                try
                {
                    var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    return methods.AsParallel()
                        .WithDegreeOfParallelism(Math.Max(1, Environment.ProcessorCount - 1))
                        .Where(IsEventHandlerMethod)
                        .OrderBy(m => m.GetCustomAttribute<EventHandlerAttribute>()?.Priority ?? 0)
                        .ToArray();
                }
                catch (ArgumentNullException ex)
                {
                    Logger.LogError(ex, "Type metadata resolution failed for {Type}", t.Name);
                    return Array.Empty<MethodInfo>();
                }
            });
        }
        catch (Exception ex) when (ex is ArgumentNullException or OverflowException)
        {
            Logger.LogCritical(ex, "Handler cache corruption detected");
            _handlerCache.Clear();
            throw;
        }
    }

    private EventWrapperBaseAsyncObserver CreateMethodObserver(
        MethodInfo method,
        Type parameterType,
        bool isResponseHandler)
    {
        return EventWrapperBaseAsyncObserver.Create(async item =>
        {
            using (Logger.BeginScope(new Dictionary<string, object>
            {
                ["GrainId"] = this.GetGrainId(),
                ["EventWrapperBase"] = item
            }))
            {
                try
                {
                    var eventId = (Guid)item.GetType().GetProperty(nameof(EventWrapper<TEvent>.EventId))
                        ?.GetValue(item)!;
                    var eventType = (TEvent)item.GetType().GetProperty(nameof(EventWrapper<TEvent>.Event))
                        ?.GetValue(item)!;
                    var grainId = (GrainId)item.GetType().GetProperty(nameof(EventWrapper<TEvent>.GrainId))
                        ?.GetValue(item)!;
                    var publishedTimestamp = (DateTime)item.GetType().GetProperty(nameof(EventWrapper<TEvent>.PublishedTimestampUtc))
                        ?.GetValue(item)!;

                    var eventWrapper = new EventWrapper<TEvent>(eventType, eventId, grainId);
                    eventWrapper.PublishedTimestampUtc = publishedTimestamp;

                    Logger.LogInformation("Handling event {EventWrapper} in method {MethodName}", eventWrapper,
                        method.Name);

                    if (ShouldSkipEvent(eventWrapper, method))
                        return;

                    _correlationId = eventWrapper.Event.CorrelationId;

                    try
                    {
                        await HandleEventWrapper(
                            method,
                            parameterType,
                            eventWrapper,
                            isResponseHandler
                        );
                    }
                    catch (Exception ex) when (ex is EventHandlingException)
                    {
                        Logger.LogError(ex,
                            "Event handling failed | Method:{Method} | EventId:{EventId}",
                            method.Name,
                            eventWrapper.EventId);                        

                        await SendExceptionAsync(new EventHandlerExceptionEvent
                        {
                            GrainId = this.GetGrainId(),
                            HandleEventType = parameterType,
                            ExceptionMessage = ex.ToString()
                        }, eventWrapper.PublisherGrainId);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex,
                            "Framework error occured | Method:{Method} | EventId:{EventId}",
                            method.Name,
                            eventWrapper.EventId);
                        
                        await SendExceptionAsync(new GAgentBaseExceptionEvent
                        {
                            GrainId = this.GetGrainId(),
                            ExceptionMessage = ex.ToString()
                        }, eventWrapper.PublisherGrainId);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Logger.LogCritical(ex, "Unhandled event processing error");
                    throw;
                }
            }
        }, ServiceProvider, method.Name, parameterType.Name);
    }

    private bool ShouldSkipEvent(EventWrapper<TEvent> eventWrapper, MethodInfo method)
    {
        return eventWrapper.GrainId == this.GetGrainId() && !method.IsSelfHandlingAllowed();
    }

    private async Task HandleEventWrapper(
        MethodInfo method,
        Type parameterType,
        EventWrapper<TEvent> eventWrapper,
        bool isResponseHandler)
    {
        switch (eventWrapper.Event)
        {
            case { } ev when parameterType.BaseType == typeof(EventBase):
                await HandleEvent(method, ev);
                break;

            case not null when parameterType == typeof(EventWrapperBase):
                await HandleEventWrapperBase(method, eventWrapper);
                break;

            case { } ev when isResponseHandler:
                await HandleEventWithResponse(method, ev, eventWrapper.EventId);
                break;

            default:
                Logger.LogWarning("Unmatched event type {Type} for method {Method}",
                    eventWrapper.Event!.GetType().Name,
                    method.Name);
                break;
        }
    }

    private async Task HandleEvent(MethodInfo method, TEvent ev)
    {
        try
        {
            await (Task)method.Invoke(this, [ev])!;
        }
        catch (ArgumentException ex)
        {
            Logger.LogError(ex, "Parameter mismatch in {Method}", method.Name);
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error while invoking {Method}", method.Name);
            throw new EventHandlingException(ex.InnerException?.ToString() ?? ex.ToString(), ex.InnerException ?? ex);
        }
    }

    private async Task HandleEventWrapperBase(MethodInfo method, EventWrapper<TEvent> wrapperBase)
    {
        try
        {
            await (Task)method.Invoke(this, [wrapperBase])!;
        }
        catch (InvalidCastException ex)
        {
            Logger.LogError(ex, "Invalid return type from {Method}", method.Name);
            throw new InvalidOperationException("Handler returned non-task result", ex);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error while invoking {Method}", method.Name);
            throw new EventHandlingException(ex.InnerException?.ToString() ?? ex.ToString(), ex.InnerException ?? ex);
        }
    }

    private async Task HandleEventWithResponse(
        MethodInfo method,
        EventBase ev,
        Guid eventId)
    {
        try
        {
            dynamic result = method.Invoke(this, [ev])!;
            if (result is not Task<EventBase> && !typeof(EventBase).IsAssignableFrom(result.GetType().GetGenericArguments()[0]))
            {
                throw new InvalidOperationException("Response handler must return Task<EventBase or its derived type>");
            }

            var eventResult = await result;
            await PublishResponse(eventResult, eventId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error while invoking {Method}", method.Name);
            throw new EventHandlingException(ex.InnerException?.ToString() ?? ex.ToString(), ex.InnerException ?? ex);
        }
    }

    private async Task PublishResponse(EventBase result, Guid eventId)
    {
        var publishGrainId = result.PublisherGrainId;
        result.CorrelationId = _correlationId;
        result.PublisherGrainId = this.GetGrainId();
        var responseWrapper = new EventWrapper<TEvent>(
            (TEvent)result,
            eventId,
            this.GetGrainId());
        responseWrapper.PublishedTimestampUtc = DateTime.UtcNow;

        await SendResponseAsync(responseWrapper, publishGrainId);
    }

    protected virtual IEnumerable<MethodInfo> GetEventHandlerMethods(Type type) =>
        _handlerCache.GetOrAdd(type, t =>
            t.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(IsEventHandlerMethod)
                .ToArray());

    protected virtual bool IsEventHandlerMethod(MethodInfo method)
    {
        var param = method.GetParameters();
        if (param.Length != 1) return false;

        var paramType = param[0].ParameterType;
        return paramType switch
        {
            _ when paramType.IsAssignableTo(typeof(TEvent)) =>
                method.HasAttribute<EventHandlerAttribute>() || IsDefaultHandler(method),

            _ when paramType.IsAssignableTo(typeof(EventWrapperBase)) =>
                method.HasAttribute<AllEventHandlerAttribute>(),

            _ when paramType.IsAssignableTo(typeof(ConfigurationBase)) =>
                method.Name == nameof(PerformConfigAsync),

            _ => false
        };

        bool IsDefaultHandler(MethodInfo m) =>
            m.Name == AevatarGAgentConstants.EventHandlerDefaultMethodName &&
            !paramType.IsAbstract;
    }

    #endregion
} 