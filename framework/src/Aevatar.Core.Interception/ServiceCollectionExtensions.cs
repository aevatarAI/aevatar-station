using System;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Core.Interception.Services;

namespace Aevatar.Core.Interception;

/// <summary>
/// Extensions for configuring interception services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds interception services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInterception(this IServiceCollection services)
    {
        // Register the trace manager service
        services.AddSingleton<ITraceManager, TraceManager>();
        
        return services;
    }

    /// <summary>
    /// Adds interception services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">Action to configure the interception services.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInterception(
        this IServiceCollection services,
        Action<InterceptionOptions> configure)
    {
        // Register the trace manager service
        services.AddSingleton<ITraceManager, TraceManager>();
        
        // Configure options if provided
        if (configure != null)
        {
            var options = new InterceptionOptions();
            configure(options);
            
            // Register options if needed
            if (options.EnableTraceManager)
            {
                services.AddSingleton(options);
            }
        }
        
        return services;
    }
}

/// <summary>
/// Configuration options for interception services.
/// </summary>
public class InterceptionOptions
{
    /// <summary>
    /// Gets or sets whether to enable the trace manager service.
    /// </summary>
    public bool EnableTraceManager { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to enable runtime trace configuration.
    /// </summary>
    public bool EnableRuntimeTraceConfiguration { get; set; } = true;
}
