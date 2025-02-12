using Aevatar.Application.Grains.Agents.Example.Event;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Application.Grains.Agents.Example.Payment;

public interface IPaymentGAgent : IStateGAgent<PaymentGState>
{

}

public class PaymentGAgent : GAgentBase<PaymentGState, PaymentStateLogEvent>, IPaymentGAgent
{
    public PaymentGAgent(ILogger<PaymentGAgent> logger) : base(logger)
    {
    }
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This agent processes payments. It responds to both InventoryCheckedEvent and UserValidationCompletedEvent and publishes PaymentProcessedEvent.");
    }
    
    public async Task HandleInventoryCheckedAsync(InventoryCheckedEvent eventData)
    {
        await ProcessPaymentAndPublishAsync(eventData.OrderId);
    }

    // Handle UserValidatedEvent
    public async Task HandleUserValidatedAsync(UserValidatedEvent eventData)
    {
        await ProcessPaymentAndPublishAsync(eventData.OrderId);
    }

    private async Task ProcessPaymentAndPublishAsync(string orderId)
    {
        bool paymentSuccessful = true;

        if (paymentSuccessful)
        {
            await PublishAsync(new PaymentProcessedEvent
            {
                OrderId = orderId,
                Amount = 100.0m
            });
        }
        else
        {
            await PublishAsync(new PaymentFailedEvent
            {
                OrderId = orderId,
                Reason = "Insufficient Funds"
            });
        }
    }
}