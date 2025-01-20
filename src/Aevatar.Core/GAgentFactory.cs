using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Streams;

namespace Aevatar.Core;

public class GAgentFactory : IGAgentFactory
{
    private readonly IClusterClient _clusterClient;
    private readonly IStreamProvider _streamProvider;

    public GAgentFactory(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
        _streamProvider =
            _clusterClient.ServiceProvider.GetRequiredKeyedService<IStreamProvider>(AevatarCoreConstants
                .StreamProvider);
    }

    public async Task<IGAgent> GetGAgentAsync(GrainId grainId, InitializationEventBase? initializationEvent = null)
    {
        var gAgent = _clusterClient.GetGrain<IGAgent>(grainId);
        await InitializeGAgentAsync(gAgent, initializationEvent);
        return gAgent;
    }

    public async Task<IGAgent> GetGAgentAsync(string alias, Guid primaryKey, string ns = "aevatar",
        InitializationEventBase? initializationEvent = null)
    {
        var (normalizedAlias, normalizedNs) = NormalizeAliasAndNamespace(alias, ns);
        var gAgent =
            _clusterClient.GetGrain<IGAgent>(GrainId.Create($"{normalizedNs}/{normalizedAlias}",
                primaryKey.ToString("N")));
        await gAgent.ActivateAsync();
        await InitializeGAgentAsync(gAgent, initializationEvent);
        return gAgent;
    }

    public async Task<IGAgent> GetGAgentAsync(string alias, string ns = "aevatar",
        InitializationEventBase? initializationEvent = null)
    {
        var (normalizedAlias, normalizedNs) = NormalizeAliasAndNamespace(alias, ns);
        return await GetGAgentAsync(normalizedAlias, Guid.NewGuid(), normalizedNs, initializationEvent);
    }

    public async Task<IGAgent> GetGAgentAsync(Type gAgentType, Guid primaryKey,
        InitializationEventBase? initializationEvent = null)
    {
        return await GetGAgentAsync(gAgentType.FullName!, primaryKey, initializationEvent: initializationEvent);
    }

    public async Task<IGAgent> GetGAgentAsync(Type gAgentType, InitializationEventBase? initializationEvent = null)
    {
        return await GetGAgentAsync(gAgentType.FullName!, initializationEvent: initializationEvent);
    }

    public async Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(Guid primaryKey,
        InitializationEventBase? initializationEvent = null)
        where TGrainInterface : IGAgent
    {
        var gAgent = _clusterClient.GetGrain<TGrainInterface>(primaryKey);
        await InitializeGAgentAsync(gAgent, initializationEvent);
        return gAgent;
    }

    public Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(InitializationEventBase? initializationEvent = null)
        where TGrainInterface : IGAgent
    {
        return GetGAgentAsync<TGrainInterface>(Guid.NewGuid(), initializationEvent);
    }

    private async Task InitializeGAgentAsync(IGAgent gAgent, InitializationEventBase? initializationEvent)
    {
        if (initializationEvent != null)
        {
            var eventWrapper = new EventWrapper<EventBase>(initializationEvent, Guid.NewGuid(), gAgent.GetGrainId());
            var streamId = StreamId.Create(AevatarCoreConstants.StreamNamespace, gAgent.GetGrainId().ToString());
            var stream = _streamProvider.GetStream<EventWrapperBase>(streamId);
            await stream.OnNextAsync(eventWrapper);
        }
    }

    /// <summary>
    /// Normalize alias and namespace.
    /// </summary>
    /// <param name="alias"></param>
    /// <param name="ns"></param>
    /// <returns></returns>
    private (string alias, string ns) NormalizeAliasAndNamespace(string alias, string ns)
    {
        if (alias.Contains('.'))
        {
            var parts = alias.Split('.');
            alias = parts.Last();
            ns = parts.SkipLast(1).Aggregate((a, b) => $"{a}/{b}");
        }
        else if (ns.Contains('.'))
        {
            ns = ns.Replace('.', '/');
        }

        return (alias.ToLower(), ns.ToLower());
    }
}