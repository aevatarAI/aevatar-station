using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.ChatManager;
using Aevatar.CQRS.Dto;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.Permissions;
using Aevatar.Quantum;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Volo.Abp;
using Volo.Abp.Users;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Quantum")]
[Microsoft.AspNetCore.Components.Route("api/quantum")]
[Authorize]
public class QuantumChatController : AevatarController
{
    private readonly IQuantumService _quantumService;
    private readonly string _defaultLLM = "OpenAI";
    private readonly string _defaultPrompt = "you are a robot";

    public QuantumChatController(IQuantumService quantumService)
    {
        _quantumService = quantumService;
    }

    [HttpPost("create-session")]
    public async Task<Guid> CreateSessionAsync()
    {
        return await _quantumService.CreateSessionAsync((Guid)CurrentUser.Id!, _defaultLLM, _defaultPrompt);
    }

    [HttpPost("chat")]
    public async Task<QuantumChatResponseDto> ChatWithSessionAsync(QuantumChatRequestDto request)
    {
        var result =
            await _quantumService.ChatWithSessionAsync((Guid)CurrentUser.Id!, request.SessionId, _defaultLLM,
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
        return await _quantumService.GetSessionListAsync((Guid)CurrentUser.Id!);
    }

    [HttpGet("{sessionId}/chat-history")]
    public async Task<List<ChatMessage>> GetSessionMessageListAsync(Guid sessionId)
    {
        return await _quantumService.GetSessionMessageListAsync((Guid)CurrentUser.Id!, sessionId);
    }

    [HttpDelete("{sessionId}")]
    public async Task DeleteSessionAsync(Guid sessionId)
    {
        await _quantumService.DeleteSessionAsync((Guid)CurrentUser.Id!, sessionId);
    }

    [HttpPut("rename")]
    public async Task RenameSessionAsync(QuantumRenameDto request)
    {
        await _quantumService.RenameSessionAsync((Guid)CurrentUser.Id!, request.SessionId, request.Title);
    }
}