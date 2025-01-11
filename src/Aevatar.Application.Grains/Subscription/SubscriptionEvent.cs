using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Subscription;

[GenerateSerializer]
public abstract class SubscriptionEvent : GEventBase
{
    
}

[GenerateSerializer]
public class AddSubscriptionEvent : SubscriptionEvent
{
    [Id(0)]   public string AgentId { get; set; }
    [Id(1)]   public List<string> EventTypes { get; set; }
    [Id(2)]   public string CallbackUrl { get; set; }
    [Id(3)]   public string Status { get; set; } // active
    [Id(4)]   public Guid SubscriptionId { get; set; }
}

[GenerateSerializer]
public class CancelSubscriptionEvent : SubscriptionEvent
{
}