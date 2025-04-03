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
[ControllerName("Organization")]
[Route("api/organizations")]
[Authorize]
public class OrganizationController : AevatarController
{
    private readonly IOrganizationService _organizationService;
    private readonly IOrganizationPermissionChecker _permissionChecker;

    public OrganizationController(IOrganizationService organizationService,
        IOrganizationPermissionChecker permissionChecker)
    {
        _organizationService = organizationService;
        _permissionChecker = permissionChecker;
    }

    [HttpGet]
    public async Task<ListResultDto<OrganizationDto>> GetListAsync(GetOrganizationListDto input)
    {
        return await _organizationService.GetListAsync(input);
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<OrganizationDto> GetAsync(Guid id)
    {
        await _permissionChecker.AuthenticateAsync(id, AevatarPermissions.Organizations.Default);
        return await _organizationService.GetAsync(id);
    }

    [HttpPost]
    public async Task<OrganizationDto> CreateAsync(CreateOrganizationDto input)
    {
        return await _organizationService.CreateAsync(input);
    }

    [HttpPut]
    [Route("{id}")]
    public async Task<OrganizationDto> UpdateAsync(Guid id, UpdateOrganizationDto input)
    {
        await _permissionChecker.AuthenticateAsync(id, AevatarPermissions.Organizations.Edit);
        return await _organizationService.UpdateAsync(id, input);
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task DeleteAsync(Guid id)
    {
        await _permissionChecker.AuthenticateAsync(id, AevatarPermissions.Organizations.Delete);
        await _organizationService.DeleteAsync(id);
    }

    [HttpGet]
    [Route("{organizationId}/members")]
    public async Task<ListResultDto<OrganizationMemberDto>> GetMemberListAsync(Guid organizationId, GetOrganizationMemberListDto input)
    {
        await _permissionChecker.AuthenticateAsync(organizationId, AevatarPermissions.OrganizationMembers.Default);
        return await _organizationService.GetMemberListAsync(organizationId, input);
    }

    [HttpPut]
    [Route("{organizationId}/members")]
    public async Task SetMemberAsync(Guid organizationId, SetOrganizationMemberDto input)
    {
        await _permissionChecker.AuthenticateAsync(organizationId, AevatarPermissions.OrganizationMembers.Manage);
        await _organizationService.SetMemberAsync(organizationId, input);
    }

    [HttpPut]
    [Route("{organizationId}/member-roles")]
    public async Task SetMemberRoleAsync(Guid organizationId, SetOrganizationMemberRoleDto input)
    {
        await _permissionChecker.AuthenticateAsync(organizationId, AevatarPermissions.OrganizationMembers.Manage);
        await _organizationService.SetMemberRoleAsync(organizationId, input);
    }
    
    [HttpGet]
    [Route("{organizationId}/roles")]
    public async Task<ListResultDto<IdentityRoleDto>> GetRoleListAsync(Guid organizationId)
    {
        await _permissionChecker.AuthenticateAsync(organizationId, AevatarPermissions.Organizations.Default);
        return await _organizationService.GetRoleListAsync(organizationId);
    }
    
    [HttpGet]
    [Route("{organizationId}/permissions")]
    public async Task<ListResultDto<PermissionGrantInfoDto>> GetPermissionsListAsync(Guid organizationId)
    {
        return await _organizationService.GetPermissionListAsync(organizationId);
    }
}