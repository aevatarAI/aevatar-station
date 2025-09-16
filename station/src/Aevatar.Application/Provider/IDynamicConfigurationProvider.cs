using System.Collections.Concurrent;
using System.Threading.Tasks;
using Orleans;

namespace Aevatar.Provider;

public interface IDynamicConfigurationProvider
{
    Task ProcessSchemaAsync(ConcurrentDictionary<string, object> concurrentData, IClusterClient clusterClient);
}

public abstract class DynamicConfigurationProviderBase : IDynamicConfigurationProvider
{
    public abstract Task ProcessSchemaAsync(ConcurrentDictionary<string, object> concurrentData, IClusterClient clusterClient);
}
