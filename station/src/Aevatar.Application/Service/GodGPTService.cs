using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Anonymous;
using Aevatar.Application.Constants;
using Aevatar.Application.Contracts.Services;
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
using Aevatar.Application.Grains.Twitter;
using Aevatar.Application.Grains.Twitter.Dtos;
using Aevatar.Application.Grains.TwitterInteraction;
using Aevatar.Application.Grains.TwitterInteraction.Dtos;
using Aevatar.Application.Grains.UserBilling;
using Aevatar.Application.Grains.UserQuota;
using Aevatar.Common.Options;
using Aevatar.Domain.Shared;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GodGPT.Dtos;
using Aevatar.Quantum;
using GodGPT.GAgents;
using GodGPT.GAgents.Awakening;
using GodGPT.GAgents.SpeechChat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Orleans;
using Orleans.Runtime;
using Stripe;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;

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
    Task<CreateShareIdResponse> GenerateShareContentAsync(Guid currentUserId, CreateShareIdRequest request, GodGPTChatLanguage language = GodGPTChatLanguage.English);
    Task<List<ChatMessage>> GetShareMessageListAsync(string shareString, GodGPTChatLanguage language = GodGPTChatLanguage.English);
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
    Task<GooglePayVerificationResponseDto> VerifyGooglePayAsync(Guid currentUserId, VerifyGooglePayInput input);
    Task<GooglePayVerificationResponseDto> VerifyGooglePlayAsync(Guid currentUserId, VerifyGooglePlayInput input);
    Task<GrainResultDto<int>> UpdateUserCreditsAsync(Guid currentUserId, UpdateUserCreditsInput input);
    Task<GrainResultDto<List<SubscriptionInfoDto>>> UpdateUserSubscriptionAsync(Guid currentUserId, UpdateUserSubscriptionsInput input);
    Task<bool> HasActiveAppleSubscriptionAsync(Guid currentUserId);
    Task<ActiveSubscriptionStatusDto> HasActiveSubscriptionAsync(Guid currentUserId);
    Task<GetInvitationInfoResponse> GetInvitationInfoAsync(Guid currentUserId);
    Task<RedeemInviteCodeResponse> RedeemInviteCodeAsync(Guid currentUserId,
        RedeemInviteCodeRequest redeemInviteCodeRequest);
    Task<CreateGuestSessionResponseDto> CreateGuestSessionAsync(string clientIp, string? guider = null);
    Task GuestChatAsync(string clientIp, string content, string chatId);
    Task<GuestChatLimitsResponseDto> GetGuestChatLimitsAsync(string clientIp);
    Task<bool> CanGuestChatAsync(string clientIp);
    Task<QuantumShareResponseDto> GetShareKeyWordWithAIAsync(Guid sessionId, string? content, string? region, SessionType sessionType, GodGPTChatLanguage language = GodGPTChatLanguage.English);

    Task<TwitterAuthResultDto> TwitterAuthVerifyAsync(Guid currentUserId, TwitterAuthVerifyInput input,GodGPTChatLanguage language = GodGPTChatLanguage.English);
    Task<PagedResultDto<RewardHistoryDto>> GetCreditsHistoryAsync(Guid currentUserId,
        GetCreditsHistoryInput getCreditsHistoryInput);
    Task<TwitterAuthParamsDto> GetTwitterAuthParamsAsync(Guid currentUserId);
    
    // Twitter Monitor Management Methods
    Task<TwitterOperationResultDto> FetchTweetsManuallyAsync();
    Task<TwitterOperationResultDto> RefetchTweetsByTimeRangeAsync(long startTimeUtcSecond, long endTimeUtcSecond);
    Task<TwitterOperationResultDto> StartTweetMonitoringAsync();
    Task<TwitterOperationResultDto> StopTweetMonitoringAsync();
    Task<TwitterOperationResultDto> GetTweetMonitoringStatusAsync();
    
    // Twitter Reward Management Methods
    Task<TwitterOperationResultDto> TriggerRewardCalculationAsync(long targetDateUtcSeconds);
    Task<TwitterOperationResultDto> ClearRewardByDayAsync(long targetDateUtcSeconds);
    Task<TwitterOperationResultDto> StartRewardCalculationAsync();
    Task<TwitterOperationResultDto> StopRewardCalculationAsync();
    
    /// <summary>
    /// Get user rewards by user ID (returns dateKey and filtered ManagerUserRewardRecordDto list)
    /// </summary>
    Task<Dictionary<string, List<ManagerUserRewardRecordDto>>> GetUserRewardsByUserIdAsync(string userId);
    
    /// <summary>
    /// Get full calculation history list
    /// </summary>
    Task<List<ManagerRewardCalculationHistoryDto>> GetCalculationHistoryListAsync();

    Task<bool> CheckIsManager(string userId);
    Task<UserProfileDto> SetVoiceLanguageAsync(Guid currentUserId, VoiceLanguageEnum voiceLanguage);

    /// <summary>
    /// Get today's awakening content for the user
    /// </summary>
    /// <param name="currentUserId">Current user ID</param>
    /// <param name="language">Voice language preference</param>
    /// <param name="region">Optional region parameter</param>
    /// <returns>Awakening content DTO</returns>
    Task<AwakeningContentDto?> GetTodayAwakeningAsync(Guid currentUserId, VoiceLanguageEnum language, string? region);

    Task<ExecuteActionResultDto> CanUploadImageAsync(Guid currentUserId,GodGPTChatLanguage language = GodGPTChatLanguage.English);
    
    /// <summary>
    /// Reset awakening state for testing purposes (Admin only)
    /// </summary>
    /// <param name="userId">User ID to reset awakening state for</param>
    /// <returns>True if reset was successful</returns>
    Task<bool> ResetAwakeningStateForTestingAsync(Guid userId);
}

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class GodGPTService : ApplicationService, IGodGPTService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<GodGPTService> _logger;
    private readonly IOptionsMonitor<StripeOptions> _stripeOptions;
    private readonly IOptionsMonitor<ManagerOptions> _managerOptions;
    private readonly ILocalizationService _localizationService;

    private readonly StripeClient _stripeClient;
    private const string PullTaskTargetId = "aevatar-twitter-monitor-PullTaskTargetId";
    private const string RewardTaskTargetId = "aevatar-twitter-reward-RewardTaskTargetId";

    public GodGPTService(IClusterClient clusterClient, ILogger<GodGPTService> logger, IOptionsMonitor<StripeOptions> stripeOptions,
        IOptionsMonitor<ManagerOptions> managerOptions, ILocalizationService localizationService)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _stripeOptions = stripeOptions;
        _managerOptions = managerOptions;

        _stripeClient = new StripeClient(_stripeOptions.CurrentValue.SecretKey);
        _localizationService = localizationService;
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
        try
        {
            var awakeningAgent = _clusterClient.GetGrain<IAwakeningGAgent>(currentUserId);
            await awakeningAgent.ResetTodayContentAsync();
        }catch(Exception e)
        {
            _logger.LogError(e,"IAwakeningGAgent ResetTodayContentAsync error currentUserId:"+currentUserId);
        }
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(currentUserId);
        return await manager.ClearAllAsync();
    }

    public async Task<CreateShareIdResponse> GenerateShareContentAsync(Guid currentUserId, CreateShareIdRequest request, GodGPTChatLanguage language = GodGPTChatLanguage.English)
    {
        try
        {
            var manager = _clusterClient.GetGrain<IChatManagerGAgent>(currentUserId);
            RequestContext.Set("GodGPTLanguage", language.ToString());
            var shareId = await manager.GenerateChatShareContentAsync(request.SessionId);
            return new CreateShareIdResponse
            {
                ShareId = GuidCompressor.CompressGuids(currentUserId, request.SessionId, shareId)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"GenerateShareContentAsync userId:{currentUserId}, sessionId:{request.SessionId}, error: {ex.Message} ");
            throw ex;
        }
    }

    public async Task<List<ChatMessage>> GetShareMessageListAsync(string shareString, GodGPTChatLanguage language = GodGPTChatLanguage.English)
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
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InvalidShare, language);
            throw new UserFriendlyException(localizedMessage);
        }

        try
        {
            var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
            RequestContext.Set("GodGPTLanguage", language.ToString());
            var shareLinkDto = await manager.GetChatShareContentAsync(sessionId, shareId);
            return shareLinkDto.Messages;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetShareMessageListAsync exception userId:{userId},shareId:{shareId}, error:{ex.Message}");
            throw ex;
        }
    }

    public async Task UpdateShowToastAsync(Guid currentUserId)
    {
        var userQuotaGAgent = _clusterClient.GetGrain<IUserQuotaGAgent>(currentUserId);
        await userQuotaGAgent.SetShownCreditsToastAsync(true);
    }

    public async Task<List<StripeProductDto>> GetStripeProductsAsync(Guid currentUserId)
    {
        var userBillingGAgent =
            _clusterClient.GetGrain<IUserBillingGAgent>(currentUserId);
        return await userBillingGAgent.GetStripeProductsAsync();
    }

    public async Task<string> CreateCheckoutSessionAsync(Guid currentUserId,
        CreateCheckoutSessionInput createCheckoutSessionInput)
    {
        var userBillingGAgent =
            _clusterClient.GetGrain<IUserBillingGAgent>(currentUserId);
        var result = await userBillingGAgent.CreateCheckoutSessionAsync(new CreateCheckoutSessionDto
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
        var userBillingGAgent =
            _clusterClient.GetGrain<IUserBillingGAgent>(internalUserId);
        return await userBillingGAgent.HandleStripeWebhookEventAsync(json, stripeSignature);
    }

    public async Task<List<PaymentSummary>> GetPaymentHistoryAsync(Guid currentUserId, GetPaymentHistoryInput input)
    {
        var userBillingGAgent =
            _clusterClient.GetGrain<IUserBillingGAgent>(currentUserId);
        return await userBillingGAgent.GetPaymentHistoryAsync(input.Page, input.PageSize);
    }

    public async Task<GetCustomerResponseDto> GetStripeCustomerAsync(Guid currentUserId)
    {
        var userBillingGAgent =
            _clusterClient.GetGrain<IUserBillingGAgent>(currentUserId);
        return await userBillingGAgent.GetStripeCustomerAsync(currentUserId.ToString());
    }

    public async Task<SubscriptionResponseDto> CreateSubscriptionAsync(Guid currentUserId, CreateSubscriptionInput input)
    {
        var userBillingGAgent =
            _clusterClient.GetGrain<IUserBillingGAgent>(currentUserId);
        return await userBillingGAgent.CreateSubscriptionAsync(new CreateSubscriptionDto
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
        var userBillingGAgent =
            _clusterClient.GetGrain<IUserBillingGAgent>(currentUserId);
        return await userBillingGAgent.CancelSubscriptionAsync(new CancelSubscriptionDto
        {
            UserId = currentUserId,
            SubscriptionId = input.SubscriptionId,
            CancellationReason = string.Empty,
            CancelAtPeriodEnd = true
        });
    }

    public async Task<List<AppleProductDto>> GetAppleProductsAsync(Guid currentUserId)
    {
        var userBillingGAgent =
            _clusterClient.GetGrain<IUserBillingGAgent>(currentUserId);
        return await userBillingGAgent.GetAppleProductsAsync();
    }

    public async Task<AppStoreSubscriptionResponseDto> VerifyAppStoreReceiptAsync(Guid currentUserId, VerifyAppStoreReceiptInput input)
    {
        var userBillingGAgent =
            _clusterClient.GetGrain<IUserBillingGAgent>(currentUserId);
        return await userBillingGAgent.CreateAppStoreSubscriptionAsync(new CreateAppStoreSubscriptionDto
        {
            UserId = currentUserId.ToString(),
            SandboxMode = input.SandboxMode,
            TransactionId = input.TransactionId
        });
    }

    public async Task<GooglePayVerificationResponseDto> VerifyGooglePayAsync(Guid currentUserId, VerifyGooglePayInput input)
    {
        _logger.LogDebug("[GodGPTService][VerifyGooglePayAsync] Starting verification for userId: {UserId}, productId: {ProductId}", 
            currentUserId, input.ProductId);
        
        try
        {
            var userBillingGAgent = _clusterClient.GetGrain<IUserBillingGAgent>(currentUserId);
            
            // Call the GodGPT.GAgents interface for Google Pay verification
            var result = await userBillingGAgent.VerifyGooglePayPaymentAsync(new GooglePayVerificationDto
            {
                PaymentToken = input.PaymentToken,
                ProductId = input.ProductId,
                OrderId = input.OrderId,
                UserId = currentUserId.ToString(),
                Environment = input.Environment
            });

            _logger.LogDebug("[GodGPTService][VerifyGooglePayAsync] Verification completed for userId: {UserId}, success: {IsValid}", 
                currentUserId, result.IsValid);

            return new GooglePayVerificationResponseDto
            {
                IsValid = result.IsValid,
                Message = result.Message,
                TransactionId = result.TransactionId,
                SubscriptionStartDate = result.SubscriptionStartDate,
                SubscriptionEndDate = result.SubscriptionEndDate,
                ErrorCode = result.ErrorCode,
                ProductId = result.ProductId,
                Platform = "GooglePay",
                PaymentState = result.PaymentState,
                AutoRenewing = result.AutoRenewing,
                PurchaseTimeMillis = result.PurchaseTimeMillis,
                PurchaseToken = input.PaymentToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GodGPTService][VerifyGooglePayAsync] Error verifying Google Pay payment for userId: {UserId}", currentUserId);
            return new GooglePayVerificationResponseDto
            {
                IsValid = false,
                Message = "Payment verification failed",
                ErrorCode = "VERIFICATION_ERROR",
                Platform = "GooglePay"
            };
        }
    }

    public async Task<GooglePayVerificationResponseDto> VerifyGooglePlayAsync(Guid currentUserId, VerifyGooglePlayInput input)
    {
        _logger.LogDebug("[GodGPTService][VerifyGooglePlayAsync] Starting verification for userId: {UserId}, productId: {ProductId}", 
            currentUserId, input.ProductId);
        
        try
        {
            var userBillingGAgent = _clusterClient.GetGrain<IUserBillingGAgent>(currentUserId);
            
            // Call the GodGPT.GAgents interface for Google Play verification
            var result = await userBillingGAgent.VerifyGooglePlayPurchaseAsync(new GooglePlayVerificationDto
            {
                PurchaseToken = input.PurchaseToken,
                ProductId = input.ProductId,
                PackageName = input.PackageName,
                OrderId = input.OrderId,
                UserId = currentUserId.ToString()
            });

            _logger.LogDebug("[GodGPTService][VerifyGooglePlayAsync] Verification completed for userId: {UserId}, success: {IsValid}", 
                currentUserId, result.IsValid);

            return new GooglePayVerificationResponseDto
            {
                IsValid = result.IsValid,
                Message = result.Message,
                TransactionId = result.TransactionId,
                SubscriptionStartDate = result.SubscriptionStartDate,
                SubscriptionEndDate = result.SubscriptionEndDate,
                ErrorCode = result.ErrorCode,
                ProductId = result.ProductId,
                Platform = "GooglePlay",
                PaymentState = result.PaymentState,
                AutoRenewing = result.AutoRenewing,
                PurchaseTimeMillis = result.PurchaseTimeMillis,
                PurchaseToken = input.PurchaseToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GodGPTService][VerifyGooglePlayAsync] Error verifying Google Play purchase for userId: {UserId}", currentUserId);
            return new GooglePayVerificationResponseDto
            {
                IsValid = false,
                Message = "Purchase verification failed",
                ErrorCode = "VERIFICATION_ERROR",
                Platform = "GooglePlay"
            };
        }
    }

    public async Task<GrainResultDto<int>> UpdateUserCreditsAsync(Guid currentUserId, UpdateUserCreditsInput input)
    {
        var userQuotaGAgent =
            _clusterClient.GetGrain<IUserQuotaGAgent>(input.UserId);
        return await userQuotaGAgent.UpdateCreditsAsync(currentUserId.ToString(), input.Credits);
    }

    public async Task<GrainResultDto<List<SubscriptionInfoDto>>> UpdateUserSubscriptionAsync(Guid currentUserId, UpdateUserSubscriptionsInput input)
    {
        var userQuotaGAgent =
            _clusterClient.GetGrain<IUserQuotaGAgent>(input.UserId);
        return await userQuotaGAgent.UpdateSubscriptionAsync(currentUserId.ToString(), input.PlanType, input.IsUltimate);
    }

    public async Task<bool> HasActiveAppleSubscriptionAsync(Guid currentUserId)
    {
        var userBillingGAgent =
            _clusterClient.GetGrain<IUserBillingGAgent>(currentUserId);
        return await userBillingGAgent.HasActiveAppleSubscriptionAsync();
    }

    public async Task<ActiveSubscriptionStatusDto> HasActiveSubscriptionAsync(Guid currentUserId)
    {
        var userBillingGAgent =
            _clusterClient.GetGrain<IUserBillingGAgent>(currentUserId);
        return await userBillingGAgent.GetActiveSubscriptionStatusAsync();
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
            RewardTiers = rewardTierDtos,
            TotalCreditsFromX = invitationStatsDto.TotalCreditsFromX,
            IsBound = invitationStatsDto.IsBound
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
        var grainId = CommonHelper.StringToGuid(CommonHelper.GetAnonymousUserGAgentId(clientIp));
        var anonymousUserGrain = _clusterClient.GetGrain<IAnonymousUserGAgent>(grainId);
        
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
        var grainId = CommonHelper.StringToGuid(CommonHelper.GetAnonymousUserGAgentId(clientIp));
        var anonymousUserGrain = _clusterClient.GetGrain<IAnonymousUserGAgent>(grainId);
        await anonymousUserGrain.GuestChatAsync(content, chatId);
    }

    /// <summary>
    /// Get chat limits for anonymous users
    /// </summary>
    public async Task<GuestChatLimitsResponseDto> GetGuestChatLimitsAsync(string clientIp)
    {
                    var grainId = CommonHelper.StringToGuid(CommonHelper.GetAnonymousUserGAgentId(clientIp));
            var anonymousUserGrain = _clusterClient.GetGrain<IAnonymousUserGAgent>(grainId);
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
        var grainId = CommonHelper.StringToGuid(CommonHelper.GetAnonymousUserGAgentId(clientIp));
        var anonymousUserGrain = _clusterClient.GetGrain<IAnonymousUserGAgent>(grainId);
        return await anonymousUserGrain.CanChatAsync();
    }

    public async Task<UserProfileDto> SetVoiceLanguageAsync(Guid currentUserId, VoiceLanguageEnum voiceLanguage)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(currentUserId);
        await manager.SetVoiceLanguageAsync(voiceLanguage);
        return await manager.GetUserProfileAsync();
    }

    public async Task<ExecuteActionResultDto> CanUploadImageAsync(Guid currentUserId,GodGPTChatLanguage language = GodGPTChatLanguage.English)
    {
        var userQuotaGAgent = _clusterClient.GetGrain<IUserQuotaGAgent>(currentUserId);
        RequestContext.Set("GodGPTLanguage", language.ToString());
        return await userQuotaGAgent.CanUploadImageAsync();
    }


        public async Task<AwakeningContentDto?> GetTodayAwakeningAsync(Guid currentUserId, VoiceLanguageEnum language, string? region)
    {
        _logger.LogInformation("[GodGPTService][GetTodayAwakeningAsync] Starting for userId: {UserId}, language: {Language}, region: {Region}",
            currentUserId, language, region);
        
        try
        {
            var awakeningAgent = _clusterClient.GetGrain<IAwakeningGAgent>(currentUserId);
            var result = await awakeningAgent.GetTodayAwakeningAsync(language, region);
            
            _logger.LogInformation("[GodGPTService][GetTodayAwakeningAsync] Completed for userId: {UserId}, result: {HasResult}",
                currentUserId, result != null);
            if (result == null)
            {
                return new AwakeningContentDto()
                {
                    AwakeningMessage = "",
                    AwakeningLevel = 0,
                    Status = (int)AwakeningStatus.NotStarted
                };
            }
            return new AwakeningContentDto()
            {
                AwakeningMessage = result.AwakeningMessage,
                AwakeningLevel = result.AwakeningLevel,
                Status = (int)result.Status
            };;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GodGPTService][GetTodayAwakeningAsync] Error for userId: {UserId}, language: {Language}, region: {Region}",
                currentUserId, language, region);
            throw;
        }
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
            var grainId = CommonHelper.StringToGuid(CommonHelper.GetAnonymousUserGAgentId("127.0.0.1"));
            var configGrain = _clusterClient.GetGrain<IAnonymousUserGAgent>(grainId);
            return await configGrain.GetMaxChatCountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get max chat count from configuration, using default: 3");
            return 3;
        }
    }

    public async Task<QuantumShareResponseDto> GetShareKeyWordWithAIAsync(Guid sessionId, string? content, string? region, SessionType sessionType, GodGPTChatLanguage language = GodGPTChatLanguage.English)
    {
        _logger.LogDebug($"[GodGPTService][GetShareKeyWordWithAIAsync] http start: sessionId={sessionId}, sessionType={sessionType}");
        var responseContent = "";
        try
        {
            var godChat = _clusterClient.GetGrain<IGodChat>(sessionId);
            var chatId = Guid.NewGuid().ToString();
            var response = await godChat.ChatWithHistory(sessionId, string.Empty, content,
                chatId, null, true, region);
            responseContent = response.IsNullOrEmpty() ? sessionType.GetDefaultContent(language) : response.FirstOrDefault().Content;
            _logger.LogDebug(
                $"[GodGPTService][GetShareKeyWordWithAIAsync] completed for sessionId={sessionId}, responseContent:{responseContent}");
        }
        catch (Exception ex)
        {
            responseContent = sessionType.GetDefaultContent(language);
            _logger.LogError(ex, $"[GodGPTService][GetShareKeyWordWithAIAsync] error for sessionId={sessionId}, sessionType={sessionType}");
        }

        return new QuantumShareResponseDto()
        {
            Success = true,
            Content = responseContent,
        };
    }

    public async Task<TwitterAuthResultDto> TwitterAuthVerifyAsync(Guid currentUserId, TwitterAuthVerifyInput input,GodGPTChatLanguage language = GodGPTChatLanguage.English)
    {
        var twitterAuthGAgent = _clusterClient.GetGrain<ITwitterAuthGAgent>(currentUserId);
        RequestContext.Set("GodGPTLanguage", language.ToString());
        return await twitterAuthGAgent.VerifyAuthCodeAsync(input.Platform, input.Code, input.RedirectUri);
    }

    public async Task<PagedResultDto<RewardHistoryDto>> GetCreditsHistoryAsync(Guid currentUserId,
        GetCreditsHistoryInput input)
    {
        var invitationAgent =  _clusterClient.GetGrain<IInvitationGAgent>(currentUserId);
        var rewardHistoryDtos = await invitationAgent.GetRewardHistoryAsync(new GetRewardHistoryRequestDto
        {
            PageNo = input.Page,
            PageSize = input.PageSize
        });
        return rewardHistoryDtos;
    }

    public async Task<TwitterAuthParamsDto> GetTwitterAuthParamsAsync(Guid currentUserId)
    {
        var twitterAuthGAgent = _clusterClient.GetGrain<ITwitterAuthGAgent>(currentUserId);
        return await twitterAuthGAgent.GetAuthParamsAsync();
    }

    // Twitter Monitor Management Methods Implementation
    
    /// <summary>
    /// Manually trigger tweets fetching with default configuration
    /// </summary>
    /// <returns>Operation result with success status</returns>
    public async Task<TwitterOperationResultDto> FetchTweetsManuallyAsync()
    {
        try
        {
            _logger.LogInformation("Starting manual tweet fetch operation");
            
            // Initialize Twitter Monitor Grain
            ITwitterMonitorGrain tweetMonitorGrain = _clusterClient.GetGrain<ITwitterMonitorGrain>(PullTaskTargetId);
            _logger.LogInformation("Twitter Monitor Grain initialized with target ID: {PullTaskTargetId}", PullTaskTargetId);

            var result = await tweetMonitorGrain.FetchTweetsManuallyAsync();
            _logger.LogInformation("Manual tweet fetch operation completed with result: {Result}", result);
            return new TwitterOperationResultDto { IsSuccess = result?.IsSuccess ?? false, ErrorMessage = result?.ErrorMessage };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch tweets manually");
            return new TwitterOperationResultDto { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Refetch tweets by specified time range
    /// </summary>
    /// <param name="startTimeUtcSecond">Start time as UTC timestamp in seconds</param>
    /// <param name="endTimeUtcSecond">End time as UTC timestamp in seconds</param>
    /// <returns>Operation result with success status</returns>
    public async Task<TwitterOperationResultDto> RefetchTweetsByTimeRangeAsync(long startTimeUtcSecond, long endTimeUtcSecond)
    {
        try
        {
            _logger.LogInformation("Starting refetch tweets by time range: {StartTime} to {EndTime}", 
                startTimeUtcSecond, endTimeUtcSecond);
            
            var timeRange = new TimeRangeDto
            {
                StartTimeUtcSecond = startTimeUtcSecond,
                EndTimeUtcSecond = endTimeUtcSecond
            };
            
            // Initialize Twitter Monitor Grain
            ITwitterMonitorGrain tweetMonitorGrain = _clusterClient.GetGrain<ITwitterMonitorGrain>(PullTaskTargetId);
            _logger.LogInformation("Twitter Monitor Grain initialized with target ID: {PullTaskTargetId}", PullTaskTargetId);
            
            var result = await tweetMonitorGrain.RefetchTweetsByTimeRangeAsync(timeRange);
            _logger.LogInformation("Refetch tweets by time range completed with result: {Result}", result);
            return new TwitterOperationResultDto { IsSuccess = result?.IsSuccess ?? false, ErrorMessage = result?.ErrorMessage };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refetch tweets by time range: {StartTime} to {EndTime}", 
                startTimeUtcSecond, endTimeUtcSecond);
            return new TwitterOperationResultDto { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Start automatic tweet monitoring task
    /// </summary>
    /// <returns>Operation result with success status</returns>
    public async Task<TwitterOperationResultDto> StartTweetMonitoringAsync()
    {
        try
        {
            _logger.LogInformation("Starting tweet monitoring task");
            // Initialize Twitter Monitor Grain
            ITwitterMonitorGrain tweetMonitorGrain = _clusterClient.GetGrain<ITwitterMonitorGrain>(PullTaskTargetId);
            _logger.LogInformation("Twitter Monitor Grain initialized with target ID: {PullTaskTargetId}", PullTaskTargetId);
            
            var result = await tweetMonitorGrain.StartMonitoringAsync();
            _logger.LogInformation("Tweet monitoring task start result: {Result}", result);
            return new TwitterOperationResultDto { IsSuccess = result?.IsSuccess ?? false, ErrorMessage = result?.ErrorMessage };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start tweet monitoring task");
            return new TwitterOperationResultDto { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Stop automatic tweet monitoring task
    /// </summary>
    /// <returns>Operation result with success status</returns>
    public async Task<TwitterOperationResultDto> StopTweetMonitoringAsync()
    {
        try
        {
            _logger.LogInformation("Stopping tweet monitoring task");
            // Initialize Twitter Monitor Grain
            ITwitterMonitorGrain tweetMonitorGrain = _clusterClient.GetGrain<ITwitterMonitorGrain>(PullTaskTargetId);
            _logger.LogInformation("Twitter Monitor Grain initialized with target ID: {PullTaskTargetId}", PullTaskTargetId);
            
            var result = await tweetMonitorGrain.StopMonitoringAsync();
            _logger.LogInformation("Tweet monitoring task stop result: {Result}", result);
            return new TwitterOperationResultDto { IsSuccess = result?.IsSuccess ?? false, ErrorMessage = result?.ErrorMessage };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop tweet monitoring task");
            return new TwitterOperationResultDto { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Get current status of tweet monitoring task
    /// </summary>
    /// <returns>Operation result with success status</returns>
    public async Task<TwitterOperationResultDto> GetTweetMonitoringStatusAsync()
    {
        try
        {
            _logger.LogDebug("Getting tweet monitoring task status");
            // Initialize Twitter Monitor Grain
            ITwitterMonitorGrain tweetMonitorGrain = _clusterClient.GetGrain<ITwitterMonitorGrain>(PullTaskTargetId);
            _logger.LogInformation("Twitter Monitor Grain initialized with target ID: {PullTaskTargetId}", PullTaskTargetId);
            
            var result = await tweetMonitorGrain.GetMonitoringStatusAsync();
            _logger.LogDebug("Retrieved tweet monitoring task status result: {Result}", result);
            return new TwitterOperationResultDto { IsSuccess = result?.IsSuccess ?? false, ErrorMessage = result?.ErrorMessage };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tweet monitoring task status");
            return new TwitterOperationResultDto { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    // Twitter Reward Management Methods Implementation
    
    /// <summary>
    /// Manually trigger reward calculation for specific date
    /// </summary>
    /// <param name="targetDateUtcSeconds">Target date as UTC timestamp in seconds</param>
    /// <returns>Operation result with success status</returns>
    public async Task<TwitterOperationResultDto> TriggerRewardCalculationAsync(long targetDateUtcSeconds)
    {
        try
        {
            _logger.LogInformation("Starting manual reward calculation for date: {TargetDate}", targetDateUtcSeconds);

            // Initialize Twitter Reward Grain
            ITwitterRewardGrain twitterRewardGrain = _clusterClient.GetGrain<ITwitterRewardGrain>(RewardTaskTargetId);
            _logger.LogInformation("Twitter Reward Grain initialized with target ID: {RewardTaskTargetId}", RewardTaskTargetId);
            
            // Convert UTC timestamp in seconds to UTC DateTime and truncate to day precision (ignore time part)
            var targetDate = DateTimeOffset.FromUnixTimeSeconds(targetDateUtcSeconds).UtcDateTime.Date;
            _logger.LogDebug("Converted timestamp {TargetDateUtcSeconds} to UTC Date (day precision): {TargetDate}", targetDateUtcSeconds, targetDate);
            
            var result = await twitterRewardGrain.TriggerRewardCalculationAsync(targetDate);
            _logger.LogInformation("Manual reward calculation completed for date: {TargetDate} with result: {Result}", targetDateUtcSeconds, result);
            return new TwitterOperationResultDto { IsSuccess = result?.IsSuccess ?? false, ErrorMessage = result?.ErrorMessage };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger reward calculation for date: {TargetDate}", targetDateUtcSeconds);
            return new TwitterOperationResultDto { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Clear reward records for specific date (for testing purposes)
    /// </summary>
    /// <param name="targetDateUtcSeconds">Target date as UTC timestamp in seconds</param>
    /// <returns>Operation result with success status</returns>
    public async Task<TwitterOperationResultDto> ClearRewardByDayAsync(long targetDateUtcSeconds)
    {
        try
        {
            _logger.LogInformation("Starting clear reward records for date: {TargetDate}", targetDateUtcSeconds);
            // Initialize Twitter Reward Grain
            ITwitterRewardGrain twitterRewardGrain = _clusterClient.GetGrain<ITwitterRewardGrain>(RewardTaskTargetId);
            _logger.LogInformation("Twitter Reward Grain initialized with target ID: {RewardTaskTargetId}", RewardTaskTargetId);
            
            // Convert UTC timestamp in seconds to UTC DateTime and truncate to day precision (ignore time part)
            var targetDate = DateTimeOffset.FromUnixTimeSeconds(targetDateUtcSeconds).UtcDateTime.Date;
            // Convert back to UTC seconds for the day start (00:00:00)
            var targetDateDayStartUtcSeconds = ((DateTimeOffset)targetDate).ToUnixTimeSeconds();
            _logger.LogDebug("Converted timestamp {TargetDateUtcSeconds} to UTC Date (day precision): {TargetDate}, day start seconds: {DayStartSeconds}", 
                targetDateUtcSeconds, targetDate, targetDateDayStartUtcSeconds);
            
            var result = await twitterRewardGrain.ClearRewardByDayUtcSecondAsync(targetDateDayStartUtcSeconds);
            _logger.LogInformation("Clear reward records completed for date: {TargetDate} with result: {Result}", targetDateUtcSeconds, result);
            return new TwitterOperationResultDto { IsSuccess = result?.IsSuccess ?? false, ErrorMessage = result?.ErrorMessage };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear reward records for date: {TargetDate}", targetDateUtcSeconds);
            return new TwitterOperationResultDto { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Start automatic reward calculation task
    /// </summary>
    /// <returns>Operation result with success status</returns>
    public async Task<TwitterOperationResultDto> StartRewardCalculationAsync()
    {
        try
        {
            _logger.LogInformation("Starting reward calculation task");
            // Initialize Twitter Reward Grain
            ITwitterRewardGrain twitterRewardGrain = _clusterClient.GetGrain<ITwitterRewardGrain>(RewardTaskTargetId);
            _logger.LogInformation("Twitter Reward Grain initialized with target ID: {RewardTaskTargetId}", RewardTaskTargetId);
            var result = await twitterRewardGrain.StartRewardCalculationAsync();
            _logger.LogInformation("Reward calculation task start result: {Result}", result);
            return new TwitterOperationResultDto { IsSuccess = result?.IsSuccess ?? false, ErrorMessage = result?.ErrorMessage };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start reward calculation task");
            return new TwitterOperationResultDto { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Stop automatic reward calculation task
    /// </summary>
    /// <returns>Operation result with success status</returns>
    public async Task<TwitterOperationResultDto> StopRewardCalculationAsync()
    {
        try
        {
            _logger.LogInformation("Stopping reward calculation task");
            // Initialize Twitter Reward Grain
            ITwitterRewardGrain twitterRewardGrain = _clusterClient.GetGrain<ITwitterRewardGrain>(RewardTaskTargetId);
            _logger.LogInformation("Twitter Reward Grain initialized with target ID: {RewardTaskTargetId}", RewardTaskTargetId);
            var result = await twitterRewardGrain.StopRewardCalculationAsync();
            _logger.LogInformation("Reward calculation task stop result: {Result}", result);
            return new TwitterOperationResultDto { IsSuccess = result?.IsSuccess ?? false, ErrorMessage = result?.ErrorMessage };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop reward calculation task");
            return new TwitterOperationResultDto { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Get user rewards by user ID (returns dateKey and filtered ManagerUserRewardRecordDto list)
    /// </summary>
    /// <param name="userId">User ID to retrieve rewards for</param>
    /// <returns>TwitterApiResultDto containing dictionary of date keys and reward records</returns>
    public async Task<Dictionary<string, List<ManagerUserRewardRecordDto>>> GetUserRewardsByUserIdAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Getting user rewards for user ID: {UserId}", userId);
            
            // Initialize Twitter Reward Grain
            ITwitterRewardGrain twitterRewardGrain = _clusterClient.GetGrain<ITwitterRewardGrain>(RewardTaskTargetId);
            _logger.LogInformation("Twitter Reward Grain initialized with target ID: {RewardTaskTargetId}", RewardTaskTargetId);
            
            var result = await twitterRewardGrain.GetUserRewardsByUserIdAsync(userId);
            _logger.LogInformation("Get user rewards completed for user ID: {UserId} with result: {Result}", userId, result);
            
            // Convert UserRewardRecordDto to ManagerUserRewardRecordDto
            if (result?.Data != null)
            {
                var convertedData = new Dictionary<string, List<ManagerUserRewardRecordDto>>();
                foreach (var kvp in result.Data)
                {
                    var convertedRecords = kvp.Value.Select(ConvertToManagerUserRewardRecordDto).ToList();
                    convertedData[kvp.Key] = convertedRecords;
                }
                return convertedData;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user rewards for user ID: {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Get full calculation history list
    /// </summary>
    /// <returns>List of ManagerRewardCalculationHistoryDto</returns>
    public async Task<List<ManagerRewardCalculationHistoryDto>> GetCalculationHistoryListAsync()
    {
        try
        {
            _logger.LogInformation("Getting calculation history list");
            
            // Initialize Twitter Reward Grain
            ITwitterRewardGrain twitterRewardGrain = _clusterClient.GetGrain<ITwitterRewardGrain>(RewardTaskTargetId);
            _logger.LogInformation("Twitter Reward Grain initialized with target ID: {RewardTaskTargetId}", RewardTaskTargetId);
            
            var result = await twitterRewardGrain.GetCalculationHistoryListAsync();
            _logger.LogInformation("Get calculation history list completed with {Count} records", result?.Count ?? 0);
            
            // Convert RewardCalculationHistoryDto to ManagerRewardCalculationHistoryDto
            if (result != null)
            {
                var convertedRecords = result.Select(ConvertToManagerRewardCalculationHistoryDto).ToList();
                return convertedRecords;
            }
            
            return new List<ManagerRewardCalculationHistoryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get calculation history list");
            return new List<ManagerRewardCalculationHistoryDto>();
        }
    }
    
    public async Task<bool> ResetAwakeningStateForTestingAsync(Guid userId)
    {
        _logger.LogInformation("[GodGPTService][ResetAwakeningStateForTestingAsync] Starting for userId: {UserId}", userId);
        
        try
        {
            var awakeningAgent = _clusterClient.GetGrain<IAwakeningGAgent>(userId);
            bool resetSuccess = await awakeningAgent.ResetAwakeningStateForTestingAsync();
            
            _logger.LogInformation("[GodGPTService][ResetAwakeningStateForTestingAsync] Completed for userId: {UserId}, success: {Success}",
                userId, resetSuccess);
            
            return resetSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GodGPTService][ResetAwakeningStateForTestingAsync] Error resetting awakening state for userId: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> CheckIsManager(string userId)
    {
        if (userId.IsNullOrEmpty())
        {
            return false;
        }

        return _managerOptions.CurrentValue.ManagerIds.Contains(userId);
    }


    /// <summary>
    /// Convert UserRewardRecordDto to ManagerUserRewardRecordDto
    /// </summary>
    /// <param name="userReward">Source UserRewardRecordDto</param>
    /// <returns>Converted ManagerUserRewardRecordDto</returns>
    private static ManagerUserRewardRecordDto ConvertToManagerUserRewardRecordDto(UserRewardRecordDto userReward)
    {
        return new ManagerUserRewardRecordDto
        {
            UserId = userReward.UserId,
            TwitterUsername = userReward.UserHandle,
            RewardAmount = userReward.FinalCredits,
            RewardDate = userReward.RewardDate ?? DateTime.UnixEpoch.AddSeconds(userReward.RewardDateUtc),
            RewardReason = $"Tweet rewards: {userReward.TweetCount} tweets, Regular: {userReward.RegularCredits}, Bonus: {userReward.BonusCredits}",
            TransactionId = userReward.RewardTransactionId,
            Status = userReward.IsRewardSent ? "Completed" : "Pending"
        };
    }

    /// <summary>
    /// Convert RewardCalculationHistoryDto to ManagerRewardCalculationHistoryDto
    /// </summary>
    /// <param name="historyRecord">Source RewardCalculationHistoryDto</param>
    /// <returns>Converted ManagerRewardCalculationHistoryDto</returns>
    private static ManagerRewardCalculationHistoryDto ConvertToManagerRewardCalculationHistoryDto(RewardCalculationHistoryDto historyRecord)
    {
        return new ManagerRewardCalculationHistoryDto
        {
            CalculationDate = historyRecord.CalculationDate,
            CalculationDateUtc = historyRecord.CalculationDateUtc,
            IsSuccess = historyRecord.IsSuccess,
            UsersRewarded = historyRecord.UsersRewarded,
            TotalCreditsDistributed = historyRecord.TotalCreditsDistributed,
            ProcessingDuration = historyRecord.ProcessingDuration,
            ErrorMessage = historyRecord.ErrorMessage,
            ProcessedTimeRangeStart = historyRecord.ProcessedTimeRangeStart,
            ProcessedTimeRangeEnd = historyRecord.ProcessedTimeRangeEnd
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