using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.CQRS;
using Aevatar.Query;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Query")]
[Route("api/query")]
public class QueryController : AevatarController
{
    private readonly ICqrsService _cqrsService;
    private readonly IIndexingService _indexingService;

    public QueryController(
        ICqrsService cqrsService, 
        IIndexingService indexingService)
    {
        _cqrsService = cqrsService;
        _indexingService = indexingService;
    }
    
    [HttpGet("logs")]
    public async Task<AgentEventLogsDto> GetEventLogs([FromQuery] Guid? id, string agentType, int pageIndex = 0, int pageSize = 20)
    {
        
        var resp = await _cqrsService.QueryGEventAsync(id, agentType, pageIndex, pageSize);
        return resp;
    }
    
    [HttpGet("state")]
    public async Task<AgentStateDto> GetStates([FromQuery] string stateName, Guid id)
    {
        
        var resp = await _cqrsService.QueryStateAsync(stateName, id);
        return resp;
    }
    
    [HttpGet("es")]
    public async Task<PagedResultDto<Dictionary<string, object>>> GetLogs(
        [FromQuery] LuceneQueryDto request)
    {
        var resp = await _indexingService.QueryWithLuceneAsync(request);
        return resp;
    }
    
}