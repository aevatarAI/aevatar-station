using Microsoft.Extensions.Logging;

namespace Aevatar.Silo.AgentWarmup.Strategies;

/// <summary>
/// Warmup strategy that randomly samples a percentage of agents from MongoDB collections
/// </summary>
/// <typeparam name="TIdentifier">The type of agent identifier (Guid, string, int, long)</typeparam>
public class SampleBasedAgentWarmupStrategy<TIdentifier> : BaseAgentWarmupStrategy<TIdentifier>
{
    private readonly IMongoDbAgentIdentifierService _mongoDbService;
    private readonly Type _agentType;
    private readonly double _sampleRatio;
    private readonly int _batchSize;
    private readonly string _name;
    private readonly Random _random;
    
    public SampleBasedAgentWarmupStrategy(
        string name,
        Type agentType,
        double sampleRatio,
        IMongoDbAgentIdentifierService mongoDbService,
        ILogger<SampleBasedAgentWarmupStrategy<TIdentifier>> logger,
        int? randomSeed = null,
        int batchSize = 100) : base(logger)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _agentType = agentType ?? throw new ArgumentNullException(nameof(agentType));
        _mongoDbService = mongoDbService ?? throw new ArgumentNullException(nameof(mongoDbService));
        _sampleRatio = sampleRatio;
        _batchSize = batchSize > 0 ? batchSize : throw new ArgumentException("Batch size must be positive", nameof(batchSize));
        
        if (sampleRatio <= 0 || sampleRatio > 1.0)
        {
            throw new ArgumentException("Sample ratio must be between 0 and 1.0", nameof(sampleRatio));
        }
        
        // Initialize random number generator
        _random = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
    }
    
    /// <inheritdoc />
    public override string Name => _name;
    
    /// <inheritdoc />
    public override IEnumerable<Type> ApplicableAgentTypes => new[] { _agentType };
    
    /// <inheritdoc />
    public override int Priority => 75; // Higher priority than default strategy for sampling
    
    /// <inheritdoc />
    public override int EstimatedAgentCount => CalculateEstimatedAgentCount();
    
    /// <inheritdoc />
    public override async IAsyncEnumerable<TIdentifier> GenerateAgentIdentifiersAsync(
        Type agentType,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Starting sample-based agent identifier generation for {AgentType} with ratio {SampleRatio} (strategy: {StrategyName})", 
            agentType.Name, _sampleRatio, Name);
        
        // Get all identifiers from MongoDB
        var allIdentifiers = new List<TIdentifier>();
        await foreach (var identifier in _mongoDbService.GetAgentIdentifiersAsync<TIdentifier>(agentType, cancellationToken: cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            allIdentifiers.Add(identifier);
        }
        
        Logger.LogDebug("Retrieved {TotalCount} identifiers from MongoDB for {AgentType}", 
            allIdentifiers.Count, agentType.Name);
        
        if (allIdentifiers.Count == 0)
        {
            Logger.LogWarning("No identifiers found for agent type {AgentType} in strategy {StrategyName}", 
                agentType.Name, Name);
            yield break;
        }
        
        // Calculate sample size
        var sampleSize = Math.Max(1, (int)(allIdentifiers.Count * _sampleRatio));
        Logger.LogDebug("Sampling {SampleSize} identifiers from {TotalCount} for {AgentType} (ratio: {SampleRatio})", 
            sampleSize, allIdentifiers.Count, agentType.Name, _sampleRatio);
        
        // Perform random sampling using Fisher-Yates shuffle
        var sampledIdentifiers = PerformRandomSampling(allIdentifiers, sampleSize);
        
        // Yield sampled identifiers with batch delays
        var batchCount = 0;
        foreach (var identifier in sampledIdentifiers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            yield return identifier;
            batchCount++;
            
            // Add small delay every batch to prevent overwhelming the system
            if (batchCount >= _batchSize)
            {
                await Task.Delay(10, cancellationToken);
                batchCount = 0;
            }
        }
        
        Logger.LogDebug("Completed sample-based agent identifier generation for strategy {StrategyName}. Sampled {SampleSize} identifiers", 
            Name, sampledIdentifiers.Count);
    }
    
    /// <summary>
    /// Performs random sampling using Fisher-Yates shuffle algorithm
    /// </summary>
    /// <param name="allIdentifiers">The complete list of identifiers</param>
    /// <param name="sampleSize">The number of samples to select</param>
    /// <returns>Randomly sampled identifiers</returns>
    private List<TIdentifier> PerformRandomSampling(List<TIdentifier> allIdentifiers, int sampleSize)
    {
        // Ensure sample size doesn't exceed available identifiers
        sampleSize = Math.Min(sampleSize, allIdentifiers.Count);
        
        // Create a copy to avoid modifying the original list
        var identifiersCopy = new List<TIdentifier>(allIdentifiers);
        
        // Fisher-Yates shuffle for the first 'sampleSize' elements
        for (int i = 0; i < sampleSize; i++)
        {
            // Pick a random index from i to end of list
            int randomIndex = _random.Next(i, identifiersCopy.Count);
            
            // Swap elements at i and randomIndex
            (identifiersCopy[i], identifiersCopy[randomIndex]) = (identifiersCopy[randomIndex], identifiersCopy[i]);
        }
        
        // Return the first 'sampleSize' elements (which are now randomly selected)
        return identifiersCopy.Take(sampleSize).ToList();
    }
    
    /// <summary>
    /// Calculates the estimated number of agents that will be warmed up
    /// </summary>
    /// <returns>The estimated agent count</returns>
    private int CalculateEstimatedAgentCount()
    {
        try
        {
            // This is an estimate - actual count will be determined at runtime
            // We can't easily get the MongoDB count synchronously here, so we return a reasonable estimate
            // The actual count will be logged during execution
            return (int)(1000 * _sampleRatio); // Assume ~1000 agents as baseline estimate
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to calculate estimated agent count for strategy {StrategyName}", Name);
            return 0;
        }
    }
    
    /// <summary>
    /// Validates strategy-specific configuration
    /// </summary>
    /// <returns>True if configuration is valid</returns>
    protected virtual bool ValidateStrategySpecificConfiguration()
    {
        if (_sampleRatio <= 0 || _sampleRatio > 1.0)
        {
            Logger.LogError("Invalid sample ratio for strategy {StrategyName}: {SampleRatio}. Must be between 0 and 1.0", 
                Name, _sampleRatio);
            return false;
        }
        
        if (_batchSize <= 0)
        {
            Logger.LogError("Invalid batch size for strategy {StrategyName}: {BatchSize}", Name, _batchSize);
            return false;
        }
        
        if (_agentType == null)
        {
            Logger.LogError("Agent type is null for strategy {StrategyName}", Name);
            return false;
        }
        
        Logger.LogDebug("Strategy {StrategyName} configuration is valid with sample ratio {SampleRatio} for agent type {AgentType}", 
            Name, _sampleRatio, _agentType.Name);
        
        return true;
    }
} 