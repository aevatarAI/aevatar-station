using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
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

    public override async Task OnConnectedAsync()
    {
        _logger.LogDebug("connectionId={connectionId} connected.", Context.ConnectionId);
        
        _logger.LogInformation($"UserIdentifier before: {Context.UserIdentifier}");
        _logger.LogInformation($"CurrentUser Id before: {CurrentUser.Id?.ToString() ?? "NULL"}");
        
        await base.OnConnectedAsync();
        
        _logger.LogInformation($"UserIdentifier: {Context.UserIdentifier}");
        _logger.LogInformation($"CurrentUser Id: {CurrentUser.Id?.ToString() ?? "NULL"}");
        
        await Clients.Client(Context.ConnectionId).SendAsync("Test", "Success");
        await Clients.User(CurrentUser.Id.ToString()).SendAsync("Test", CurrentUser.Id.ToString());
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("connectionId={connectionId} disconnected.", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
