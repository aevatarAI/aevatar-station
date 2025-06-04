namespace Aevatar.Core.Abstractions.Plugin;

/// <summary>
/// Core interface for user-implemented agent plugins.
/// This interface has zero dependencies on Orleans or GAgentBase.
/// </summary>
public interface IAgentPlugin
{
    /// <summary>
    /// Plugin metadata
    /// </summary>
    AgentPluginMetadata Metadata { get; }
    
    /// <summary>
    /// Initialize the plugin with the given context
    /// </summary>
    Task InitializeAsync(IAgentContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute a method call on the plugin
    /// </summary>
    Task<object?> ExecuteMethodAsync(string methodName, object?[] parameters, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Handle an incoming event
    /// </summary>
    Task HandleEventAsync(IAgentEvent agentEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the current state of the agent
    /// </summary>
    Task<object?> GetStateAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Set the state of the agent
    /// </summary>
    Task SetStateAsync(object? state, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cleanup resources
    /// </summary>
    Task DisposeAsync();
}

/// <summary>
/// Plugin metadata
/// </summary>
public record AgentPluginMetadata(
    string Name,
    string Version,
    string Description,
    Dictionary<string, object>? Properties = null);

/// <summary>
/// Agent execution context provided by the station
/// </summary>
public interface IAgentContext
{
    /// <summary>
    /// Agent's unique identifier
    /// </summary>
    string AgentId { get; }
    
    /// <summary>
    /// Publish an event from this agent
    /// </summary>
    Task PublishEventAsync(IAgentEvent agentEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publish an event and wait for response
    /// </summary>
    Task<TResponse> PublishEventWithResponseAsync<TResponse>(IAgentEvent agentEvent, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        where TResponse : class;
    
    /// <summary>
    /// Get reference to another agent
    /// </summary>
    Task<IAgentReference> GetAgentAsync(string agentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Register child agents
    /// </summary>
    Task RegisterAgentsAsync(IEnumerable<string> agentIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Subscribe to other agents
    /// </summary>
    Task SubscribeToAgentsAsync(IEnumerable<string> agentIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Logger for the agent
    /// </summary>
    IAgentLogger Logger { get; }
    
    /// <summary>
    /// Configuration values
    /// </summary>
    IReadOnlyDictionary<string, object> Configuration { get; }
}

/// <summary>
/// Version-agnostic event interface
/// </summary>
public interface IAgentEvent
{
    /// <summary>
    /// Event type identifier
    /// </summary>
    string EventType { get; }
    
    /// <summary>
    /// Event timestamp
    /// </summary>
    DateTime Timestamp { get; }
    
    /// <summary>
    /// Event data payload
    /// </summary>
    object? Data { get; }
    
    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    string? CorrelationId { get; }
    
    /// <summary>
    /// Source agent ID
    /// </summary>
    string? SourceAgentId { get; }
}

/// <summary>
/// Reference to another agent for communication
/// </summary>
public interface IAgentReference
{
    /// <summary>
    /// Agent ID
    /// </summary>
    string AgentId { get; }
    
    /// <summary>
    /// Call a method on the referenced agent
    /// </summary>
    Task<TResult> CallMethodAsync<TResult>(string methodName, params object?[] parameters);
    
    /// <summary>
    /// Send an event to the referenced agent
    /// </summary>
    Task SendEventAsync(IAgentEvent agentEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Simple logger interface for agents
/// </summary>
public interface IAgentLogger
{
    void LogDebug(string message);
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? exception = null);
}