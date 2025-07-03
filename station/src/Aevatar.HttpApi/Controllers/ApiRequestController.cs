using System;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.ApiRequests;
using Aevatar.Organizations;
using Aevatar.Permissions;
using Aevatar.Projects;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("ApiRequest")]
[Route("api/api-requests")]
[Authorize]
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
    public async Task<ApiRequestDashboardDto> GetAsync(GetApiRequestDto input)
    {
        if (input.ProjectId.HasValue)
        {
            await _permissionChecker.AuthenticateAsync(input.ProjectId.Value, AevatarPermissions.ApiRequests.Default);
        }
        else
        {
            await _permissionChecker.AuthenticateAsync(input.OrganizationId.Value, AevatarPermissions.ApiRequests.Default);
        }

        var list = await _apiRequestService.GetListAsync(input);
        return new ApiRequestDashboardDto
        {
            TotalRequests = list.Items.Sum(o => o.Count),
            Requests = list.Items.ToList()
        };
    }
}