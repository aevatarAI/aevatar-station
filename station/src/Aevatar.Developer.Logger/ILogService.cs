using Aevatar.Developer.Logger.Entities;

namespace Aevatar.Developer.Logger;

public interface ILogService
{
    Task<List<HostLogIndex>> GetHostLatestLogAsync(string indexName, int pageSize);

    string GetHostLogIndexAliasName(string nameSpace, string appId, string version);
    
    /// <summary>
    /// Get workflow logs using structured fields for precise filtering
    /// </summary>
    /// <param name="indexName">ES index name</param>
    /// <param name="workflowId">Workflow ID to search for (required)</param>
    /// <param name="grainId">Optional GrainId for precise filtering</param>
    /// <param name="level">Optional log level for filtering (Information, Warning, Error, etc.)</param>
    /// <param name="messagePattern">Optional message pattern for fuzzy matching in @m field</param>
    /// <param name="pageSize">Number of logs to return</param>
    /// <returns>List of workflow logs</returns>
    Task<List<HostLogIndex>> GetWorkflowLogsAsync(string indexName, string workflowId, string? grainId = null, string? level = null, string? messagePattern = null, int pageSize = 100);
}