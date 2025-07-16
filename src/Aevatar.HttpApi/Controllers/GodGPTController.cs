using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Aevatar.Account;
using Aevatar.Anonymous;
using Aevatar.Application.Grains.Agents.ChatManager;
using Aevatar.Application.Grains.Agents.ChatManager.Chat;
using Aevatar.Application.Grains.Agents.ChatManager.Common;
using Aevatar.Application.Grains.Agents.ChatManager.Dtos;
using Aevatar.Application.Grains.ChatManager.Dtos;
using Aevatar.BlobStorings;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Extensions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GodGPT.Dtos;
using Aevatar.Quantum;
using Aevatar.Service;
using Asp.Versioning;
using HandlebarsDotNet;
using Json.Schema;
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
using Volo.Abp.BlobStoring;

namespace Aevatar.Controllers;

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
    private readonly IAccountService _accountService;
    private readonly IBlobContainer _blobContainer;
    private readonly BlobStoringOptions _blobStoringOptions;
    const string Version = "1.20.0";


    public GodGPTController(IGodGPTService godGptService, IClusterClient clusterClient,
        IOptions<AevatarOptions> aevatarOptions, ILogger<GodGPTController> logger, IAccountService accountService,
        IBlobContainer blobContainer, IOptionsSnapshot<BlobStoringOptions> blobStoringOptions)
    {
        _godGptService = godGptService;
        _clusterClient = clusterClient;
        _aevatarOptions = aevatarOptions;
        _logger = logger;
        _accountService = accountService;
        _blobContainer = blobContainer;
        _blobStoringOptions = blobStoringOptions.Value;
    }

    [AllowAnonymous]
    [HttpGet("godgpt/query-version")]
    public Task<string> QueryVersion()
    {
        return Task.FromResult(Version);
    }

    [HttpPost("godgpt/create-session")]
    public async Task<Guid> CreateSessionAsync(CreateSessionRequestDto request)
    {
        var stopwatch = Stopwatch.StartNew();
        var sessionId = await _godGptService.CreateSessionAsync((Guid)CurrentUser.Id!, _defaultLLM, _defaultPrompt, request.Guider);
        _logger.LogDebug("[GodGPTController][CreateSessionAsync] sessionId: {0}, duration: {1}ms",
            sessionId.ToString(), stopwatch.ElapsedMilliseconds);
        return sessionId;
    }
    
    //Deprecated
    [HttpPost("gotgpt/create-session")]
    public async Task<Guid> CreateSessionAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var sessionId = await _godGptService.CreateSessionAsync((Guid)CurrentUser.Id!, _defaultLLM, _defaultPrompt, "");
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

    [HttpGet("godgpt/sessions/search")]
    public async Task<List<SessionInfoDto>> SearchSessionsAsync([FromQuery] string keyword)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return new List<SessionInfoDto>();
        }
        
        var searchResults = await _godGptService.SearchSessionsAsync(currentUserId, keyword);
        _logger.LogDebug("[GodGPTController][SearchSessionsAsync] userId: {0}, keyword: {1}, results: {2}, duration: {3}ms",
            currentUserId, keyword, searchResults.Count, stopwatch.ElapsedMilliseconds);
        return searchResults;
    }

    [AllowAnonymous]
    [HttpGet("godgpt/session-info/{sessionId}")]
    public async Task<Aevatar.Quantum.SessionCreationInfoDto?> GetSessionCreationInfoAsync(Guid sessionId, [FromQuery] string? shareId = null)
    {
        var stopwatch = Stopwatch.StartNew();
        Guid currentUserId;
        
        // Check if shareId is provided
        if (!string.IsNullOrWhiteSpace(shareId))
        {
            try
            {
                // Extract userId from shareId using GuidCompressor
                (currentUserId, var extractedSessionId, var extractedShareId) = GuidCompressor.DecompressGuids(shareId);
                
                // Validate that the sessionId matches
                if (extractedSessionId != sessionId)
                {
                    _logger.LogWarning("[GodGPTController][GetSessionCreationInfoAsync] SessionId mismatch. URL sessionId: {0}, extracted sessionId: {1}", 
                        sessionId, extractedSessionId);
                    return new Aevatar.Quantum.SessionCreationInfoDto();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GodGPTController][GetSessionCreationInfoAsync] Failed to decompress shareId: {0}", shareId);
                return new Aevatar.Quantum.SessionCreationInfoDto();
            }
        }
        else
        {
            // Regular sessionId format - requires authentication
            if (CurrentUser?.Id == null)
            {
                _logger.LogWarning("[GodGPTController][GetSessionCreationInfoAsync] Authentication required for regular sessionId access");
                return new Aevatar.Quantum.SessionCreationInfoDto();
            }
            currentUserId = (Guid)CurrentUser.Id!;
        }
        
        // Validate sessionId format (Guid validation is automatic by ASP.NET Core)
        if (sessionId == Guid.Empty)
        {
            _logger.LogWarning("[GodGPTController][GetSessionCreationInfoAsync] Invalid sessionId: {0}", sessionId);
            return new Aevatar.Quantum.SessionCreationInfoDto();
        }

        var sessionInfo = await _godGptService.GetSessionCreationInfoAsync(currentUserId, sessionId);
        _logger.LogDebug("[GodGPTController][GetSessionCreationInfoAsync] sessionId: {0}, userId: {1}, found: {2}, duration: {3}ms",
            sessionId, currentUserId, sessionInfo != null, stopwatch.ElapsedMilliseconds);
        
        return sessionInfo;
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
        _logger.LogDebug("[GodGPTController][GetUserProfileAsync] userId: {0}, duration: {1}ms",
            currentUserId, stopwatch.ElapsedMilliseconds);
        return userProfileDto;
    }

    [HttpPut("godgpt/account")]
    public async Task<Guid> SetUserProfileAsync(SetUserProfileInput userProfile)
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

    [HttpPost("godgpt/account/show-toast")]
    public async Task<Guid> UpdateShowToastAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        await _godGptService.UpdateShowToastAsync(currentUserId);
        _logger.LogDebug("[GodGPTController][UpdateShowToastAsync] userId: {0}, duration: {1}ms",
            currentUserId.ToString(), stopwatch.ElapsedMilliseconds);
        return currentUserId;
    }
    
    [HttpPost("godgpt/account/credits")]
    public async Task<GrainResultDto<int>> UpdateUserCreditsAsync(UpdateUserCreditsInput input)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var resultDto = await _godGptService.UpdateUserCreditsAsync(currentUserId, input);
        _logger.LogDebug("[GodGPTController][UpdateUserCreditsAsync] userId: {0}, duration: {1}ms",
            currentUserId.ToString(), stopwatch.ElapsedMilliseconds);
        return resultDto;
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

    /// <summary>
    /// Check if the email is registered. Returns a strict structure for frontend compatibility.
    /// </summary>
    /// <param name="email">Email address to check</param>
    /// <returns>Registration status in strict JSON structure</returns>
    [AllowAnonymous]
    [HttpGet("godgpt/check-email-registered")]
    public async Task<IActionResult> CheckEmailRegisteredAsync([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new
            {
                error = new { code = 1, message = "Email is required" },
                result = false
            });
        }
        var result = await _accountService.VerifyEmailRegistrationWithTimeAsync(new CheckEmailRegisteredDto { EmailAddress = email });
        if (result)
        {
            return Ok(new { result = true });
        }
        else
        {
            return Ok(new
            {
                error = new { code = 0, message = "User not registered" },
                result = false
            });
        }
    }

    #region Guest Chat APIs for Anonymous Users

    /// <summary>
    /// Create guest session for anonymous users
    /// </summary>
    [AllowAnonymous]
    [HttpPost("godgpt/guest/create-session")]
    public async Task<IActionResult> CreateGuestSessionAsync([FromBody] CreateGuestSessionRequestDto request)
    {
        var stopwatch = Stopwatch.StartNew();
        var clientIp = HttpContext.GetClientIpAddress();
        var userHashId = CommonHelper.GetAnonymousUserGAgentId(clientIp).Replace("AnonymousUser_", "");
        
        try
        {
            // Always check limits first to provide graceful response
            var limits = await _godGptService.GetGuestChatLimitsAsync(clientIp);
            
            // If no remaining chats, return limits info without creating session
            if (limits.RemainingChats <= 0)
            {
                _logger.LogDebug("[GodGPTController][CreateGuestSessionAsync] User: {0} has no remaining chats, returning limits", userHashId);
                return Ok(new CreateGuestSessionResponseDto
                {
                    RemainingChats = limits.RemainingChats,
                    TotalAllowed = limits.TotalAllowed
                });
            }
            
            // User has remaining chats, proceed with session creation
            var result = await _godGptService.CreateGuestSessionAsync(clientIp, request.Guider);
            _logger.LogDebug("[GodGPTController][CreateGuestSessionAsync] User: {0}, guider: {1}, remaining: {2}, duration: {3}ms",
                userHashId, request.Guider, result.RemainingChats, stopwatch.ElapsedMilliseconds);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GodGPTController][CreateGuestSessionAsync] User: {0}, unexpected error", userHashId);
            // Return default limits instead of error
            return Ok(new CreateGuestSessionResponseDto
            {
                RemainingChats = 0,
                TotalAllowed = 3
            });
        }
    }

    /// <summary>
    /// Get chat limits for anonymous users
    /// </summary>
    [AllowAnonymous]
    [HttpGet("godgpt/guest/limits")]
    public async Task<IActionResult> GetGuestChatLimitsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var clientIp = HttpContext.GetClientIpAddress();
        var userHashId = CommonHelper.GetAnonymousUserGAgentId(clientIp).Replace("AnonymousUser_", "");
        
        try
        {
            var result = await _godGptService.GetGuestChatLimitsAsync(clientIp);
            _logger.LogDebug("[GodGPTController][GetGuestChatLimitsAsync] User: {0}, remaining: {1}, duration: {2}ms",
                userHashId, result.RemainingChats, stopwatch.ElapsedMilliseconds);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GodGPTController][GetGuestChatLimitsAsync] User: {0}, unexpected error", userHashId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #endregion
    
    [HttpGet("godgpt/share/keyword")]
    public async Task<QuantumShareResponseDto> GetShareKeyWordWithAIAsync(
        [FromQuery] Guid sessionId, 
        [FromQuery] string? content, 
        [FromQuery] string? region, 
        [FromQuery] SessionType sessionType)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await _godGptService.GetShareKeyWordWithAIAsync(sessionId, content, region, sessionType);
        _logger.LogDebug(
            $"[GodGPTController][GetShareKeyWordWithAIAsync] completed for sessionId={sessionId}, duration: {stopwatch.ElapsedMilliseconds}ms");
        return response;
    }
    
    [HttpPost("godgpt/blob")]
    public async Task<string> SaveAsync([FromForm] SaveBlobInput input)
    {
        var file = input.File.OpenReadStream();
        if (file.Length > _blobStoringOptions.MaxSizeBytes)
        {
            throw new UserFriendlyException(
                $"The file is too large, with a maximum of {_blobStoringOptions.MaxSizeBytes} bytes.");
        }

        var originalFileName = input.File.FileName;
        var fileExtension = Path.GetExtension(originalFileName);
        var fileName = Guid.NewGuid().ToString() + fileExtension;

        await _blobContainer.SaveAsync(fileName, input.File.OpenReadStream(), true);

        return fileName;
    }
    
    [HttpDelete("godgpt/blob/{name}")]
    public async Task DeleteAsync(string name)
    {
        if (name.IsNullOrWhiteSpace())
        {
            return;
        }

        await _blobContainer.DeleteAsync(name);
    }
}