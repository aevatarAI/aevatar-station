using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Subscription;

[GenerateSerializer]
public abstract class SubscriptionGEvent : StateLogEventBase<SubscriptionGEvent>
{
    
}

[GenerateSerializer]
public class AddSubscriptionGEvent : SubscriptionGEvent
{
    [Id(0)]   public string AgentId { get; set; }
    [Id(1)]   public List<string> EventTypes { get; set; }
    [Id(2)]   public string CallbackUrl { get; set; }
    [Id(3)]   public string Status { get; set; } // active
    [Id(4)]   public Guid SubscriptionId { get; set; }
}

[GenerateSerializer]
public class CancelSubscriptionGEvent : SubscriptionGEvent
{
}