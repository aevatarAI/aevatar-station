using Aevatar.Core.Abstractions;

namespace SimpleAIGAgent.Grains.Agents.Publisher;

public interface IPublishingGAgent : IGAgent
{
    Task PublishEventAsync<T>(T @event) where T : EventBase;
}