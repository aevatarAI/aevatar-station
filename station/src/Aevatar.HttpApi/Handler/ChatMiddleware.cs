using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Anonymous;
using Aevatar.Application.Grains.Agents.Anonymous;
using Aevatar.Application.Grains.Agents.ChatManager;
using Aevatar.Application.Grains.Agents.ChatManager.Chat;
using Aevatar.Application.Grains.Agents.ChatManager.Common;
using Aevatar.Application.Grains.ChatManager.UserQuota;
using Aevatar.Core.Abstractions;
using Aevatar.Extensions;
using Aevatar.Quantum;
using GodGPT.GAgents.SpeechChat;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using Volo.Abp;

namespace Aevatar.Handler;

public class ChatMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<ChatMiddleware> _logger;
    private readonly IOptions<AevatarOptions> _aevatarOptions;

    private const int MaxImageCount = 10;

    public ChatMiddleware(RequestDelegate next, ILogger<ChatMiddleware> logger, IClusterClient clusterClient,
        IOptions<AevatarOptions> aevatarOptions)
    {
        _next = next;
        _logger = logger;
        _clusterClient = clusterClient;
        _aevatarOptions = aevatarOptions;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var pathBase = context.Request.PathBase.Value ?? "";
        var fullPath = pathBase + path;
        
        _logger.LogDebug("[ChatMiddleware] Processing request - PathBase: {0}, Path: {1}, FullPath: {2}", 
            pathBase, path, fullPath);
        
        // Handle regular authenticated chat
        if (pathBase == "/api/gotgpt/chat" || fullPath.Contains("/api/gotgpt/chat"))
        {
            await HandleAuthenticatedChatAsync(context);
            return;
        }
        // Handle voice chat
        else if (pathBase == "/api/godgpt/voice/chat" || fullPath.Contains("/api/godgpt/voice/chat"))
        {
            await HandleVoiceChatAsync(context);
            return;
        }
        // Handle guest (anonymous) chat
        else if (pathBase == "/api/godgpt/guest/chat" || fullPath.Contains("/api/godgpt/guest/chat"))
        {
            await HandleGuestChatAsync(context);
            return;
        }
        else
        {
            await _next(context);
        }
    }

    private async Task HandleAuthenticatedChatAsync(HttpContext context)
    {
        if (context.User?.Identity == null || !context.User.Identity.IsAuthenticated)
        {
            _logger.LogDebug("[GodGPTController][ChatWithSessionAsync] Unauthorized: User is not authenticated");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized: User is not authenticated.");
            await context.Response.Body.FlushAsync();
            return;
        }
        
        var userIdStr = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdStr.IsNullOrWhiteSpace() || !Guid.TryParse(userIdStr, out var userId))
        {
            _logger.LogDebug("[GodGPTController][ChatWithSessionAsync] Unauthorized: Unable to retrieve UserId.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized: Unable to retrieve UserId.");
            await context.Response.Body.FlushAsync();
            return;
        }

        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var request = JsonConvert.DeserializeObject<QuantumChatRequestDto>(body);
        if (!request.Images.IsNullOrEmpty() && request.Images.Count > MaxImageCount)
        {
            _logger.LogDebug("[GodGPTController][ChatWithSessionAsync] {0} Too many files. {1}", userId, request.Images.Count);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync($"Too many files. Maximum {MaxImageCount} images per upload.");
            await context.Response.Body.FlushAsync();
            return;
        }

        userId = request.UserId;
        try
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug(
                $"[GodGPTController][ChatWithSessionAsync] http start:{request.SessionId}, userId {userId}");

            var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
            if (!await manager.IsUserSessionAsync(request.SessionId))
            {
                // TODO: remove this for pressure test
                // _logger.LogError("[GodGPTController][ChatWithSessionAsync] sessionInfoIsNull sessionId={A}",
                    // request.SessionId);
                // context.Response.StatusCode = StatusCodes.Status400BadRequest;
                // await context.Response.WriteAsync($"Unable to load conversation {request.SessionId}");
                // await context.Response.Body.FlushAsync();
                // return;
            }

            var streamProvider = _clusterClient.GetStreamProvider("Aevatar");
            var streamId = StreamId.Create(_aevatarOptions.Value.StreamNamespace, request.SessionId);
            _logger.LogDebug(
                $"[GodGPTController][ChatWithSessionAsync] sessionId {request.SessionId}, namespace {_aevatarOptions.Value.StreamNamespace}, streamId {streamId.ToString()}");
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.Connection = "keep-alive";
            context.Response.Headers.CacheControl = "no-cache";
            var responseStream = streamProvider.GetStream<ResponseStreamGodChat>(streamId);
            var godChat = _clusterClient.GetGrain<IGodChat>(request.SessionId);

            var chatId = Guid.NewGuid().ToString();
            await godChat.StreamChatWithSessionAsync(request.SessionId, string.Empty, request.Content,
                chatId, null, true, request.Region, request.Images);
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
                    await context.Response.StartAsync();
                    firstFlag = true;
                    _logger.LogDebug(
                        $"[GodGPTController][ChatWithSessionAsync] SubscribeAsync get first message:{request.SessionId}, duration: {stopwatch.ElapsedMilliseconds}ms");
                }

                var responseData = $"data: {JsonConvert.SerializeObject(chatResponse.ConvertToHttpResponse())}\n\n";
                await context.Response.WriteAsync(responseData);
                await context.Response.Body.FlushAsync();

                if (chatResponse.IsLastChunk)
                {
                    await context.Response.WriteAsync("event: completed\n");
                    context.Response.Body.Close();
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
                await exitSignal.Task.WaitAsync(context.RequestAborted);
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
                _logger.LogDebug(
                    $"[GodGPTController][ChatWithSessionAsync] No LastChunk:{request.SessionId},chatId:{chatId}");
            }

            _logger.LogDebug(
                $"[GodGPTController][ChatWithSessionAsync] complete done sessionId:{request.SessionId}");
        }
        catch (InvalidOperationException e)
        {
            var statusCode = StatusCodes.Status500InternalServerError;
            if (e.Data.Contains("Code") && int.TryParse((string)e.Data["Code"], out var code))
            {
                if (code == ExecuteActionStatus.InsufficientCredits)
                {
                    statusCode = StatusCodes.Status402PaymentRequired;
                } else if (code == ExecuteActionStatus.RateLimitExceeded)
                {
                    statusCode = StatusCodes.Status429TooManyRequests;
                }
            }
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsync(e.Message);
            await context.Response.Body.FlushAsync();
            _logger.LogDebug("[GodGPTController][ChatWithSessionAsync] {0}", e.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in SSE stream: {ex.Message}");
        }
    }

    private async Task HandleGuestChatAsync(HttpContext context)
    {
        var userHashId = "";
        try
        {
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<GuestChatRequestDto>(body);
            var clientIp = request.Ip.IsNullOrWhiteSpace() ? context.GetClientIpAddress() : request.Ip;

            userHashId = CommonHelper.GetAnonymousUserGAgentId(clientIp).Replace("AnonymousUser_", "");
            _logger.LogDebug($"[GuestChatMiddleware] Processing request for user: {userHashId} ip:{clientIp}");
            if (request == null || string.IsNullOrWhiteSpace(request.Content))
            {
                _logger.LogWarning($"[GuestChatMiddleware] Invalid request body for user: {userHashId}");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid request body");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug($"[GuestChatMiddleware] Start processing guest chat for user: {userHashId}");

            // Get or create anonymous user grain for this IP
            var grainId = CommonHelper.StringToGuid(CommonHelper.GetAnonymousUserGAgentId(clientIp));
            var anonymousUserGrain = _clusterClient.GetGrain<IAnonymousUserGAgent>(grainId);
            
            // Check if user can still chat
            if (!await anonymousUserGrain.CanChatAsync())
            {
                _logger.LogWarning($"[GuestChatMiddleware] Chat limit exceeded for user: {userHashId}");
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Daily chat limit exceeded");
                return;
            }

            // Get current session
            var sessionInfo = await anonymousUserGrain.GetCurrentSessionAsync();
            if (sessionInfo == null)
            {
                _logger.LogWarning($"[GuestChatMiddleware] No active session for user: {userHashId}");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("No active guest session. Please create a session first.");
                return;
            }

            var chatId = Guid.NewGuid().ToString();
            var sessionId = sessionInfo.SessionId;

            _logger.LogDebug($"[GuestChatMiddleware] Found session {sessionId} for user: {userHashId}");

            // Set up SSE response headers
            var streamProvider = _clusterClient.GetStreamProvider("Aevatar");
            var streamId = StreamId.Create(_aevatarOptions.Value.StreamNamespace, sessionId);
            _logger.LogDebug($"[GuestChatMiddleware] sessionId {sessionId}, namespace {_aevatarOptions.Value.StreamNamespace}, streamId {streamId.ToString()}");
            
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.Connection = "keep-alive";
            context.Response.Headers.CacheControl = "no-cache";
            
            var responseStream = streamProvider.GetStream<ResponseStreamGodChat>(streamId);

            // Execute chat through grain (this will trigger the stream)
            await anonymousUserGrain.GuestChatAsync(request.Content, chatId);
            _logger.LogDebug($"[GuestChatMiddleware] Guest chat executed for user: {userHashId}, ChatId: {chatId}");

            // Handle streaming response
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

                if (!firstFlag)
                {
                    await context.Response.StartAsync();
                    firstFlag = true;
                    _logger.LogDebug($"[GuestChatMiddleware] First message received for user: {userHashId}, duration: { stopwatch.ElapsedMilliseconds}ms");
                }

                var responseData = $"data: {JsonConvert.SerializeObject(chatResponse.ConvertToHttpResponse())}\n\n";
                await context.Response.WriteAsync(responseData);
                await context.Response.Body.FlushAsync();

                if (chatResponse.IsLastChunk)
                {
                    await context.Response.WriteAsync("event: completed\n");
                    context.Response.Body.Close();
                    ifLastChunk = true;
                    exitSignal.TrySetResult();
                    if (subscription != null)
                    {
                        await subscription.UnsubscribeAsync();
                    }
                }
            }, ex =>
            {
                _logger.LogError($"[GuestChatMiddleware] Stream error for user: {userHashId}, ChatId: {chatId}, Error: {ex.Message}");
                exitSignal.TrySetException(ex);
                return Task.CompletedTask;
            }, () =>
            {
                _logger.LogDebug($"[GuestChatMiddleware] Stream completed for user: {userHashId}");
                exitSignal.TrySetResult();
                return Task.CompletedTask;
            });

            try
            {
                await exitSignal.Task.WaitAsync(context.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[GuestChatMiddleware] Error waiting for stream completion: {ex.Message}");
            }
            finally
            {
                if (subscription != null)
                {
                    await subscription.UnsubscribeAsync();
                }
            }

            if (!ifLastChunk)
            {
                _logger.LogDebug($"[GuestChatMiddleware] No LastChunk received for user: {userHashId}, ChatId: {chatId}");
            }

            _logger.LogDebug($"[GuestChatMiddleware] Completed guest chat for user: {userHashId}, duration: {stopwatch.ElapsedMilliseconds}ms");
        }
        catch (InvalidOperationException ex)
        {
            // Handle guest chat specific errors with appropriate status codes
            var statusCode = StatusCodes.Status400BadRequest;
            
            if (ex.Message.Contains("Daily chat limit exceeded"))
            {
                statusCode = StatusCodes.Status429TooManyRequests;
            }
            else if (ex.Message.Contains("No active guest session"))
            {
                statusCode = StatusCodes.Status400BadRequest;
            }
            // Handle specific business logic errors that might contain error codes
            else if (ex.Data.Contains("Code") && int.TryParse(ex.Data["Code"]?.ToString(), out var code))
            {
                if (code == ExecuteActionStatus.InsufficientCredits)
                {
                    statusCode = StatusCodes.Status402PaymentRequired;
                } 
                else if (code == ExecuteActionStatus.RateLimitExceeded)
                {
                    statusCode = StatusCodes.Status429TooManyRequests;
                }
                // Ensure we never use business error codes as HTTP status codes
                else if (code >= 10000) // Business error codes are typically large numbers
                {
                    statusCode = StatusCodes.Status400BadRequest;
                    _logger.LogWarning($"[GuestChatMiddleware] Business error code {code} converted to 400 for user: {userHashId}");
                }
            }
            
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsync(ex.Message);
            _logger.LogWarning($"[GuestChatMiddleware] Operation error for user: {userHashId}, status: {statusCode} error:{ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[GuestChatMiddleware] Unexpected error for user: {userHashId} ex:{ex.Message}");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Internal server error");
        }
    }

    /// <summary>
    /// Handle voice chat requests with streaming SSE response
    /// </summary>
    /// <param name="context">HTTP context</param>
    private async Task HandleVoiceChatAsync(HttpContext context)
    {
        // Check user authentication
        if (context.User?.Identity == null || !context.User.Identity.IsAuthenticated)
        {
            _logger.LogDebug("[VoiceChatMiddleware] Unauthorized: User is not authenticated");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized: User is not authenticated.");
            await context.Response.Body.FlushAsync();
            return;
        }
        
        // Extract user ID from claims
        var userIdStr = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdStr.IsNullOrWhiteSpace() || !Guid.TryParse(userIdStr, out var userId))
        {
            _logger.LogDebug("[VoiceChatMiddleware] Unauthorized: Unable to retrieve UserId.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized: Unable to retrieve UserId.");
            await context.Response.Body.FlushAsync();
            return;
        }

        // Parse request body
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var request = JsonConvert.DeserializeObject<VoiceChatRequestDto>(body);
        
        if (request == null || string.IsNullOrWhiteSpace(request.Content))
        {
            _logger.LogWarning($"[VoiceChatMiddleware] Invalid request body for user: {userId}");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid request body");
            return;
        }
        if (request.VoiceLanguage == VoiceLanguageEnum.Unset)
        {
            _logger.LogWarning($"[VoiceChatMiddleware] unset language userId: {userId} language:{request.VoiceLanguage}");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Unset language request body");
            return;
        }

        try
        {
            userId = request.UserId;
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug($"[VoiceChatMiddleware] HTTP start - SessionId: {request.SessionId}, UserId: {userId}, MessageType: {request.MessageType}, VoiceLanguage: {request.VoiceLanguage}");

            // Validate session access
            var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
            if (!await manager.IsUserSessionAsync(request.SessionId))
            {
                _logger.LogError($"[VoiceChatMiddleware] Session not found or access denied - SessionId: {request.SessionId}, UserId: {userId}");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync($"Unable to load conversation {request.SessionId}");
                await context.Response.Body.FlushAsync();
                return;
            }

            // Set up streaming infrastructure
            var streamProvider = _clusterClient.GetStreamProvider("Aevatar");
            var streamId = StreamId.Create(_aevatarOptions.Value.StreamNamespace, request.SessionId);
            _logger.LogDebug($"[VoiceChatMiddleware] SessionId: {request.SessionId}, Namespace: {_aevatarOptions.Value.StreamNamespace}, StreamId: {streamId.ToString()}");

            // Set SSE response headers
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.Connection = "keep-alive";
            context.Response.Headers.CacheControl = "no-cache";
            
            var responseStream = streamProvider.GetStream<ResponseStreamGodChat>(streamId);
            var godChat = _clusterClient.GetGrain<IGodChat>(request.SessionId);

            // Generate unique chat ID and initiate voice chat
            var chatId = Guid.NewGuid().ToString();
            await godChat.StreamVoiceChatWithSessionAsync(request.SessionId, string.Empty, request.Content, "",
                chatId, null, true, request.Region, request.VoiceLanguage, request.VoiceDurationSeconds);
            _logger.LogDebug($"[VoiceChatMiddleware] Voice chat initiated - SessionId: {request.SessionId}, ChatId: {chatId} Duration: {stopwatch.ElapsedMilliseconds}ms");

            // Handle streaming response
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

                if (!firstFlag)
                {
                    await context.Response.StartAsync();
                    firstFlag = true;
                    _logger.LogDebug($"[VoiceChatMiddleware] First message received - SessionId: {request.SessionId}, Duration: {stopwatch.ElapsedMilliseconds}ms");
                }

                var responseData = $"data: {JsonConvert.SerializeObject(chatResponse.ConvertToHttpResponse())}\n\n";
                await context.Response.WriteAsync(responseData);
                await context.Response.Body.FlushAsync();

                if (chatResponse.IsLastChunk)
                {
                    await context.Response.WriteAsync("event: completed\n");
                    context.Response.Body.Close();
                    ifLastChunk = true;
                    exitSignal.TrySetResult();
                    if (subscription != null)
                    {
                        await subscription.UnsubscribeAsync();
                    }
                }
            }, ex =>
            {
                _logger.LogError($"[VoiceChatMiddleware] Stream error - SessionId: {request.SessionId}, ChatId: {chatId}, Error: {ex.Message}");
                exitSignal.TrySetException(ex);
                return Task.CompletedTask;
            }, () =>
            {
                _logger.LogDebug($"[VoiceChatMiddleware] Stream completed - SessionId: {request.SessionId}");
                exitSignal.TrySetResult();
                return Task.CompletedTask;
            });

            try
            {
                await exitSignal.Task.WaitAsync(context.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[VoiceChatMiddleware] Error waiting for stream completion - SessionId: {request.SessionId}, Error: {ex.Message}");
            }
            finally
            {
                if (subscription != null)
                {
                    await subscription.UnsubscribeAsync();
                }
            }

            if (!ifLastChunk)
            {
                _logger.LogDebug($"[VoiceChatMiddleware] No LastChunk received - SessionId: {request.SessionId}, ChatId: {chatId}");
            }

            _logger.LogDebug($"[VoiceChatMiddleware] Voice chat completed - SessionId: {request.SessionId}, Duration: {stopwatch.ElapsedMilliseconds}ms");
        }
        catch (InvalidOperationException ex)
        {
            // Handle voice chat specific errors with appropriate status codes
            var statusCode = StatusCodes.Status500InternalServerError;
            if (ex.Data.Contains("Code") && int.TryParse(ex.Data["Code"]?.ToString(), out var code))
            {
                if (code == ExecuteActionStatus.InsufficientCredits)
                {
                    statusCode = StatusCodes.Status402PaymentRequired;
                } 
                else if (code == ExecuteActionStatus.RateLimitExceeded)
                {
                    statusCode = StatusCodes.Status429TooManyRequests;
                }
                // Ensure we never use business error codes as HTTP status codes
                else if (code >= 10000) // Business error codes are typically large numbers
                {
                    statusCode = StatusCodes.Status400BadRequest;
                    _logger.LogWarning($"[VoiceChatMiddleware] Business error code {code} converted to 400 for SessionId: {request.SessionId}");
                }
            }
            
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsync(ex.Message);
            _logger.LogWarning(ex, "[VoiceChatMiddleware] Operation error - SessionId: {0}, Status: {1}", request.SessionId, statusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VoiceChatMiddleware] Unexpected error - SessionId: {0}", request.SessionId);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Internal server error");
        }
    }
}