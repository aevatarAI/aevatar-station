using System.Collections.Concurrent;
using System.Reflection;

namespace Aevatar.Core.Abstractions.Plugin;

/// <summary>
/// Base class for agent plugins that provides common functionality
/// </summary>
public abstract class AgentPluginBase : IAgentPlugin
{
    private readonly ConcurrentDictionary<string, MethodInfo> _methodCache = new();
    private readonly ConcurrentDictionary<string, MethodInfo> _eventHandlerCache = new();
    private object? _state;
    
    protected IAgentContext? Context { get; private set; }
    protected IAgentLogger? Logger => Context?.Logger;

    public virtual AgentPluginMetadata Metadata { get; protected set; } = new("Unknown", "1.0.0", "Agent Plugin");

    public virtual async Task InitializeAsync(IAgentContext context, CancellationToken cancellationToken = default)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        
        // Cache method information for performance
        CacheMethodInfo();
        
        // Initialize plugin metadata from attributes
        InitializeMetadata();
        
        // Call user initialization
        await OnInitializeAsync(cancellationToken);
    }

    public virtual async Task<object?> ExecuteMethodAsync(string methodName, object?[] parameters, CancellationToken cancellationToken = default)
    {
        if (!_methodCache.TryGetValue(methodName, out var methodInfo))
        {
            throw new InvalidOperationException($"Method '{methodName}' not found or not marked with [AgentMethod]");
        }

        try
        {
            // Handle async methods
            var result = methodInfo.Invoke(this, parameters);
            
            if (result is Task task)
            {
                await task;
                
                // Get result from Task<T>
                if (task.GetType().IsGenericType)
                {
                    var property = task.GetType().GetProperty("Result");
                    return property?.GetValue(task);
                }
                
                return null;
            }
            
            return result;
        }
        catch (Exception ex)
        {
            Logger?.LogError($"Error executing method '{methodName}': {ex.Message}", ex);
            throw;
        }
    }

    public virtual async Task HandleEventAsync(IAgentEvent agentEvent, CancellationToken cancellationToken = default)
    {
        var eventType = agentEvent.EventType;
        
        // Try to find specific event handler
        if (_eventHandlerCache.TryGetValue(eventType, out var specificHandler))
        {
            await InvokeEventHandler(specificHandler, agentEvent);
            return;
        }
        
        // Try to find generic event handler
        if (_eventHandlerCache.TryGetValue("*", out var genericHandler))
        {
            await InvokeEventHandler(genericHandler, agentEvent);
            return;
        }
        
        // Call virtual method for unhandled events
        await OnUnhandledEventAsync(agentEvent, cancellationToken);
    }

    public virtual Task<object?> GetStateAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_state);
    }

    public virtual Task SetStateAsync(object? state, CancellationToken cancellationToken = default)
    {
        _state = state;
        return Task.CompletedTask;
    }

    public virtual Task DisposeAsync()
    {
        return OnDisposeAsync();
    }

    // Virtual methods for subclasses to override
    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnUnhandledEventAsync(IAgentEvent agentEvent, CancellationToken cancellationToken = default)
    {
        Logger?.LogWarning($"Unhandled event: {agentEvent.EventType}");
        return Task.CompletedTask;
    }

    protected virtual Task OnDisposeAsync()
    {
        return Task.CompletedTask;
    }

    // Helper methods
    protected async Task PublishEventAsync(string eventType, object? data, string? correlationId = null)
    {
        if (Context == null) throw new InvalidOperationException("Plugin not initialized");
        
        var agentEvent = new AgentEvent
        {
            EventType = eventType,
            Data = data,
            Timestamp = DateTime.UtcNow,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
            SourceAgentId = Context.AgentId
        };
        
        await Context.PublishEventAsync(agentEvent);
    }

    protected async Task<TResponse> PublishEventWithResponseAsync<TResponse>(string eventType, object? data, TimeSpan? timeout = null, string? correlationId = null)
        where TResponse : class
    {
        if (Context == null) throw new InvalidOperationException("Plugin not initialized");
        
        var agentEvent = new AgentEvent
        {
            EventType = eventType,
            Data = data,
            Timestamp = DateTime.UtcNow,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
            SourceAgentId = Context.AgentId
        };
        
        return await Context.PublishEventWithResponseAsync<TResponse>(agentEvent, timeout);
    }

    private void CacheMethodInfo()
    {
        var type = GetType();
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var method in methods)
        {
            // Cache agent methods
            var agentMethodAttr = method.GetCustomAttribute<AgentMethodAttribute>();
            if (agentMethodAttr != null)
            {
                var methodName = agentMethodAttr.MethodName ?? method.Name;
                _methodCache[methodName] = method;
                
                // Also cache under the actual method name if it's different from the AgentMethod name
                // This allows interface method calls to be properly routed
                if (!string.IsNullOrEmpty(agentMethodAttr.MethodName) && agentMethodAttr.MethodName != method.Name)
                {
                    _methodCache[method.Name] = method;
                }
            }
            
            // Cache event handlers
            var eventHandlerAttr = method.GetCustomAttribute<AgentEventHandlerAttribute>();
            if (eventHandlerAttr != null)
            {
                var eventType = eventHandlerAttr.EventType ?? "*";
                _eventHandlerCache[eventType] = method;
            }
        }
    }

    private void InitializeMetadata()
    {
        var type = GetType();
        var pluginAttr = type.GetCustomAttribute<AgentPluginAttribute>();
        
        if (pluginAttr != null)
        {
            Metadata = new AgentPluginMetadata(
                pluginAttr.Name,
                pluginAttr.Version,
                pluginAttr.Description ?? "Agent Plugin");
        }
    }

    private async Task InvokeEventHandler(MethodInfo handler, IAgentEvent agentEvent)
    {
        try
        {
            var parameters = handler.GetParameters();
            object?[] args;
            
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(IAgentEvent))
            {
                args = new object[] { agentEvent };
            }
            else if (parameters.Length == 1)
            {
                // Try to convert event data to expected type
                args = new object?[] { agentEvent.Data };
            }
            else
            {
                args = Array.Empty<object>();
            }
            
            var result = handler.Invoke(this, args);
            
            if (result is Task task)
            {
                await task;
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError($"Error in event handler for '{agentEvent.EventType}': {ex.Message}", ex);
            throw;
        }
    }
}

/// <summary>
/// Default implementation of IAgentEvent
/// </summary>
public class AgentEvent : IAgentEvent
{
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public object? Data { get; set; }
    public string? CorrelationId { get; set; }
    public string? SourceAgentId { get; set; }
}