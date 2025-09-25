using System.Text.Json.Serialization;

namespace Aevatar.GAgents.Router.GAgents.Features.Common;

[GenerateSerializer]
public class EventSchema
{
    [Id(0)] [JsonPropertyName(@"agentName")] public string AgentName { get; set; }
    [Id(1)] [JsonPropertyName(@"eventName")] public string EventName { get; set; }
    [Id(2)] [JsonPropertyName(@"parameters")] public string Parameters { get; set; }
}

public class RouterOutputSchema : EventSchema 
{
    [Id(3)] [JsonPropertyName(@"complete")] public bool Completed { get; set; }
    [Id(4)] [JsonPropertyName(@"terminated")] public bool Terminated { get; set; }
    [Id(5)] [JsonPropertyName(@"reason")] public string Reason { get; set; }
}