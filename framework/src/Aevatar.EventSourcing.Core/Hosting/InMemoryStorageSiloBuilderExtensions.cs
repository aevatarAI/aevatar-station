using Orleans.Hosting;
using Orleans.Providers;

namespace Aevatar.EventSourcing.Core.Hosting;

public static class InMemoryStorageSiloBuilderExtensions
{
    public static ISiloBuilder AddInMemoryBasedLogConsistencyProviderAsDefault(this ISiloBuilder builder)
    {
        return builder.AddInMemoryBasedLogConsistencyProvider(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME);
    }

    public static ISiloBuilder AddInMemoryBasedLogConsistencyProvider(this ISiloBuilder builder, string name)
    {
        return builder.ConfigureServices(services => services.AddInMemoryBasedLogConsistencyProvider(name));
    }
}