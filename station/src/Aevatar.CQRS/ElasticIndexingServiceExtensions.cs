using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.CQRS;

namespace Aevatar;

public static class ElasticIndexingServiceExtensions
{
    public static IServiceCollection UseElasticIndexingWithMetrics(this IServiceCollection services)
    {
        var originalDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IIndexingService));
        if (originalDescriptor == null)
            return services;

        services.Remove(originalDescriptor);

        Func<IServiceProvider, IIndexingService> innerFactory = null;
        if (originalDescriptor.ImplementationFactory != null)
        {
            innerFactory = (sp) => (IIndexingService)originalDescriptor.ImplementationFactory(sp);
        }
        else if (originalDescriptor.ImplementationInstance != null)
        {
            innerFactory = (sp) => (IIndexingService)originalDescriptor.ImplementationInstance;
        }
        else
        {
            innerFactory = (sp) => (IIndexingService)sp.GetRequiredService(originalDescriptor.ImplementationType);
        }

        services.AddSingleton<IIndexingService>(sp =>
            new MetricsElasticIndexingService(
                innerFactory(sp),
                sp.GetRequiredService<ILogger<MetricsElasticIndexingService>>()
            ));
        return services;
    }
} 