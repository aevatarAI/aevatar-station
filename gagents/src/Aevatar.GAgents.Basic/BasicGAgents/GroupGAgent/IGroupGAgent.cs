using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;

public interface IGroupGAgent: IGAgent
{
    Task PublishEventAsync<T>(T @event) where T : EventBase;
}