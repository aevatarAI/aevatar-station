using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Aevatar.Application.Constants;
using Aevatar.Application.Contracts.Services;
using Aevatar.Application.Grains.ChatManager.Dtos;
using Aevatar.Application.Grains.ChatManager.UserBilling;
using Aevatar.Application.Grains.Common.Constants;
using Aevatar.Application.Grains.Common.Options;
using Aevatar.Extensions;
using Aevatar.GodGPT.Dtos;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Stripe;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Payment")]
[Route("api/godgpt/payment")]
[Authorize]
public class GodGPTPaymentController : AevatarController
{
    private readonly ILogger<GodGPTPaymentController> _logger;
    private readonly IOptionsMonitor<StripeOptions> _stripeOptions;
    private readonly IClusterClient _clusterClient;
    private readonly IGodGPTService _godGptService;
    private readonly ILocalizationService _localizationService;

    public GodGPTPaymentController(IClusterClient clusterClient, ILogger<GodGPTPaymentController> logger,
        IGodGPTService godGptService, IOptionsMonitor<StripeOptions> stripeOptions, ILocalizationService localizationService)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _godGptService = godGptService;
        _stripeOptions = stripeOptions;
        _localizationService = localizationService;
    }

    [HttpGet("keys")]
    public async Task<StripePaymentKeysDto> GetStripePaymentKeysAsync()
    {
        return new StripePaymentKeysDto
        {
            PublishableKey = string.Empty
        };
    }

    [HttpGet("products")]
    public async Task<List<StripeProductDto>> GetStripeProductsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var productDtos = await _godGptService.GetStripeProductsAsync(currentUserId);
        _logger.LogDebug("[GodGPTPaymentController][GetStripeProductsAsync] userId: {0}, duration: {1}ms",
            currentUserId.ToString(), stopwatch.ElapsedMilliseconds);
        return productDtos;
    }

    [HttpGet("iap-products")]
    public async Task<List<AppleProductDto>> GetAppleProductsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var productDtos = await _godGptService.GetAppleProductsAsync(currentUserId);
        _logger.LogDebug("[GodGPTPaymentController][GetAppleProductsAsync] userId: {0}, duration: {1}ms",
            currentUserId.ToString(), stopwatch.ElapsedMilliseconds);
        return productDtos;
    }

    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSessionAsync(CreateCheckoutSessionInput createCheckoutSessionInput)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        
        // Check PriceId parameter
        if (string.IsNullOrWhiteSpace(createCheckoutSessionInput.PriceId))
            return BadRequest("PriceId cannot be empty");
        
        try
        {
            var result = await _godGptService.CreateCheckoutSessionAsync(currentUserId, createCheckoutSessionInput);
            _logger.LogDebug("[GodGPTPaymentController][CreateCheckoutSessionAsync] userId: {0}, duration: {1}ms",
                currentUserId.ToString(), stopwatch.ElapsedMilliseconds);
            if (createCheckoutSessionInput.UiMode == StripeUiMode.EMBEDDED)
            {
                return Ok(result);
            }
            else
            {
                return Ok(result);
            }
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "[GodGPTPaymentController][GetStripeProductsAsync] create-checkout-session error.");
            return BadRequest();
        }
    }

    [HttpPost("create-subscription")]
    public async Task<SubscriptionResponseDto> CreateSubscriptionAsync(CreateSubscriptionInput input)
    {
        _logger.LogWarning("CreateSubscriptionAsync Platform={A}",input.DevicePlatform);
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var responseDto = await _godGptService.CreateSubscriptionAsync(currentUserId, input);
        _logger.LogDebug("[GodGPTPaymentController][CreateSubscriptionAsync] userId: {0}, duration: {1}ms",
            currentUserId.ToString(), stopwatch.ElapsedMilliseconds);
        return responseDto;
    }

    [HttpGet("list")]
    public async Task<List<PaymentSummary>> GetPaymentHistoryAsync(GetPaymentHistoryInput input)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var paymentHistories = await _godGptService.GetPaymentHistoryAsync(currentUserId, input);
        _logger.LogDebug("[GodGPTPaymentController][GetPaymentHistoryAsync] userId: {0}, duration: {1}ms",
            currentUserId.ToString(), stopwatch.ElapsedMilliseconds);
        return paymentHistories;
    }

    [HttpPost("customer")]
    public async Task<GetCustomerResponseDto> GetStripeCustomerAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        GetCustomerResponseDto customerResponseDto = await _godGptService.GetStripeCustomerAsync(currentUserId);
        _logger.LogDebug("[GodGPTPaymentController][GetPaymentHistoryAsync] userId: {0}, duration: {1}ms",
            currentUserId.ToString(), stopwatch.ElapsedMilliseconds);
        return customerResponseDto;
    }

    [HttpPost("cancel-subscription")]
    public async Task<CancelSubscriptionResponseDto> CancelSubscriptionAsync(CancelSubscriptionInput input)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var cancelSubscription = await _godGptService.CancelSubscriptionAsync(currentUserId, input);
        _logger.LogDebug("[GodGPTPaymentController][GetPaymentHistoryAsync] userId: {0}, duration: {1}ms",
            currentUserId.ToString(), stopwatch.ElapsedMilliseconds);
        return cancelSubscription;
    }

    [HttpPost("refunded")]
    public async Task<bool> RefundedAsync()
    {
        return true;
    }

    [HttpPost("verify-receipt")]
    public async Task<AppStoreSubscriptionResponseDto> VerifyAppStoreReceiptAsync(VerifyAppStoreReceiptInput input)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var response = await _godGptService.VerifyAppStoreReceiptAsync(currentUserId, input);
        // _logger.LogDebug("[GodGPTPaymentController][VerifyAppStoreReceiptAsync] userId: {0}, sandboxMode: {1}, duration: {2}ms",
        //     currentUserId.ToString(), input.SandboxMode.ToString(), stopwatch.ElapsedMilliseconds);
        _logger.LogDebug($"[GodGPTPaymentController][VerifyAppStoreReceiptAsync] userId: {currentUserId.ToString()}, sandboxMode: {input.SandboxMode.ToString()}, duration: {stopwatch.ElapsedMilliseconds}ms");
        _logger.LogDebug($"[GodGPTPaymentController][VerifyAppStoreReceiptAsync] result: {response.Success}, {response.Error}");
        return response;
    }

    [HttpPost("google-play/verify-transaction")]
    public async Task<PaymentVerificationResponseDto> VerifyGooglePlayTransactionAsync(GooglePlayTransactionVerificationRequestDto input)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        
        _logger.LogInformation("[GodGPTPaymentController][VerifyGooglePlayTransactionAsync] Request received for userId: {UserId}, transactionId: {TransactionId}, requestIP: {RequestIP}", 
            currentUserId, input.TransactionIdentifier, HttpContext.Connection.RemoteIpAddress?.ToString());
            
        // Validate input
        if (input == null)
        {
            _logger.LogWarning("[GodGPTPaymentController][VerifyGooglePlayTransactionAsync] Null input received for userId: {UserId}", currentUserId);
            return new PaymentVerificationResponseDto
            {
                IsValid = false,
                Message = "Invalid request",
                ErrorCode = "INVALID_REQUEST"
            };
        }
        
        try
        {
            var response = await _godGptService.VerifyGooglePlayTransactionAsync(currentUserId, input);
            
            _logger.LogInformation("[GodGPTPaymentController][VerifyGooglePlayTransactionAsync] Request completed for userId: {UserId}, transactionId: {TransactionId}, duration: {Duration}ms, result: {IsValid}, errorCode: {ErrorCode}", 
                currentUserId, input.TransactionIdentifier, stopwatch.ElapsedMilliseconds, response.IsValid, response.ErrorCode);
            
            // Log additional details for failed verifications
            if (!response.IsValid)
            {
                _logger.LogWarning("[GodGPTPaymentController][VerifyGooglePlayTransactionAsync] Verification failed for userId: {UserId}, transactionId: {TransactionId}, errorCode: {ErrorCode}, message: {Message}", 
                    currentUserId, input.TransactionIdentifier, response.ErrorCode, response.Message);
            }
            else
            {
                _logger.LogInformation("[GodGPTPaymentController][VerifyGooglePlayTransactionAsync] Verification successful for userId: {UserId}, transactionId: {TransactionId}, productId: {ProductId}", 
                    currentUserId, input.TransactionIdentifier, response.ProductId);
            }
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GodGPTPaymentController][VerifyGooglePlayTransactionAsync] Unexpected exception in controller for userId: {UserId}, transactionId: {TransactionId}, duration: {Duration}ms", 
                currentUserId, input.TransactionIdentifier, stopwatch.ElapsedMilliseconds);
            
            return new PaymentVerificationResponseDto
            {
                IsValid = false,
                Message = "Internal server error during verification",
                ErrorCode = "CONTROLLER_ERROR"
            };
        }
    }

    [Obsolete("Use has-active-subscription instead")]
    [HttpGet("has-apple-subscription")]
    public async Task<bool> HasActiveAppleSubscriptionAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var result = await _godGptService.HasActiveAppleSubscriptionAsync(currentUserId);
        _logger.LogDebug($"[GodGPTPaymentController][HasActiveAppleSubscriptionAsync] userId: {currentUserId.ToString()}, result: {result}, duration: {stopwatch.ElapsedMilliseconds}ms");
        return result;
    }
    
    [HttpGet("has-active-subscription")]
    public async Task<ActiveSubscriptionStatusDto> HasActiveSubscriptionAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var result = await _godGptService.HasActiveSubscriptionAsync(currentUserId);
        _logger.LogDebug($"[GodGPTPaymentController][HasActiveSubscriptionAsync] userId: {currentUserId.ToString()}, duration: {stopwatch.ElapsedMilliseconds}ms");
        return result;
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var language = HttpContext.GetGodGPTLanguage();
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        _logger.LogInformation("[GodGPTPaymentController][webhook] josn: {0}", json);
        
        string internalUserId = null;
        try
        {
            internalUserId = await _godGptService.ParseEventAndGetUserIdAsync(json);
        }
        catch (Exception e)
        {
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.WebhookValidatingError, language);
            _logger.LogError(e, "[GodGPTPaymentController][Webhook] Error validating webhook: {Message}", e.Message);
            return BadRequest(localizedMessage);
        }

        if (!internalUserId.IsNullOrWhiteSpace() && Guid.TryParse(internalUserId, out var userId))
        {
            var result = await _godGptService.HandleStripeWebhookEventAsync(userId,  json, Request.Headers["Stripe-Signature"]);
            if (!result)
            {
                return BadRequest();
            }

            return Ok();
        } 
        
        _logger.LogWarning("[GodGPTPaymentController][Webhook] ");
        return Ok();
    }

    // [HttpPost("create-subscription")]
    // public ActionResult<SubscriptionCreateResponse> CreateSubscription([FromBody] CreateSubscriptionRequest req)
    // {
    //     var customerId = HttpContext.Request.Cookies["customer"];
    //
    //     // Automatically save the payment method to the subscription
    //     // when the first payment is successful.
    //     var paymentSettings = new SubscriptionPaymentSettingsOptions {
    //         SaveDefaultPaymentMethod = "on_subscription",
    //     };
    //
    //     // Create the subscription. Note we're expanding the Subscription's
    //     // latest invoice and that invoice's confirmation_secret
    //     // so we can pass it to the front end to confirm the payment
    //     var subscriptionOptions = new SubscriptionCreateOptions
    //     {
    //         Customer = customerId,
    //         Items = new List<SubscriptionItemOptions>
    //         {
    //             new SubscriptionItemOptions
    //             {
    //                 Price = req.PriceId,
    //             },
    //         },
    //         PaymentSettings = paymentSettings,
    //         PaymentBehavior = "default_incomplete",
    //     };
    //     subscriptionOptions.AddExpand("latest_invoice.confirmation_secret");
    //     var subscriptionService = new SubscriptionService();
    //     try
    //     {
    //         Stripe.Subscription subscription = subscriptionService.Create(subscriptionOptions);
    //
    //         return new SubscriptionCreateResponse
    //         {
    //             SubscriptionId = subscription.Id,
    //             ClientSecret = subscription.LatestInvoice.ConfirmationSecret.ClientSecret,
    //         };
    //     }
    //     catch (StripeException e)
    //     {
    //         Console.WriteLine($"Failed to create subscription.{e}");
    //         return BadRequest();
    //     }
    // }
}