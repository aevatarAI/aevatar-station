using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Anonymous;
using Aevatar.Application.Grains.Agents.Anonymous;
using Aevatar.Application.Grains.Agents.ChatManager;
using Aevatar.Application.Grains.Agents.ChatManager.Chat;
using Aevatar.Application.Grains.Agents.ChatManager.Common;
using Aevatar.Application.Grains.Agents.ChatManager.ConfigAgent;
using Aevatar.Application.Grains.Agents.ChatManager.Dtos;
using Aevatar.Application.Grains.ChatManager.Dtos;
using Aevatar.Application.Grains.ChatManager.UserBilling;
using Aevatar.Application.Grains.ChatManager.UserQuota;
using Aevatar.Application.Grains.Common.Constants;
using Aevatar.Application.Grains.Common.Options;
using Aevatar.Application.Grains.Invitation;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GodGPT.Dtos;
using Aevatar.Quantum;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Orleans;
using Stripe;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Service;

public interface IGodGPTService
{
    Task<Guid> CreateSessionAsync(Guid userId, string systemLLM, string prompt, string? guider = null);

    Task<Tuple<string, string>> ChatWithSessionAsync(Guid userId, Guid sessionId, string sysmLLM, string content,
        ExecutionPromptSettings promptSettings = null);

    Task<List<SessionInfoDto>> GetSessionListAsync(Guid userId);
    Task<List<ChatMessage>> GetSessionMessageListAsync(Guid userId, Guid sessionId);
    Task<Aevatar.Quantum.SessionCreationInfoDto?> GetSessionCreationInfoAsync(Guid userId, Guid sessionId);
    Task<Guid> DeleteSessionAsync(Guid userId, Guid sessionId);
    Task<Guid> RenameSessionAsync(Guid userId, Guid sessionId, string title);
    Task<List<SessionInfoDto>> SearchSessionsAsync(Guid userId, string keyword);

    Task<string> GetSystemPromptAsync();
    Task UpdateSystemPromptAsync(GodGPTConfigurationDto godGptConfigurationDto);

