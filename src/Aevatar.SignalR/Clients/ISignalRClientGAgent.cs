using Aevatar.Core.Abstractions;
using Aevatar.SignalR.Core;

namespace Aevatar.SignalR.Clients;

/// <summary>
/// A single connection
/// </summary>
public interface ISignalRClientGAgent : IHubMessageInvoker, IGAgent
{
    Task OnConnect(Guid serverId);
    Task OnDisconnect(string? reason = null);
}
