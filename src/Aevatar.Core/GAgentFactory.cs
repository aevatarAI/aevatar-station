using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Streams;

namespace Aevatar.Core;

public interface IGAgentFactory
{
    Task<IGAgent> GetGAgentAsync(GrainId grainId, InitializeDtoBase? initializeDto = null);

    Task<IGAgent> GetGAgentAsync(string alias, Guid primaryKey,
        string ns = AevatarGAgentConstants.GAgentDefaultNamespace, InitializeDtoBase? initializeDto = null);

    Task<IGAgent> GetGAgentAsync(string alias, string ns = AevatarGAgentConstants.GAgentDefaultNamespace,
        InitializeDtoBase? initializeDto = null);

    Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(Guid primaryKey, InitializeDtoBase? initializeDto = null)
        where TGrainInterface : IGAgent;

    Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(InitializeDtoBase? initializeDto = null)
        where TGrainInterface : IGAgent;

    List<Type> GetAvailableGAgentTypes();
}

public class GAgentFactory : IGAgentFactory
{
    private readonly IClusterClient _grainFactory;
    private readonly IStreamProvider _streamProvider;

    public GAgentFactory(IClusterClient grainFactory)
    {
        _grainFactory = grainFactory;
        _streamProvider =
            _grainFactory.ServiceProvider.GetRequiredKeyedService<IStreamProvider>(AevatarCoreConstants.StreamProvider);
    }

    public async Task<IGAgent> GetGAgentAsync(GrainId grainId, InitializeDtoBase? initializeDto = null)
    {
        var gAgent = _grainFactory.GetGrain<IGAgent>(grainId);
        if (initializeDto != null)
        {
            await InitializeAsync(gAgent, new EventWrapper<EventBase>(initializeDto, Guid.NewGuid(), grainId));
        }

        return gAgent;
    }

    public async Task<IGAgent> GetGAgentAsync(string alias, Guid primaryKey, string ns = "aevatar", InitializeDtoBase? initializeDto = null)
    {
        var gAgent = _grainFactory.GetGrain<IGAgent>(GrainId.Create($"{ns}/{alias}", primaryKey.ToString()));
        await gAgent.ActivateAsync();
        if (initializeDto != null)
        {
            await InitializeAsync(gAgent,
                new EventWrapper<EventBase>(initializeDto, Guid.NewGuid(), gAgent.GetGrainId()));
        }

        return gAgent;
    }

    public async Task<IGAgent> GetGAgentAsync(string alias, string ns = "aevatar", InitializeDtoBase? initializeDto = null)
    {
        return await GetGAgentAsync(alias, Guid.NewGuid(), ns, initializeDto);
    }

    public async Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(Guid primaryKey, InitializeDtoBase? initializeDto = null)
        where TGrainInterface : IGAgent
    {
        var gAgent = _grainFactory.GetGrain<TGrainInterface>(primaryKey);
        if (initializeDto != null)
        {
            await InitializeAsync(gAgent,
                new EventWrapper<EventBase>(initializeDto, Guid.NewGuid(), gAgent.GetGrainId()));
        }

        return gAgent;
    }

    public Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(InitializeDtoBase? initializeDto = null)
        where TGrainInterface : IGAgent
    {
        var guid = Guid.NewGuid();
        return GetGAgentAsync<TGrainInterface>(guid, initializeDto);
    }

    public List<Type> GetAvailableGAgentTypes()
    {
        var gAgentType = typeof(IGAgent);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var gAgentTypes = new List<Type>();

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => gAgentType.IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });
            gAgentTypes.AddRange(types);
        }

        return gAgentTypes;
    }

    private async Task InitializeAsync(IGAgent gAgent, EventWrapperBase eventWrapper)
    {
        var stream = await gAgent.GetStreamAsync();
        await stream.OnNextAsync(eventWrapper);
    }
}