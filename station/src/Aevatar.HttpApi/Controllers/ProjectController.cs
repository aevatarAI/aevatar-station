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
[ControllerName("Project")]
[Route("api/projects")]
[Authorize]
public class ProjectController : AevatarController
{
    private readonly IProjectService _projectService;
    private readonly IOrganizationPermissionChecker _permissionChecker;

    public ProjectController(IProjectService projectService,
        IOrganizationPermissionChecker permissionChecker)
    {
        _projectService = projectService;
        _permissionChecker = permissionChecker;
    }

    [HttpGet]
    public async Task<ListResultDto<ProjectDto>> GetListAsync(GetProjectListDto input)
    {
        return await _projectService.GetListAsync(input);
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<ProjectDto> GetAsync(Guid id)
    {
        await _permissionChecker.AuthenticateAsync(id, AevatarPermissions.Projects.Default);
        return await _projectService.GetProjectAsync(id);
    }

    [HttpPost]
    public async Task<ProjectDto> CreateAsync(CreateProjectDto input)
    {
        await _permissionChecker.AuthenticateAsync(input.OrganizationId, AevatarPermissions.Projects.Create);
        return await _projectService.CreateAsync(input);
    }
    
    [HttpPost]
    [Route("default")]
    public async Task<ProjectDto> CreateDefaultAsync(CreateDefaultProjectDto input)
    {
        await _permissionChecker.AuthenticateAsync(input.OrganizationId, AevatarPermissions.Projects.Create);
        return await _projectService.CreateDefaultAsync(input);
    }

    [HttpPut]
    [Route("{id}")]
    public async Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectDto input)
    {
        await _permissionChecker.AuthenticateAsync(id, AevatarPermissions.Projects.Edit);
        return await _projectService.UpdateAsync(id, input);
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task DeleteAsync(Guid id)
    {
        await _permissionChecker.AuthenticateAsync(id, AevatarPermissions.Projects.Delete);
        await _projectService.DeleteAsync(id);
    }

    [HttpGet]
    [Route("{projectId}/members")]
    public async Task<ListResultDto<OrganizationMemberDto>> GetMemberListAsync(Guid projectId, GetOrganizationMemberListDto input)
    {
        await _permissionChecker.AuthenticateAsync(projectId, AevatarPermissions.Members.Default);
        return await _projectService.GetMemberListAsync(projectId, input);
    }

    [HttpPut]
    [Route("{projectId}/members")]
    public async Task SetMemberAsync(Guid projectId, SetOrganizationMemberDto input)
    {
        await _permissionChecker.AuthenticateAsync(projectId, AevatarPermissions.Members.Manage);
        await _projectService.SetMemberAsync(projectId, input);
    }

    [HttpPut]
    [Route("{projectId}/member-roles")]
    public async Task SetMemberRoleAsync(Guid projectId, SetOrganizationMemberRoleDto input)
    {
        await _permissionChecker.AuthenticateAsync(projectId, AevatarPermissions.Members.Manage);
        
        if (input.UserId == CurrentUser.Id)
        {
            throw new UserFriendlyException("Unable to set your own role.");
        }
        
        await _projectService.SetMemberRoleAsync(projectId, input);
    }
    
    [HttpGet]
    [Route("{projectId}/permissions")]
    public async Task<ListResultDto<PermissionGrantInfoDto>> GetPermissionsListAsync(Guid projectId)
    {
        return await _projectService.GetPermissionListAsync(projectId);
    }
    
    [HttpPost]
    [Route("recent-used")]
    public async Task SaveRecentUsedProjectAsync(RecentUsedProjectDto recentUsedProjectDto)
    {
        await _projectService.SaveRecentUsedProjectAsync(recentUsedProjectDto);
    }
    
    [HttpGet]
    [Route("recent-used")]
    public async Task<RecentUsedProjectDto> GetRecentUsedProjectAsync()
    {
        return await _projectService.GetRecentUsedProjectAsync();
    }
}