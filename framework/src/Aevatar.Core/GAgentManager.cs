using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Metadata;

namespace Aevatar.Core;

public class GAgentManager : IGAgentManager
{
    private readonly GrainTypeResolver _grainTypeResolver;

    public GAgentManager(IClusterClient clusterClient)
    {
        _grainTypeResolver = clusterClient.ServiceProvider.GetRequiredService<GrainTypeResolver>();
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

    public List<Type> GetAvailableEventTypes()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var eventTypes = new List<Type>();

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(EventBase)) && t is { IsClass: true, IsAbstract: false });
            eventTypes.AddRange(types);
        }

        return eventTypes;
    }

    public List<GrainType> GetAvailableGAgentGrainTypes()
    {
        var types = GetAvailableGAgentTypes();
        var grainTypes = types.Select(_grainTypeResolver.GetGrainType);
        return grainTypes.ToList();
    }
}