using Aevatar.Core.Abstractions;
using Aevatar.SignalR.GAgents;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace Aevatar.SignalR;

// ReSharper disable InconsistentNaming
// [Authorize]
public class AevatarSignalRHub<TEvent> : Hub, IAevatarSignalRHub where TEvent : EventBase
{
    private readonly IGAgentFactory _gAgentFactory;

    public AevatarSignalRHub(IGAgentFactory gAgentFactory)
    {
        _gAgentFactory = gAgentFactory;
    }

    public async Task PublishEventAsync(string grainType, string grainKey, string eventJson)
    {
        var @event = JsonConvert.DeserializeObject<TEvent>(eventJson);
        var grainId = GrainId.Create(grainType, grainKey);
        var gAgent = await _gAgentFactory.GetGAgentAsync(grainId);
        var parentGAgentGrainId = await gAgent.GetParentAsync();
        if (parentGAgentGrainId.IsDefault)
        {
            return;
        }

        var parentGAgent = await _gAgentFactory.GetGAgentAsync(parentGAgentGrainId);
        var signalRGAgent = await _gAgentFactory.GetGAgentAsync<ISignalRGAgent<TEvent>>();
        await parentGAgent.RegisterAsync(signalRGAgent);
        await signalRGAgent.PublishEventAsync(@event!);
    }
}