using Microsoft.Extensions.Logging;

namespace Aevatar.Silo.GrainWarmup.Strategies;

/// <summary>
/// Warmup strategy for grains with predefined identifiers
/// </summary>
/// <typeparam name="TIdentifier">The type of grain identifier (Guid, string, int, long)</typeparam>
public class PredefinedGrainWarmupStrategy<TIdentifier> : BaseGrainWarmupStrategy<TIdentifier>
{
    private readonly IReadOnlyCollection<TIdentifier> _grainIdentifiers;
    private readonly Type _grainType;
    private readonly string _name;
    
    public PredefinedGrainWarmupStrategy(
        string name,
        Type grainType,
        IEnumerable<TIdentifier> grainIdentifiers,
        ILogger<PredefinedGrainWarmupStrategy<TIdentifier>> logger) : base(logger)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _grainType = grainType ?? throw new ArgumentNullException(nameof(grainType));
        _grainIdentifiers = grainIdentifiers?.ToList() ?? throw new ArgumentNullException(nameof(grainIdentifiers));
        
        if (!_grainIdentifiers.Any())
        {
            throw new ArgumentException("At least one grain identifier must be provided", nameof(grainIdentifiers));
        }
    }
    
    /// <inheritdoc />
    public override string Name => _name;
    
    /// <inheritdoc />
    public override IEnumerable<Type> ApplicableGrainTypes => new[] { _grainType };
    
    /// <inheritdoc />
    public override int Priority => 100; // High priority for predefined strategies
    
    /// <inheritdoc />
    public override int EstimatedGrainCount => _grainIdentifiers.Count;
    
    /// <inheritdoc />
    public override async IAsyncEnumerable<TIdentifier> GenerateGrainIdentifiersAsync(
        Type grainType,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Generating {Count} predefined grain identifiers for strategy {StrategyName}", 
            _grainIdentifiers.Count, Name);
        
        foreach (var identifier in _grainIdentifiers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            yield return identifier;
            
            // Small delay to allow for cancellation and prevent overwhelming the system
            await Task.Delay(1, cancellationToken);
        }
        
        Logger.LogDebug("Completed generating grain identifiers for strategy {StrategyName}", Name);
    }
    
    /// <summary>
    /// Validates strategy-specific configuration
    /// </summary>
    /// <returns>True if configuration is valid</returns>
    protected virtual bool ValidateStrategySpecificConfiguration()
    {
        if (!_grainIdentifiers.Any())
        {
            Logger.LogError("No grain identifiers provided for strategy {StrategyName}", Name);
            return false;
        }
        
        // Check for null identifiers
        if (_grainIdentifiers.Any(id => id == null))
        {
            Logger.LogError("Strategy {StrategyName} contains null identifiers", Name);
            return false;
        }
        
        Logger.LogDebug("Strategy {StrategyName} configuration is valid with {Count} identifiers", 
            Name, _grainIdentifiers.Count);
        
        return true;
    }
} 