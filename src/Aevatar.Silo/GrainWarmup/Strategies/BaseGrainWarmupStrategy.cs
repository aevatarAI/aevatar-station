using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.Silo.GrainWarmup.Strategies;

/// <summary>
/// Base class for grain warmup strategies providing common functionality
/// </summary>
/// <typeparam name="TIdentifier">The identifier type (Guid, string, int, long)</typeparam>
public abstract class BaseGrainWarmupStrategy<TIdentifier> : IGrainWarmupStrategy<TIdentifier>
{
    protected readonly ILogger Logger;

    protected BaseGrainWarmupStrategy(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Strategy name for identification
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Grain types this strategy applies to (empty = applies to all)
    /// </summary>
    public abstract IEnumerable<Type> ApplicableGrainTypes { get; }

    /// <summary>
    /// Priority for execution order (higher = earlier)
    /// </summary>
    public abstract int Priority { get; }

    /// <summary>
    /// Gets the primary grain type (for interface compatibility)
    /// </summary>
    public virtual Type GrainType => ApplicableGrainTypes.FirstOrDefault() ?? typeof(object);

    /// <summary>
    /// Gets the estimated number of grains that will be warmed up
    /// </summary>
    public abstract int EstimatedGrainCount { get; }

    /// <summary>
    /// Generates grain identifiers for a specific grain type
    /// </summary>
    /// <param name="grainType">The grain type to generate identifiers for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of grain identifiers</returns>
    public abstract IAsyncEnumerable<TIdentifier> GenerateGrainIdentifiersAsync(
        Type grainType, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the grain identifiers to warm up (strongly typed version)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enumerable of strongly typed grain identifiers</returns>
    public virtual IAsyncEnumerable<TIdentifier> GenerateGrainIdentifiersAsync(CancellationToken cancellationToken = default)
    {
        return GenerateGrainIdentifiersAsync(GrainType, cancellationToken);
    }

    /// <summary>
    /// Generates the grain identifiers to warm up (non-generic version)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enumerable of grain identifiers as objects</returns>
    async IAsyncEnumerable<object> IGrainWarmupStrategy.GenerateGrainIdentifiersAsync(CancellationToken cancellationToken)
    {
        await foreach (var identifier in GenerateGrainIdentifiersAsync(GrainType, cancellationToken))
        {
            yield return identifier!;
        }
    }

    /// <summary>
    /// Checks if strategy applies to a grain type
    /// </summary>
    /// <param name="grainType">The grain type to check</param>
    /// <returns>True if the strategy applies to this grain type</returns>
    public virtual bool AppliesTo(Type grainType)
    {
        var applicableTypes = ApplicableGrainTypes.ToList();
        
        // If no specific types are defined, applies to all
        if (!applicableTypes.Any())
            return true;

        // Check if the grain type is in the applicable types list
        return applicableTypes.Contains(grainType) || 
               applicableTypes.Any(t => t.IsAssignableFrom(grainType));
    }

    /// <summary>
    /// Creates grain reference for a specific grain type and identifier
    /// </summary>
    /// <typeparam name="T">The grain interface type that inherits from IGrainBase</typeparam>
    /// <param name="grainFactory">The grain factory</param>
    /// <param name="grainType">The grain type</param>
    /// <param name="identifier">The grain identifier</param>
    /// <returns>The grain reference of type T</returns>
    public virtual T CreateGrainReference<T>(IGrainFactory grainFactory, Type grainType, TIdentifier identifier) where T : IGrainBase
    {
        try
        {
            // Determine the grain interface type
            var grainInterface = GetGrainInterface(grainType);
            if (grainInterface == null)
            {
                throw new InvalidOperationException($"Could not determine grain interface for type {grainType.Name}");
            }

            // Create grain reference using non-generic GetGrain and cast to T
            var grain = typeof(TIdentifier) switch
            {
                var t when t == typeof(Guid) => grainFactory.GetGrain(grainInterface, (Guid)(object)identifier!),
                var t when t == typeof(string) => grainFactory.GetGrain(grainInterface, (string)(object)identifier!),
                var t when t == typeof(long) => grainFactory.GetGrain(grainInterface, (long)(object)identifier!),
                var t when t == typeof(int) => grainFactory.GetGrain(grainInterface, (long)(int)(object)identifier!),
                _ => throw new NotSupportedException($"Identifier type {typeof(TIdentifier).Name} is not supported")
            };

            return (T)grain;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating grain reference for type {GrainType} with identifier {Identifier}", 
                grainType.Name, identifier);
            throw;
        }
    }

    /// <summary>
    /// Gets the grain interface type for a grain implementation type
    /// </summary>
    /// <param name="grainType">The grain implementation type</param>
    /// <returns>The grain interface type</returns>
    protected virtual Type? GetGrainInterface(Type grainType)
    {
        // Look for Orleans grain interfaces
        var interfaces = grainType.GetInterfaces();
        
        // Find the most specific grain interface
        var grainInterfaces = interfaces.Where(i => 
            typeof(IGrainWithGuidKey).IsAssignableFrom(i) ||
            typeof(IGrainWithStringKey).IsAssignableFrom(i) ||
            typeof(IGrainWithIntegerKey).IsAssignableFrom(i) ||
            typeof(IGrainWithGuidCompoundKey).IsAssignableFrom(i) ||
            typeof(IGrainWithIntegerCompoundKey).IsAssignableFrom(i))
            .ToList();

        // Return the most specific interface (not the base Orleans interfaces)
        return grainInterfaces
            .Where(i => i != typeof(IGrainWithGuidKey) && 
                       i != typeof(IGrainWithStringKey) && 
                       i != typeof(IGrainWithIntegerKey) &&
                       i != typeof(IGrainWithGuidCompoundKey) &&
                       i != typeof(IGrainWithIntegerCompoundKey))
            .FirstOrDefault() ?? grainInterfaces.FirstOrDefault();
    }

    /// <summary>
    /// Validates that the identifier type matches the grain's expected identifier type
    /// </summary>
    /// <param name="grainType">The grain type</param>
    /// <returns>True if the identifier type is compatible</returns>
    protected virtual bool ValidateIdentifierType(Type grainType)
    {
        var interfaces = grainType.GetInterfaces();

        if (typeof(TIdentifier) == typeof(Guid))
        {
            return interfaces.Any(i => 
                typeof(IGrainWithGuidKey).IsAssignableFrom(i) ||
                typeof(IGrainWithGuidCompoundKey).IsAssignableFrom(i));
        }
        else if (typeof(TIdentifier) == typeof(string))
        {
            return interfaces.Any(i => typeof(IGrainWithStringKey).IsAssignableFrom(i));
        }
        else if (typeof(TIdentifier) == typeof(long) || typeof(TIdentifier) == typeof(int))
        {
            return interfaces.Any(i => 
                typeof(IGrainWithIntegerKey).IsAssignableFrom(i) ||
                typeof(IGrainWithIntegerCompoundKey).IsAssignableFrom(i));
        }

        return false;
    }

    /// <summary>
    /// Logs strategy execution information
    /// </summary>
    /// <param name="grainType">The grain type being processed</param>
    /// <param name="identifierCount">Number of identifiers being processed</param>
    protected virtual void LogStrategyExecution(Type grainType, int identifierCount)
    {
        Logger.LogInformation("Strategy {StrategyName} processing {Count} identifiers for grain type {GrainType}", 
            Name, identifierCount, grainType.Name);
    }

    /// <summary>
    /// Creates a grain reference for the given identifier (strongly typed version)
    /// </summary>
    /// <typeparam name="T">The grain interface type that inherits from IGrainBase</typeparam>
    /// <param name="grainFactory">The grain factory</param>
    /// <param name="identifier">The strongly typed grain identifier</param>
    /// <returns>The grain reference of type T</returns>
    public virtual T CreateGrainReference<T>(IGrainFactory grainFactory, TIdentifier identifier) where T : IGrainBase
    {
        return CreateGrainReference<T>(grainFactory, GrainType, identifier);
    }

    /// <summary>
    /// Creates a grain reference for the given identifier (non-generic version)
    /// </summary>
    /// <typeparam name="T">The grain interface type that inherits from IGrainBase</typeparam>
    /// <param name="grainFactory">The grain factory</param>
    /// <param name="identifier">The grain identifier</param>
    /// <returns>The grain reference of type T</returns>
    public virtual T CreateGrainReference<T>(IGrainFactory grainFactory, object identifier) where T : IGrainBase
    {
        return CreateGrainReference<T>(grainFactory, (TIdentifier)identifier);
    }

    /// <summary>
    /// Validates that the strategy configuration is correct
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public virtual bool IsValid()
    {
        return ApplicableGrainTypes.Any() && EstimatedGrainCount > 0;
    }
} 