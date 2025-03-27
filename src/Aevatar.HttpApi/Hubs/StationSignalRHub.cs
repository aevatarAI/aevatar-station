using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.SignalR;

namespace Aevatar.Hubs;

[Authorize]
public class StationSignalRHub : AbpHub
{
    private readonly ILogger<StationSignalRHub> _logger;

    public StationSignalRHub(ILogger<StationSignalRHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogDebug("connectionId={connectionId} connected.", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("connectionId={connectionId} disconnected.", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
