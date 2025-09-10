using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.Router.GAgents.Features.Common;

namespace Aevatar.GAgents.Router.GAgents;

[GenerateSerializer]
public class RouterGAgentState : AIGAgentStateBase
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public Dictionary<Guid, TaskInfo> TasksInfo { get; set; } = new Dictionary<Guid, TaskInfo>();
    [Id(2)] public Dictionary<string, AgentDescriptionInfo> AgentDescriptions =
        new Dictionary<string, AgentDescriptionInfo>();
}