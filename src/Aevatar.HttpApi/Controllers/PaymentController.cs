using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Payment;
using Aevatar.Options;
using Aevatar.Payment;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Orleans;
using Stripe;
using Stripe.Checkout;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Payment")]
[Route("api/godgpt/payment")]
// [Authorize]
public class PaymentController  : AevatarController
{
    private readonly IOptionsMonitor<StripeOptions> _options;
    private readonly IStripeClient _client;
    private readonly IClusterClient _clusterClient;

    public PaymentController(
        IOptionsMonitor<StripeOptions> options, 
        IClusterClient clusterClient)
    {
        _options  = options;
        _client = new StripeClient(options.CurrentValue.SecretKey);
        _clusterClient = clusterClient;
    }
    
    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> Create([FromForm] CreateCheckouSessionDto createCheckoutSessionDto)
    {
        var orderId = Guid.NewGuid().ToString();
        var options = new SessionCreateOptions
        {
            ClientReferenceId = orderId, 
            SuccessUrl = $"{_options.CurrentValue.Domain}/success.html?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{_options.CurrentValue.Domain}/canceled.html",
            Mode = createCheckoutSessionDto.Mode,
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = createCheckoutSessionDto.PriceId,
                    Quantity = createCheckoutSessionDto.Quantity
                },
            },
            Metadata = new Dictionary<string, string>
            {
                { "userId", CurrentUser.Id.ToString() ?? string.Empty },
                { "priceId", createCheckoutSessionDto.PriceId },
                { "quantity", createCheckoutSessionDto.Quantity.ToString() }
            },
            SubscriptionData = createCheckoutSessionDto.Mode == "subscription" ?
                new SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", CurrentUser.Id.ToString() ?? string.Empty }
                    }
                } : null
        };
        var service = new SessionService(_client);
        try
        {
            var session = await service.CreateAsync(options);
            var id = session.Id;
            
            // var orderId = string.Join("_", CurrentUser.Id, session.Id);

            // 激活Grain并初始化状态
            var paymentGrain = _clusterClient.GetGrain<IPaymentGrain>(orderId);
            await paymentGrain.InitializeAsync(
                createCheckoutSessionDto.PriceId,
                1, CurrentUser.Id!);
            
            return Redirect(session.Url);
        }
        catch (StripeException e)
        {
            Console.WriteLine(e.StripeError.Message);
            return BadRequest();
        }
    }
    
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _options.CurrentValue.WebhookSecret
            );
            Console.WriteLine($"Webhook notification with type: {stripeEvent.Type} found for {stripeEvent.Id}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Something failed {e}");
            return BadRequest();
        }
    
        if (stripeEvent.Type == "checkout.session.completed")
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            Console.WriteLine($"Session ID: {session.Id}");
            
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