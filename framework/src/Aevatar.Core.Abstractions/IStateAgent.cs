using Orleans.Concurrency;

namespace Aevatar.Core.Abstractions;

public interface IGAgent : IGrainWithGuidKey
{
    /// <summary>
    /// Used for activating the agent manually.
    /// </summary>
    /// <returns></returns>
    Task ActivateAsync();

    /// <summary>
    /// Get GAgent description.
    /// </summary>
    /// <returns></returns>
    [ReadOnly]
    Task<string> GetDescriptionAsync();

    /// <summary>
    /// Register a GAgent as the next level of the current GAgent.
    /// </summary>
    /// <param name="gAgent"></param>
    /// <returns></returns>
    Task RegisterAsync(IGAgent gAgent);

    /// <summary>
    /// Register current GAgent as the next level of the provided GAgent.
    /// </summary>
    /// <param name="gAgent"></param>
    /// <returns></returns>
    Task SubscribeToAsync(IGAgent gAgent);

    /// <summary>
    /// Unregister current GAgent from the next level of the provided GAgent.
    /// </summary>
    /// <param name="gAgent"></param>
    /// <returns></returns>
    Task UnsubscribeFromAsync(IGAgent gAgent);

    /// <summary>
    /// Undo the registration.
    /// </summary>
    /// <param name="gAgent"></param>
    /// <returns></returns>
    Task UnregisterAsync(IGAgent gAgent);

    /// <summary>
    /// Get all subscribed events of current GAgent.
    /// </summary>
    /// <param name="includeBaseHandlers"></param>
    /// <returns></returns>
    [ReadOnly]
    Task<List<Type>?> GetAllSubscribedEventsAsync(bool includeBaseHandlers = false);

    /// <summary>
    /// Get subscriber list of current GAgent.
    /// </summary>
    /// <returns></returns>
    [ReadOnly]
    Task<List<GrainId>> GetChildrenAsync();

    /// <summary>
    /// Get the subscription of current GAgent.
    /// </summary>
    /// <returns></returns>
    [ReadOnly]
    Task<GrainId> GetParentAsync();

    /// <summary>
    /// Get the type of GAgent initialization event.
    /// </summary>
    /// <returns></returns>
    [ReadOnly]
    Task<Type?> GetConfigurationTypeAsync();

    /// <summary>
    /// Config the GAgent.
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    Task ConfigAsync(ConfigurationBase configuration);
    
    /// <summary>
    /// Prepare the agent with available resource context.
    /// This allows agents to discover and utilize external resources without explicit configuration.
    /// </summary>
    /// <param name="context">The resource context containing available resources and metadata</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task PrepareResourceContextAsync(ResourceContext context);
}

public interface IStateGAgent<TState> : IGAgent
{
    [ReadOnly]
    Task<TState> GetStateAsync();
}