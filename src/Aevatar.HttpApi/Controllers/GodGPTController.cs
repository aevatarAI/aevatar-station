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
[ControllerName("GodGPT")]
[Route("api/gotgpt")]
[Authorize]
public class GodGPTController : AevatarController
{
    private readonly IGodGPTService _godGptService;
    private readonly string _defaultLLM = "OpenAI";
    private readonly string _defaultPrompt = "you are a robot";

    public GodGPTController(IGodGPTService godGptService)
    {
        _godGptService = godGptService;
    }

    [HttpPost("create-session")]
    public async Task<Guid> CreateSessionAsync()
    {
        return await _godGptService.CreateSessionAsync((Guid)CurrentUser.Id!, _defaultLLM, _defaultPrompt);
    }

    [HttpPost("chat")]
    public async Task<QuantumChatResponseDto> ChatWithSessionAsync(QuantumChatRequestDto request)
    {
        var result =
            await _godGptService.ChatWithSessionAsync((Guid)CurrentUser.Id!, request.SessionId, _defaultLLM,
                request.Content);

        return new QuantumChatResponseDto()
        {
            Content = result.Item1,
            NewTitle = result.Item2,
        };
    }

    [HttpGet("session-list")]
    public async Task<List<SessionInfoDto>> GetSessionListAsync()
    {
        return await _godGptService.GetSessionListAsync((Guid)CurrentUser.Id!);
    }

    [HttpGet("{sessionId}/chat-history")]
    public async Task<List<ChatMessage>> GetSessionMessageListAsync(Guid sessionId)
    {
        return await _godGptService.GetSessionMessageListAsync((Guid)CurrentUser.Id!, sessionId);
    }

    [HttpDelete("{sessionId}")]
    public async Task DeleteSessionAsync(Guid sessionId)
    {
        await _godGptService.DeleteSessionAsync((Guid)CurrentUser.Id!, sessionId);
    }

    [HttpPut("rename")]
    public async Task RenameSessionAsync(QuantumRenameDto request)
    {
        await _godGptService.RenameSessionAsync((Guid)CurrentUser.Id!, request.SessionId, request.Title);
    }
}