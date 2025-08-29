using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Basic.PublishGAgent;

public interface IPublishingGAgent : IGAgent
{
    Task PublishEventAsync<T>(T @event) where T : EventBase;
}