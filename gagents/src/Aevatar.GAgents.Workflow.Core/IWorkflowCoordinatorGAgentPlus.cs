using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Workflow.Core.States;
using Aevatar.GAgents.Core;

namespace Aevatar.GAgents.Workflow.Core;

public interface IWorkflowCoordinatorGAgentPlus : IStateGAgentPlus<WorkflowCoordinatorStatePlus>, IBusinessAgentBase
{
    /// <summary>
    /// ✅ TASK 16: Get start node agent IDs for service-direct workflow execution
    /// </summary>
    Task<List<string>> GetStartNodeAgentIdsAsync();
    
    /// <summary>
    /// Get execution record ID by execution name
    /// </summary>
    /// <param name="executionName">Name of the execution</param>
    /// <returns>Execution record ID or Guid.Empty if not found</returns>
    Task<Guid> GetExecutionRecordIdAsync(string executionName);
    
    /// <summary>
    /// Get all execution records (name -> record ID mapping)
    /// </summary>
    /// <returns>Dictionary of execution names to record IDs</returns>
    Task<Dictionary<string, Guid>> GetAllExecutionRecordsAsync();
    
    /// <summary>
    /// Check if an execution with the given name exists
    /// </summary>
    /// <param name="executionName">Name of the execution</param>
    /// <returns>True if execution exists, false otherwise</returns>
    Task<bool> HasExecutionAsync(string executionName);
}