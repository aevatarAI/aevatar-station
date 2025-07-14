using Aevatar.Payment;
using Microsoft.Extensions.Logging;

namespace Aevatar.Application.Grains.Payment;

public interface IPaymentGrain : IGrainWithStringKey
{
    Task InitializeAsync(string priceId, decimal amount, Guid? userId);
    
    Task UpdateStatusAsync(PaymentStatus status);
    
    Task<PaymentOrder> GetOrderAsync();
}

public class PaymentGrain : Grain<PaymentOrder>, IPaymentGrain
{
    private readonly ILogger<PaymentGrain> _logger;

    public PaymentGrain(ILogger<PaymentGrain> logger)
    {
        _logger = logger;
    }

    public Task InitializeAsync(string priceId, decimal amount, Guid? userId)
    {
        State = new PaymentOrder
        {
            OrderId = this.GetPrimaryKeyString(),
            PriceId = priceId,
            Amount = amount,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        return WriteStateAsync();
    }

    public async Task UpdateStatusAsync(PaymentStatus status)
    {
        State.Status = status;
        if (status == PaymentStatus.Succeeded || status == PaymentStatus.Canceled)
        {
            State.CompletedAt = DateTime.UtcNow;
        }
        await WriteStateAsync();
        _logger.LogInformation($"Order {State.OrderId} status updated to {status}");
    }

    public Task<PaymentOrder> GetOrderAsync() => Task.FromResult(State);
}