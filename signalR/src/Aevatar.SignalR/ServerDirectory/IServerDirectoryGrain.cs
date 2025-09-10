namespace Aevatar.SignalR.Core;

public interface IServerDirectoryGrain : IGrainWithIntegerKey
{
    Task Heartbeat(Guid serverId);
    Task Unregister(Guid serverId);
}
