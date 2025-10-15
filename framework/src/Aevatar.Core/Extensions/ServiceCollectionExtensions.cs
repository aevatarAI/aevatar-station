using Aevatar.Core.Abstractions.StateManagement;
using Aevatar.Core.StateManagement;
using Microsoft.Extensions.DependencyInjection;

namespace Aevatar.Core.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register Core services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Core state publisher services to the DI container.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCoreStatePublisher(this IServiceCollection services)
    {
        services.AddSingleton<IStatePublisher, StatePublisher>();
        return services;
    }

    /// <summary>
    /// Adds all Core services including observer management and state publishing.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddCoreStatePublisher();
        // Add other core services here as they are created
        return services;
    }
} 