using Aevatar.Application.Grains.Agents.Example.Event;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Application.Grains.Agents.Example.Order;

public interface IOrderGAgent : IStateGAgent<OrderGState>
{

}

public class OrderGAgent : GAgentBase<OrderGState, OrderStateLogGEvent>, IOrderGAgent
{
    public OrderGAgent(ILogger<OrderGAgent> logger) : base(logger)
    {
    }
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This agent validates user information, e.g., address verification and blacklist check. It responds to OrderCreatedEvent and publishes UserValidationCompletedEvent.");
    }
    
    public async Task HandlePaymentProcessedAsync(PaymentProcessedEvent eventData)
    {
        Console.WriteLine($"[OrderFulfillmentAgent] Processing Order ID: {eventData.OrderId}...");

        // Simulate order fulfillment processing
        await Task.Delay(1000);

        await PublishAsync(new OrderFulfilledEvent
        {
            OrderId = eventData.OrderId
        });
    }
}