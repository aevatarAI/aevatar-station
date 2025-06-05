using Microsoft.Extensions.Logging;

namespace Aevatar.Silo.GrainWarmup.Strategies;

/// <summary>
/// Warmup strategy for grains with range-based identifiers (numeric types)
/// </summary>
/// <typeparam name="TIdentifier">The type of grain identifier (int, long)</typeparam>
public class RangeBasedGrainWarmupStrategy<TIdentifier> : BaseGrainWarmupStrategy<TIdentifier>
    where TIdentifier : struct, IComparable<TIdentifier>, IConvertible
{
    private readonly Type _grainType;
    private readonly TIdentifier _startId;
    private readonly TIdentifier _endId;
    private readonly int _batchSize;
    private readonly string _name;
    
    public RangeBasedGrainWarmupStrategy(
        string name,
        Type grainType,
        TIdentifier startId,
        TIdentifier endId,
        ILogger<RangeBasedGrainWarmupStrategy<TIdentifier>> logger,
        int batchSize = 100) : base(logger)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _grainType = grainType ?? throw new ArgumentNullException(nameof(grainType));
        _startId = startId;
        _endId = endId;
        _batchSize = batchSize > 0 ? batchSize : throw new ArgumentException("Batch size must be positive", nameof(batchSize));
        
        if (_startId.CompareTo(_endId) >= 0)
        {
            throw new ArgumentException("Start ID must be less than end ID");
        }
        
        // Validate that TIdentifier is a supported numeric type
        if (typeof(TIdentifier) != typeof(int) && typeof(TIdentifier) != typeof(long))
        {
            throw new NotSupportedException($"Range-based strategy only supports int and long identifiers, got {typeof(TIdentifier).Name}");
        }
    }
    
    /// <inheritdoc />
    public override string Name => _name;
    
    /// <inheritdoc />
    public override IEnumerable<Type> ApplicableGrainTypes => new[] { _grainType };
    
    /// <inheritdoc />
    public override int Priority => 50; // Medium priority for range-based strategies
    
    /// <inheritdoc />
    public override int EstimatedGrainCount => CalculateGrainCount();
    
    /// <inheritdoc />
    public override async IAsyncEnumerable<TIdentifier> GenerateGrainIdentifiersAsync(
        Type grainType,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Generating range-based grain identifiers from {StartId} to {EndId} for strategy {StrategyName}", 
            _startId, _endId, Name);
        
        var current = _startId;
        var batchCount = 0;
        
        while (current.CompareTo(_endId) < 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            yield return current;
            
            // Increment the current value
            current = IncrementValue(current);
            batchCount++;
            
            // Add small delay every batch to prevent overwhelming the system
            if (batchCount >= _batchSize)
            {
                await Task.Delay(10, cancellationToken);
                batchCount = 0;
            }
        }
        
        Logger.LogDebug("Completed generating range-based grain identifiers for strategy {StrategyName}", Name);
    }
    
    /// <summary>
    /// Calculates the total number of grains in the range
    /// </summary>
    /// <returns>The number of grains</returns>
    private int CalculateGrainCount()
    {
        if (typeof(TIdentifier) == typeof(int))
        {
            var start = (int)(object)_startId;
            var end = (int)(object)_endId;
            return Math.Max(0, end - start);
        }
        else if (typeof(TIdentifier) == typeof(long))
        {
            var start = (long)(object)_startId;
            var end = (long)(object)_endId;
            var count = end - start;
            return count > int.MaxValue ? int.MaxValue : (int)count;
        }
        
        return 0;
    }
    
    /// <summary>
    /// Increments a value by 1
    /// </summary>
    /// <param name="value">The value to increment</param>
    /// <returns>The incremented value</returns>
    private TIdentifier IncrementValue(TIdentifier value)
    {
        if (typeof(TIdentifier) == typeof(int))
        {
            var intValue = (int)(object)value;
            return (TIdentifier)(object)(intValue + 1);
        }
        else if (typeof(TIdentifier) == typeof(long))
        {
            var longValue = (long)(object)value;
            return (TIdentifier)(object)(longValue + 1);
        }
        
        throw new NotSupportedException($"Increment not supported for type {typeof(TIdentifier).Name}");
    }
    
    /// <summary>
    /// Validates strategy-specific configuration
    /// </summary>
    /// <returns>True if configuration is valid</returns>
    protected virtual bool ValidateStrategySpecificConfiguration()
    {
        if (_startId.CompareTo(_endId) >= 0)
        {
            Logger.LogError("Invalid range for strategy {StrategyName}: start {StartId} >= end {EndId}", 
                Name, _startId, _endId);
            return false;
        }
        
        if (_batchSize <= 0)
        {
            Logger.LogError("Invalid batch size for strategy {StrategyName}: {BatchSize}", Name, _batchSize);
            return false;
        }
        
        Logger.LogDebug("Strategy {StrategyName} configuration is valid with range {StartId}-{EndId}", 
            Name, _startId, _endId);
        
        return true;
    }
} 