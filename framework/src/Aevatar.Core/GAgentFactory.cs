using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Streams;

namespace Aevatar.Core;

public class GAgentFactory : IGAgentFactory
{
    private readonly IClusterClient _clusterClient;

    public GAgentFactory(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<IGAgent> GetGAgentAsync(GrainId grainId, ConfigurationBase? configuration = null)
    {
        var gAgent = _clusterClient.GetGrain<IGAgent>(grainId);
        await ConfigGAgentAsync(gAgent, configuration);
        return gAgent;
    }

    public async Task<IGAgent> GetGAgentAsync(Guid primaryKey, string alias, string ns,
        ConfigurationBase? configuration = null)
    {
        var gAgent =
            _clusterClient.GetGrain<IGAgent>(GrainId.Create($"{ns}{AevatarCoreConstants.GAgentNamespaceSeparator}{alias}",
                primaryKey.ToString("N")));
        await ConfigGAgentAsync(gAgent, configuration);
        return gAgent;
    }

    public async Task<IGAgent> GetGAgentAsync(string alias, string ns,
        ConfigurationBase? configuration = null)
    {
        return await GetGAgentAsync(Guid.NewGuid(), alias, ns, configuration);
    }

    public async Task<IGAgent> GetGAgentAsync(Guid primaryKey, Type gAgentType,
        ConfigurationBase? configuration = null)
    {
        return await GetGAgentAsync(primaryKey, gAgentType.Name, gAgentType.Namespace!,
            configuration: configuration);
    }

    public async Task<IGAgent> GetGAgentAsync(Type gAgentType, ConfigurationBase? configuration = null)
    {
        return await GetGAgentAsync(gAgentType.Name, ns: gAgentType.Namespace!,
            configuration: configuration);
    }

    public async Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(Guid primaryKey,
        ConfigurationBase? configuration = null)
        where TGrainInterface : IGAgent
    {
        var gAgent = _clusterClient.GetGrain<TGrainInterface>(primaryKey);
        await ConfigGAgentAsync(gAgent, configuration);
        return gAgent;
    }

    public async Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(GrainId grainId,
        ConfigurationBase? configuration = null)
        where TGrainInterface : IGAgent
    {
        var gAgent = _clusterClient.GetGrain<TGrainInterface>(grainId);
        await ConfigGAgentAsync(gAgent, configuration);
        return gAgent;
    }

    public Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(ConfigurationBase? configuration = null)
        where TGrainInterface : IGAgent
    {
        return GetGAgentAsync<TGrainInterface>(Guid.NewGuid(), configuration);
    }

    public async Task<IArtifactGAgent<TArtifact, TState, TStateLogEvent>> GetArtifactGAgentAsync<TArtifact, TState, TStateLogEvent>(ConfigurationBase? configuration = null) where TArtifact : IArtifact<TState, TStateLogEvent>, new() where TState : StateBase, new() where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        return await GetGAgentAsync<IArtifactGAgent<TArtifact, TState, TStateLogEvent>>(configuration);
    }

    private async Task ConfigGAgentAsync(IGAgent gAgent, ConfigurationBase? configuration)
    {
        // Must activate the GAgent before sending events.
        await gAgent.ActivateAsync();
        if (configuration != null)
        {
            await gAgent.ConfigAsync(configuration);
        }
    }
}