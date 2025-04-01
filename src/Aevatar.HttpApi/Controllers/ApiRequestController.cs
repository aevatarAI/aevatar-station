using System;
using System.Threading.Tasks;
using Aevatar.ApiRequests;
using Aevatar.Organizations;
using Aevatar.Permissions;
using Aevatar.Projects;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("ApiRequest")]
[Route("api/api-requests")]
public class ApiRequestController : AevatarController
{
    private readonly IApiRequestService _apiRequestService;
    private readonly IOrganizationPermissionChecker _permissionChecker;

    public ApiRequestController(
        IOrganizationPermissionChecker permissionChecker, IApiRequestService apiRequestService)
    {
        _permissionChecker = permissionChecker;
        _apiRequestService = apiRequestService;
    }

    [HttpGet]
    public async Task<ListResultDto<ApiRequestDto>> GetListAsync(GetApiRequestDto input)
    {
        return await _apiRequestService.GetListAsync(input);
    }
}