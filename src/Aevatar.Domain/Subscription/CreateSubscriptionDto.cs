using System.Collections.Generic;

namespace Aevatar.Subscription;

public class CreateSubscriptionDto
{
    public string AgentId { get; set; }
    public List<string> EventTypes { get; set; } 
    public string CallbackUrl { get; set; }
}