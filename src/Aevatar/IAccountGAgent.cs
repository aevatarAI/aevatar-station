using Aevatar.Core.Abstractions;

namespace Aevatar;

public interface IAccountGAgent : IGAgent
{
    Task PublishEventAsync<T>(T @event) where T : EventBase;
}

