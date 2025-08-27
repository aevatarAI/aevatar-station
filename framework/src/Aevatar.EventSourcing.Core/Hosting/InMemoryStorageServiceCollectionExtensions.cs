using Aevatar.EventSourcing.Core.LogConsistency;
using Aevatar.EventSourcing.Core.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans;
using Orleans.EventSourcing;
using Orleans.Providers;
using Orleans.Runtime;

namespace Aevatar.EventSourcing.Core.Hosting;

public static class InMemoryStorageServiceCollectionExtensions
{
    public static IServiceCollection AddInMemoryBasedLogConsistencyProviderAsDefault(this IServiceCollection services)
    {
        return services.AddInMemoryBasedLogConsistencyProvider(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME);
    }

    public static IServiceCollection AddInMemoryBasedLogConsistencyProvider(this IServiceCollection services,
        string name)
    {
        // Configure log storage.
        if (string.Equals(name, ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, StringComparison.Ordinal))
        {
            services.TryAddSingleton(sp =>
                sp.GetKeyedService<ILogConsistentStorage>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME));
        }

        services.AddKeyedSingleton<ILogConsistentStorage>(name, InMemoryLogConsistentStorageFactory.Create);
        services.AddKeyedSingleton<ILifecycleParticipant<ISiloLifecycle>>(name,
            (sp, n) => (ILifecycleParticipant<ISiloLifecycle>)sp.GetKeyedService<ILogConsistentStorage>(n));

        // Configure log consistency.
        services.TryAddSingleton<Factory<IGrainContext, ILogConsistencyProtocolServices>>(serviceProvider =>
        {
            var protocolServicesFactory =
                ActivatorUtilities.CreateFactory(typeof(DefaultProtocolServices), [typeof(IGrainContext)]);
            return grainContext =>
                (ILogConsistencyProtocolServices)protocolServicesFactory(serviceProvider,
                    [grainContext]);
        });

        // Configure log view adaptor.
        if (string.Equals(name, ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, StringComparison.Ordinal))
        {
            services.TryAddSingleton(sp =>
                sp.GetKeyedService<ILogViewAdaptorFactory>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME));
        }

        services.AddKeyedSingleton(name, LogConsistencyProviderFactory.Create);
        return services;
    }
}