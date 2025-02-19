using Aevatar.Core.Abstractions;
using Aevatar.SignalR.GAgents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Aevatar.SignalR;

// ReSharper disable InconsistentNaming
[Authorize]
public class AevatarSignalRHub : Hub, IAevatarSignalRHub
{
    private readonly IGAgentFactory _gAgentFactory;

    public AevatarSignalRHub(IGAgentFactory gAgentFactory)
    {
        _gAgentFactory = gAgentFactory;
    }

    public async Task PublishEventAsync(GrainId grainId, string eventTypeName, string eventJson)
    {
        await PerformPublishEventAsync(grainId, eventTypeName, eventJson);
    }

    private async Task PerformPublishEventAsync(GrainId grainId, string eventTypeName, string eventJson)
    {
        var serializer = new EventDeserializer();
        var @event = serializer.DeserializeEvent(eventJson, eventTypeName);
        var gAgent = await _gAgentFactory.GetGAgentAsync(grainId);
        var parentGAgentGrainId = await gAgent.GetParentAsync();
        if (parentGAgentGrainId.IsDefault)
        {
            return;
        }

        var parentGAgent = await _gAgentFactory.GetGAgentAsync(parentGAgentGrainId);
        var signalRGAgent = await _gAgentFactory.GetGAgentAsync<ISignalRGAgent>(new SignalRGAgentConfiguration
        {
            ConnectionId = Context?.ConnectionId ?? string.Empty
        });
        await parentGAgent.RegisterAsync(signalRGAgent);
        await signalRGAgent.PublishEventAsync(@event);
    }
}