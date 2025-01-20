using Aevatar.Core.Abstractions;
using Orleans.Configuration;

namespace Aevatar.ArtifactGAgent.Extensions;

public static class OrleansHostExtensions
{
    public static ISiloBuilder UseArtifactGAgent(this ISiloBuilder siloBuilder)
    {
        return siloBuilder
            .Configure<GrainTypeOptions>(RegisterArtifactGAgents);
    }
    
    private static void RegisterArtifactGAgents(GrainTypeOptions options)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IArtifactGAgent).IsAssignableFrom(t));
            foreach (var type in types)
            {
                options.Classes.Add(type);
            }
        }
    }
}