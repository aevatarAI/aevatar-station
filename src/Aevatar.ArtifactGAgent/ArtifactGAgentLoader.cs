using Microsoft.Extensions.Logging;
using Aevatar.Core.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

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
                        .Where(t => typeof(IArtifactGAgent).IsAssignableFrom(t) &&
                                    t is { IsClass: true, IsAbstract: false })
                        .ToList();

                    if (artifactGAgentTypes.Count != 0)
                    {
                        _applicationPartManager.ApplicationParts.Add(new AssemblyPart(assembly));
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