namespace Aevatar.Domain.Grains.Event;
[GenerateSerializer]
public class AgentTaskState
{
    [Id(0)]   public Guid Id { get; set; }
    
    [Id(1)]  public List<Guid> ProcessingEvents { get; set; }
    
    [Id(2)]  public List<Guid> FailedEvents { get; set; }
    
    [Id(3)]  public Dictionary<Guid,EventResult> EventResultDictionary { get; set; }
    
    [Id(4)] public EventStateEnum State { get; set; }

}
[GenerateSerializer]
public class EventResult
{
    [Id(0)]  public string AgentTopic { get; set; }
    [Id(1)]   public string Result { get; set; }
}

