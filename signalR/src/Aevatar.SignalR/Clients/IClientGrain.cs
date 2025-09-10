using Aevatar.SignalR.Core;

namespace Aevatar.SignalR.Clients;

/// <summary>
/// A single connection
/// </summary>
public interface IClientGrain : IHubMessageInvoker, IGrainWithStringKey
{
    Task OnConnect(Guid serverId);
    Task OnDisconnect(string? reason = null);
}
