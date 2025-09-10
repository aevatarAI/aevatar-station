using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.ApiKeys;
using Aevatar.Organizations;
using Aevatar.Permissions;
using Aevatar.Projects;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrleansCodeGen.Orleans.EventSourcing.LogStorage;
using Volo.Abp;
using Volo.Abp.Identity;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("AppId")]
[Route("api/appId")]
[Authorize]
public class AppIdController : AevatarController
{
    private readonly IProjectAppIdService _appIdService;
    private readonly IdentityUserManager _identityUserManager;
    private readonly IOrganizationPermissionChecker _organizationPermission;
    private readonly IProjectService _projectService;

    public AppIdController(IProjectAppIdService appIdService, IdentityUserManager identityUserManager, IOrganizationPermissionChecker organizationPermission, IProjectService projectService)
    {
        _appIdService = appIdService;
        _identityUserManager = identityUserManager;
        _organizationPermission = organizationPermission;
        _projectService = projectService;
    }


    [HttpPost]
    public async Task CreateApiKey(CreateAppIdDto createDto)
    {
        // check projectId
        await _projectService.GetAsync(createDto.ProjectId);
        
        await _organizationPermission.AuthenticateAsync(createDto.ProjectId, AevatarPermissions.ApiKeys.Create);
        await _appIdService.CreateAsync(createDto.ProjectId, createDto.Name, CurrentUser.Id);
    }

    [HttpGet("{guid}")]
    public async Task<List<ProjectAppIdListResponseDto>> GetApiKeys(Guid guid)
    {
        await _organizationPermission.AuthenticateAsync(guid, AevatarPermissions.ApiKeys.Default);
        return await _appIdService.GetApiKeysAsync(guid);
    }

    [HttpDelete("{guid}")]
    public async Task DeleteApiKey(Guid guid)
    {
        await _appIdService.DeleteAsync(guid);
    }

    [HttpPut("{guid}")]
    public async Task ModifyApiKeyName(Guid guid, [FromBody] ModifyAppNameDto modifyAppNameDto)
    {
        await _appIdService.ModifyApiKeyAsync(guid, modifyAppNameDto.AppName);
    }
}