using System;
using System.Threading.Tasks;
using Aevatar.Organizations;
using Aevatar.Permissions;
using Aevatar.Plugins;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Plugin")]
[Route("api/plugins")]
[Authorize]
[RequestFormLimits(MultipartBodyLengthLimit = 15 * 1024 * 1024)] // 15MB
public class PluginController : AevatarController
{
    private readonly IPluginService _pluginService;
    private readonly IOrganizationPermissionChecker _permissionChecker;

    public PluginController(
        IOrganizationPermissionChecker permissionChecker, IPluginService pluginService)
    {
        _permissionChecker = permissionChecker;
        _pluginService = pluginService;
    }

    [HttpGet]
    public async Task<ListResultDto<PluginDto>> GetListAsync(GetPluginDto input)
    {
        await _permissionChecker.AuthenticateAsync(input.ProjectId, AevatarPermissions.Plugins.Default);
        return await _pluginService.GetListAsync(input);
    }

    [HttpPost]
    public async Task<PluginDto> CreateAsync([FromForm] CreatePluginDto input)
    {
        await _permissionChecker.AuthenticateAsync(input.ProjectId, AevatarPermissions.Plugins.Create);
        return await _pluginService.CreateAsync(input.ProjectId, input.Code.FileName, input.Code.GetAllBytes());
    }

    [HttpPut]
    [Route("{id}")]
    public async Task<PluginDto> UpdateAsync(Guid id, [FromForm] UpdatePluginDto input)
    {
        var plugin = await _pluginService.GetAsync(id);
        await _permissionChecker.AuthenticateAsync(plugin.ProjectId, AevatarPermissions.Plugins.Edit);
        
        return await _pluginService.UpdateAsync(id, input.Code.FileName, input.Code.GetAllBytes());
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task DeleteAsync(Guid id)
    {
        var plugin = await _pluginService.GetAsync(id);
        await _permissionChecker.AuthenticateAsync(plugin.ProjectId, AevatarPermissions.Plugins.Delete);
        
        await _pluginService.DeleteAsync(id);
    }
}