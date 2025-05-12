using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.ChatManager;
using Aevatar.Application.Grains.Agents.ChatManager.Chat;
using Aevatar.Application.Grains.Agents.ChatManager.Dtos;
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

// 添加语音对话请求DTO
public class VoiceChatRequestDto
{
    public Guid SessionId { get; set; }
    public string AudioData { get; set; } // Base64编码的音频数据
    public string Region { get; set; }
    public string RecognitionLanguage { get; set; } // 语音识别使用的语言（中文版默认"zh-CN"，国际版默认"en-US"）
    public string SynthesisLanguage { get; set; } // 语音合成使用的语言（中文版默认"zh-CN"，国际版默认"en-US"）
    public string SynthesisVoiceName { get; set; } // 语音合成使用的声音名称
}

// 添加语音对话响应DTO
public class VoiceChatResponseDto
{
    public string AudioData { get; set; } // Base64编码的响应音频数据
    public string AudioUrl { get; set; } // 响应音频的URL
    public string NewTitle { get; set; } // 新标题
    public string Message { get; set; } // 可选的消息
    
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string Code { get; set; } = "20000";
    
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public VoiceChatResponseData Data { get; set; }
}

public class VoiceChatResponseData
{
    public string AudioData { get; set; }
    public string AudioUrl { get; set; }
    public string NewTitle { get; set; }
}

