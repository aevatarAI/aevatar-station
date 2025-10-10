using Orleans.Concurrency;

namespace Aevatar.Core.Abstractions;

public interface IGAgentPlus : ICoreGAgent
{

    /// <summary>
    /// Register a GAgent as the next level of the current GAgent.
    /// </summary>
    /// <param name="gAgent"></param>
    /// <returns></returns>
    Task RegisterAsync(IGAgentPlus gAgent);
    
    /// <summary>
    /// Register current GAgent as the next level of the provided GAgent using broadcast communication.
    /// Supports multiple parents and bidirectional streams.
    /// </summary>
    /// <param name="gAgent"></param>
    /// <returns></returns>
    Task SubscribeToParentAsync(IGAgentPlus gAgent);
    
    /// <summary>
    /// Unregister current GAgent from the next level of the provided GAgent using broadcast communication.
    /// Supports multiple parents and bidirectional streams.
    /// </summary>
    /// <param name="gAgent"></param>
    /// <returns></returns>
    Task UnsubscribeFromParentAsync(IGAgentPlus gAgent);
    
    /// <summary>
    /// Subscribe to child's upward streams for bidirectional communication.
    /// </summary>
    /// <param name="childAgent">The child agent to subscribe to</param>
    /// <returns></returns>
    Task SubscribeToChildAsync(IGAgentPlus childAgent);

    /// <summary>
    /// Unsubscribe from child's upward streams.
    /// </summary>
    /// <param name="childAgent">The child agent to unsubscribe from</param>
    /// <returns></returns>
    Task UnsubscribeFromChildAsync(IGAgentPlus childAgent);
    
    /// <summary>
    /// Subscribe to many children's upward streams using batch pattern.
    /// </summary>
    /// <param name="childAgents">The child agents to subscribe to</param>
    /// <returns></returns>
    Task SubscribeToManyChildAsync(List<IGAgentPlus> childAgents);
    
    /// <summary>
    /// Unregister the current GAgent from one of its parents.
    /// </summary>
    /// <param name="parentAgent">The parent agent to unregister from</param>
    /// <returns></returns>
    Task UnregisterParentAsync(IGAgentPlus parentAgent);
    
    /// <summary>
    /// Remove a specific parent from the current GAgent's parents list.
    /// This method is called by parent agents during unregistration.
    /// </summary>
    /// <param name="parentGrainId">The grain ID of the parent to remove</param>
    /// <returns></returns>
    Task RemoveSpecificParentAsync(GrainId parentGrainId);

    /// <summary>
    /// Remove a specific child from the current GAgent's children list.
    /// This method is called by child agents during unregistration.
    /// </summary>
    /// <param name="childGrainId">The grain ID of the child to remove</param>
    /// <returns></returns>
    Task RemoveSpecificChildAsync(GrainId childGrainId);

    /// <summary>
    /// Undo the registration.
    /// </summary>
    /// <param name="gAgent"></param>
    /// <returns></returns>
    Task UnregisterAsync(IGAgentPlus gAgent);


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
    /// Get all parents of current GAgent (multiple parent support).
    /// </summary>
    /// <returns></returns>
    [ReadOnly]
    Task<List<GrainId>> GetParentsAsync();

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
    
    /// <summary>
    /// Publishes an event directly based on its direction property using stream-based broadcasting
    /// </summary>
    /// <typeparam name="T">The event type</typeparam>
    /// <param name="event">The event to publish</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task PublishEventByDirectionAsync<T>(T @event) where T : EventBase;
}

public interface IStateGAgentPlus<TState> : IGAgentPlus, ICoreStateGAgent<TState>
{

}
