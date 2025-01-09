using System.Collections.Generic;

namespace Aevatar.Domain.Grains.Subscription;

public class GetEventTypesInputDto
{
    public string AgentId { get; set; }
}

public class EventTypeDto
{
    public string EventType { get; set; }
    public string Description { get; set; }
    public Dictionary<string, string> Payload { get; set; }
}