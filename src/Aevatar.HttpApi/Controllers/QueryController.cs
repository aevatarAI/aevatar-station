using System;
using System.Threading.Tasks;
using Aevatar.Controllers;
using Aevatar.CQRS;
using Aevatar.Permissions;
using Aevatar.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[Route("api/query")]
public class QueryController : AevatarController
{
    private readonly ICqrsService _cqrsService;

    public QueryController(ICqrsService cqrsService)
    {
        _cqrsService = cqrsService;
    }

    [HttpGet("logs")]
    [Authorize(Policy = AevatarPermissions.CqrsManagement.Logs)] 
    public async Task<AgentEventLogsDto> GetEventLogs([FromQuery] Guid? id, string agentType, int pageIndex = 0, int pageSize = 20)
    {
        var resp = await _cqrsService.QueryGEventAsync(id, agentType, pageIndex, pageSize);
        return resp;
    }

    [HttpGet("state")]
    [Authorize(Policy = AevatarPermissions.CqrsManagement.States)] 
    public async Task<AgentStateDto> GetStates([FromQuery] string stateName, Guid id)
    {
        var resp = await _cqrsService.QueryStateAsync(stateName, id);
        return resp;
    }
}