using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.GroupGAgent;

namespace Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;

public interface IGroupGAgent: IStateGAgent<GroupGAgentState>
{
    Task PublishEventAsync<T>(T @event) where T : EventBase;
}