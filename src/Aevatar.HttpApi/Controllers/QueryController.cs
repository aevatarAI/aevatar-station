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
    public async Task<AgentEventLogsDto> GetEventLogs([FromQuery] Guid? guid, string agentType, int pageIndex, int pageSize)
    {
        
        var resp = await _cqrsService.QueryGEventAsync(guid, agentType, pageIndex, pageSize);
        return resp;
    }
    
    [HttpGet("state")]
    public async Task<AgentStateDto> GetStates([FromQuery] string stateName, Guid guid)
    {
        
        var resp = await _cqrsService.QueryStateAsync(stateName, guid);
        return resp;
    }
    
}