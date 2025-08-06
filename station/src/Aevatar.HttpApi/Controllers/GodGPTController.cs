using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Account;
using Aevatar.Anonymous;
using Aevatar.Application.Constants;
using Aevatar.Application.Contracts.Analytics;
using Aevatar.Application.Contracts.Services;
using Aevatar.Application.Grains.Agents.ChatManager;
using Aevatar.Application.Grains.Agents.ChatManager.Chat;
using Aevatar.Application.Grains.Agents.ChatManager.Common;
using Aevatar.Application.Grains.Agents.ChatManager.Dtos;
using Aevatar.Application.Grains.ChatManager.Dtos;
using Aevatar.Application.Grains.ChatManager.UserQuota;
using Aevatar.BlobStorings;
using Aevatar.Core.Abstractions;
using Aevatar.Extensions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GodGPT.Dtos;
using Aevatar.Options;
using Aevatar.Quantum;
using Aevatar.Service;
using Asp.Versioning;
using GodGPT.GAgents;
using GodGPT.GAgents.SpeechChat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IThumbnailService _thumbnailService;
    private readonly IOptions<GodGPTOptions> _godGptOptions;
    private readonly ILocalizationService _localizationService;
    private readonly IGoogleAnalyticsService _googleAnalyticsService;


    public GodGPTController(IGodGPTService godGptService, IClusterClient clusterClient,
        IOptions<AevatarOptions> aevatarOptions, ILogger<GodGPTController> logger, IAccountService accountService,
        IBlobContainer blobContainer, IOptionsSnapshot<BlobStoringOptions> blobStoringOptions,
        IThumbnailService thumbnailService, IOptions<GodGPTOptions> godGptOptions, ILocalizationService localizationService,
        IGoogleAnalyticsService googleAnalyticsService)
    {
        _godGptService = godGptService;
        _clusterClient = clusterClient;
        _aevatarOptions = aevatarOptions;
        _logger = logger;
        _accountService = accountService;
        _blobContainer = blobContainer;
        _blobStoringOptions = blobStoringOptions.Value;
        _thumbnailService = thumbnailService;
        _godGptOptions = godGptOptions;
        _localizationService = localizationService;
        _googleAnalyticsService = googleAnalyticsService;
    }

    [AllowAnonymous]
    [HttpGet("godgpt/query-version")]
    public Task<string> QueryVersion()
    {
        return Task.FromResult(_godGptOptions.Value.Version);
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
    public async Task<List<ChatMessageWithMetaDto>> GetSessionMessageListAsync(Guid sessionId)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var chatMessages = new List<ChatMessageWithMetaDto>();
        try
        {
            var manager = _clusterClient.GetGrain<IChatManagerGAgent>(currentUserId);
            var language = HttpContext.GetGodGPTLanguage();
            _logger.LogDebug(
                $"[GodGPTController][GetSessionMessageListAsync] sessionId: {sessionId}, language:{language}");
            RequestContext.Set("GodGPTLanguage", language.ToString());
            chatMessages = await manager.GetSessionMessageListWithMetaAsync(sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[GodGPTController][GetSessionMessageListAsync] exception sessionId: {sessionId}, , duration: {stopwatch.ElapsedMilliseconds}ms, error:{ex.Message}");
            throw ex;
        }

        _logger.LogDebug(
            $"[GodGPTController][GetSessionMessageListAsync] sessionId: {sessionId}, messageCount: {chatMessages.Count}, duration: {stopwatch.ElapsedMilliseconds}ms ");
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
    
    [HttpPost("godgpt/account/subscription")]
    public async Task<GrainResultDto<List<SubscriptionInfoDto>>> UpdateUserSubscriptionAsync(UpdateUserSubscriptionsInput input)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var resultDto = await _godGptService.UpdateUserSubscriptionAsync(currentUserId, input);
        _logger.LogDebug("[GodGPTController][UpdateUserSubscriptionAsync] userId: {0}, duration: {1}ms",
            currentUserId.ToString(), stopwatch.ElapsedMilliseconds);
        return resultDto;
    }

    [HttpPost("godgpt/share")]
    public async Task<CreateShareIdResponse> CreateShareStringAsync(CreateShareIdRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var language = HttpContext.GetGodGPTLanguage();
        var response = await _godGptService.GenerateShareContentAsync(currentUserId, request, language);
        _logger.LogDebug("[GodGPTController][CreateShareStringAsync] userId: {0} sessionId: {1}, ShareId={2}, duration: {3}ms",
            currentUserId, request.SessionId, response.ShareId, stopwatch.ElapsedMilliseconds);
        return response;
    }

    [AllowAnonymous]
    [HttpGet("godgpt/share/{shareString}")]
    public async Task<List<ChatMessage>> GetShareMessageListAsync(string shareString)
    {
        var stopwatch = Stopwatch.StartNew();
        var language = HttpContext.GetGodGPTLanguage();
        var response = await _godGptService.GetShareMessageListAsync(shareString,language);
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
        var language = HttpContext.GetGodGPTLanguage();
        if (string.IsNullOrWhiteSpace(email))
        {
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.EmailIsRequired, language);
            return BadRequest(new
            {
                error = new { code = 1, message = localizedMessage },
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
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.UserUnRegister, language);
            return Ok(new
            {
                error = new { code = 0, message = localizedMessage },
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
        var language = HttpContext.GetGodGPTLanguage();
        try
        {
            var result = await _godGptService.GetGuestChatLimitsAsync(clientIp);
            _logger.LogDebug("[GodGPTController][GetGuestChatLimitsAsync] User: {0}, remaining: {1}, duration: {2}ms",
                userHashId, result.RemainingChats, stopwatch.ElapsedMilliseconds);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InternalServerError, language);
            _logger.LogError(ex, "[GodGPTController][GetGuestChatLimitsAsync] User: {0}, unexpected error", userHashId);
            return StatusCode(500, new { error = localizedMessage });
        }
    }
    [HttpPost("godgpt/voice/set")]
    public async Task<UserProfileDto> SetVoiceLanguageAsync([FromBody] SetVoiceLanguageRequestDto request)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var userProfileDto = new UserProfileDto();
        try
        {
            userProfileDto = await _godGptService.SetVoiceLanguageAsync(currentUserId, request.VoiceLanguage);
        }
        catch (Exception ex)
        {
            userProfileDto = new UserProfileDto();
            userProfileDto.VoiceLanguage = VoiceLanguageEnum.Unset;
            _logger.LogError($"[GodGPTController][SetVoiceLanguageAsync] exception userId: {currentUserId},voiceLanguage:{request.VoiceLanguage} duration: {stopwatch.ElapsedMilliseconds}ms error:{ex.Message}");
            return userProfileDto;
        }
        _logger.LogDebug($"[GodGPTController][SetVoiceLanguageAsync] userId: {currentUserId},voiceLanguage:{request.VoiceLanguage} duration: {stopwatch.ElapsedMilliseconds}ms");
        return userProfileDto;
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
        
        // Get language from request headers
        var language = HttpContext.GetGodGPTLanguage();
        
        // Append language-specific prompt requirement if content is provided
        var processedContent = SessionTypeExtensions.SharePrompt;
        processedContent = processedContent.AppendLanguagePrompt(language);
        
        var response = await _godGptService.GetShareKeyWordWithAIAsync(sessionId, processedContent, region, sessionType, language);
        _logger.LogDebug(
            $"[GodGPTController][GetShareKeyWordWithAIAsync] completed for sessionId={sessionId}, language={language},processedContent={processedContent}, duration: {stopwatch.ElapsedMilliseconds}ms");
        return response;
    }
    
    [HttpGet("godgpt/can-upload-image")]
    public async Task<CanUploadImageResponseDto> CanUploadImageAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var language = HttpContext.GetGodGPTLanguage();
        var response = await _godGptService.CanUploadImageAsync(currentUserId, language);
        
        var result = new CanUploadImageResponseDto
        {
            CanUpload = response.Success,
            Reason = response.Message
        };
        
        _logger.LogDebug($"[GodGPTController][CanUploadImageAsync] userId: {currentUserId}, canUpload: {result.CanUpload}, duration: {stopwatch.ElapsedMilliseconds}ms");
        
        return result;
    }

    [HttpPost("godgpt/blob")]
    public async Task<string> SaveAsync([FromForm] SaveBlobInput input)
    {
        var language = HttpContext.GetGodGPTLanguage();
        if (input.File.Length > _blobStoringOptions.MaxSizeBytes)
        {
            var parameters = new Dictionary<string, string>
            {
                ["MaxSizeBytes"] = _blobStoringOptions.MaxSizeBytes.ToString()
            };
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.FileTooLarge, language,parameters);
            throw new UserFriendlyException(localizedMessage);
        }
        
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        
        var response = await _godGptService.CanUploadImageAsync(currentUserId);
        if (!response.Success)
        {
            _logger.LogDebug("[GodGPTController][BlobSaveAsync] Daily upload limit reached");
            throw new UserFriendlyException("Daily upload limit reached. Upgrade to premium to continue.");
        }
        
        var originalFileName = input.File.FileName;
        var fileExtension = Path.GetExtension(originalFileName);
        var fileName = Guid.NewGuid().ToString() + fileExtension;
        
        // Read file content to memory to avoid stream reuse issues
        var readStopwatch = Stopwatch.StartNew();
        byte[] fileContent;
        using (var fileStream = input.File.OpenReadStream())
        {
            fileContent = new byte[input.File.Length];
            await fileStream.ReadAsync(fileContent, 0, fileContent.Length);
        }
        readStopwatch.Stop();
        _logger.LogDebug("[GodGPTController][BlobSaveAsync] File reading completed: Duration={ReadTime}ms, Size={FileSize} bytes",
            readStopwatch.ElapsedMilliseconds, input.File.Length);

        // Save original file using memory stream
        var saveStopwatch = Stopwatch.StartNew();
        using (var originalStream = new MemoryStream(fileContent))
        {
            await _blobContainer.SaveAsync(fileName, originalStream, true);
        }
        saveStopwatch.Stop();
        _logger.LogDebug("[GodGPTController][BlobSaveAsync] Original file save completed: FileName={FileName}, Duration={SaveTime}ms",
            fileName, saveStopwatch.ElapsedMilliseconds);

        // Generate thumbnails using separate memory stream
        _ = Task.Run(async () =>
        {
            using var thumbnailStream = new MemoryStream(fileContent);
            await _thumbnailService.GenerateThumbnailsAsync(thumbnailStream, fileName);
        });

        _logger.LogDebug("[GodGPTController][BlobSaveAsync] userId: {0}, duration: {2}ms",
            currentUserId, stopwatch.ElapsedMilliseconds);
        // var response = new SaveBlobResponse
        // {
        //     OriginalFileName = fileName,
        //     OriginalSize = input.File.Length
        // };
        //
        // return response;
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

    /// <summary>
    /// Get today's awakening content
    /// </summary>
    /// <param name="region">Optional region parameter</param>
    /// <returns>Awakening content DTO</returns>
    [HttpGet("godgpt/awakening/today")]
    public async Task<AwakeningContentDto?> GetTodayAwakeningAsync([FromQuery] string? region)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        
        // Get language from header, default to English if not provided
        var languageHeader = HttpContext.Request.Headers["GodgptLanguage"].FirstOrDefault();
        var language = VoiceLanguageEnum.English; // Default value
        
        if (!string.IsNullOrEmpty(languageHeader))
        {
            // Try to parse the language from header
            if (Enum.TryParse<VoiceLanguageEnum>(languageHeader, true, out var parsedLanguage))
            {
                language = parsedLanguage;
            }
            else
            {
                _logger.LogWarning("[GodGPTController][GetTodayAwakeningAsync] Invalid language header: {Language}, using default: {Default}", 
                    languageHeader, language);
            }
        }
        
        try
        {
            var result = await _godGptService.GetTodayAwakeningAsync(currentUserId, language, region);
            
            _logger.LogDebug("[GodGPTController][GetTodayAwakeningAsync] userId: {UserId}, language: {Language}, region: {Region}, hasResult: {HasResult}, duration: {Duration}ms",
                currentUserId, language, region, result != null, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GodGPTController][GetTodayAwakeningAsync] Error for userId: {UserId}, language: {Language}, region: {Region}",
                currentUserId, language, region);
            throw;
        }
    }

    /// <summary>
    /// Track event to Google Analytics
    /// </summary>
    /// <param name="request">GA event request</param>
    /// <returns>Tracking result</returns>
    [HttpPost("godgpt/analytics/track")]
    public async Task<IActionResult> TrackAnalyticsEventAsync(GoogleAnalyticsEventRequestDto request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (CurrentUser?.Id != null)
            {
                request.UserId = CurrentUser.Id.ToString();
            }
            
            _ = Task.Run(async () =>
            {
                try
                {
                    await _googleAnalyticsService.TrackEventAsync(request);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[GodGPTController][TrackAnalyticsEventAsync] Background GA tracking failed for event: {EventName}", 
                        request.EventName);
                }
            });
            
            _logger.LogDebug("[GodGPTController][TrackAnalyticsEventAsync] Event queued: {EventName}, ClientId: {ClientId}, UserId: {UserId}, duration: {Duration}ms",
                request.EventName, request.ClientId, request.UserId, stopwatch.ElapsedMilliseconds);
                
            return Ok(new GoogleAnalyticsEventResponseDto
            {
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GodGPTController][TrackAnalyticsEventAsync] Error processing analytics event: {EventName}",
                request.EventName);
                
            return StatusCode(500, new GoogleAnalyticsEventResponseDto
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }
}