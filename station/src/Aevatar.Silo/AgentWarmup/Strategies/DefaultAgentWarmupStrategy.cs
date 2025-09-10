using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aevatar.Silo.AgentWarmup.Strategies;

/// <summary>
/// Default strategy that applies to all agent types not covered by specific strategies
/// Uses MongoDB integration for automatic identifier retrieval
/// </summary>
/// <typeparam name="TIdentifier">The identifier type (Guid, string, int, long)</typeparam>
public class DefaultAgentWarmupStrategy<TIdentifier> : BaseAgentWarmupStrategy<TIdentifier>
{
    private readonly IMongoDbAgentIdentifierService _mongoDbService;
    private readonly DefaultStrategyConfiguration _config;

    public DefaultAgentWarmupStrategy(
        IMongoDbAgentIdentifierService mongoDbService,
        IOptions<AgentWarmupConfiguration> options,
        ILogger<DefaultAgentWarmupStrategy<TIdentifier>> logger)
        : base(logger)
    {
        _mongoDbService = mongoDbService;
        _config = options.Value.DefaultStrategy;
    }

    /// <summary>
    /// Strategy name for identification
    /// </summary>
    public override string Name => "DefaultStrategy";

    /// <summary>
    /// Agent types this strategy applies to (empty = applies to all)
    /// </summary>
    public override IEnumerable<Type> ApplicableAgentTypes => Enumerable.Empty<Type>();

    /// <summary>
    /// Priority for execution order (lowest priority - executes last)
    /// </summary>
    public override int Priority => _config.Priority;

    /// <summary>
    /// Gets the estimated number of agents that will be warmed up
    /// </summary>
    public override int EstimatedAgentCount => _config.MaxIdentifiersPerType;

    /// <summary>
    /// Generates agent identifiers for a specific agent type using MongoDB
    /// </summary>
    /// <param name="agentType">The agent type to generate identifiers for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of agent identifiers</returns>
    public override async IAsyncEnumerable<TIdentifier> GenerateAgentIdentifiersAsync(
        Type agentType, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled)
        {
            Logger.LogDebug("Default strategy is disabled, skipping agent type {AgentType}", agentType.Name);
            yield break;
        }

        if (!ValidateIdentifierType(agentType))
        {
            Logger.LogWarning("Identifier type {IdentifierType} is not compatible with agent type {AgentType}", 
                typeof(TIdentifier).Name, agentType.Name);
            yield break;
        }

        Logger.LogInformation("Default strategy generating identifiers for agent type {AgentType} using {IdentifierSource}", 
            agentType.Name, _config.IdentifierSource);

        switch (_config.IdentifierSource.ToLowerInvariant())
        {
            case "mongodb":
                await foreach (var identifier in GenerateFromMongoDb(agentType, cancellationToken))
                {
                    yield return identifier;
                }
                break;

            case "predefined":
                await foreach (var identifier in GeneratePredefinedIdentifiers(agentType, cancellationToken))
                {
                    yield return identifier;
                }
                break;

            case "range":
                await foreach (var identifier in GenerateRangeIdentifiers(agentType, cancellationToken))
                {
                    yield return identifier;
                }
                break;

            default:
                Logger.LogWarning("Unknown identifier source {IdentifierSource}, falling back to MongoDB", 
                    _config.IdentifierSource);
                await foreach (var identifier in GenerateFromMongoDb(agentType, cancellationToken))
                {
                    yield return identifier;
                }
                break;
        }
    }

    /// <summary>
    /// Generates identifiers from MongoDB collections
    /// </summary>
    private async IAsyncEnumerable<TIdentifier> GenerateFromMongoDb(
        Type agentType, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var identifiers = new List<TIdentifier>();
        try
        {
            // Check if collection exists
            if (!await _mongoDbService.CollectionExistsAsync(agentType))
            {
                Logger.LogInformation("No MongoDB collection found for agent type {AgentType}, skipping", agentType.Name);
                yield break;
            }

            var count = 0;
            await foreach (var identifier in _mongoDbService.GetAgentIdentifiersAsync<TIdentifier>(
                agentType, _config.MaxIdentifiersPerType, cancellationToken))
            {
                identifiers.Add(identifier);
                count++;

                if (count >= _config.MaxIdentifiersPerType)
                {
                    Logger.LogInformation("Reached maximum identifier limit {MaxCount} for agent type {AgentType}", 
                        _config.MaxIdentifiersPerType, agentType.Name);
                    break;
                }
            }

            Logger.LogInformation("Generated {Count} identifiers from MongoDB for agent type {AgentType}", 
                count, agentType.Name);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating identifiers from MongoDB for agent type {AgentType}", agentType.Name);
        }

        foreach (var identifier in identifiers)
        {
            yield return identifier;
        }
    }

    /// <summary>
    /// Generates predefined identifiers (for testing or specific scenarios)
    /// </summary>
    private async IAsyncEnumerable<TIdentifier> GeneratePredefinedIdentifiers(
        Type agentType, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // This could be extended to read from configuration or external sources
        Logger.LogInformation("Generating predefined identifiers for agent type {AgentType}", agentType.Name);

        if (typeof(TIdentifier) == typeof(Guid))
        {
            // Generate a few test GUIDs
            for (int i = 0; i < Math.Min(10, _config.MaxIdentifiersPerType); i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                yield return (TIdentifier)(object)Guid.NewGuid();
                await Task.Delay(1, cancellationToken); // Yield control
            }
        }
        else if (typeof(TIdentifier) == typeof(string))
        {
            // Generate test string identifiers
            for (int i = 0; i < Math.Min(10, _config.MaxIdentifiersPerType); i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                yield return (TIdentifier)(object)$"test-{agentType.Name}-{i}";
                await Task.Delay(1, cancellationToken);
            }
        }
        else if (typeof(TIdentifier) == typeof(long))
        {
            // Generate test long identifiers
            for (long i = 1; i <= Math.Min(10, _config.MaxIdentifiersPerType); i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                yield return (TIdentifier)(object)i;
                await Task.Delay(1, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Generates range-based identifiers
    /// </summary>
    private async IAsyncEnumerable<TIdentifier> GenerateRangeIdentifiers(
        Type agentType, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Logger.LogInformation("Generating range-based identifiers for agent type {AgentType}", agentType.Name);

        if (typeof(TIdentifier) == typeof(Guid))
        {
            // For GUIDs, generate sequential GUIDs
            for (int i = 0; i < _config.MaxIdentifiersPerType; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                // Create a deterministic GUID based on agent type and index
                var guidBytes = new byte[16];
                var typeHash = agentType.GetHashCode();
                BitConverter.GetBytes(typeHash).CopyTo(guidBytes, 0);
                BitConverter.GetBytes(i).CopyTo(guidBytes, 4);
                
                yield return (TIdentifier)(object)new Guid(guidBytes);
                await Task.Delay(1, cancellationToken);
            }
        }
        else if (typeof(TIdentifier) == typeof(long))
        {
            // Generate sequential long identifiers
            for (long i = 1; i <= _config.MaxIdentifiersPerType; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                yield return (TIdentifier)(object)i;
                await Task.Delay(1, cancellationToken);
            }
        }
        else if (typeof(TIdentifier) == typeof(string))
        {
            // Generate sequential string identifiers
            for (int i = 1; i <= _config.MaxIdentifiersPerType; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                yield return (TIdentifier)(object)$"{agentType.Name}-{i:D6}";
                await Task.Delay(1, cancellationToken);
            }
        }
    }
} 