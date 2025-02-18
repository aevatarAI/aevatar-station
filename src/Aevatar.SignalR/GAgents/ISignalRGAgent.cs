using Aevatar.Core.Abstractions;

namespace Aevatar.SignalR.GAgents;

public interface ISignalRGAgent<in TEvent> : IGAgent where TEvent : EventBase
{
    Task PublishEventAsync(TEvent @event);
}