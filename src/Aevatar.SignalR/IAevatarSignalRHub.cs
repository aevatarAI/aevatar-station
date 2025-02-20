namespace Aevatar.SignalR;

public interface IAevatarSignalRHub
{
    Task PublishEventAsync(GrainId grainId, string eventTypeName, string eventJson);
}