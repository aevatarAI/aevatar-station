using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.ChatManager;
using Aevatar.GAgents.AI.Common;
using Aevatar.Quantum;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("GodGPTConfig")]
[Route("api/gotgpt/configuration")]
public class GodGPTConfigController : AevatarController
{
    private readonly IGodGPTService _godGptService;
    public GodGPTConfigController(IGodGPTService godGptService)
    {
        _godGptService = godGptService;
    }
    

    [HttpGet("system-prompt")]
    public async Task<string> GetSystemPrompt()
    {
        return await _godGptService.GetSystemPromptAsync();
    }

    [HttpPost("system-prompt")]
    public async Task UpdateSystemPrompt(GodGPTConfigurationDto godGptConfigurationDto)
    {
        await _godGptService.UpdateSystemPromptAsync(godGptConfigurationDto);
    }
}