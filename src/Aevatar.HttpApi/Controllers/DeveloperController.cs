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

    [HttpPost("service")]
    public async Task DeveloperServiceRestartAsync([FromBody] DeveloperServiceOperationDto input)
    {
        await _permissionChecker.AuthenticateAsync(input.ProjectId, AevatarPermissions.Members.Manage);
        await _developerService.RestartServiceAsync(input);
    }
}