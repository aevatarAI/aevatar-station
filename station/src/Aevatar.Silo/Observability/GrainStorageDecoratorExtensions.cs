using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Storage;

namespace Aevatar.Silo.Observability
{
    /// <summary>
    /// Extension methods for registering grain storage decorators
    /// </summary>
    public static class GrainStorageDecoratorExtensions
    {
        /// <summary>
        /// Replaces the default IGrainStorage implementation with a MetricsGrainStorage decorator
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection ReplaceDefaultImplementation(this IServiceCollection services)
        {
            var originalDescriptor = services.FirstOrDefault(
                sd =>
                    !sd.IsKeyedService &&
                    sd.ServiceType == typeof(IGrainStorage));

            if (originalDescriptor == null)
                return services;
                
            // Remove the original registration
            services.Remove(originalDescriptor);

            Func<IServiceProvider, IGrainStorage> innerFactory = null;

            if (originalDescriptor.ImplementationFactory != null)
            {
                innerFactory = (sp) => (IGrainStorage)originalDescriptor.ImplementationFactory(sp);
            }
            else if (originalDescriptor.ImplementationInstance != null)
            {
                innerFactory = (sp) => (IGrainStorage)originalDescriptor.ImplementationInstance;
            }
            else
            {
                innerFactory = (sp) =>
                    (IGrainStorage)sp.GetRequiredService(originalDescriptor.ImplementationType);
            }

            // Add the decorator with the original key
            services.AddSingleton<IGrainStorage>((sp) =>
                new MetricsGrainStorage(
                    innerFactory(sp),
                    sp.GetRequiredService<ILogger<MetricsGrainStorage>>()));
                    
            return services;
        }

        /// <summary>
        /// Replaces a keyed IGrainStorage implementation with a MetricsGrainStorage decorator
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="name">The key name of the service to replace</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection ReplaceKeyedImplementation(this IServiceCollection services, string name)
        {
            var originalDescriptor = services.FirstOrDefault(sd =>
                sd.ServiceKey?.ToString() == name &&
                sd.ServiceType == typeof(IGrainStorage));

            if (originalDescriptor == null)
                return services;
                
            // Remove the original registration
            services.Remove(originalDescriptor);

            Func<IServiceProvider, object?, IGrainStorage> innerFactory = null;

            if (originalDescriptor.KeyedImplementationFactory != null)
            {
                innerFactory = (sp, key) => (IGrainStorage)originalDescriptor.KeyedImplementationFactory(sp, key);
            }
            else if (originalDescriptor.KeyedImplementationInstance != null)
            {
                innerFactory = (sp, key) => (IGrainStorage)originalDescriptor.KeyedImplementationInstance;
            }
            else
            {
                innerFactory = (sp, key) =>
                    (IGrainStorage)sp.GetRequiredService(originalDescriptor.KeyedImplementationType);
            }

            // Add the decorator with the original key
            services.AddKeyedSingleton<IGrainStorage>(name, (sp, key) =>
                new MetricsGrainStorage(
                    innerFactory(sp, key),
                    sp.GetRequiredService<ILogger<MetricsGrainStorage>>()));
                    
            return services;
        }

        /// <summary>
        /// Wraps all registered IGrainStorage implementations with MetricsGrainStorage decorators
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection UseGrainStorageWithMetrics(this IServiceCollection services)
        {
            var allKeys = services.Where(s => s.IsKeyedService && s.ServiceType == typeof(IGrainStorage))
                .Select(s => s.ServiceKey.ToString()).ToList();
                
            foreach (var key in allKeys)
            {
                services.ReplaceKeyedImplementation(key);
            }

            services.ReplaceDefaultImplementation();
            return services;
        }
    }
}
