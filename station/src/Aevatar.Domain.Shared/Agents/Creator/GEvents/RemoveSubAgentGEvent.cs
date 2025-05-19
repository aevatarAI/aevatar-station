using System.Collections.Generic;
using Orleans;

namespace Aevatar.Agents.Creator.GEvents;

[GenerateSerializer]
public class RemoveSubAgentGEvent
{
    [Id(0)] public List<string> RemovedSubagents { get; set; }
}