using System;
using System.Threading.Tasks;
using Aevatar.Organizations;
using Aevatar.Permissions;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Developer")]
[Route("api/developers")]
[Authorize]
public class DeveloperController : AevatarController
{
    private readonly IDeveloperService _developerService;
    private readonly IOrganizationPermissionChecker _permissionChecker;

    public DeveloperController(IDeveloperService developerService, IOrganizationPermissionChecker permissionChecker)
    {
        _developerService = developerService;
        _permissionChecker = permissionChecker;
    }

    [HttpPut("service")]
    public async Task DeveloperServiceStartAsync([FromBody] DeveloperServiceOperationDto request)
    {
        await _permissionChecker.AuthenticateAsync(request.ProjectId, AevatarPermissions.Members.Manage);
        await _developerService.CreateAsync(request.ClientId, request.ProjectId);
    }

    [HttpPost("service")]
    public async Task DeveloperServiceRestartAsync([FromBody] DeveloperServiceOperationDto request)
    {
        await _permissionChecker.AuthenticateAsync(request.ProjectId, AevatarPermissions.Members.Manage);
        await _developerService.RestartAsync(request.ClientId, request.ProjectId);
    }

    [HttpDelete("service")]
    public async Task DeveloperServiceDeleteAsync([FromBody] DeveloperServiceOperationDto request)
    {
        await _permissionChecker.AuthenticateAsync(request.ProjectId, AevatarPermissions.Members.Manage);
        await _developerService.DeleteAsync(request.ClientId);
    }
}