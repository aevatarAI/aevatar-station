namespace Aevatar.SignalR;

// ReSharper disable InconsistentNaming
public interface IAevatarSignalRHub
{
    Task<GrainId?> PublishEventAsync(GrainId grainId, string eventTypeName, string eventJson);
    Task<GrainId?> SubscribeAsync(GrainId grainId, string eventTypeName, string eventJson);
    Task UnsubscribeAsync(GrainId signalRGAgentGrainId);
}