using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Silo.GrainWarmup.Strategies;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Aevatar.Silo.GrainWarmup.Extensions;

/// <summary>
/// Extension methods for integrating grain warmup with Orleans
/// 
/// Usage Examples:
/// 
/// For GUID-based grains:
/// services.AddGrainWarmup<Guid>();
/// builder.AddGrainWarmup<Guid>();
/// 
/// For string-based grains:
/// services.AddGrainWarmup<string>();
/// builder.AddGrainWarmup<string>();
/// 
/// For long-based grains:
/// services.AddGrainWarmup<long>();
/// builder.AddGrainWarmup<long>();
/// 
/// With configuration:
/// services.AddGrainWarmup<Guid>(options => {
///     options.Enabled = true;
///     options.MaxConcurrency = 10;
/// });
/// </summary>
public static class GrainWarmupExtensions
{
    /// <summary>
    /// Adds grain warmup services for GUID-based grains to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddGrainWarmupForGuid(
        this IServiceCollection services,
        Action<GrainWarmupConfiguration>? configureOptions = null)
    {
        return AddGrainWarmup<Guid>(services, configureOptions);
    }

    /// <summary>
    /// Adds grain warmup services for string-based grains to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddGrainWarmupForString(
        this IServiceCollection services,
        Action<GrainWarmupConfiguration>? configureOptions = null)
    {
        return AddGrainWarmup<string>(services, configureOptions);
    }

    /// <summary>
    /// Adds grain warmup services for long-based grains to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddGrainWarmupForLong(
        this IServiceCollection services,
        Action<GrainWarmupConfiguration>? configureOptions = null)
    {
        return AddGrainWarmup<long>(services, configureOptions);
    }

    /// <summary>
    /// Adds grain warmup services for a specific identifier type to the service collection
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type (Guid, string, long, int)</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddGrainWarmup<TIdentifier>(
        this IServiceCollection services,
        Action<GrainWarmupConfiguration>? configureOptions = null)
    {
        // Register configuration
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        
        // Use the Orleans-registered MongoDB client and database instead of creating a separate one
        services.AddSingleton<IMongoDatabase>(provider =>
        {
            var mongoClient = provider.GetRequiredService<IMongoClient>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("GrainWarmup.MongoDB");
            
            // Use the same database name that Orleans is configured to use
            var orleansSection = configuration.GetSection("Orleans");
            var databaseName = orleansSection.GetValue<string>("DataBase");
            
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new InvalidOperationException("Orleans:DataBase configuration is required for grain warmup MongoDB access");
            }
            
            logger.LogInformation("Grain warmup using Orleans MongoDB database: {DatabaseName}", databaseName);
            return mongoClient.GetDatabase(databaseName);
        });
        
        // Register core services
        services.AddSingleton<IGrainDiscoveryService, GrainDiscoveryService>();
        services.AddSingleton<IMongoDbGrainIdentifierService, MongoDbGrainIdentifierService>();
        services.AddSingleton<IGrainWarmupOrchestrator<TIdentifier>, GrainWarmupOrchestrator<TIdentifier>>();
        
        // Register default strategy for the specific identifier type
        services.AddSingleton<IGrainWarmupStrategy>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<DefaultGrainWarmupStrategy<TIdentifier>>>();
            return new DefaultGrainWarmupStrategy<TIdentifier>(
                provider.GetRequiredService<IMongoDbGrainIdentifierService>(),
                provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<GrainWarmupConfiguration>>(),
                logger);
        });
        
        // Register the warmup service using standard hosted service pattern
        services.AddSingleton<IGrainWarmupService, GrainWarmupService<TIdentifier>>();
        services.AddHostedService<GrainWarmupService<TIdentifier>>();
        
        return services;
    }
    
    /// <summary>
    /// Adds grain warmup services to the Orleans silo builder for GUID-based grains
    /// </summary>
    /// <param name="builder">The silo builder</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>The silo builder for chaining</returns>
    public static ISiloBuilder AddGrainWarmupForGuid(
        this ISiloBuilder builder,
        Action<GrainWarmupConfiguration>? configureOptions = null)
    {
        builder.ConfigureServices(services => services.AddGrainWarmupForGuid(configureOptions));
        return builder;
    }

    /// <summary>
    /// Adds grain warmup services to the Orleans silo builder for string-based grains
    /// </summary>
    /// <param name="builder">The silo builder</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>The silo builder for chaining</returns>
    public static ISiloBuilder AddGrainWarmupForString(
        this ISiloBuilder builder,
        Action<GrainWarmupConfiguration>? configureOptions = null)
    {
        builder.ConfigureServices(services => services.AddGrainWarmupForString(configureOptions));
        return builder;
    }

    /// <summary>
    /// Adds grain warmup services to the Orleans silo builder for long-based grains
    /// </summary>
    /// <param name="builder">The silo builder</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>The silo builder for chaining</returns>
    public static ISiloBuilder AddGrainWarmupForLong(
        this ISiloBuilder builder,
        Action<GrainWarmupConfiguration>? configureOptions = null)
    {
        builder.ConfigureServices(services => services.AddGrainWarmupForLong(configureOptions));
        return builder;
    }

    /// <summary>
    /// Adds grain warmup services to the Orleans silo builder for a specific identifier type
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type (Guid, string, long, int)</typeparam>
    /// <param name="builder">The silo builder</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>The silo builder for chaining</returns>
    public static ISiloBuilder AddGrainWarmup<TIdentifier>(
        this ISiloBuilder builder,
        Action<GrainWarmupConfiguration>? configureOptions = null)
    {
        builder.ConfigureServices(services => services.AddGrainWarmup<TIdentifier>(configureOptions));
        return builder;
    }
    
    /// <summary>
    /// Registers a custom grain warmup strategy
    /// </summary>
    /// <typeparam name="TStrategy">The strategy implementation type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddGrainWarmupStrategy<TStrategy>(
        this IServiceCollection services)
        where TStrategy : class, IGrainWarmupStrategy
    {
        services.AddSingleton<IGrainWarmupStrategy, TStrategy>();
        return services;
    }
    
    /// <summary>
    /// Registers a custom grain warmup strategy with a factory
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="strategyFactory">Factory function to create the strategy</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddGrainWarmupStrategy(
        this IServiceCollection services,
        Func<IServiceProvider, IGrainWarmupStrategy> strategyFactory)
    {
        services.AddSingleton(strategyFactory);
        return services;
    }

    /// <summary>
    /// Configure grain warmup with direct Type-based base types (for grain discovery)
    /// </summary>
    /// <typeparam name="TBaseType">The base grain type for discovery</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddGrainWarmupWithBaseType<TBaseType>(this IServiceCollection services)
        where TBaseType : class
    {
        return services.AddGrainWarmup<Guid>(config =>
        {
            config.AutoDiscovery.BaseTypes.Add(typeof(TBaseType));
        });
    }

    /// <summary>
    /// Configure grain warmup with multiple base types directly (for grain discovery)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="baseTypes">The base grain types for discovery</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddGrainWarmupWithBaseTypes(this IServiceCollection services, params Type[] baseTypes)
    {
        return services.AddGrainWarmup<Guid>(config =>
        {
            config.AutoDiscovery.BaseTypes.AddRange(baseTypes);
        });
    }

    /// <summary>
    /// Configure grain warmup for Orleans silo with direct Type-based base types (for grain discovery)
    /// </summary>
    /// <typeparam name="TBaseType">The base grain type for discovery</typeparam>
    /// <param name="builder">The silo builder</param>
    /// <returns>The silo builder for chaining</returns>
    public static ISiloBuilder AddGrainWarmupWithBaseType<TBaseType>(this ISiloBuilder builder)
        where TBaseType : class
    {
        return builder.AddGrainWarmup<Guid>(config =>
        {
            config.AutoDiscovery.BaseTypes.Add(typeof(TBaseType));
        });
    }

    /// <summary>
    /// Configure grain warmup for Orleans silo with multiple base types directly (for grain discovery)
    /// </summary>
    /// <param name="builder">The silo builder</param>
    /// <param name="baseTypes">The base grain types for discovery</param>
    /// <returns>The silo builder for chaining</returns>
    public static ISiloBuilder AddGrainWarmupWithBaseTypes(this ISiloBuilder builder, params Type[] baseTypes)
    {
        return builder.AddGrainWarmup<Guid>(config =>
        {
            config.AutoDiscovery.BaseTypes.AddRange(baseTypes);
        });
    }
} 