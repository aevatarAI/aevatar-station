using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.ChatManager;
using Aevatar.Application.Grains.Agents.ChatManager.Chat;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.Quantum;
using Aevatar.Service;
using Asp.Versioning;
using HandlebarsDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("GodGPT")]
[Route("api/gotgpt")]
[Authorize]
public class GodGPTController : AevatarController
{
    private readonly IGodGPTService _godGptService;
    private readonly IClusterClient _clusterClient;
    private readonly string _defaultLLM = "OpenAI";
    private readonly string _defaultPrompt = "you are a robot";
    private readonly IOptions<AevatarOptions> _aevatarOptions;
    const string Version = "1.0.0";


    public GodGPTController(IGodGPTService godGptService, IClusterClient clusterClient,
        IOptions<AevatarOptions> aevatarOptions)
    {
        _godGptService = godGptService;
        _clusterClient = clusterClient;
        _aevatarOptions = aevatarOptions;
    }

    [HttpGet("query-version")]
    public Task<string> QueryVersion()
    {
        return Task.FromResult(Version);
    }

    [HttpPost("create-session")]
    public async Task<Guid> CreateSessionAsync()
    {
        return await _godGptService.CreateSessionAsync((Guid)CurrentUser.Id!, _defaultLLM, _defaultPrompt);
    }

    [HttpPost("chat")]
    public async Task<QuantumChatResponseDto> ChatWithSessionAsync(QuantumChatRequestDto request)
    {
        var streamProvider = _clusterClient.GetStreamProvider("Aevatar");
        var streamId = StreamId.Create(_aevatarOptions.Value.StreamNamespace, request.SessionId);
        Response.ContentType = "text/event-stream";
        var responseStream = streamProvider.GetStream<ResponseStreamGodChat>(streamId);
        var godChat = _clusterClient.GetGrain<IGodChat>(request.SessionId);

        var chatId = Guid.NewGuid().ToString(); 
        await godChat.StreamChatWithSessionAsync(request.SessionId, string.Empty, request.Content,
            chatId, null, true);

        var exitSignal = new TaskCompletionSource();
        StreamSubscriptionHandle<ResponseStreamGodChat>? subscription = null;
        subscription = await responseStream.SubscribeAsync(async (chatResponse, token) =>
        {
            if (chatResponse.ChatId != chatId)
            {
                return;
            }
            
            await Response.WriteAsync(JsonConvert.SerializeObject(chatResponse));
            await Response.Body.FlushAsync();
            
            if (chatResponse.IsLastChunk)
            {
                exitSignal.TrySetResult();
                if (subscription != null)
                {
                    await subscription.UnsubscribeAsync();
                }
            }
        }, ex =>
        {
            exitSignal.TrySetException(ex);
            return Task.CompletedTask;
        }, () =>
        {
            exitSignal.TrySetResult();
            return Task.CompletedTask;
        });

        try
        {
            await exitSignal.Task.WaitAsync(HttpContext.RequestAborted);
        }
        finally
        {
            if (subscription != null)
            {
                await subscription.UnsubscribeAsync();
            }
        }

        return new QuantumChatResponseDto()
        {
            Content = "",
            NewTitle = "",
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