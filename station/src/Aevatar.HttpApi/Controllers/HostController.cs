using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Developer.Logger;
using Aevatar.Developer.Logger.Entities;
using Aevatar.Enum;
using Aevatar.Options;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Host")]
[Route("api/host")]
[Authorize]
public class HostController
{
    private readonly ILogService _logService;
    private readonly KubernetesOptions _kubernetesOptions;

    public HostController(
        ILogService logService, 
        IOptionsSnapshot<KubernetesOptions> kubernetesOptions
      )
    {
        _logService = logService;
        _kubernetesOptions = kubernetesOptions.Value;
    }
    
    [HttpGet("log")]
    public async Task<List<HostLogIndex>> GetLatestRealTimeLogs(string appId,HostTypeEnum hostType,int offset)
    {
        var indexName = _logService.GetHostLogIndexAliasName(_kubernetesOptions.AppNameSpace, appId + "-"+hostType.ToString().ToLower(), "1");
        return await _logService.GetHostLatestLogAsync(indexName, offset);
    }
    
    /// <summary>
    /// Get workflow logs using structured fields for precise filtering
    /// Requires LogCategory=WORKFLOW and WorkflowId. Supports optional GrainId, log level, and message pattern filtering.
    /// </summary>
    /// <param name="appId">Application ID</param>
    /// <param name="hostType">Host type (e.g., api, worker)</param>
    /// <param name="workflowId">Workflow ID to search for (required)</param>
    /// <param name="grainId">Optional GrainId for precise filtering</param>
    /// <param name="level">Optional log level (Information, Warning, Error, etc.)</param>
    /// <param name="messagePattern">Optional message pattern for fuzzy matching in @m field</param>
    /// <param name="pageSize">Number of logs to return (default: 100)</param>
    /// <returns>List of filtered workflow logs</returns>
    [HttpGet("workflow-log")]
    public async Task<List<HostLogIndex>> GetWorkflowLogs(
        string appId, 
        HostTypeEnum hostType, 
        string workflowId, 
        string? grainId = null, 
        string? level = null,
        string? messagePattern = null,
        int pageSize = 100)
    {
        var indexName = _logService.GetHostLogIndexAliasName(_kubernetesOptions.AppNameSpace, appId + "-"+hostType.ToString().ToLower(), "1");
        return await _logService.GetWorkflowLogsAsync(indexName, workflowId, grainId, level, messagePattern, pageSize);
    }
}