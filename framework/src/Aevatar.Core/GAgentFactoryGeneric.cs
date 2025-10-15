using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Streams;

namespace Aevatar.Core;

/// <summary>
/// Generic factory for creating and configuring GAgents of type T.
/// </summary>
/// <typeparam name="T">The grain interface type that implements IGrainWithGuidKey, IActivatable, and IConfigurable</typeparam>
public class GAgentFactory<T> : IGAgentFactory<T> where T : IGAgentBase
{
    private readonly IClusterClient _clusterClient;

    public GAgentFactory(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<T> GetGAgentAsync(GrainId grainId, ConfigurationBase? configuration = null)
    {
        var gAgent = _clusterClient.GetGrain<T>(grainId);
        await ConfigGAgentAsync(gAgent, configuration);
        return gAgent;
    }

    public async Task<T> GetGAgentAsync(Guid primaryKey, string alias, string ns,
        ConfigurationBase? configuration = null)
    {
        var gAgent =
            _clusterClient.GetGrain<T>(GrainId.Create($"{ns}{AevatarCoreConstants.GAgentNamespaceSeparator}{alias}",
                primaryKey.ToString("N")));
        await ConfigGAgentAsync(gAgent, configuration);
        return gAgent;
    }

    public async Task<T> GetGAgentAsync(string alias, string ns,
        ConfigurationBase? configuration = null)
    {
        return await GetGAgentAsync(Guid.NewGuid(), alias, ns, configuration);
    }

    public async Task<T> GetGAgentAsync(Guid primaryKey, Type gAgentType,
        ConfigurationBase? configuration = null)
    {
        return await GetGAgentAsync(primaryKey, gAgentType.Name, gAgentType.Namespace!,
            configuration: configuration);
    }

    public async Task<T> GetGAgentAsync(Type gAgentType, ConfigurationBase? configuration = null)
    {
        return await GetGAgentAsync(gAgentType.Name, ns: gAgentType.Namespace!,
            configuration: configuration);
    }

    public async Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(Guid primaryKey,
        ConfigurationBase? configuration = null)
        where TGrainInterface : T
    {
        var gAgent = _clusterClient.GetGrain<TGrainInterface>(primaryKey);
        await ConfigGAgentAsync(gAgent, configuration);
        return gAgent;
    }

    public async Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(GrainId grainId,
        ConfigurationBase? configuration = null)
        where TGrainInterface : T
    {
        var gAgent = _clusterClient.GetGrain<TGrainInterface>(grainId);
        await ConfigGAgentAsync(gAgent, configuration);
        return gAgent;
    }

    public Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(ConfigurationBase? configuration = null)
        where TGrainInterface : T
    {
        return GetGAgentAsync<TGrainInterface>(Guid.NewGuid(), configuration);
    }

    private async Task ConfigGAgentAsync(T gAgent, ConfigurationBase? configuration)
    {
        // Must activate the GAgent before sending events.
        // T is constrained to IActivatable and IConfigurable, so we can call methods directly
        await gAgent.ActivateAsync();
        if (configuration != null)
        {
            await gAgent.ConfigAsync(configuration);
        }
    }
}
