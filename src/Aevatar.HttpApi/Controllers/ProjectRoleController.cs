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
[ControllerName("ProjectRole")]
[Route("api/projects/{projectId}/roles")]
//[Authorize]
public class ProjectRoleController : AevatarController
{
    private readonly IOrganizationRoleService _organizationRoleService;
    private readonly IOrganizationPermissionChecker _permissionChecker;

    public ProjectRoleController(
        IOrganizationRoleService organizationRoleService, IOrganizationPermissionChecker permissionChecker)
    {
        _organizationRoleService = organizationRoleService;
        _permissionChecker = permissionChecker;
    }

    [HttpGet]
    public async Task<ListResultDto<IdentityRoleDto>> GetRoleListAsync(Guid projectId)
    {
        // await _permissionChecker.AuthenticateAsync(projectId, AevatarPermissions.Organizations.Default);
        return await _organizationRoleService.GetListAsync(projectId);
    }
    
    [HttpPost]
    public virtual async Task<IdentityRoleDto> CreateAsync(Guid projectId, IdentityRoleCreateDto input)
    {
        return await _organizationRoleService.CreateAsync(projectId, input);
    }

    [HttpPut]
    [Route("{id}")]
    public virtual async Task<IdentityRoleDto> UpdateAsync(Guid projectId, Guid id, IdentityRoleUpdateDto input)
    {
        return await _organizationRoleService.UpdateAsync(projectId, id, input);
    }

    [HttpDelete]
    [Route("{id}")]
    public virtual async Task DeleteAsync(Guid projectId, Guid id)
    {
        await _organizationRoleService.DeleteAsync(projectId, id);
    }
}