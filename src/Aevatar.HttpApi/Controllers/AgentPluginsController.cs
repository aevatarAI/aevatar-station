using System;
using System.Threading.Tasks;
using Aevatar.AgentPlugins;
using Aevatar.Common;
using Aevatar.Controllers;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Aevatar;
[RemoteService]
[ControllerName("Agent")]
[Route("api/agentPlugins")]
public class AgentPluginsController : AevatarController
{
    private readonly IPluginGAgentManager _pluginGAgentManager;
    private readonly IUserAppService _userAppService;
    public AgentPluginsController(
        IPluginGAgentManager pluginGAgentManager, 
        IUserAppService userAppService)
    {
        _pluginGAgentManager = pluginGAgentManager;
        _userAppService = userAppService;
    }

    [HttpPut]
    [Authorize]
    [Route("AgentPlugin")]
    [RequestSizeLimit(209715200)]
    [RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
    public async Task<Guid> UploadAgentPluginsAsync([FromForm]CreateAgentPluginDto input)
    {
        if (input.Code == null || input.Code.Length == 0)
        {
            throw new UserFriendlyException("code cannot be empty.");
        }
        byte[] codeBytes = input.Code.GetAllBytes();
           
        return await _pluginGAgentManager.AddPluginAsync(new AddPluginDto
        {
            Code = codeBytes,
            TenantId = _userAppService.GetCurrentUserId()
        });
    }
    
    [HttpGet("PluginsWithDescription")]
    public async Task<PluginsInformation> GetPluginsWithDescriptionAsync()
    {
        return await  _pluginGAgentManager.GetPluginsWithDescriptionAsync(_userAppService.GetCurrentUserId());
    }
    
    [HttpPost("RemovePluginAsync")]
    public async Task RemovePluginAsync(RemoveAgentPluginDto removeAgentPluginDto)
    {
         await  _pluginGAgentManager.RemovePluginAsync(new RemovePluginDto()
        {
            TenantId = _userAppService.GetCurrentUserId(),
            PluginCodeId = removeAgentPluginDto.PluginCodeId
        }
        );
    }
    
    [HttpPost("UpdatePluginAsync")]
    public async Task UpdatePluginAsync(UpdateAgentPluginDto input)
    {
        if (input.Code == null || input.Code.Length == 0)
        {
            throw new UserFriendlyException("code cannot be empty.");
        }
        byte[] codeBytes = input.Code.GetAllBytes();
        await  _pluginGAgentManager.UpdatePluginAsync(new UpdatePluginDto
            {
                TenantId = _userAppService.GetCurrentUserId(),
                PluginCodeId = input.PluginCodeId,
                Code = codeBytes
            }
        );
    }
    
}