    Task<UserProfileDto> GetUserProfileAsync(Guid currentUserId);
    Task<Guid> SetUserProfileAsync(Guid currentUserId, SetUserProfileInput userProfileDto);
    Task<Guid> DeleteAccountAsync(Guid currentUserId);
    Task<CreateShareIdResponse> GenerateShareContentAsync(Guid currentUserId, CreateShareIdRequest request);
    Task<List<ChatMessage>> GetShareMessageListAsync(string shareString);
    Task UpdateShowToastAsync(Guid currentUserId);
    Task<List<StripeProductDto>> GetStripeProductsAsync(Guid currentUserId);
    Task<string> CreateCheckoutSessionAsync(Guid currentUserId, CreateCheckoutSessionInput createCheckoutSessionInput);
    Task<string> ParseEventAndGetUserIdAsync(string json);
    Task<bool> HandleStripeWebhookEventAsync(Guid internalUserId, string json, StringValues stripeSignature);
    Task<List<PaymentSummary>> GetPaymentHistoryAsync(Guid currentUserId, GetPaymentHistoryInput input);
    Task<GetCustomerResponseDto> GetStripeCustomerAsync(Guid currentUserId);
    Task<SubscriptionResponseDto> CreateSubscriptionAsync(Guid currentUserId, CreateSubscriptionInput input);
    Task<CancelSubscriptionResponseDto> CancelSubscriptionAsync(Guid currentUserId, CancelSubscriptionInput input);
    Task<List<AppleProductDto>> GetAppleProductsAsync(Guid currentUserId);
    Task<AppStoreSubscriptionResponseDto> VerifyAppStoreReceiptAsync(Guid currentUserId, VerifyAppStoreReceiptInput input);
    Task<GrainResultDto<int>> UpdateUserCreditsAsync(Guid currentUserId, UpdateUserCreditsInput input);
    Task<bool> HasActiveAppleSubscriptionAsync(Guid currentUserId);
    Task<GetInvitationInfoResponse> GetInvitationInfoAsync(Guid currentUserId);
    Task<RedeemInviteCodeResponse> RedeemInviteCodeAsync(Guid currentUserId,
        RedeemInviteCodeRequest redeemInviteCodeRequest);
    Task<CreateGuestSessionResponseDto> CreateGuestSessionAsync(string clientIp, string? guider = null);
    Task GuestChatAsync(string clientIp, string content, string chatId);
    Task<GuestChatLimitsResponseDto> GetGuestChatLimitsAsync(string clientIp);
    Task<bool> CanGuestChatAsync(string clientIp);
    Task<QuantumShareResponseDto> GetShareKeyWordWithAIAsync(Guid sessionId, string? content, string? region, SessionType sessionType);
}

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class GodGPTService : ApplicationService, IGodGPTService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<GodGPTService> _logger;
    private readonly IOptionsMonitor<StripeOptions> _stripeOptions;

    private readonly StripeClient _stripeClient;

    public GodGPTService(IClusterClient clusterClient, ILogger<GodGPTService> logger, IOptionsMonitor<StripeOptions> stripeOptions)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _stripeOptions = stripeOptions;

        _stripeClient = new StripeClient(_stripeOptions.CurrentValue.SecretKey);
    }

    public async Task<Guid> CreateSessionAsync(Guid userId, string systemLLM, string prompt, string? guider = null)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
        return await manager.CreateSessionAsync(systemLLM, prompt, null, guider);
    }

    public async Task<Tuple<string, string>> ChatWithSessionAsync(Guid userId, Guid sessionId, string sysmLLM,
        string content,
        ExecutionPromptSettings promptSettings = null)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
        return await manager.ChatWithSessionAsync(sessionId, sysmLLM, content, promptSettings);
    }

    public async Task<List<SessionInfoDto>> GetSessionListAsync(Guid userId)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
        return await manager.GetSessionListAsync();
    }

    public async Task<List<ChatMessage>> GetSessionMessageListAsync(Guid userId, Guid sessionId)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
        return await manager.GetSessionMessageListAsync(sessionId);
    }

    public async Task<Aevatar.Quantum.SessionCreationInfoDto?> GetSessionCreationInfoAsync(Guid userId, Guid sessionId)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
        var grainsResult = await manager.GetSessionCreationInfoAsync(sessionId);
        
        if (grainsResult != null)
        {
            return new Aevatar.Quantum.SessionCreationInfoDto
            {
                SessionId = grainsResult.SessionId,
                Title = grainsResult.Title,
                CreateAt = grainsResult.CreateAt,
                Guider = grainsResult.Guider
            };
        }
        
        return null;
    }

    public async Task<Guid> DeleteSessionAsync(Guid userId, Guid sessionId)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
        return await manager.DeleteSessionAsync(sessionId);
    }

    public async Task<Guid> RenameSessionAsync(Guid userId, Guid sessionId, string title)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
        return await manager.RenameSessionAsync(sessionId, title);
    }

    public async Task<List<SessionInfoDto>> SearchSessionsAsync(Guid userId, string keyword)
    {
        // Input validation according to downstream team requirements
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return new List<SessionInfoDto>(); // Return empty list for empty keyword
        }

        // Length limit validation
        if (keyword.Length > 200)
        {
            _logger.LogWarning($"Search keyword too long: {keyword.Length} characters");
            return new List<SessionInfoDto>();
        }

        try
        {
            var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
            return await manager.SearchSessionsAsync(keyword.Trim(), 1000);
        }
        catch (Exception ex)
        {
            // Error handling according to downstream team requirements
            _logger.LogError(ex, $"Search sessions failed for keyword: {keyword}");
            return new List<SessionInfoDto>();
        }
    }

    public Task<string> GetSystemPromptAsync()
    {
        var configurationAgent =
            _clusterClient.GetGrain<IConfigurationGAgent>(CommonHelper.GetSessionManagerConfigurationId());
        return configurationAgent.GetPrompt();
    }

    public Task UpdateSystemPromptAsync(GodGPTConfigurationDto godGptConfigurationDto)
    {
        var configurationAgent =
            _clusterClient.GetGrain<IConfigurationGAgent>(CommonHelper.GetSessionManagerConfigurationId());
        return configurationAgent.UpdateSystemPromptAsync(godGptConfigurationDto.SystemPrompt);
    }

    public async Task<UserProfileDto> GetUserProfileAsync(Guid currentUserId)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(currentUserId);
        return await manager.GetUserProfileAsync();
    }

    public async Task<Guid> SetUserProfileAsync(Guid currentUserId, SetUserProfileInput userProfileDto)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(currentUserId);
        return await manager.SetUserProfileAsync(userProfileDto.Gender, userProfileDto.BirthDate,
            userProfileDto.BirthPlace, userProfileDto.FullName);
    }

    public async Task<Guid> DeleteAccountAsync(Guid currentUserId)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(currentUserId);
        return await manager.ClearAllAsync();
    }

    public async Task<CreateShareIdResponse> GenerateShareContentAsync(Guid currentUserId, CreateShareIdRequest request)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(currentUserId);
        var shareId = await manager.GenerateChatShareContentAsync(request.SessionId);
        return new CreateShareIdResponse
        {
            ShareId = GuidCompressor.CompressGuids(currentUserId, request.SessionId, shareId)
        };
    }

    public async Task<List<ChatMessage>> GetShareMessageListAsync(string shareString)
    {
        if (shareString.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("Invalid Share string");
        }

        Guid userId;
        Guid sessionId;
        Guid shareId;
        try
        {
            (userId, sessionId, shareId) = GuidCompressor.DecompressGuids(shareString);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Invalid Share string. {0}", shareString);
            throw new UserFriendlyException("Invalid Share string");
        }

        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
        var shareLinkDto = await manager.GetChatShareContentAsync(sessionId, shareId);
        return shareLinkDto.Messages;
    }

    public async Task UpdateShowToastAsync(Guid currentUserId)
    {
        var userQuotaGrain = _clusterClient.GetGrain<IUserQuotaGrain>(CommonHelper.GetUserQuotaGAgentId(currentUserId));
        await userQuotaGrain.SetShownCreditsToastAsync(true);
    }

    public async Task<List<StripeProductDto>> GetStripeProductsAsync(Guid currentUserId)
    {
        var userBillingGrain =
            _clusterClient.GetGrain<IUserBillingGrain>(CommonHelper.GetUserBillingGAgentId(currentUserId));
        return await userBillingGrain.GetStripeProductsAsync();
    }

    public async Task<string> CreateCheckoutSessionAsync(Guid currentUserId,
        CreateCheckoutSessionInput createCheckoutSessionInput)
    {
        var userBillingGrain =
            _clusterClient.GetGrain<IUserBillingGrain>(CommonHelper.GetUserBillingGAgentId(currentUserId));
        var result = await userBillingGrain.CreateCheckoutSessionAsync(new CreateCheckoutSessionDto
        {
            UserId = currentUserId.ToString(),
            PriceId = createCheckoutSessionInput.PriceId,
            Mode = createCheckoutSessionInput.Mode ?? PaymentMode.SUBSCRIPTION,
            Quantity = createCheckoutSessionInput.Quantity <= 0 ? 1 : createCheckoutSessionInput.Quantity,
            UiMode = createCheckoutSessionInput.UiMode ?? StripeUiMode.HOSTED,
            CancelUrl = createCheckoutSessionInput.CancelUrl
        });
        return result;
    }

    public async Task<string> ParseEventAndGetUserIdAsync(string json)
    {
        var stripeEvent = EventUtility.ParseEvent(json);
        _logger.LogInformation("[GodGPTPaymentController][webhook] Type: {0}", stripeEvent.Type);
        if (stripeEvent.Type == "checkout.session.completed")
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            var b = session.Metadata;
            if (TryGetUserIdFromMetadata(session.Metadata, out  var userId))
            {
                _logger.LogDebug("[GodGPTService][ParseEventAndGetUserIdAsync] Type={0}, UserId={1}",stripeEvent.Type, userId);
                return userId;
            }
            _logger.LogWarning("[GodGPTService][ParseEventAndGetUserIdAsync] Type={0}, not found uerid",stripeEvent.Type);
        }
        // else if (stripeEvent.Type == "invoice.payment_succeeded")
        // {
        //     var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        //     if (TryGetUserIdFromMetadata(invoice?.Parent?.SubscriptionDetails?.Metadata, out  var userId))
        //     {
        //         return userId;
        //     }
        // } 
        else if (stripeEvent.Type is "invoice.paid" or "invoice.payment_failed")
        {
            var invoice = stripeEvent.Data.Object as Stripe.Invoice;
            if (TryGetUserIdFromMetadata(invoice?.Parent?.SubscriptionDetails?.Metadata, out  var userId))
            {
                _logger.LogDebug("[GodGPTService][ParseEventAndGetUserIdAsync] Type={0}, UserId={1}",stripeEvent.Type, userId);
                return userId;
            }
            _logger.LogWarning("[GodGPTService][ParseEventAndGetUserIdAsync] Type={0}, not found uerid",stripeEvent.Type);
        }
        // else if (stripeEvent.Type == "invoice.payment_failed")
        // {
        //     var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        //     if (TryGetUserIdFromMetadata(invoice.Metadata, out  var userId))
        //     {
        //         return userId;
        //     }
        // }
        // else if (stripeEvent.Type == "payment_intent.succeeded")
        // {
        //     var paymentIntent = stripeEvent.Data.Object as Stripe.PaymentIntent;
        //     if (TryGetUserIdFromMetadata(paymentIntent.Metadata, out  var userId))
        //     {
        //         return userId;
        //     }
        // }
        else if (stripeEvent.Type is "customer.subscription.deleted" or "customer.subscription.updated")
        {
            var subscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (TryGetUserIdFromMetadata(subscription.Metadata, out  var userId))
            {
                return userId;
            }
        }
        else if (stripeEvent.Type == "charge.refunded")
        {
            var charge = stripeEvent.Data.Object as Stripe.Charge;
            var paymentIntentService = new PaymentIntentService(_stripeClient);
            var paymentIntent = paymentIntentService.Get(charge.PaymentIntentId);
            if (TryGetUserIdFromMetadata(paymentIntent.Metadata, out  var userId))
            {
                return userId;
            }
        }

        return string.Empty;
    }

    public async Task<bool> HandleStripeWebhookEventAsync(Guid internalUserId, string json, StringValues stripeSignature)
    {
        var userBillingGrain =
            _clusterClient.GetGrain<IUserBillingGrain>(CommonHelper.GetUserBillingGAgentId(internalUserId));
        return await userBillingGrain.HandleStripeWebhookEventAsync(json, stripeSignature);
    }

    public async Task<List<PaymentSummary>> GetPaymentHistoryAsync(Guid currentUserId, GetPaymentHistoryInput input)
    {
        var userBillingGrain =
            _clusterClient.GetGrain<IUserBillingGrain>(CommonHelper.GetUserBillingGAgentId(currentUserId));
        return await userBillingGrain.GetPaymentHistoryAsync(input.Page, input.PageSize);
    }

    public async Task<GetCustomerResponseDto> GetStripeCustomerAsync(Guid currentUserId)
    {
        var userBillingGrain =
            _clusterClient.GetGrain<IUserBillingGrain>(CommonHelper.GetUserBillingGAgentId(currentUserId));
        return await userBillingGrain.GetStripeCustomerAsync(currentUserId.ToString());
    }

    public async Task<SubscriptionResponseDto> CreateSubscriptionAsync(Guid currentUserId, CreateSubscriptionInput input)
    {
        var userBillingGrain =
            _clusterClient.GetGrain<IUserBillingGrain>(CommonHelper.GetUserBillingGAgentId(currentUserId));
        return await userBillingGrain.CreateSubscriptionAsync(new CreateSubscriptionDto
        {
            UserId = currentUserId,
            PriceId = input.PriceId,
            Quantity = input.Quantity,
            PaymentMethodId = input.PaymentMethodId,
            Description = input.Description,
            Metadata = input.Metadata,
            TrialPeriodDays = input.TrialPeriodDays,
            Platform = input.DevicePlatform
        });
    }

    public async Task<CancelSubscriptionResponseDto> CancelSubscriptionAsync(Guid currentUserId, CancelSubscriptionInput input)
    {
        var userBillingGrain =
            _clusterClient.GetGrain<IUserBillingGrain>(CommonHelper.GetUserBillingGAgentId(currentUserId));
        return await userBillingGrain.CancelSubscriptionAsync(new CancelSubscriptionDto
        {
            UserId = currentUserId,
            SubscriptionId = input.SubscriptionId,
            CancellationReason = string.Empty,
            CancelAtPeriodEnd = true
        });
    }

    public async Task<List<AppleProductDto>> GetAppleProductsAsync(Guid currentUserId)
    {
        var userBillingGrain =
            _clusterClient.GetGrain<IUserBillingGrain>(CommonHelper.GetUserBillingGAgentId(currentUserId));
        return await userBillingGrain.GetAppleProductsAsync();
    }

    public async Task<AppStoreSubscriptionResponseDto> VerifyAppStoreReceiptAsync(Guid currentUserId, VerifyAppStoreReceiptInput input)
    {
        var userBillingGrain =
            _clusterClient.GetGrain<IUserBillingGrain>(CommonHelper.GetUserBillingGAgentId(currentUserId));
        return await userBillingGrain.CreateAppStoreSubscriptionAsync(new CreateAppStoreSubscriptionDto
        {
            UserId = currentUserId.ToString(),
            SandboxMode = input.SandboxMode,
            TransactionId = input.TransactionId
        });
    }

    public async Task<GrainResultDto<int>> UpdateUserCreditsAsync(Guid currentUserId, UpdateUserCreditsInput input)
    {
        var userQuotaGrain =
            _clusterClient.GetGrain<IUserQuotaGrain>(CommonHelper.GetUserQuotaGAgentId(input.UserId));
        return await userQuotaGrain.UpdateCreditsAsync(currentUserId.ToString(), input.Credits);
    }

    public async Task<bool> HasActiveAppleSubscriptionAsync(Guid currentUserId)
    {
        var userBillingGrain =
            _clusterClient.GetGrain<IUserBillingGrain>(CommonHelper.GetUserBillingGAgentId(currentUserId));
        return await userBillingGrain.HasActiveAppleSubscriptionAsync();
    }

    public async Task<GetInvitationInfoResponse> GetInvitationInfoAsync(Guid currentUserId)
    {
        var invitationAgent =  _clusterClient.GetGrain<IInvitationGAgent>(currentUserId);
        var inviteCode = await invitationAgent.GenerateInviteCodeAsync();
        var invitationStatsDto = await invitationAgent.GetInvitationStatsAsync();
        var rewardTierDtos = await invitationAgent.GetRewardTiersAsync();
        return new GetInvitationInfoResponse
        {
            InviteCode = inviteCode,
            TotalInvites = invitationStatsDto.TotalInvites,
            ValidInvites = invitationStatsDto.ValidInvites,
            TotalCreditsEarned = invitationStatsDto.TotalCreditsEarned,
            RewardTiers = rewardTierDtos
        };
    }

    public async Task<RedeemInviteCodeResponse> RedeemInviteCodeAsync(Guid currentUserId,
        RedeemInviteCodeRequest input)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(currentUserId);
        var result = await manager.RedeemInviteCodeAsync(input.InviteCode);
        return new RedeemInviteCodeResponse
        {
            IsValid = result
        };
    }

    #region Anonymous User Methods

    /// <summary>
    /// Create guest session for anonymous users (IP-based)
    /// </summary>
    public async Task<CreateGuestSessionResponseDto> CreateGuestSessionAsync(string clientIp, string? guider = null)
    {
        var anonymousUserGrain = _clusterClient.GetGrain<IAnonymousUserGAgent>(CommonHelper.GetAnonymousUserGAgentId(clientIp));
        
        // Check if user can still chat
        if (!await anonymousUserGrain.CanChatAsync())
        {
            var remainingChats = await anonymousUserGrain.GetRemainingChatsAsync();
            return new CreateGuestSessionResponseDto
            {
                RemainingChats = remainingChats,
                TotalAllowed = await GetMaxChatCountAsync()
            };
        }

        // Create new session (this will replace any existing session for the IP)
        await anonymousUserGrain.CreateGuestSessionAsync(guider);
        
        var remaining = await anonymousUserGrain.GetRemainingChatsAsync();
        return new CreateGuestSessionResponseDto
        {
            RemainingChats = remaining,
            TotalAllowed = await GetMaxChatCountAsync()
        };
    }

    /// <summary>
    /// Execute guest chat for anonymous users
    /// </summary>
    public async Task GuestChatAsync(string clientIp, string content, string chatId)
    {
        var anonymousUserGrain = _clusterClient.GetGrain<IAnonymousUserGAgent>(CommonHelper.GetAnonymousUserGAgentId(clientIp));
        await anonymousUserGrain.GuestChatAsync(content, chatId);
    }

    /// <summary>
    /// Get chat limits for anonymous users
    /// </summary>
    public async Task<GuestChatLimitsResponseDto> GetGuestChatLimitsAsync(string clientIp)
    {
        var anonymousUserGrain = _clusterClient.GetGrain<IAnonymousUserGAgent>(CommonHelper.GetAnonymousUserGAgentId(clientIp));
        var remaining = await anonymousUserGrain.GetRemainingChatsAsync();
        
        return new GuestChatLimitsResponseDto
        {
            RemainingChats = remaining,
            TotalAllowed = await GetMaxChatCountAsync()
        };
    }

    /// <summary>
    /// Check if anonymous user can chat
    /// </summary>
    public async Task<bool> CanGuestChatAsync(string clientIp)
    {
        var anonymousUserGrain = _clusterClient.GetGrain<IAnonymousUserGAgent>(CommonHelper.GetAnonymousUserGAgentId(clientIp));
        return await anonymousUserGrain.CanChatAsync();
    }

    #endregion

    /// <summary>
    /// Get max chat count from AnonymousUserGAgent configuration, default to 3 if unable to retrieve
    /// </summary>
    private async Task<int> GetMaxChatCountAsync()
    {
        try
        {
            // Use a dummy IP to get configuration from AnonymousUserGAgent
            var configGrain = _clusterClient.GetGrain<IAnonymousUserGAgent>("AnonymousUser_config");
            return await configGrain.GetMaxChatCountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get max chat count from configuration, using default: 3");
            return 3;
        }
    }

    public async Task<QuantumShareResponseDto> GetShareKeyWordWithAIAsync(Guid sessionId, string? content, string? region, SessionType sessionType)
    {
        _logger.LogDebug($"[GodGPTService][GetShareKeyWordWithAIAsync] http start: sessionId={sessionId}, sessionType={sessionType}");
        var responseContent = "";
        try
        {
            var godChat = _clusterClient.GetGrain<IGodChat>(sessionId);
            var chatId = Guid.NewGuid().ToString();
            var response = await godChat.ChatWithHistory(sessionId, string.Empty, SessionTypeExtensions.SharePrompt,
                chatId, null, true, region);
            responseContent = response.IsNullOrEmpty() ? sessionType.GetDefaultContent() : response.FirstOrDefault().Content;
            _logger.LogDebug(
                $"[GodGPTService][GetShareKeyWordWithAIAsync] completed for sessionId={sessionId}, responseContent:{responseContent}");
        }
        catch (Exception ex)
        {
            responseContent = sessionType.GetDefaultContent();
            _logger.LogError(ex, $"[GodGPTService][GetShareKeyWordWithAIAsync] error for sessionId={sessionId}, sessionType={sessionType}");
        }

        return new QuantumShareResponseDto()
        {
            Success = true,
            Content = responseContent,
        };
    }

    private bool TryGetUserIdFromMetadata(IDictionary<string, string> metadata, out string userId)
    {
        userId = null;
        if (metadata != null && metadata.TryGetValue("internal_user_id", out var id) && !string.IsNullOrEmpty(id))
        {
            userId = id;
            return true;
        }
        return false;
    }
}

