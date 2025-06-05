using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aevatar.Silo.GrainWarmup.Strategies;

/// <summary>
/// Default strategy that applies to all grain types not covered by specific strategies
/// Uses MongoDB integration for automatic identifier retrieval
/// </summary>
/// <typeparam name="TIdentifier">The identifier type (Guid, string, int, long)</typeparam>
public class DefaultGrainWarmupStrategy<TIdentifier> : BaseGrainWarmupStrategy<TIdentifier>
{
    private readonly IMongoDbGrainIdentifierService _mongoDbService;
    private readonly DefaultStrategyConfiguration _config;

    public DefaultGrainWarmupStrategy(
        IMongoDbGrainIdentifierService mongoDbService,
        IOptions<GrainWarmupConfiguration> options,
        ILogger<DefaultGrainWarmupStrategy<TIdentifier>> logger)
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
    /// Grain types this strategy applies to (empty = applies to all)
    /// </summary>
    public override IEnumerable<Type> ApplicableGrainTypes => Enumerable.Empty<Type>();

    /// <summary>
    /// Priority for execution order (lowest priority - executes last)
    /// </summary>
    public override int Priority => _config.Priority;

    /// <summary>
    /// Gets the estimated number of grains that will be warmed up
    /// </summary>
    public override int EstimatedGrainCount => _config.MaxIdentifiersPerType;

    /// <summary>
    /// Generates grain identifiers for a specific grain type using MongoDB
    /// </summary>
    /// <param name="grainType">The grain type to generate identifiers for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of grain identifiers</returns>
    public override async IAsyncEnumerable<TIdentifier> GenerateGrainIdentifiersAsync(
        Type grainType, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled)
        {
            Logger.LogDebug("Default strategy is disabled, skipping grain type {GrainType}", grainType.Name);
            yield break;
        }

        if (!ValidateIdentifierType(grainType))
        {
            Logger.LogWarning("Identifier type {IdentifierType} is not compatible with grain type {GrainType}", 
                typeof(TIdentifier).Name, grainType.Name);
            yield break;
        }

        Logger.LogInformation("Default strategy generating identifiers for grain type {GrainType} using {IdentifierSource}", 
            grainType.Name, _config.IdentifierSource);

        switch (_config.IdentifierSource.ToLowerInvariant())
        {
            case "mongodb":
                await foreach (var identifier in GenerateFromMongoDb(grainType, cancellationToken))
                {
                    yield return identifier;
                }
                break;

            case "predefined":
                await foreach (var identifier in GeneratePredefinedIdentifiers(grainType, cancellationToken))
                {
                    yield return identifier;
                }
                break;

            case "range":
                await foreach (var identifier in GenerateRangeIdentifiers(grainType, cancellationToken))
                {
                    yield return identifier;
                }
                break;

            default:
                Logger.LogWarning("Unknown identifier source {IdentifierSource}, falling back to MongoDB", 
                    _config.IdentifierSource);
                await foreach (var identifier in GenerateFromMongoDb(grainType, cancellationToken))
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
        Type grainType, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var identifiers = new List<TIdentifier>();
        try
        {
            // Check if collection exists
            if (!await _mongoDbService.CollectionExistsAsync(grainType))
            {
                Logger.LogInformation("No MongoDB collection found for grain type {GrainType}, skipping", grainType.Name);
                yield break;
            }

            var count = 0;
            await foreach (var identifier in _mongoDbService.GetGrainIdentifiersAsync<TIdentifier>(
                grainType, _config.MaxIdentifiersPerType, cancellationToken))
            {
                identifiers.Add(identifier);
                count++;

                if (count >= _config.MaxIdentifiersPerType)
                {
                    Logger.LogInformation("Reached maximum identifier limit {MaxCount} for grain type {GrainType}", 
                        _config.MaxIdentifiersPerType, grainType.Name);
                    break;
                }
            }

            Logger.LogInformation("Generated {Count} identifiers from MongoDB for grain type {GrainType}", 
                count, grainType.Name);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating identifiers from MongoDB for grain type {GrainType}", grainType.Name);
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
        Type grainType, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // This could be extended to read from configuration or external sources
        Logger.LogInformation("Generating predefined identifiers for grain type {GrainType}", grainType.Name);

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

                yield return (TIdentifier)(object)$"test-{grainType.Name}-{i}";
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
        Type grainType, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Logger.LogInformation("Generating range-based identifiers for grain type {GrainType}", grainType.Name);

        if (typeof(TIdentifier) == typeof(Guid))
        {
            // For GUIDs, generate sequential GUIDs
            for (int i = 0; i < _config.MaxIdentifiersPerType; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                // Create a deterministic GUID based on grain type and index
                var guidBytes = new byte[16];
                var typeHash = grainType.GetHashCode();
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

                yield return (TIdentifier)(object)$"{grainType.Name}-{i:D6}";
                await Task.Delay(1, cancellationToken);
            }
        }
    }
} 