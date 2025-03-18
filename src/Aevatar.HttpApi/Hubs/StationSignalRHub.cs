using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Aevatar.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using LoggerExtensions = DnsClient.Internal.LoggerExtensions;

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

public class AbpSignalRUserIdProvider : IUserIdProvider, ISingletonDependency
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var userId =connection.User?.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            userId =connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        return userId;
    }
}