public static class GuidCompressor
{
    public static string CompressGuids(Guid guid1, Guid guid2, Guid guid3)
    {
        var combinedBytes = CombineBytes(guid1.ToByteArray(), guid2.ToByteArray());
        combinedBytes = CombineBytes(combinedBytes, guid3.ToByteArray());

        var base64 = Convert.ToBase64String(combinedBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
        return base64;
    }

    public static (Guid, Guid, Guid) DecompressGuids(string compressedString)
    {
        var restoredBase64 = compressedString
            .Replace('-', '+')
            .Replace('_', '/')
            .PadRight(compressedString.Length + (4 - compressedString.Length % 4) % 4, '=');

        var bytes = Convert.FromBase64String(restoredBase64);

        var guid1Bytes = new byte[16];
        var guid2Bytes = new byte[16];
        var guid3Bytes = new byte[16];
        Array.Copy(bytes, 0, guid1Bytes, 0, 16);
        Array.Copy(bytes, 16, guid2Bytes, 0, 16);
        Array.Copy(bytes, 32, guid3Bytes, 0, 16);

        return (new Guid(guid1Bytes), new Guid(guid2Bytes), new Guid(guid3Bytes));
    }

    private static byte[] CombineBytes(byte[] bytes1, byte[] bytes2)
    {
        var combined = new byte[bytes1.Length + bytes2.Length];
        Buffer.BlockCopy(bytes1, 0, combined, 0, bytes1.Length);
        Buffer.BlockCopy(bytes2, 0, combined, bytes1.Length, bytes2.Length);
        return combined;
    }
}