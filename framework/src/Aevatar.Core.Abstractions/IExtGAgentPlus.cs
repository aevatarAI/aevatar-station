namespace Aevatar.Core.Abstractions;

/// <summary>
/// Represents an extended GAgentPlus which intends to boost the performance of the current GAgentPlus. 
/// This is the Plus version of IExtGAgent with enhanced functionality.
/// </summary>
public interface IExtGAgentPlus : IGAgentPlus
{

    /// <summary>
    /// Register many GAgents as the next level of the current GAgent.
    /// To compare with RegisterAsync, this method is more efficient via batch processing.
    /// </summary>
    /// <param name="gAgents"></param>
    /// <returns></returns>
    Task RegisterManyAsync(List<IGAgentPlus> gAgents);

}
