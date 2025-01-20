using Aevatar.Core.Abstractions;

namespace Aevatar.ArtifactGAgent.Extensions;

public static class GAgentFactoryExtensions
{
    public static async Task<IGAgent> GetGAgentAsync(this IGAgentFactory gAgentFactory, Type artifactType, Guid? primaryKey = null, InitializationEventBase? initializeDto = null)
    {
        primaryKey ??= Guid.NewGuid();
        var alias = artifactType.Name;
        var ns = artifactType.Namespace!.ToLower().Replace('.', '/');
        return await gAgentFactory.GetGAgentAsync(alias, primaryKey.Value, ns, initializeDto);
    }
}