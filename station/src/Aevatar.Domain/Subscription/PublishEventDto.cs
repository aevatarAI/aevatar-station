using System;
using System.Collections.Generic;

namespace Aevatar.Subscription;

public class PublishEventDto
{
    public Guid AgentId { get; set; }
    public string EventType { get; set; } 
    public Dictionary<string, object> EventProperties { get; set; }
}