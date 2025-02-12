using Aevatar.Application.Grains.Agents.Example.Event;
using Aevatar.Application.Grains.Agents.Investment;
using Aevatar.Application.Grains.Agents.MarketLeader;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Application.Grains.Agents.Inventory;

public interface IInventoryGAgent :  IStateGAgent<InventoryGState>
{

}

public class InventoryGAgent : GAgentBase<InventoryGState, InventoryStateLogEvent>, IInventoryGAgent
{
    public InventoryGAgent(ILogger<InventoryGAgent> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This agent checks inventory availability for orders. It responds to `OrderCreatedEvent` and publishes `InventoryCheckedEvent` or `InventoryOutOfStockEvent` in HandleOrderCreated");
    }

    public async Task HandleEventAsync(OrderCreatedEvent eventData)
    {
        Console.WriteLine($"[InventoryCheckAgent] Checking inventory for Order ID: {eventData.OrderId}...");
        bool inStock = await CheckInventoryAsync(eventData.OrderId);

        if (inStock)
        {
            await PublishAsync(new InventoryCheckedEvent
            {
                OrderId = eventData.OrderId
            });
        }
        else
        {
            await PublishAsync(new InventoryOutOfStockEvent
            {
                OrderId = eventData.OrderId
            });
        }
    }

    private Task<bool> CheckInventoryAsync(string orderId)
    {
        return Task.FromResult(true); // Simulating inventory check
    }
}