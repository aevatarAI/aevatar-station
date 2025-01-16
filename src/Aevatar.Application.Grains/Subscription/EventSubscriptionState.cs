using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Subscription;

[GenerateSerializer]
public class EventSubscriptionState : StateBase
{
    [Id(0)]   public Guid Id { get; set; }
    [Id(1)]   public string AgentId { get; set; }
    [Id(2)]   public List<string> EventTypes { get; set; }
    [Id(3)]   public string CallbackUrl { get; set; }
    [Id(4)]   public string Status { get; set; } // active
    [Id(5)]   public DateTime CreateTime { get; set; } 
}