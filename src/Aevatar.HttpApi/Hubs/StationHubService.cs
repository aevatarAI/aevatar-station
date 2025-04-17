using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace Aevatar.Hubs;

public class StationHubService : IHubService, ISingletonDependency
{
    private readonly IHubContext<StationSignalRHub> _hubContext;
    private readonly ILogger<StationHubService> _logger;

    public StationHubService(ILogger<StationHubService> logger, IHubContext<StationSignalRHub> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task ResponseAsync<T>(List<Guid> userIds, ISignalRMessage<T> message)
    {
        try
        {
            _logger.LogInformation(
                $"[StationHubService][ResponseAsync] userId:{userIds} , message:{JsonConvert.SerializeObject(message)} start");

            foreach (var userId in userIds)
            {
                _logger.LogInformation(
                    $"[StationHubService][ResponseAsync] userId:{userId} , message:{JsonConvert.SerializeObject(message)}");
                var userProxy = _hubContext.Clients.User(userId.ToString());
                await userProxy.SendAsync(message.MessageType, message.Data);
                // await _hubContext.Clients.Users(userId.ToString())
                //     .SendAsync(message.MessageType, message.Data);
            }

            _logger.LogInformation(
                $"[StationHubService][ResponseAsync] userId:{userIds} , message:{JsonConvert.SerializeObject(message)} end");
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Signalr response failed.{userIds}");
        }
    }
}