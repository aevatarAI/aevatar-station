using System.Reflection;
using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Plugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Streams;

namespace Aevatar.Core.Plugin;

/// <summary>
/// Interface for grains that can receive method calls dynamically
/// </summary>
public interface IMethodCallable
{
    Task<object?> CallMethodAsync(string methodName, object?[] parameters);
}

/// <summary>
/// Interface for grains that can receive events
/// </summary>
public interface IEventReceiver
{
    Task ReceiveEventAsync(EventBase eventBase, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of IAgentContext that bridges plugin calls to Orleans/GAgentBase
/// </summary>
public class AgentContext : IAgentContext
{
    private readonly IGAgent _hostGAgent;
    private readonly ILogger _logger;
    private readonly IReadOnlyDictionary<string, object> _configuration;
    private readonly IGrainFactory _grainFactory;
    private readonly IServiceProvider _serviceProvider;

    public AgentContext(
        IGAgent hostGAgent, 
        ILogger logger,
        IGrainFactory grainFactory,
        IServiceProvider serviceProvider,
        IReadOnlyDictionary<string, object>? configuration = null)
    {
        _hostGAgent = hostGAgent ?? throw new ArgumentNullException(nameof(hostGAgent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _grainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _configuration = configuration ?? new Dictionary<string, object>();
        
        AgentId = hostGAgent.GetGrainId().ToString();
        Logger = new AgentLoggerAdapter(logger);
    }

    public string AgentId { get; }
    public IAgentLogger Logger { get; }
    public IReadOnlyDictionary<string, object> Configuration => _configuration;

    public async Task PublishEventAsync(IAgentEvent agentEvent, CancellationToken cancellationToken = default)
    {
        // Convert IAgentEvent to Orleans EventBase
        var orleansEvent = ConvertToOrleansEvent(agentEvent);
        
        // Use GAgent's publishing mechanism (this would need to be exposed or we'd need a different approach)
        // For now, we'll assume there's a way to publish through the host GAgent
        if (_hostGAgent is GAgentBase<PluginAgentState, PluginStateLogEvent> gAgentBase)
        {
            // This would require exposing PublishAsync as protected internal or adding a public wrapper
            await PublishThroughGAgent(gAgentBase, orleansEvent);
        }
    }

    public async Task<TResponse> PublishEventWithResponseAsync<TResponse>(
        IAgentEvent agentEvent, 
        TimeSpan? timeout = null, 
        CancellationToken cancellationToken = default) where TResponse : class
    {
        var orleansEvent = ConvertToOrleansEvent(agentEvent);
        
        // Similar to above - this would need a mechanism to publish with response
        if (_hostGAgent is GAgentBase<PluginAgentState, PluginStateLogEvent> gAgentBase)
        {
            var response = await PublishWithResponseThroughGAgent(gAgentBase, orleansEvent, timeout ?? TimeSpan.FromSeconds(30));
            return ConvertFromOrleansResponse<TResponse>(response);
        }
        
        throw new NotSupportedException("Host GAgent does not support publish with response");
    }

    public async Task<IAgentReference> GetAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        // Create Orleans GrainId from string
        var grainId = GrainId.Create("IGAgent", agentId);
        
        // Get grain reference through Orleans
        var grain = _grainFactory.GetGrain<IGAgent>(grainId);
        
        // Get required services
        var pluginRegistry = GetPluginRegistry();
        
        return new AgentReference(grain, agentId, pluginRegistry, _logger, _grainFactory);
    }

    public async Task RegisterAgentsAsync(IEnumerable<string> agentIds, CancellationToken cancellationToken = default)
    {
        var grains = agentIds.Select(id => _grainFactory.GetGrain<IGAgent>(GrainId.Create("IGAgent", id))).ToList();
        
        if (_hostGAgent is GAgentBase<PluginAgentState, PluginStateLogEvent> gAgentBase)
        {
            await gAgentBase.RegisterManyAsync(grains);
        }
        else
        {
            await Task.CompletedTask;
        }
    }

    public async Task SubscribeToAgentsAsync(IEnumerable<string> agentIds, CancellationToken cancellationToken = default)
    {
        foreach (var agentId in agentIds)
        {
            var grain = _grainFactory.GetGrain<IGAgent>(GrainId.Create("IGAgent", agentId));
            await _hostGAgent.SubscribeToAsync(grain);
        }
    }

    // Helper methods
    private EventBase ConvertToOrleansEvent(IAgentEvent agentEvent)
    {
        // Create a generic event wrapper that can carry plugin events
        return new PluginEventWrapper
        {
            PluginEventType = agentEvent.EventType,
            PluginEventData = agentEvent.Data,
            Timestamp = agentEvent.Timestamp,
            CorrelationId = string.IsNullOrEmpty(agentEvent.CorrelationId) ? null : Guid.Parse(agentEvent.CorrelationId),
            SourceAgentId = agentEvent.SourceAgentId
        };
    }

    private TResponse ConvertFromOrleansResponse<TResponse>(object response) where TResponse : class
    {
        if (response is TResponse typedResponse)
        {
            return typedResponse;
        }
        
        // Try to convert or deserialize
        // This would need more sophisticated conversion logic
        throw new InvalidCastException($"Cannot convert response of type {response?.GetType()} to {typeof(TResponse)}");
    }

    private IAgentPluginRegistry GetPluginRegistry()
    {
        // In a real implementation, this would be injected through DI
        return _serviceProvider.GetService(typeof(IAgentPluginRegistry)) as IAgentPluginRegistry
            ?? throw new InvalidOperationException("IAgentPluginRegistry not found in service provider");
    }

    private async Task PublishThroughGAgent(GAgentBase<PluginAgentState, PluginStateLogEvent> gAgent, EventBase orleansEvent)
    {
        // This would require either:
        // 1. Making PublishAsync public/internal
        // 2. Adding a new public method to GAgentBase for plugin use
        // 3. Using reflection (not recommended)
        
        // For now, we'll use reflection as a demonstration
        var publishMethod = gAgent.GetType().GetMethod("PublishAsync", new[] { typeof(EventBase) });
        if (publishMethod != null)
        {
            var task = (Task)publishMethod.Invoke(gAgent, new object[] { orleansEvent })!;
            await task;
        }
    }

    private async Task<object> PublishWithResponseThroughGAgent(
        GAgentBase<PluginAgentState, PluginStateLogEvent> gAgent, 
        EventBase orleansEvent, 
        TimeSpan timeout)
    {
        // Similar reflection-based approach for publish with response
        var publishMethod = gAgent.GetType().GetMethods()
            .FirstOrDefault(m => m.Name == "PublishAsync" && m.GetParameters().Length == 2);
            
        if (publishMethod != null)
        {
            var task = (Task)publishMethod.Invoke(gAgent, new object[] { orleansEvent, timeout })!;
            await task;
            
            // Get result from Task<T>
            if (task.GetType().IsGenericType)
            {
                var resultProperty = task.GetType().GetProperty("Result");
                return resultProperty?.GetValue(task) ?? new object();
            }
        }
        
        return new object();
    }

    private IStreamProvider GetStreamProvider(GAgentBase<PluginAgentState, PluginStateLogEvent> gAgent)
    {
        // This method needs to be implemented to get stream provider for Orleans streams
        // For now, we'll remove the call to this method since it's causing issues
        try
        {
            // Try to get stream provider from service provider using grain factory
            // Since we don't have access to service provider here, we'll try a different approach
            throw new NotImplementedException("Stream provider access needs to be implemented through a different mechanism");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Unable to get stream provider", ex);
        }
    }
}

/// <summary>
/// Adapter for IAgentLogger to Microsoft.Extensions.Logging.ILogger
/// </summary>
public class AgentLoggerAdapter : IAgentLogger
{
    private readonly ILogger _logger;

    public AgentLoggerAdapter(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void LogDebug(string message)
    {
        _logger.LogDebug(message);
    }

    public void LogInformation(string message)
    {
        _logger.LogInformation(message);
    }

    public void LogWarning(string message)
    {
        _logger.LogWarning(message);
    }

    public void LogError(string message, Exception? exception = null)
    {
        if (exception != null)
            _logger.LogError(exception, message);
        else
            _logger.LogError(message);
    }
}

/// <summary>
/// Implementation of IAgentReference for calling other agents
/// </summary>
public class AgentReference : IAgentReference
{
    private readonly IGAgent _grain;
    private readonly IAgentPluginRegistry _pluginRegistry;
    private readonly ILogger _logger;
    private readonly IGrainFactory _grainFactory;

    public AgentReference(IGAgent grain, string agentId, IAgentPluginRegistry pluginRegistry, ILogger logger, IGrainFactory grainFactory)
    {
        _grain = grain ?? throw new ArgumentNullException(nameof(grain));
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        _pluginRegistry = pluginRegistry ?? throw new ArgumentNullException(nameof(pluginRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _grainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
    }

    public string AgentId { get; }

    public async Task<TResult> CallMethodAsync<TResult>(string methodName, params object?[] parameters)
    {
        try
        {
            // First try to call through plugin if it's registered
            var (success, result) = await TryCallPluginMethodAsync<TResult>(methodName, parameters);
            if (success)
            {
                return result;
            }

            // Fallback to Orleans grain call
            return await CallOrleansGrainMethodAsync<TResult>(methodName, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling method {MethodName} on agent {AgentId}", methodName, AgentId);
            throw new AgentMethodCallException(AgentId, methodName, $"Failed to call method {methodName}", ex);
        }
    }

    public async Task SendEventAsync(IAgentEvent agentEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert plugin event to Orleans event format
            var orleansEvent = new PluginEventWrapper
            {
                PluginEventType = agentEvent.EventType,
                PluginEventData = agentEvent.Data,
                Timestamp = agentEvent.Timestamp,
                CorrelationId = string.IsNullOrEmpty(agentEvent.CorrelationId) ? (Guid?)null : Guid.Parse(agentEvent.CorrelationId),
                SourceAgentId = agentEvent.SourceAgentId
            };

            // First try to send to plugin if it exists
            if (await TrySendToPluginAsync(agentEvent, cancellationToken))
            {
                return;
            }

            // Fallback to Orleans messaging
            await SendThroughOrleansAsync(orleansEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending event {EventType} to agent {AgentId}", agentEvent.EventType, AgentId);
            throw new AgentEventSendException(AgentId, agentEvent.EventType, $"Failed to send event {agentEvent.EventType}", ex);
        }
    }

    // Private helper methods
    private async Task<(bool Success, TResult Value)> TryCallPluginMethodAsync<TResult>(string methodName, object?[] parameters)
    {
        try
        {
            var plugin = _pluginRegistry.GetPlugin(AgentId);
            if (plugin == null)
            {
                return (false, default(TResult)!);
            }

            var result = await plugin.ExecuteMethodAsync(methodName, parameters);
            return (true, ConvertResult<TResult>(result));
        }
        catch (MethodNotFoundException)
        {
            // Method not found in plugin, will try Orleans grain
            return (false, default(TResult)!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calling plugin method {MethodName}, will try Orleans grain", methodName);
            return (false, default(TResult)!);
        }
    }

    private async Task<TResult> CallOrleansGrainMethodAsync<TResult>(string methodName, object?[] parameters)
    {
        // This would require the Orleans grain to implement a generic method calling interface
        // For example, the grain could implement an IMethodCallable interface
        
        if (_grain is IMethodCallable methodCallable)
        {
            var result = await methodCallable.CallMethodAsync(methodName, parameters);
            return ConvertResult<TResult>(result);
        }

        throw new MethodNotFoundException($"Method {methodName} not found on agent {AgentId} and grain does not implement IMethodCallable");
    }

    private async Task<bool> TrySendToPluginAsync(IAgentEvent agentEvent, CancellationToken cancellationToken)
    {
        try
        {
            var plugin = _pluginRegistry.GetPlugin(AgentId);
            if (plugin == null)
            {
                return false;
            }

            await plugin.HandleEventAsync(agentEvent, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error sending event to plugin for agent {AgentId}, will try Orleans messaging", AgentId);
            return false;
        }
    }

    private async Task SendThroughOrleansAsync(PluginEventWrapper orleansEvent, CancellationToken cancellationToken)
    {
        // This would require the grain to implement event handling
        if (_grain is IEventReceiver eventReceiver)
        {
            await eventReceiver.ReceiveEventAsync(orleansEvent, cancellationToken);
        }
        else
        {
            // Fallback: try to use Orleans streams or messaging
            await InvokeGenericEventHandler(orleansEvent, cancellationToken);
        }
    }

    private async Task InvokeGenericEventHandler(PluginEventWrapper orleansEvent, CancellationToken cancellationToken)
    {
        try
        {
            // Use reflection to call a generic event handler if it exists
            var grainType = _grain.GetType();
            var handleEventMethod = grainType.GetMethod("HandleEventAsync") 
                                 ?? grainType.GetMethod("ReceiveEventAsync")
                                 ?? grainType.GetMethod("OnEventAsync");

            if (handleEventMethod != null)
            {
                var parameters = handleEventMethod.GetParameters();
                object[] args;

                if (parameters.Length == 1)
                {
                    args = new object[] { orleansEvent };
                }
                else if (parameters.Length == 2 && parameters[1].ParameterType == typeof(CancellationToken))
                {
                    args = new object[] { orleansEvent, cancellationToken };
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported event handler signature on grain {grainType.Name}");
                }

                var result = handleEventMethod.Invoke(_grain, args);
                if (result is Task task)
                {
                    await task;
                }
            }
            else
            {
                throw new InvalidOperationException($"No suitable event handler found on grain {grainType.Name}");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to invoke event handler on grain {_grain.GetType().Name}", ex);
        }
    }

    private TResult ConvertResult<TResult>(object? result)
    {
        if (result == null)
        {
            if (default(TResult) == null)
            {
                return default(TResult)!;
            }
            throw new InvalidCastException($"Cannot convert null to non-nullable type {typeof(TResult)}");
        }

        if (result is TResult directResult)
        {
            return directResult;
        }

        // Try standard type conversion
        try
        {
            return (TResult)Convert.ChangeType(result, typeof(TResult));
        }
        catch (InvalidCastException)
        {
            // Try JSON serialization for complex types
            return TryJsonConversion<TResult>(result);
        }
    }

    private TResult TryJsonConversion<TResult>(object result)
    {
        try
        {
            var json = JsonSerializer.Serialize(result);
            var converted = JsonSerializer.Deserialize<TResult>(json);
            return converted!;
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"Cannot convert {result.GetType()} to {typeof(TResult)}", ex);
        }
    }

    // Equality and hash code based on AgentId for consistent identity
    public override bool Equals(object? obj)
    {
        if (obj is AgentReference other)
        {
            _logger.LogDebug("Comparing AgentReference: {AgentId1} == {AgentId2}", AgentId, other.AgentId);
            return string.Equals(AgentId, other.AgentId, StringComparison.Ordinal);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return AgentId?.GetHashCode() ?? 0;
    }

    public static bool operator ==(AgentReference? left, AgentReference? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(AgentReference? left, AgentReference? right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Wrapper for plugin events in Orleans event system
/// </summary>
public class PluginEventWrapper : EventBase
{
    public string PluginEventType { get; set; } = string.Empty;
    public object? PluginEventData { get; set; }
    public DateTime Timestamp { get; set; }
    public new Guid? CorrelationId { get; set; }
    public string? SourceAgentId { get; set; }
}

/// <summary>
/// Exception thrown when agent method calls fail
/// </summary>
public class AgentMethodCallException : Exception
{
    public string AgentId { get; }
    public string MethodName { get; }

    public AgentMethodCallException(string agentId, string methodName, string message) 
        : base(message)
    {
        AgentId = agentId;
        MethodName = methodName;
    }

    public AgentMethodCallException(string agentId, string methodName, string message, Exception innerException) 
        : base(message, innerException)
    {
        AgentId = agentId;
        MethodName = methodName;
    }
}

/// <summary>
/// Exception thrown when sending events to agents fails
/// </summary>
public class AgentEventSendException : Exception
{
    public string AgentId { get; }
    public string EventType { get; }

    public AgentEventSendException(string agentId, string eventType, string message) 
        : base(message)
    {
        AgentId = agentId;
        EventType = eventType;
    }

    public AgentEventSendException(string agentId, string eventType, string message, Exception innerException) 
        : base(message, innerException)
    {
        AgentId = agentId;
        EventType = eventType;
    }
}

/// <summary>
/// Exception thrown when methods are not found
/// </summary>
public class MethodNotFoundException : Exception
{
    public MethodNotFoundException(string message) : base(message) { }
    public MethodNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}