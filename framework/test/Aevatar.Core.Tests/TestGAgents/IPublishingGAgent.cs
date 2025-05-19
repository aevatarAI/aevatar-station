using Aevatar.Core.Abstractions;

namespace Aevatar.Core.Tests.TestGAgents;

public interface IPublishingGAgent : IGAgent
{
    Task PublishEventAsync<T>(T @event) where T : EventBase;
}