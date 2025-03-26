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
[ControllerName("OrganizationRole")]
[Route("api/organizations/{organizationId}/roles")]
//[Authorize]
public class OrganizationRoleController : AevatarController
{
    private readonly IOrganizationRoleService _organizationRoleService;
    private readonly IOrganizationPermissionChecker _permissionChecker;

    public OrganizationRoleController(
        IOrganizationRoleService organizationRoleService, IOrganizationPermissionChecker permissionChecker)
    {
        _organizationRoleService = organizationRoleService;
        _permissionChecker = permissionChecker;
    }

    [HttpGet]
    public async Task<ListResultDto<IdentityRoleDto>> GetRoleListAsync(Guid organizationId)
    {
        //await _permissionChecker.AuthenticateAsync(organizationId, AevatarPermissions.Organizations.Default);
        return await _organizationRoleService.GetListAsync(organizationId);
    }
    
    [HttpPost]
    public virtual async Task<IdentityRoleDto> CreateAsync(Guid organizationId, IdentityRoleCreateDto input)
    {
        return await _organizationRoleService.CreateAsync(organizationId, input);
    }

    [HttpPut]
    [Route("{id}")]
    public virtual async Task<IdentityRoleDto> UpdateAsync(Guid organizationId, Guid id, IdentityRoleUpdateDto input)
    {
        return await _organizationRoleService.UpdateAsync(organizationId, id, input);
    }

    [HttpDelete]
    [Route("{id}")]
    public virtual async Task DeleteAsync(Guid organizationId, Guid id)
    {
        await _organizationRoleService.DeleteAsync(organizationId, id);
    }
    
}