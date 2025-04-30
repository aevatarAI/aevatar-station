using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.Extensions.Logging;
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
// [Authorize]
public class GodGPTController : AevatarController
{
    private readonly IGodGPTService _godGptService;
    private readonly IClusterClient _clusterClient;
    private readonly string _defaultLLM = "OpenAI";
    private readonly string _defaultPrompt = "you are a robot";
    private readonly IOptions<AevatarOptions> _aevatarOptions;
    private readonly ILogger<GodGPTController> _logger;
    const string Version = "1.0.0";


    public GodGPTController(IGodGPTService godGptService, IClusterClient clusterClient,
        IOptions<AevatarOptions> aevatarOptions, ILogger<GodGPTController> logger)
    {
        _godGptService = godGptService;
        _clusterClient = clusterClient;
        _aevatarOptions = aevatarOptions;
        _logger = logger;
    }

    [HttpGet("query-version")]
    public Task<string> QueryVersion()
    {
        return Task.FromResult(Version);
    }

    [HttpPost("create-session")]
    public async Task<Guid> CreateSessionAsync()
    {
        var mockUserId = Guid.NewGuid();
        return await _godGptService.CreateSessionAsync(mockUserId, _defaultLLM, _defaultPrompt);
    }

    [HttpPost("chat_old")]
    public async Task<QuantumChatResponseDto> ChatWithSessionAsync(QuantumChatRequestDto request)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug($"[GodGPTController][ChatWithSessionAsync] http start:{request.SessionId}");
        var streamProvider = _clusterClient.GetStreamProvider("Aevatar");
        var streamId = StreamId.Create(_aevatarOptions.Value.StreamNamespace, request.SessionId);
        _logger.LogDebug(
            $"[GodGPTController][ChatWithSessionAsync] sessionId {request.SessionId}, namespace {_aevatarOptions.Value.StreamNamespace}, streamId {streamId.ToString()}");
        Response.ContentType = "text/event-stream";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.CacheControl = "no-cache";
        var responseStream = streamProvider.GetStream<ResponseStreamGodChat>(streamId);
        var godChat = _clusterClient.GetGrain<IGodChat>(request.SessionId);

        var chatId = Guid.NewGuid().ToString();
        await godChat.StreamChatWithSessionAsync(request.SessionId, string.Empty, request.Content,
            chatId, null, true);
        _logger.LogDebug($"[GodGPTController][ChatWithSessionAsync] http request llm:{request.SessionId}");
        var exitSignal = new TaskCompletionSource();
        StreamSubscriptionHandle<ResponseStreamGodChat>? subscription = null;
        var firstFlag = false;
        var ifLastChunk = false;
        subscription = await responseStream.SubscribeAsync(async (chatResponse, token) =>
        {
            if (chatResponse.ChatId != chatId)
            {
                return;
            }

            if (firstFlag == false)
            {
                firstFlag = true;
                _logger.LogDebug(
                    $"[GodGPTController][ChatWithSessionAsync] SubscribeAsync get first message:{request.SessionId}, duration: {stopwatch.ElapsedMilliseconds}ms");
            }

            var responseData = $"data: {JsonConvert.SerializeObject(chatResponse.ConvertToHttpResponse())}\n\n";
            await Response.WriteAsync(responseData);
            await Response.Body.FlushAsync();

            if (chatResponse.IsLastChunk)
            {
                await Response.WriteAsync("event: completed\n");
                Response.Body.Close();
                ifLastChunk = true;
                exitSignal.TrySetResult();
                if (subscription != null)
                {
                    await subscription.UnsubscribeAsync();
                }
            }
        }, ex =>
        {
            _logger.LogError(
                $"[GodGPTController][ChatWithSessionAsync] on stream error async:{ex.Message} - session:{request.SessionId.ToString()}, chatId:{chatId}");
            exitSignal.TrySetException(ex);
            return Task.CompletedTask;
        }, () =>
        {
            _logger.LogError($"[GodGPTController][ChatWithSessionAsync] oncomplete");
            exitSignal.TrySetResult();
            return Task.CompletedTask;
        });

        try
        {
            await exitSignal.Task.WaitAsync(HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[GodGPTController][ChatWithSessionAsync] catch error:{ex.ToString()}");
        }
        finally
        {
            if (subscription != null)
            {
                await subscription.UnsubscribeAsync();
            }
        }

        if (ifLastChunk == false)
        {
            _logger.LogDebug($"[GodGPTController][ChatWithSessionAsync] No LastChunk:{request.SessionId},chatId:{chatId}");
        }

        _logger.LogDebug($"[GodGPTController][ChatWithSessionAsync] complete done sessionId:{request.SessionId}");
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