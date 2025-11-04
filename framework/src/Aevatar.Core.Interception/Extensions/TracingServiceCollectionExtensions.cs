using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Aevatar.Core.Interception.Configurations;
using Aevatar.Core.Interception.Models;

namespace Aevatar.Core.Interception.Extensions;

/// <summary>
/// Extensions for configuring tracing services with Fody IL weaving.
/// </summary>
public static class TracingServiceCollectionExtensions
{
    /// <summary>
    /// Adds tracing services based on the specified configuration.
    /// </summary>
    public static IServiceCollection AddTracing(
        this IServiceCollection services,
        InterceptionConfiguration config)
    {
        // Always add the trace configuration
        services.AddSingleton(config);
        services.AddSingleton(config.TraceConfig);
        
        switch (config.Mode)
        {
            case InterceptionMode.None:
                // No tracing services registered
                break;
                
            case InterceptionMode.Fody:
                services.AddFodyTracing(config);
                break;
                
            default:
                throw new ArgumentException($"Unsupported interception mode: {config.Mode}");
        }
        
        return services;
    }
    
    /// <summary>
    /// Adds tracing services with configuration from IConfiguration.
    /// </summary>
    public static IServiceCollection AddTracing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var config = new InterceptionConfiguration();
        configuration.GetSection("Tracing").Bind(config);
        
        return services.AddTracing(config);
    }
    
    /// <summary>
    /// Adds tracing services with a configuration action.
    /// </summary>
    public static IServiceCollection AddTracing(
        this IServiceCollection services,
        Action<InterceptionConfiguration> configure)
    {
        var config = new InterceptionConfiguration();
        configure(config);
        
        return services.AddTracing(config);
    }
    
    /// <summary>
    /// Adds Fody tracing services.
    /// </summary>
    private static IServiceCollection AddFodyTracing(
        this IServiceCollection services,
        InterceptionConfiguration config)
    {
        // Fody weavers handle everything at build time
        // No runtime services needed for basic tracing
        
        // Register tracing service factory
        services.AddSingleton<ITracingServiceFactory, FodyTracingServiceFactory>();
        
        return services;
    }
}

/// <summary>
/// Factory interface for creating tracing services based on configuration.
/// </summary>
public interface ITracingServiceFactory
{
    /// <summary>
    /// Creates a tracing service of the specified type.
    /// </summary>
    T CreateService<T>(IServiceProvider serviceProvider) where T : class;
}

/// <summary>
/// Factory for Fody tracing services.
/// </summary>
public class FodyTracingServiceFactory : ITracingServiceFactory
{
    public T CreateService<T>(IServiceProvider serviceProvider) where T : class
    {
        // Fody services are created normally - no factory needed
        return serviceProvider.GetRequiredService<T>();
    }
}
