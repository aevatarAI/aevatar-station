using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Exceptions;
using Aevatar.Core.Extensions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Core;

public abstract partial class GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
{
    private static readonly ConcurrentDictionary<Type, MethodInfo[]> HandlerCache = new();
    private static readonly ConcurrentDictionary<MethodInfo, HandlerMetadata> MethodMetadataCache = new();

    private delegate Task MethodInvoker(object target, TEvent eventWrapper);

    private delegate Task<EventBase> ResponseHandlerInvoker(object target, EventBase eventBase);

    protected virtual Task UpdateObserverListAsync(Type type)
    {
        var handlerMethods = GetCachedHandlerMethods(type);

        var observers = ArrayPool<EventWrapperBaseAsyncObserver>.Shared.Rent(handlerMethods.Length);
        try
        {
            var count = 0;
            foreach (var method in handlerMethods)
            {
                var (parameterType, isResponseHandler) = GetMethodMetadata(method);
                var observer = CreateMethodObserver(method, parameterType, isResponseHandler);
                observers[count++] = observer;
            }

            foreach (var observer in observers.Take(count))
            {
                _observers.Add(observer);
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
        return HandlerCache.GetOrAdd(type, t =>
        {
            var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            return methods.AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Where(IsEventHandlerMethod)
                .OrderBy(m => m.GetCustomAttribute<EventHandlerAttribute>()?.Priority ?? 0)
                .ToArray();
        });
    }

    private static (Type, bool) GetMethodMetadata(MethodInfo method)
    {
        var paramInfo = method.GetParameters()[0];
        var paramType = paramInfo.ParameterType;
        var isResponse = paramInfo.ParameterType.BaseType is { IsGenericType: true } &&
                         paramInfo.ParameterType.BaseType.GetGenericTypeDefinition() == typeof(EventWithResponseBase<>);
        return (paramType, isResponse);
    }

    private EventWrapperBaseAsyncObserver CreateMethodObserver(
        MethodInfo method,
        Type parameterType,
        bool isResponseHandler)
    {
        return new EventWrapperBaseAsyncObserver(async item =>
        {
            var eventId = (Guid)item.GetType().GetProperty(nameof(EventWrapper<TEvent>.EventId))
                ?.GetValue(item)!;
            var eventType = (TEvent)item.GetType().GetProperty(nameof(EventWrapper<TEvent>.Event))
                ?.GetValue(item)!;
            var grainId = (GrainId)item.GetType().GetProperty(nameof(EventWrapper<TEvent>.GrainId))
                ?.GetValue(item)!;

            var eventWrapper = new EventWrapper<TEvent>(eventType, eventId, grainId);

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
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    "Event handling failed | Method:{Method} | EventId:{EventId}",
                    method.Name,
                    eventWrapper.EventId);

                throw new EventHandlingException(
                    $"Error processing {eventWrapper.Event.GetType().Name} with {method.Name}",
                    ex);
            }
        })
        {
            MethodName = method.Name,
            ParameterTypeName = parameterType.Name
        };
    }

    private bool ShouldSkipEvent(EventWrapper<TEvent> eventWrapper, MethodInfo method)
    {
        return eventWrapper.GrainId == this.GetGrainId() && !IsSelfHandlingAllowed(method);
    }

    private static bool IsSelfHandlingAllowed(MethodInfo method)
    {
        return (method.GetCustomAttribute<EventHandlerAttribute>()?.AllowSelfHandling ??
                method.GetCustomAttribute<AllEventHandlerAttribute>()?.AllowSelfHandling) ?? false;
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

            case { } ev when isResponseHandler:
                await HandleEventWithResponse(method, ev, eventWrapper.EventId);
                break;

            case not null when parameterType == typeof(EventWrapperBase):
                await HandleEventWrapperBase(method, eventWrapper);
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
        method.Invoke(this, [ev]);
    }

    private async Task HandleWrappedEvent(MethodInfo method, EventWrapper<TEvent> wrapper)
    {
        var parameterType = method.GetParameters()[0].ParameterType;
        var arg = parameterType == typeof(EventWrapper<TEvent>)
            ? wrapper
            : Activator.CreateInstance(
                method.GetParameters()[0].ParameterType,
                wrapper.Event,
                wrapper.EventId,
                wrapper.GrainId);

        await (Task)method.Invoke(this, [arg])!;
    }

    private async Task HandleEventWrapperBase(MethodInfo method, EventWrapper<TEvent> wrapperBase)
    {
        await (Task)method.Invoke(this, [wrapperBase])!;
    }

    private async Task HandleEventWithResponse(
        MethodInfo method,
        EventBase ev,
        Guid eventId)
    {
        var eventResult = await (dynamic)method.Invoke(this, [ev])!;
        await PublishResponse(eventResult, eventId);
    }

    private async Task PublishResponse(EventBase result, Guid eventId)
    {
        result.CorrelationId = _correlationId;
        result.PublisherGrainId = this.GetGrainId();
        var responseWrapper = new EventWrapper<TEvent>(
            (TEvent)result,
            eventId,
            this.GetGrainId());

        await PublishAsync(responseWrapper);
    }

    private ResponseHandlerInvoker CompileResponseInvoker(MethodInfo method)
    {
        var targetParam = Expression.Parameter(typeof(object));
        var eventParam = Expression.Parameter(typeof(EventBase));
        var castTarget = Expression.Convert(targetParam, GetType());
        var castEvent = Expression.Convert(eventParam, method.GetParameters()[0].ParameterType);
        var call = Expression.Call(castTarget, method, castEvent);
        var castResult = Expression.Convert(call, typeof(Task<EventBase>));
        var lambda = Expression.Lambda<ResponseHandlerInvoker>(castResult, targetParam, eventParam);
        return lambda.Compile();
    }

    protected virtual IEnumerable<MethodInfo> GetEventHandlerMethods(Type type) =>
        HandlerCache.GetOrAdd(type, t =>
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

    private class HandlerMetadata
    {
        public MethodInvoker? Invoker { get; init; }
        public ResponseHandlerInvoker? ResponseInvoker { get; init; }
    }
}