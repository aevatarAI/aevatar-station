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
    
    public void Apply(AddSubscriptionEvent add)
    {
        Id = Guid.NewGuid();
        AgentId = add.AgentId;
        EventTypes = add.EventTypes;
        CallbackUrl = add.CallbackUrl;
        Status = "Active";
        CreateTime = DateTime.Now;
    }
    
    public void Apply(CancelSubscriptionEvent cancel)
    {
        Status = "Cancelled";
    }
}