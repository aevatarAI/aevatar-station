using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.ChatManager;
using Aevatar.Application.Grains.Agents.ChatManager.Common;
using Aevatar.Application.Grains.Agents.ChatManager.ConfigAgent;
using Aevatar.Application.Grains.Agents.ChatManager.Dtos;
using Aevatar.Application.Grains.ChatManager.Dtos;
using Aevatar.Application.Grains.ChatManager.UserBilling;
using Aevatar.Application.Grains.ChatManager.UserQuota;
using Aevatar.Application.Grains.Common.Constants;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GodGPT.Dtos;
using Aevatar.Quantum;
using Microsoft.Extensions.Logging;
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
    Task<Guid> CreateSessionAsync(Guid userId, string systemLLM, string prompt);

    Task<Tuple<string, string>> ChatWithSessionAsync(Guid userId, Guid sessionId, string sysmLLM, string content,
        ExecutionPromptSettings promptSettings = null);

    Task<List<SessionInfoDto>> GetSessionListAsync(Guid userId);
    Task<List<ChatMessage>> GetSessionMessageListAsync(Guid userId, Guid sessionId);
    Task<Guid> DeleteSessionAsync(Guid userId, Guid sessionId);
    Task<Guid> RenameSessionAsync(Guid userId, Guid sessionId, string title);

    Task<string> GetSystemPromptAsync();
    Task UpdateSystemPromptAsync(GodGPTConfigurationDto godGptConfigurationDto);

    Task<UserProfileDto> GetUserProfileAsync(Guid currentUserId);
    Task<Guid> SetUserProfileAsync(Guid currentUserId, UserProfileDto userProfileDto);
    Task<Guid> DeleteAccountAsync(Guid currentUserId);
    Task<CreateShareIdResponse> GenerateShareContentAsync(Guid currentUserId, CreateShareIdRequest request);
    Task<List<ChatMessage>> GetShareMessageListAsync(string shareString);
    Task UpdateShowToastAsync(Guid currentUserId);
    Task<List<StripeProductDto>> GetStripeProductsAsync(Guid currentUserId);
    Task<string> CreateCheckoutSessionAsync(Guid currentUserId, CreateCheckoutSessionInput createCheckoutSessionInput);
    Task<string> ParseEventAndGetUserIdAsync(string json);
    Task<bool> HandleStripeWebhookEventAsync(Guid internalUserId, string json, StringValues stripeSignature);
    Task<List<PaymentSummary>> GetPaymentHistoryAsync(Guid currentUserId, GetPaymentHistoryInput input);
}

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class GodGPTService : ApplicationService, IGodGPTService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<GodGPTService> _logger;

    public GodGPTService(IClusterClient clusterClient, ILogger<GodGPTService> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    public async Task<Guid> CreateSessionAsync(Guid userId, string systemLLM, string prompt)
    {
        var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
        return await manager.CreateSessionAsync(systemLLM, prompt);
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

    public async Task<Guid> SetUserProfileAsync(Guid currentUserId, UserProfileDto userProfileDto)
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
            UiMode = createCheckoutSessionInput.UiMode ?? StripeUiMode.HOSTED
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
        else if (stripeEvent.Type == "invoice.paid")
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
        // else if (stripeEvent.Type == "customer.subscription.created")
        // {
        //     var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        //     if (TryGetUserIdFromMetadata(subscription.Metadata, out  var userId))
        //     {
        //         return userId;
        //     }
        // }
        // else if (stripeEvent.Type == "charge.refunded")
        // {
        //     var charge = stripeEvent.Data.Object as Stripe.Charge;
        //     if (TryGetUserIdFromMetadata(charge.Metadata, out  var userId))
        //     {
        //         return userId;
        //     }
        // }

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