using System.Collections.Generic;

namespace Aevatar.Subscription;

public class EventDescriptionDto
{
    public string EventType { get; set; }
    public string Description { get; set; }
    public List<EventProperty> EventProperties { get; set; }
}

public class EventProperty
{
    public string Name { get; set; } 
    public string Type { get; set; }
    public string Description { get; set; }
}