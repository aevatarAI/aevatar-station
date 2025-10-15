using Orleans.Concurrency;

namespace Aevatar.Core.Abstractions;

public interface IGAgent : IGAgentBase
{
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
    /// Prepare the agent with available resource context.
    /// This allows agents to discover and utilize external resources without explicit configuration.
    /// </summary>
    /// <param name="context">The resource context containing available resources and metadata</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task PrepareResourceContextAsync(ResourceContext context);

    /// <summary>
    /// Get the current state as JSON string for snapshot purposes.
    /// Returns null if the GAgent does not implement IStateGAgent&lt;TState&gt;.
    /// </summary>
    /// <returns>JSON representation of the current state, or null if not stateful</returns>
    [ReadOnly]
    Task<string?> GetStateSnapshotAsync();
}

public interface IStateGAgent<TState> : IGAgent
{
    [ReadOnly]
    Task<TState> GetStateAsync();
}