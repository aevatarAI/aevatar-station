using Microsoft.Extensions.Logging;

namespace Aevatar.Silo.AgentWarmup.Strategies;

/// <summary>
/// Warmup strategy for agents with predefined identifiers
/// </summary>
/// <typeparam name="TIdentifier">The type of agent identifier (Guid, string, int, long)</typeparam>
public class PredefinedAgentWarmupStrategy<TIdentifier> : BaseAgentWarmupStrategy<TIdentifier>
{
    private readonly IReadOnlyCollection<TIdentifier> _agentIdentifiers;
    private readonly Type _agentType;
    private readonly string _name;
    
    public PredefinedAgentWarmupStrategy(
        string name,
        Type agentType,
        IEnumerable<TIdentifier> agentIdentifiers,
        ILogger<PredefinedAgentWarmupStrategy<TIdentifier>> logger) : base(logger)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _agentType = agentType ?? throw new ArgumentNullException(nameof(agentType));
        _agentIdentifiers = agentIdentifiers?.ToList() ?? throw new ArgumentNullException(nameof(agentIdentifiers));
        
        if (!_agentIdentifiers.Any())
        {
            throw new ArgumentException("At least one agent identifier must be provided", nameof(agentIdentifiers));
        }
    }
    
    /// <inheritdoc />
    public override string Name => _name;
    
    /// <inheritdoc />
    public override IEnumerable<Type> ApplicableAgentTypes => new[] { _agentType };
    
    /// <inheritdoc />
    public override int Priority => 100; // High priority for predefined strategies
    
    /// <inheritdoc />
    public override int EstimatedAgentCount => _agentIdentifiers.Count;
    
    /// <inheritdoc />
    public override async IAsyncEnumerable<TIdentifier> GenerateAgentIdentifiersAsync(
        Type agentType,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Generating {Count} predefined agent identifiers for strategy {StrategyName}", 
            _agentIdentifiers.Count, Name);
        
        foreach (var identifier in _agentIdentifiers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            yield return identifier;
            
            // Small delay to allow for cancellation and prevent overwhelming the system
            await Task.Delay(1, cancellationToken);
        }
        
        Logger.LogDebug("Completed generating agent identifiers for strategy {StrategyName}", Name);
    }
    
    /// <summary>
    /// Validates strategy-specific configuration
    /// </summary>
    /// <returns>True if configuration is valid</returns>
    protected virtual bool ValidateStrategySpecificConfiguration()
    {
        if (!_agentIdentifiers.Any())
        {
            Logger.LogError("No agent identifiers provided for strategy {StrategyName}", Name);
            return false;
        }
        
        // Check for null identifiers
        if (_agentIdentifiers.Any(id => id == null))
        {
            Logger.LogError("Strategy {StrategyName} contains null identifiers", Name);
            return false;
        }
        
        Logger.LogDebug("Strategy {StrategyName} configuration is valid with {Count} identifiers", 
            Name, _agentIdentifiers.Count);
        
        return true;
    }
} 