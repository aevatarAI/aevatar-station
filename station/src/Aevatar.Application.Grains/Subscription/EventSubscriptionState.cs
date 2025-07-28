using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Subscription;

[GenerateSerializer]
public class EventSubscriptionState : StateBase
{
    [Id(0)]   public Guid Id { get; set; }
    [Id(1)]   public Guid UserId { get; set; }
    [Id(2)]   public Guid AgentId { get; set; }
    [Id(3)]   public List<string> EventTypes { get; set; }
    [Id(4)]   public string CallbackUrl { get; set; }
    [Id(5)]   public string Status { get; set; } // active
    [Id(6)]   public DateTime CreateTime { get; set; } 
}