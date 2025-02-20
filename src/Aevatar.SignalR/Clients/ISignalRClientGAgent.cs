using Aevatar.Core.Abstractions;
using Aevatar.SignalR.Core;

namespace Aevatar.SignalR.Clients;

/// <summary>
/// A single connection
/// </summary>
public interface ISignalRClientGAgent : IHubMessageInvoker, IStateGAgent<SignalRClientGAgentState>
{
    Task OnConnect(Guid serverId);
    Task OnDisconnect(string? reason = null);
}
