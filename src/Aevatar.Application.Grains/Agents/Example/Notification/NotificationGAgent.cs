using Aevatar.Application.Grains.Agents.Example.Event;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Application.Grains.Agents.Example.Notification;

public interface INotificationGAgent : IStateGAgent<NotificationGState>
{

}

public class NotificationGAgent : GAgentBase<NotificationGState, NotificationStateLogEvent>, INotificationGAgent
{
    public NotificationGAgent(ILogger<NotificationGAgent> logger) : base(logger)
    {
    }
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This agent validates user information, e.g., address verification and blacklist check. It responds to OrderCreatedEvent and publishes UserValidationCompletedEvent.");
    }
    
    public async Task HandlePaymentProcessedAsync(PaymentProcessedEvent eventData)
    {
        Console.WriteLine($"[NotificationAgent] Sending notification for Order ID: {eventData.OrderId}...");
    }
}