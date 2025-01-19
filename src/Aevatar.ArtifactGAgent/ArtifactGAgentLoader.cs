using System.Reflection;
using Microsoft.Extensions.Logging;
using Aevatar.Core.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

public class ArtifactGAgentLoader : ILifecycleParticipant<ISiloLifecycle>
{
    private readonly ILogger<ArtifactGAgentLoader> _logger;
    private readonly ApplicationPartManager _applicationPartManager;

    public ArtifactGAgentLoader(ILogger<ArtifactGAgentLoader> logger, ApplicationPartManager applicationPartManager)
    {
        _logger = logger;
        _applicationPartManager = applicationPartManager;
    }

    public void Participate(ISiloLifecycle lifecycle)
    {
        lifecycle.Subscribe(nameof(ArtifactGAgentLoader), ServiceLifecycleStage.ApplicationServices, async ct =>
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies.Where(a => a.FullName.StartsWith("Aevatar")))
                {
                    var artifactGAgentTypes = assembly.GetTypes()
                        .Where(t => typeof(IArtifactGAgent).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
                        .ToList();

                    foreach (var type in artifactGAgentTypes)
                    {
                        _applicationPartManager.ApplicationParts.Add(new AssemblyPart(type.Assembly));
                        _applicationPartManager.FeatureProviders.Add(new CustomFeatureProvider());
                        _logger.LogInformation($"Added {type.FullName} to ApplicationPartManager.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load IArtifactGAgent implementations.");
            }
        });
    }
}

public class CustomFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies.Where(a => a.FullName.StartsWith("Aevatar")))
        {
            var types = assembly.GetTypes()
                .Where(t => typeof(IArtifactGAgent).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });

            foreach (var type in types)
            {
                feature.Controllers.Add(type.GetTypeInfo());
            }
        }
    }
}