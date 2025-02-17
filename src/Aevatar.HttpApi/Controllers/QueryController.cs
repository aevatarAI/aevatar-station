using System;
using System.Threading.Tasks;
using Aevatar.CQRS;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Query")]
[Route("api/query")]
public class QueryController : AevatarController
{
    private readonly ICqrsService _cqrsService;

    public QueryController(
        ICqrsService cqrsService)
    {
        _cqrsService = cqrsService;
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
    
}