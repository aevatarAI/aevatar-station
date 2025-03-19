using System.Collections.Concurrent;
using Aevatar.Core.Abstractions;
using Aevatar.SignalR.GAgents;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Aevatar.SignalR;

// ReSharper disable InconsistentNaming
// [Authorize]
public class AevatarSignalRHub : Hub, IAevatarSignalRHub
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<AevatarSignalRHub> _logger;

    public AevatarSignalRHub(IGAgentFactory gAgentFactory, ILogger<AevatarSignalRHub> logger)
    {
        _gAgentFactory = gAgentFactory;
        _logger = logger;
    }

    public async Task<GrainId?> PublishEventAsync(GrainId grainId, string eventTypeName, string eventJson)
    {
        _logger.LogInformation($"PublishEventAsync: {grainId} \n{eventTypeName} \n{eventJson}");
        using var _ = new ActivityScope(nameof(PublishEventAsync));

        var (parentGAgent, signalRGAgent) = await InitializeGroupMembers(grainId);
        if (parentGAgent == null || signalRGAgent == null) return null;

        var connectionId = GetConnectionId();
        _logger.LogInformation($"ConnectionId: {connectionId}");
        await AddConnectionIdIfNeeded(signalRGAgent, connectionId, true);
        await parentGAgent.RegisterAsync(signalRGAgent);
        _logger.LogInformation($"{signalRGAgent.GetGrainId().ToString()} registered.");
        await signalRGAgent.PublishEventAsync(DeserializeEvent(eventTypeName, eventJson), connectionId);
        return signalRGAgent.GetGrainId();
    }

    public async Task<GrainId?> SubscribeAsync(GrainId grainId, string eventTypeName, string eventJson)
    {
        _logger.LogInformation($"SubscribeAsync: {grainId} \n{eventTypeName} \n{eventJson}");

        using var _ = new ActivityScope(nameof(SubscribeAsync));

        var (parentGAgent, signalRGAgent) = await InitializeGroupMembers(grainId);
        if (parentGAgent == null || signalRGAgent == null) return null;

        var connectionId = GetConnectionId();
        _logger.LogInformation($"ConnectionId: {connectionId}");
        await AddConnectionIdIfNeeded(signalRGAgent, connectionId, false);
        await parentGAgent.RegisterAsync(signalRGAgent);
        _logger.LogInformation($"{signalRGAgent.GetGrainId().ToString()} registered.");
        await signalRGAgent.PublishEventAsync(DeserializeEvent(eventTypeName, eventJson), connectionId);
        return signalRGAgent.GetGrainId();
    }

    private static EventBase DeserializeEvent(string eventTypeName, string eventJson) =>
        new EventDeserializer().DeserializeEvent(eventJson, eventTypeName);

    private async Task<(IGAgent? ParentGAgent, ISignalRGAgent? SignalRGAgent)> InitializeGroupMembers(
        GrainId grainId)
    {
        var targetGAgent = await _gAgentFactory.GetGAgentAsync(grainId);
        var parentGrainId = await targetGAgent.GetParentAsync();
        if (parentGrainId.IsDefault)
        {
            var signalRParentGAgent = await _gAgentFactory.GetGAgentAsync<ISignalRGAgent>();
            var gAgent = await _gAgentFactory.GetGAgentAsync(grainId);
            await signalRParentGAgent.RegisterAsync(gAgent);
            return (signalRParentGAgent, signalRParentGAgent);
        }

        var parentGAgent = await _gAgentFactory.GetGAgentAsync(parentGrainId);
        if (parentGrainId.Type == GrainTypeCache.Get(typeof(SignalRGAgent)))
        {
            return (parentGAgent, await _gAgentFactory.GetGAgentAsync<ISignalRGAgent>(parentGrainId.GetGuidKey()));
        }

        var signalRGAgent = await GetOrCreateSignalRGAgentAsync(parentGAgent);
        return (parentGAgent, signalRGAgent);
    }

    private async Task<ISignalRGAgent> GetOrCreateSignalRGAgentAsync(IGAgent parentGAgent)
    {
        var siblings = await parentGAgent.GetChildrenAsync();
        var existingGAgentId = siblings.FirstOrDefault(id =>
            id.Type == GrainTypeCache.Get(typeof(SignalRGAgent)));

        return existingGAgentId.IsDefault is false
            ? await _gAgentFactory.GetGAgentAsync<ISignalRGAgent>(existingGAgentId.GetGuidKey())
            : await _gAgentFactory.GetGAgentAsync<ISignalRGAgent>();
    }

    private string GetConnectionId() => Context?.ConnectionId ?? string.Empty;

    private static async Task AddConnectionIdIfNeeded(ISignalRGAgent agent, string connectionId, bool fireAndForget)
    {
        if (!string.IsNullOrEmpty(connectionId))
        {
            await agent.AddConnectionIdAsync(connectionId, fireAndForget);
        }
    }

    private static async Task RemoveConnectionIdIfNeeded(ISignalRGAgent agent, string connectionId)
    {
        if (!string.IsNullOrEmpty(connectionId))
        {
            await agent.RemoveConnectionIdAsync(connectionId);
        }
    }

    public async Task UnsubscribeAsync(GrainId signalRGAgentGrainId)
    {
        var connectionId = GetConnectionId();
        if (!connectionId.IsNullOrEmpty())
        {
            var signalRGAgent = await _gAgentFactory.GetGAgentAsync<ISignalRGAgent>(signalRGAgentGrainId.GetGuidKey());
            await signalRGAgent.RemoveConnectionIdAsync(connectionId);
        }
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        await Groups.AddToGroupAsync(Context.ConnectionId, Guid.Empty.ToString());
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, Guid.Empty.ToString());
    }
}

internal static class GrainTypeCache
{
    private static readonly ConcurrentDictionary<Type, GrainType> _cache = new();

    public static GrainType Get(Type grainType) =>
        _cache.GetOrAdd(grainType, t => GrainType.Create(t.FullName!));
}