using System;
using Orleans;

namespace Aevatar.Payment;


[GenerateSerializer]
public class PaymentOrder
{
    [Id(0)] public string OrderId { get; set; }          // order ID（Grain Key）
    [Id(1)] public Guid? UserId { get; set; }        
    [Id(2)] public string PriceId { get; set; }           // Stripe price ID
    [Id(3)] public decimal Amount { get; set; }          
    [Id(4)] public string Currency { get; set; } = "USD";
    [Id(5)] public PaymentStatus Status { get; set; } = PaymentStatus.Created;
    [Id(6)] public PaymentMethod Method { get; set; } 
    [Id(7)] public PaymentPlatform Platform { get; set; } = PaymentPlatform.Stripe;
    [Id(8)] public string Mode { get; set; }
    [Id(9)] public DateTime CreatedAt { get; set; }
    [Id(10)] public DateTime LastUpdated { get; set; }
    [Id(11)] public DateTime? CompletedAt { get; set; }
}


[GenerateSerializer]
public enum PaymentStatus
{
    [Id(0)] Created = 0,
    [Id(1)] Pending = 1,
    [Id(2)] Succeeded = 2,
    [Id(3)] Failed = 3,
    [Id(4)] Canceled = 4
}

[GenerateSerializer]
public enum PaymentMethod
{
    [Id(0)] AppleWallet = 0,
}

[GenerateSerializer]
public enum PaymentPlatform {
    [Id(0)] Stripe = 0
}