using Orleans.Concurrency;

namespace Aevatar.Core.Abstractions;

public interface IGAgent : ICoreGAgent
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
    
}

public interface IStateGAgent<TState> : IGAgent, ICoreStateGAgent<TState>
{

}