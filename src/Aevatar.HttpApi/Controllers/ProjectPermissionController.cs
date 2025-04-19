using System;
using System.Threading.Tasks;
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
[ControllerName("ProjectPermission")]
[Route("api/project-permissions/{projectId}")]
[Authorize]
public class ProjectPermissionController : AevatarController
{
    private readonly IProjectPermissionService _projectPermissionService;
    private readonly IOrganizationPermissionChecker _permissionChecker;

    public ProjectPermissionController(
        IOrganizationPermissionChecker permissionChecker, IProjectPermissionService projectPermissionService)
    {
        _permissionChecker = permissionChecker;
        _projectPermissionService = projectPermissionService;
    }

    [HttpGet]
    public virtual async Task<GetPermissionListResultDto> GetAsync(Guid projectId, string providerName,
        string providerKey)
    {
        await _permissionChecker.AuthenticateAsync(projectId,AevatarPermissions.Roles.Default);
        return await _projectPermissionService.GetAsync(projectId, providerName, providerKey);
    }

    [HttpPut]
    public virtual async Task UpdateAsync(Guid projectId, string providerName, string providerKey,
        UpdatePermissionsDto input)
    {
        await _permissionChecker.AuthenticateAsync(projectId,AevatarPermissions.Roles.Default);
        await _projectPermissionService.UpdateAsync(projectId, providerName, providerKey, input);
    }
}