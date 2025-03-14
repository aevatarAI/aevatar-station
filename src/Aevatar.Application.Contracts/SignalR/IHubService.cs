using System;
using System.Threading.Tasks;

namespace Aevatar.SignalR;

public interface IHubService
{
    Task ResponseAsync<T>(Guid userId, ISignalRMessage<T> message);
}