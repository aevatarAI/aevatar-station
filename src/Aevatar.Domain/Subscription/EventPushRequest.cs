using System;
using System.Collections.Generic;
using Aevatar.AtomicAgent;
using Orleans;

namespace Aevatar.Subscription;
public class EventPushRequest
{
     public string AgentId { get; set; }
     public Guid EventId { get; set; }
     public string EventType { get; set; }
     public DateTime Timestamp { get; set; }
     public string Payload { get; set; }
     public AtomicAgentDto AtomicAgent { get; set; }
     public Dictionary<string, string> Metadata { get; set; }
}

