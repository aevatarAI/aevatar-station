using System;
using System.Threading.Tasks;
using Aevatar.Organizations;
using Aevatar.Permissions;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("OrganizationPermission")]
[Route("api/organization-permissions/{organizationId}")]
[Authorize]
public class OrganizationPermissionController : AevatarController
{
    private readonly IOrganizationPermissionService _organizationPermissionService;
    private readonly IOrganizationPermissionChecker _permissionChecker;

    public OrganizationPermissionController(
        IOrganizationPermissionChecker permissionChecker, IOrganizationPermissionService organizationPermissionService)
    {
        _permissionChecker = permissionChecker;
        _organizationPermissionService = organizationPermissionService;
    }

    [HttpGet]
    public virtual async Task<GetPermissionListResultDto> GetAsync(Guid organizationId, string providerName,
        string providerKey)
    {
        await _permissionChecker.AuthenticateAsync(organizationId,AevatarPermissions.Roles.Default);
        return await _organizationPermissionService.GetAsync(organizationId, providerName, providerKey);
    }

    [HttpPut]
    public virtual async Task UpdateAsync(Guid organizationId, string providerName, string providerKey,
        UpdatePermissionsDto input)
    {
        await _permissionChecker.AuthenticateAsync(organizationId,AevatarPermissions.Roles.Edit);
        await _organizationPermissionService.UpdateAsync(organizationId, providerName, providerKey, input);
    }
}