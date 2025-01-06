using Aevatar.Core.Abstractions;

namespace Aevatar.Core;

public interface IGAgentFactory
{
    Task<IGAgent> GetGAgentAsync(GrainId grainId, InitializeDtoBase? initializeDto = null);

    Task<IGAgent> GetGAgentAsync<TGrainInterface>(Guid primaryKey, InitializeDtoBase? initializeDto = null)
        where TGrainInterface : IGAgent;

    List<Type> GetAvailableGAgentTypes();
}

public class GAgentFactory : IGAgentFactory
{
    private readonly IGrainFactory _grainFactory;

    public GAgentFactory(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }
    
    public async Task<IGAgent> GetGAgentAsync(GrainId grainId, InitializeDtoBase? initializeDto = null)
    {
        dynamic gAgent = _grainFactory.GetGrain<IGAgent>(grainId);
        if (initializeDto != null)
        {
            await gAgent.InitializeAsync(initializeDto);
        }

        return gAgent;
    }

    public async Task<IGAgent> GetGAgentAsync<TGrainInterface>(Guid primaryKey, InitializeDtoBase? initializeDto = null) where TGrainInterface : IGAgent
    {
        dynamic gAgent = _grainFactory.GetGrain<TGrainInterface>(primaryKey);
        if (initializeDto != null)
        {
            await gAgent.InitializeAsync(initializeDto);
        }

        return gAgent;
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
}