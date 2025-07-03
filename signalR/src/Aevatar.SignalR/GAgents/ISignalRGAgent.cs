using Aevatar.Core.Abstractions;

namespace Aevatar.SignalR.GAgents;

public interface ISignalRGAgent : IGAgent
{
    Task PublishEventAsync<T>(T @event, string connectionId) where T : EventBase;
    Task AddConnectionIdAsync(string connectionId, bool fireAndForget);
    Task RemoveConnectionIdAsync(string connectionId);
}