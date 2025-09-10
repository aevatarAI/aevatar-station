using Aevatar.GAgents.AIGAgent.State;

namespace Aevatar.GAgents.GraphRetrievalAgent.GAgent;

[GenerateSerializer]
public class GraphRetrievalAgentState : AIGAgentStateBase
{
    [Id(0)] public string RetrievalSchema { get; set; }
    [Id(1)] public string RetrievalExample { get; set; }
}