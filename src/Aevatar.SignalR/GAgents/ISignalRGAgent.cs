using Aevatar.Core.Abstractions;

namespace Aevatar.SignalR.GAgents;

public interface ISignalRGAgent : IGAgent
{
    Task PublishEventAsync<T>(T @event) where T : EventBase;
}