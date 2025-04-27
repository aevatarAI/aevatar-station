using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.ChatManager;
using Aevatar.Application.Grains.Agents.ChatManager.Chat;
using Aevatar.Core.Abstractions;
using Aevatar.Quantum;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aevatar.Handler;

public class ChatMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<ChatMiddleware> _logger;
    private readonly IOptions<AevatarOptions> _aevatarOptions;

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
        if (context.Request.PathBase == "/api/gotgpt/chat")
        {
            if (context.User?.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: User is not authenticated.");
                return;
            }
            
            var userIdStr = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr.IsNullOrWhiteSpace() || !Guid.TryParse(userIdStr, out var userId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: Unable to retrieve UserId.");
                return;
            }

            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<QuantumChatRequestDto>(body);
            try
            {
                var stopwatch = Stopwatch.StartNew();
                _logger.LogDebug($"[GodGPTController][ChatWithSessionAsync] http start:{request.SessionId}, userId {userId}");
                
                var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
                if (!await manager.IsUserSessionAsync(request.SessionId))
                {
                    _logger.LogError("[GodGPTController][ChatWithSessionAsync] sessionInfoIsNull sessionId={A}", request.SessionId);
                    await context.Response.WriteAsync("Unauthorized: Unable to retrieve UserId.");
                    return;
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
            catch (Exception ex)
            {
                _logger.LogError($"Error in SSE stream: {ex.Message}");
            }
        }
        else
        {
            await _next(context);
        }
    }
}