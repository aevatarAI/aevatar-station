using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.GraphRetrievalAgent.GAgent.SEvent;

[GenerateSerializer]
public class SetGraphSchemaSEvent : GraphRetrievalAgentSEvent
{
    [Id(0)] public required string Schema { get; set; }
    [Id(1)] public string Example { get; set; } 
}