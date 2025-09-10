using System;
using System.Collections.Generic;

namespace Aevatar.Subscription;

public class SubscriptionDto
{
    public string SubscriptionId { get; set; }
    public string AgentId { get; set; }
    public List<string> EventTypes { get; set; }
    public string CallbackUrl { get; set; }
    public string Status { get; set; } // active
    public DateTime CreatedAt { get; set; }
}