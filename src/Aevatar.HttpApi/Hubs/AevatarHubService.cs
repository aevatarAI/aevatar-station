using System;
using System.Threading.Tasks;
using Aevatar.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace Aevatar.Hubs;

public class AevatarHubService : IHubService, ISingletonDependency
{
    private readonly IHubContext<AevatarSignalHub> _hubContext;
    private readonly ILogger<AevatarHubService> _logger;

    public AevatarHubService(ILogger<AevatarHubService> logger)
    {
        _logger = logger;
    }

    public async Task ResponseAsync<T>(Guid userId, ISignalRMessage<T> message)
    {
        try
        {
            _logger.LogDebug($"[AevatarHubService][ResponseAsync] userId:{userId.ToString()} , message:{JsonConvert.SerializeObject(message)} start");
            await _hubContext.Clients.Users(userId.ToString()).SendAsync(message.MessageType, message.Data);
            _logger.LogDebug($"[AevatarHubService][ResponseAsync] userId:{userId.ToString()} , message:{JsonConvert.SerializeObject(message)} end");
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Signalr response failed.{userId.ToString()}");
        }
    }
}