using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Silo.AgentWarmup.Strategies;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Aevatar.Silo.AgentWarmup.Extensions;

/// <summary>
/// Extension methods for integrating agent warmup with Orleans
/// 
/// Usage Examples:
/// 
/// For GUID-based agents:
/// services.AddAgentWarmup<Guid>();
/// builder.AddAgentWarmup<Guid>();
/// 
/// For string-based agents:
/// services.AddAgentWarmup<string>();
/// builder.AddAgentWarmup<string>();
/// 
/// For long-based agents:
/// services.AddAgentWarmup<long>();
/// builder.AddAgentWarmup<long>();
/// 
/// With configuration:
/// services.AddAgentWarmup<Guid>(options => {
///     options.Enabled = true;
///     options.MaxConcurrency = 10;
/// });
/// </summary>
public static class AgentWarmupExtensions
{


    /// <summary>
    /// Adds agent warmup services for a specific identifier type to the service collection
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type (Guid, string, long, int)</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAgentWarmup<TIdentifier>(
        this IServiceCollection services,
        Action<AgentWarmupConfiguration>? configureOptions = null)
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
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("AgentWarmup.MongoDB");
            
            // Use the same database name that Orleans is configured to use
            var orleansSection = configuration.GetSection("Orleans");
            var databaseName = orleansSection.GetValue<string>("DataBase");
            
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new InvalidOperationException("Orleans:DataBase configuration is required for agent warmup MongoDB access");
            }
            
            logger.LogInformation("Agent warmup using Orleans MongoDB database: {DatabaseName}", databaseName);
            return mongoClient.GetDatabase(databaseName);
        });
        
        // Register core services
        services.AddSingleton<IAgentDiscoveryService, AgentDiscoveryService>();
        services.AddSingleton<IMongoDbAgentIdentifierService, MongoDbAgentIdentifierService>();
        services.AddSingleton<IAgentWarmupOrchestrator<TIdentifier>, AgentWarmupOrchestrator<TIdentifier>>();
        
        // Register default strategy for the specific identifier type
        services.AddSingleton<IAgentWarmupStrategy>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<DefaultAgentWarmupStrategy<TIdentifier>>>();
            return new DefaultAgentWarmupStrategy<TIdentifier>(
                provider.GetRequiredService<IMongoDbAgentIdentifierService>(),
                provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AgentWarmupConfiguration>>(),
                logger);
        });
        
        // Register the warmup service using standard hosted service pattern
        services.AddSingleton<IAgentWarmupService, AgentWarmupService<TIdentifier>>();
        services.AddHostedService<AgentWarmupService<TIdentifier>>();
        
        return services;
    }
    


    /// <summary>
    /// Adds agent warmup services to the Orleans silo builder for a specific identifier type
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type (Guid, string, long, int)</typeparam>
    /// <param name="builder">The silo builder</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>The silo builder for chaining</returns>
    public static ISiloBuilder AddAgentWarmup<TIdentifier>(
        this ISiloBuilder builder,
        Action<AgentWarmupConfiguration>? configureOptions = null)
    {
        builder.ConfigureServices(services => services.AddAgentWarmup<TIdentifier>(configureOptions));
        return builder;
    }
    
    /// <summary>
    /// Registers a custom agent warmup strategy
    /// </summary>
    /// <typeparam name="TStrategy">The strategy implementation type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAgentWarmupStrategy<TStrategy>(
        this IServiceCollection services)
        where TStrategy : class, IAgentWarmupStrategy
    {
        services.AddSingleton<IAgentWarmupStrategy, TStrategy>();
        return services;
    }
    
    /// <summary>
    /// Registers a custom agent warmup strategy with a factory
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="strategyFactory">Factory function to create the strategy</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAgentWarmupStrategy(
        this IServiceCollection services,
        Func<IServiceProvider, IAgentWarmupStrategy> strategyFactory)
    {
        services.AddSingleton(strategyFactory);
        return services;
    }

    /// <summary>
    /// Adds a predefined agent warmup strategy for a specific agent type with predefined identifiers
    /// </summary>
    /// <typeparam name="TAgent">The agent type to warm up</typeparam>
    /// <typeparam name="TIdentifier">The identifier type (Guid, string, int, long)</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="strategyName">Name for the strategy</param>
    /// <param name="agentIdentifiers">Collection of predefined agent identifiers</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPredefinedAgentWarmupStrategy<TAgent, TIdentifier>(
        this IServiceCollection services,
        string strategyName,
        IEnumerable<TIdentifier> agentIdentifiers)
    {
        services.AddSingleton<IAgentWarmupStrategy>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<PredefinedAgentWarmupStrategy<TIdentifier>>>();
            return new PredefinedAgentWarmupStrategy<TIdentifier>(
                strategyName,
                typeof(TAgent),
                agentIdentifiers,
                logger);
        });
        return services;
    }

    /// <summary>
    /// Adds a sample-based agent warmup strategy that randomly selects a percentage of agents from MongoDB
    /// </summary>
    /// <typeparam name="TAgent">The agent type to warm up</typeparam>
    /// <typeparam name="TIdentifier">The identifier type (Guid, string, int, long)</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="strategyName">Name for the strategy</param>
    /// <param name="sampleRatio">The ratio of agents to sample (0.0 to 1.0, e.g., 0.1 for 10%)</param>
    /// <param name="randomSeed">Optional random seed for deterministic sampling (useful for testing)</param>
    /// <param name="batchSize">Number of agents to process before adding a small delay</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSampleBasedAgentWarmupStrategy<TAgent, TIdentifier>(
        this IServiceCollection services,
        string strategyName,
        double sampleRatio,
        int? randomSeed = null,
        int batchSize = 100)
    {
        services.AddSingleton<IAgentWarmupStrategy>(provider =>
        {
            var mongoDbService = provider.GetRequiredService<IMongoDbAgentIdentifierService>();
            var logger = provider.GetRequiredService<ILogger<SampleBasedAgentWarmupStrategy<TIdentifier>>>();
            return new SampleBasedAgentWarmupStrategy<TIdentifier>(
                strategyName,
                typeof(TAgent),
                sampleRatio,
                mongoDbService,
                logger,
                randomSeed,
                batchSize);
        });
        return services;
    }

    /// <summary>
    /// Configure agent warmup with direct Type-based base types (for agent discovery)
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type (Guid, string, long, int)</typeparam>
    /// <typeparam name="TBaseType">The base agent type for discovery</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAgentWarmupWithBaseType<TIdentifier, TBaseType>(this IServiceCollection services)
        where TBaseType : class
    {
        return services.AddAgentWarmup<TIdentifier>(config =>
        {
            config.AutoDiscovery.BaseTypes.Add(typeof(TBaseType));
        });
    }

    /// <summary>
    /// Configure agent warmup with multiple base types directly (for agent discovery)
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type (Guid, string, long, int)</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="baseTypes">The base agent types for discovery</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAgentWarmupWithBaseTypes<TIdentifier>(this IServiceCollection services, params Type[] baseTypes)
    {
        return services.AddAgentWarmup<TIdentifier>(config =>
        {
            config.AutoDiscovery.BaseTypes.AddRange(baseTypes);
        });
    }

    /// <summary>
    /// Configure agent warmup for Orleans silo with direct Type-based base types (for agent discovery)
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type (Guid, string, long, int)</typeparam>
    /// <typeparam name="TBaseType">The base agent type for discovery</typeparam>
    /// <param name="builder">The silo builder</param>
    /// <returns>The silo builder for chaining</returns>
    public static ISiloBuilder AddAgentWarmupWithBaseType<TIdentifier, TBaseType>(this ISiloBuilder builder)
        where TBaseType : class
    {
        return builder.AddAgentWarmup<TIdentifier>(config =>
        {
            config.AutoDiscovery.BaseTypes.Add(typeof(TBaseType));
        });
    }

    /// <summary>
    /// Configure agent warmup for Orleans silo with multiple base types directly (for agent discovery)
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type (Guid, string, long, int)</typeparam>
    /// <param name="builder">The silo builder</param>
    /// <param name="baseTypes">The base agent types for discovery</param>
    /// <returns>The silo builder for chaining</returns>
    public static ISiloBuilder AddAgentWarmupWithBaseTypes<TIdentifier>(this ISiloBuilder builder, params Type[] baseTypes)
    {
        return builder.AddAgentWarmup<TIdentifier>(config =>
        {
            config.AutoDiscovery.BaseTypes.AddRange(baseTypes);
        });
    }
} 