[RemoteService]
[ControllerName("GodGPT")]
[Route("api")]
[Authorize]
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

    [HttpGet("godgpt/query-version")]
    public Task<string> QueryVersion()
    {
        return Task.FromResult(Version);
    }

    [HttpPost("gotgpt/create-session")]
    public async Task<Guid> CreateSessionAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var sessionId = await _godGptService.CreateSessionAsync((Guid)CurrentUser.Id!, _defaultLLM, _defaultPrompt);
        _logger.LogDebug("[GodGPTController][CreateSessionAsync] sessionId: {0}, duration: {1}ms",
            sessionId.ToString(), stopwatch.ElapsedMilliseconds);
        return sessionId;
    }

    [HttpPost("gotgpt/chat_old")]
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
            chatId, null, true, request.Region);
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

    [HttpGet("godgpt/session-list")]
    public async Task<List<SessionInfoDto>> GetSessionListAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var sessionList = await _godGptService.GetSessionListAsync(currentUserId);
        _logger.LogDebug("[GodGPTController][GetSessionListAsync] userId: {0}, duration: {1}ms",
            currentUserId.ToString(), stopwatch.ElapsedMilliseconds);
        return sessionList;
    }

    [HttpGet("godgpt/chat/{sessionId}")]
    public async Task<List<ChatMessage>> GetSessionMessageListAsync(Guid sessionId)
    {
        var stopwatch = Stopwatch.StartNew();
        var chatMessages = await _godGptService.GetSessionMessageListAsync((Guid)CurrentUser.Id!, sessionId);
        _logger.LogDebug("[GodGPTController][GetSessionMessageListAsync] sessionId: {0}, duration: {1}ms",
            sessionId, stopwatch.ElapsedMilliseconds);
        return chatMessages;
    }

    [HttpPut("godgpt/chat/rename")]
    public async Task<Guid> RenameSessionAsync(QuantumRenameDto request)
    {
        var stopwatch = Stopwatch.StartNew();
        var sessionId =
            await _godGptService.RenameSessionAsync((Guid)CurrentUser.Id!, request.SessionId, request.Title);
        _logger.LogDebug("[GodGPTController][RenameSessionAsync] sessionId: {0}, duration: {1}ms",
            sessionId, stopwatch.ElapsedMilliseconds);
        return sessionId;
    }

    [HttpDelete("godgpt/chat/{sessionId}")]
    public async Task<Guid> DeleteSessionAsync(Guid sessionId)
    {
        var stopwatch = Stopwatch.StartNew();
        var deleteSessionId = await _godGptService.DeleteSessionAsync((Guid)CurrentUser.Id!, sessionId);
        _logger.LogDebug("[GodGPTController][DeleteSessionAsync] sessionId: {0}, duration: {1}ms",
            deleteSessionId, stopwatch.ElapsedMilliseconds);
        return deleteSessionId;
    }

    [HttpGet("godgpt/account")]
    public async Task<UserProfileDto> GetUserProfileAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var userProfileDto = await _godGptService.GetUserProfileAsync(currentUserId);
        _logger.LogDebug("[GodGPTController][GetUserProfileAsync] sessionId: {0}, duration: {1}ms",
            currentUserId, stopwatch.ElapsedMilliseconds);
        return userProfileDto;
    }

    [HttpPut("godgpt/account")]
    public async Task<Guid> SetUserProfileAsync(UserProfileDto userProfile)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var updateUserId = await _godGptService.SetUserProfileAsync(currentUserId, userProfile);
        _logger.LogDebug("[GodGPTController][SetUserProfileAsync] sessionId: {0}, duration: {1}ms",
            updateUserId, stopwatch.ElapsedMilliseconds);
        return updateUserId;
    }

    [HttpDelete("godgpt/account")]
    public async Task<Guid> DeleteAccountAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var deleteUserId = await _godGptService.DeleteAccountAsync(currentUserId);
        _logger.LogDebug("[GodGPTController][SetUserProfileAsync] sessionId: {0}, duration: {1}ms",
            deleteUserId, stopwatch.ElapsedMilliseconds);
        return deleteUserId;
    }

    [HttpPost("godgpt/share")]
    public async Task<CreateShareIdResponse> CreateShareStringAsync(CreateShareIdRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var response = await _godGptService.GenerateShareContentAsync(currentUserId, request);
        _logger.LogDebug("[GodGPTController][CreateShareStringAsync] userId: {0} sessionId: {1}, ShareId={2}, duration: {3}ms",
            currentUserId, request.SessionId, response.ShareId, stopwatch.ElapsedMilliseconds);
        return response;
    }

    [AllowAnonymous]
    [HttpGet("godgpt/share/{shareString}")]
    public async Task<List<ChatMessage>> GetShareMessageListAsync(string shareString)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await _godGptService.GetShareMessageListAsync(shareString);
        _logger.LogDebug("[GodGPTController][GetShareMessageListAsync] shareString: {0} duration: {1}ms",
            shareString, stopwatch.ElapsedMilliseconds);
        return response;
    }

    // 添加语音对话接口
    [HttpPost("godgpt/chat/voice")]
    public async Task<VoiceChatResponseDto> ChatWithVoiceAsync(VoiceChatRequestDto request)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[GodGPTController][ChatWithVoiceAsync] sessionId: {0}, language: {1}", 
            request.SessionId, request.RecognitionLanguage);
        
        try
        {
            // 假设语音识别已经在其他地方处理，这里我们只需要获取识别后的文本内容
            // 实际实现中，这里应该调用语音识别服务，将音频数据转换为文本
            
            // 调用聊天服务获取文本响应
            var currentUserId = (Guid)CurrentUser.Id!;
            // 这里假设我们已经将语音识别为文本，现在需要与GodGPT对话
            // 实际实现中，你需要替换下面的代码，调用真实的语音识别服务
            string recognizedText = "识别后的文本将在这里"; // 实际中应从语音识别服务获取
            
            // 调用聊天服务获取文本响应
            var textResponse = await _godGptService.ChatWithSessionAsync(
                currentUserId, 
                request.SessionId, 
                _defaultLLM, 
                recognizedText);
            
            // 假设语音合成在其他地方处理，这里我们只需要返回合成后的音频数据
            // 实际实现中，这里应该调用语音合成服务，将文本转换为音频
            
            // 生成响应
            var response = new VoiceChatResponseDto
            {
                Code = "20000",
                Data = new VoiceChatResponseData
                {
                    AudioData = "合成后的音频数据将在这里", // 实际中应从语音合成服务获取
                    AudioUrl = "可选的音频URL",
                    NewTitle = textResponse.Item2 // 使用聊天服务返回的标题
                }
            };
            
            _logger.LogDebug("[GodGPTController][ChatWithVoiceAsync] sessionId: {0}, duration: {1}ms",
                request.SessionId, stopwatch.ElapsedMilliseconds);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GodGPTController][ChatWithVoiceAsync] Error processing voice chat request for sessionId: {0}", request.SessionId);
            return new VoiceChatResponseDto
            {
                Code = "50000",
                Message = "Internal server error occurred while processing voice chat"
            };
        }
    }
}