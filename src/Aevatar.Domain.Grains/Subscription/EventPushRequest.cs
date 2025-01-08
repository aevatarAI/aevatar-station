namespace Aevatar.Domain.Grains.Subscription;
public class EventPushRequest
{
     public string AgentId { get; set; }
     public Guid EventId { get; set; }
     public string EventType { get; set; }
     public DateTime Timestamp { get; set; }
     public string Payload { get; set; }
     public AtomicAgent AtomicAgent { get; set; }
     public Dictionary<string, string> Metadata { get; set; }
}

public class AtomicAgent
{
    [Id(0)]  public Guid Id { get; set; }
    [Id(1)]  public string Type { get; set; }
    [Id(2)]  public string Name { get; set; }
}
