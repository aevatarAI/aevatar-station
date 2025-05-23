using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aevatar.SignalR;

public interface IHubService
{
    Task ResponseAsync<T>(List<Guid> userIds, ISignalRMessage<T> message);
}