using Aevatar.Core.Abstractions;

namespace MessagingGAgent.Grains.Agents.Publisher;

public interface IPublishingGAgent : IGAgent
{
    Task PublishEventAsync<T>(T @event) where T : EventBase;
}