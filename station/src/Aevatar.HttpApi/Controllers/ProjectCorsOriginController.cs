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

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("ProjectCorsOrigin")]
[Route("api/projects/{projectId}/cors-origins")]
[Authorize]
public class ProjectCorsOriginController : AevatarController
{
    private readonly IProjectCorsOriginService _projectCorsOriginService;
    private readonly IOrganizationPermissionChecker _permissionChecker;

    public ProjectCorsOriginController(
        IOrganizationPermissionChecker permissionChecker, IProjectCorsOriginService pluginService)
    {
        _permissionChecker = permissionChecker;
        _projectCorsOriginService = pluginService;
    }

    [HttpGet]
    public async Task<ListResultDto<ProjectCorsOriginDto>> GetListAsync(Guid projectId)
    {
        await _permissionChecker.AuthenticateAsync(projectId, AevatarPermissions.ProjectCorsOrigins.Default);
        return await _projectCorsOriginService.GetListAsync(projectId);
    }

    [HttpPost]
    public async Task<ProjectCorsOriginDto> CreateAsync(Guid projectId, CreateProjectCorsOriginDto input)
    {
        await _permissionChecker.AuthenticateAsync(projectId, AevatarPermissions.ProjectCorsOrigins.Create);
        return await _projectCorsOriginService.CreateAsync(projectId, input);
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task DeleteAsync(Guid projectId, Guid id)
    {
        await _permissionChecker.AuthenticateAsync(projectId, AevatarPermissions.ProjectCorsOrigins.Delete);
        await _projectCorsOriginService.DeleteAsync(projectId, id);
    }
}