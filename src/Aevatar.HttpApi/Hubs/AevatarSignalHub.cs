using System;
using System.Threading.Tasks;
using Aevatar.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.SignalR;
using LoggerExtensions = DnsClient.Internal.LoggerExtensions;

namespace Aevatar.Hubs;

[Authorize]
public class AevatarSignalHub : AbpHub
{
    private readonly ILogger<AevatarSignalHub> _logger;

    public AevatarSignalHub(ILogger<AevatarSignalHub> logger)
    {
        _logger = logger;
    }
    
    public override Task OnConnectedAsync()
    {
        _logger.LogDebug("connectionId={connectionId} connected. useId:{userId}", Context.ConnectionId, CurrentUser.Id);
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("connectionId={connectionId} disconnected. user:{userId}", Context.ConnectionId,
            CurrentUser.Id);
        await base.OnDisconnectedAsync(exception);
    }
}