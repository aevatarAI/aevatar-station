using Volo.Abp.Domain.Entities;

namespace Aevatar.Domain.Grains.Event;


[GenerateSerializer]
public abstract class AgentTaskEvent
{
    [Id(0)]  public Guid Id { get; set; } = Guid.NewGuid();
    
    [Id(1)]   public Guid TemplateId { get; set; } 
    [Id(2)] public Guid TaskId { get; set; }
    [Id(3)] public string Name { get; set; }
    [Id(4)] public string agentTopic { get; set; }
}
[GenerateSerializer]
public  class CreatedEvent : AgentTaskEvent
{
    [Id(5)] public bool IsCompleted { get; set; } = false;
    [Id(6)] public string Param { get; set; } 
}

[GenerateSerializer]
public  class CompletedEvent : AgentTaskEvent
{
    [Id(7)] public Guid  CreatedEventId{ get; set; }
    [Id(8)] public bool IsSuccess { get; set; }
    [Id(9)] public string FailReason { get; set; }
    [Id(10)] public string Result { get; set; }
}



