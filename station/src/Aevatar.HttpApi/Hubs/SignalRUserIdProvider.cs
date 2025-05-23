using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Hubs;

public class SignalRUserIdProvider: IUserIdProvider, ISingletonDependency
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