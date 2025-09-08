using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator;
using GroupChat.GAgent.Feature.Common;

public interface IWorkflowCoordinatorGAgent : IStateGAgent<WorkflowCoordinatorState>
{
    /// <summary>
    /// Execute the current node with the provided parameters
    /// Used for both initial execution and re-execution during debugging
    /// </summary>
    Task ExecuteCurrentNodeAsync(long term, List<ChatMessage> coordinatorMessages);
    
    /// <summary>
    /// Continue execution to downstream nodes after current node completion
    /// Used when debugging confirms current results are acceptable
    /// </summary>
    Task ContinueToDownstreamAsync(long term);
}