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
        return await _pluginService.GetListAsync(input);
    }

    [HttpPost]
    public async Task<PluginDto> CreateAsync([FromForm] CreatePluginDto input)
    {
        return await _pluginService.CreateAsync(input.ProjectId, input.Code.Name, input.Code.GetAllBytes());
    }

    [HttpPut]
    [Route("{id}")]
    public async Task<PluginDto> UpdateAsync(Guid id, [FromForm] UpdatePluginDto input)
    {
        return await _pluginService.UpdateAsync(id, input.Code.Name, input.Code.GetAllBytes());
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task DeleteAsync(Guid id)
    {
        await _pluginService.DeleteAsync(id);
    }
}