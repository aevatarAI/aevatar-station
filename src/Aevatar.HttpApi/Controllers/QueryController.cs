using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Controllers;
using Aevatar.CQRS;
using Aevatar.Permissions;
using Aevatar.Query;
using Aevatar.Service;
using Aevatar.Validator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;


[Route("api/query")]
public class QueryController : AevatarController
{
    private readonly ICqrsService _cqrsService;
    private readonly IIndexingService _indexingService;

    public QueryController(ICqrsService cqrsService,
        IIndexingService indexingService)
    {
        _cqrsService = cqrsService;
        _indexingService = indexingService;
    }

    [HttpGet("state")]
    [Authorize(Policy = AevatarPermissions.CqrsManagement.States)]
    public async Task<AgentStateDto> GetStates([FromQuery] string stateName, Guid id)
    {
        var resp = await _cqrsService.QueryStateAsync(stateName, id);
        return resp;
    }

    [HttpGet("es")]
    public async Task<PagedResultDto<Dictionary<string, object>>> QueryEs(
        [FromQuery] LuceneQueryDto request)
    {
        var validator = new LuceneQueryValidator();
        var result = validator.Validate(request);
        if (!result.IsValid)
        {
            throw new UserFriendlyException(result.Errors[0].ErrorMessage);
        }

        var resp = await _indexingService.QueryWithLuceneAsync(request);
        return resp;
    }

    [HttpGet("user-id")]
    [Authorize]
    public Task<Guid> GetUserId()
    {
        return Task.FromResult((Guid)CurrentUser.Id!);
    }

}