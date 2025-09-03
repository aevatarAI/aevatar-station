using System.Text.Json.Serialization;

namespace Aevatar.GAgents.Router.GAgents.Features.Common;

[GenerateSerializer]
public class AgentDescriptionInfo
{
    [Id(0)] public string AgentName { get; set; }
    [Id(1)] public string AgentDescription { get; set; }

    [Id(2)] public List<AgentEventDescription> EventList = new List<AgentEventDescription>();
}

[GenerateSerializer]
public class AgentEventDescription
{
    [Id(0)]  public string EventName { get; set; }
    
    [JsonIgnore]
    [Id(1)] public Type EventType { get; set; }
    
    [Id(2)] public string EventDescription { get; set; }

    [Id(3)] public List<AgentEventTypeFieldDescription> EventParameters { get; set; } =
        new List<AgentEventTypeFieldDescription>();
}

[GenerateSerializer]
public class AgentEventTypeFieldDescription
{
    [Id(0)]  public string FieldName { get; set; }
    [Id(1)] public string FieldDescription { get; set; }
    [Id(2)] public string FieldType { get; set; }
}