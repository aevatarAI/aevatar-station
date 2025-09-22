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
using System.Linq;
using Volo.Abp.Application.Dtos;
using Volo.Abp;
using Aevatar.Application.Contracts.Query;
using Microsoft.Extensions.Configuration;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Host")]
[Route("api/host")]
[Authorize]
public class HostController : AevatarController
{
    private readonly ILogService _logService;
    private readonly KubernetesOptions _kubernetesOptions;
    private readonly IConfiguration _configuration;

    public HostController(
        ILogService logService, 
        IOptionsSnapshot<KubernetesOptions> kubernetesOptions,
        IConfiguration configuration
      )
    {
        _logService = logService;
        _kubernetesOptions = kubernetesOptions.Value;
        _configuration = configuration;
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
    /// <param name="workflowId">Workflow ID to search for (required)</param>
    /// <param name="roundId">Optional RoundId for filtering</param>
    /// <param name="grainId">Optional GrainId for precise filtering</param>
    /// <param name="level">Optional log level (Information, Warning, Error, etc.)</param>
    /// <param name="messagePattern">Optional message pattern for fuzzy matching in @m field</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Page size (default: 100)</param>
    /// <returns>Paged workflow logs (no total for performance)</returns>
    [HttpGet("workflow-log")]
    public async Task<List<HostLogIndex>> GetWorkflowLogs([FromQuery] WorkflowLogQueryDto input)
    {
        var page = input.Page < 1 ? 1 : input.Page;
        var pageSize = input.PageSize <= 0 ? 100 : input.PageSize;
        var from = (page - 1) * pageSize;

        var hostId = _configuration.GetValue<string>("Host:HostId");
        var indexName = _logService.GetHostLogIndexAliasName(
            _kubernetesOptions.AppNameSpace, 
            hostId + "-" + HostTypeEnum.Silo.ToString().ToLower(), 
            "1");

        // Query ES paged directly (no total for performance)
        var logs = await _logService.GetWorkflowLogsAsync(indexName, input.WorkflowId, input.RoundId, input.GrainId, input.Level, input.MessagePattern, from, pageSize);
        return logs ?? new List<HostLogIndex>();
    }
}