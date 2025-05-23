using System;
using System.Collections.Generic;
using Aevatar.Agent;
using Orleans;

namespace Aevatar.Subscription;
public class EventPushRequest
{
     public Guid AgentId { get; set; }
     public Guid EventId { get; set; }
     public string EventType { get; set; }
     public DateTime Timestamp { get; set; }
     public string Payload { get; set; }
     public AgentDto AgentData { get; set; }
     public Dictionary<string, string> Metadata { get; set; }
}

