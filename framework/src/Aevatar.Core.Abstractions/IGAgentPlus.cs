using Aevatar.Core.Abstractions.Communication;

namespace Aevatar.Core.Abstractions;

public interface IGAgentPlus : ICoreGAgent, ILayeredCommunication, ILayeredRelationshipManager
{
    /// <summary>
    /// Prepare the agent with available resource context.
    /// This allows agents to discover and utilize external resources without explicit configuration.
    /// </summary>
    /// <param name="context">The resource context containing available resources and metadata</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task PrepareResourceContextAsync(ResourceContext context);
}

public interface IStateGAgentPlus<TState> : IGAgentPlus, ICoreStateGAgent<TState>
{

}
