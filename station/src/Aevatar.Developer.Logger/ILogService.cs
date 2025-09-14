using Aevatar.Developer.Logger.Entities;

namespace Aevatar.Developer.Logger;

public interface ILogService
{
    Task<List<HostLogIndex>> GetHostLatestLogAsync(string indexName, int pageSize);

    string GetHostLogIndexAliasName(string nameSpace, string appId, string version);
    
    /// <summary>
    /// Get workflow logs by WorkflowId
    /// </summary>
    /// <param name="indexName">ES index name</param>
    /// <param name="workflowId">Workflow ID to search for</param>
    /// <param name="pageSize">Number of logs to return</param>
    /// <returns>List of workflow logs</returns>
    Task<List<HostLogIndex>> GetWorkflowLogsAsync(string indexName, string workflowId, int pageSize = 100);
    
    /// <summary>
    /// Get workflow logs by WorkflowId with step filtering
    /// </summary>
    /// <param name="indexName">ES index name</param>
    /// <param name="workflowId">Workflow ID to search for</param>
    /// <param name="workflowStep">Optional workflow step to filter</param>
    /// <param name="workflowAction">Optional workflow action to filter (ENTER, INPUT, OUTPUT, EXIT, EXCEPTION)</param>
    /// <param name="pageSize">Number of logs to return</param>
    /// <returns>List of workflow logs</returns>
    Task<List<HostLogIndex>> GetWorkflowLogsWithFilterAsync(string indexName, string workflowId, string? workflowStep = null, string? workflowAction = null, int pageSize = 100);
}