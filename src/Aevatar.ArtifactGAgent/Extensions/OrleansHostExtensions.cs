using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Aevatar.ArtifactGAgent.Extensions;

public static class OrleansHostExtensions
{
    public static ISiloBuilder UseArtifactGAgent(this ISiloBuilder siloBuilder)
    {
        return siloBuilder
            .ConfigureServices(services =>
            {
                services.AddSingleton<IGAgentFactory, GAgentFactory>();
                services.AddSingleton<ApplicationPartManager>();
                services.AddSingleton<ArtifactGAgentLoader>();
                services.AddSingleton<ILifecycleParticipant<ISiloLifecycle>>(sp =>
                    sp.GetRequiredService<ArtifactGAgentLoader>());
            });
    }
}