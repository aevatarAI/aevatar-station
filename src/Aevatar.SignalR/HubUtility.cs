using Microsoft.AspNetCore.SignalR;

namespace Aevatar.SignalR;

internal static class HubUtility
{
    internal static string GetHubName<THub>() where THub : Hub
    {
        return typeof(THub).Name;
    }
}