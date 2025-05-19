using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Aevatar.Application.Grains.ChatManager.Dtos;
using Aevatar.Application.Grains.Common.Constants;
using Aevatar.Application.Grains.Common.Options;
using Aevatar.Application.Grains.Payment;
using Aevatar.GodGPT.Dtos;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Orleans;
using Stripe;
using Volo.Abp;
using Volo.Abp.Security.Claims;
using PaymentStatus = Aevatar.Payment.PaymentStatus;

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

    public GodGPTPaymentController(IClusterClient clusterClient, ILogger<GodGPTPaymentController> logger,
        IGodGPTService godGptService, IOptionsMonitor<StripeOptions> stripeOptions)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _godGptService = godGptService;
        _stripeOptions = stripeOptions;
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

    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSessionAsync(CreateCheckoutSessionInput createCheckoutSessionInput)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
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

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        _logger.LogDebug("[GodGPTPaymentController][webhook] josn: {0}", json);
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _stripeOptions.CurrentValue.WebhookSecret
            );
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[GodGPTPaymentController][Webhook] Error validating webhook: {Message}", e.Message);
            return BadRequest("Error validating webhook");
        }
    
        if (stripeEvent.Type == "checkout.session.completed" 
            || stripeEvent.Type == "invoice.payment_succeeded" 
            || stripeEvent.Type == "invoice.payment_failed"
            || stripeEvent.Type == "payment_intent.succeeded"
            || stripeEvent.Type == "customer.subscription.created" 
            || stripeEvent.Type == "charge.refunded")
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            _logger.LogDebug($"Session ID: {session.Id}");
    
            var orderId = session.ClientReferenceId;
            var paymentGrain = _clusterClient.GetGrain<IPaymentGrain>(orderId);
    
            // 标记订单为成功
            await paymentGrain.UpdateStatusAsync(PaymentStatus.Succeeded);
    
            // 触发后续业务逻辑（例如开通服务）
            // await OnPaymentSucceededAsync(orderId);
            // Take some action based on session.
        }
    
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