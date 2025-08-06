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

        request.QueryString = GetQueryWithPermissionFilter(request);
        var resp = await _indexingService.QueryWithLuceneAsync(request);
        return resp;
    }

    [HttpGet("es/count")]
    public async Task<CountResultDto> CountEs([FromQuery] LuceneQueryDto request)
    {
        var validator = new LuceneQueryValidator();
        var result = validator.Validate(request);
        if (!result.IsValid)
        {
            throw new UserFriendlyException(result.Errors[0].ErrorMessage);
        }

        request.QueryString = GetQueryWithPermissionFilter(request);
        var count = await _indexingService.CountWithLuceneAsync(request);
        return new CountResultDto { Count = count };
    }
    
    
    [HttpGet("es/count")]
    public async Task<CountResultDto> CountEs([FromQuery] LuceneQueryDto request)
    {
        var validator = new LuceneQueryValidator();
        var result = validator.Validate(request);
        if (!result.IsValid)
        {
            throw new UserFriendlyException(result.Errors[0].ErrorMessage);
        }

		request.QueryString = GetQueryWithPermissionFilter(request);
        var count = await _indexingService.CountWithLuceneAsync(request);
        return new CountResultDto { Count = count };
    }

    [HttpGet("user-id")]
    [Authorize]
    public Task<Guid> GetUserId()
    {
        return Task.FromResult((Guid)CurrentUser.Id!);
    }
    
    private string GetQueryWithPermissionFilter(LuceneQueryDto queryDto)
    {
        var userId = CurrentUser.Id.HasValue ? CurrentUser.Id.ToString() : "null";
        var permissionFilter = $"((isPublic:true) OR (authorizedUserIds.keyword:{userId}) OR (NOT _exists_:isPublic AND NOT _exists_:authorizedUserIds))";
        if (queryDto.QueryString.IsNullOrWhiteSpace())
        {
            return permissionFilter;
        }
        else
        {
            return $"({queryDto.QueryString}) AND {permissionFilter}";
        }
    }
}