using Aevatar.Application.Grains.Agents.Example.Event;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Application.Grains.Agents.Example.UserValidation;

public interface IUserValidationGAgent : IStateGAgent<UserValidationGState>
{

}

public class UserValidationGAgent : GAgentBase<UserValidationGState, UserValidationStateLogEvent>, IUserValidationGAgent
{
    public UserValidationGAgent(ILogger<UserValidationGAgent> logger) : base(logger)
    {
    }
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This agent validates user information, e.g., address verification and blacklist check. It responds to OrderCreatedEvent and publishes UserValidationCompletedEvent.");
    }

    public async Task HandleEventAsync(OrderCreatedEvent eventData)
    {
        Console.WriteLine($"[UserValidationAgent] Validating user information for Order ID: {eventData.OrderId}...");
        bool isValid = await ValidateUserAsync(eventData.UserId);

        if (isValid)
        {
            await PublishAsync(new UserValidatedEvent
            {
                OrderId = eventData.OrderId,
                UserId = eventData.UserId
            });
        }
        else
        {
            throw new Exception("User validation failed.");
        }
    }

    private Task<bool> ValidateUserAsync(string userId)
    {
        return Task.FromResult(true); // Simulating user validation
    }
}