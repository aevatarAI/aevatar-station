using System.Collections.Generic;

namespace Aevatar.Silo.AgentWarmup;

/// <summary>
/// Configuration for the agent warmup system
/// </summary>
public class AgentWarmupConfiguration
{
    /// <summary>
    /// Whether the warmup system is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Maximum number of concurrent warmup operations
    /// </summary>
    public int MaxConcurrency { get; set; } = 10;
    
    /// <summary>
    /// Initial batch size for warmup operations
    /// </summary>
    public int InitialBatchSize { get; set; } = 5;
    
    /// <summary>
    /// Maximum batch size for warmup operations
    /// </summary>
    public int MaxBatchSize { get; set; } = 50;
    
    /// <summary>
    /// Batch size increase factor for progressive warmup
    /// </summary>
    public double BatchSizeIncreaseFactor { get; set; } = 1.5;
    
    /// <summary>
    /// Delay between warmup batches in milliseconds
    /// </summary>
    public int DelayBetweenBatchesMs { get; set; } = 100;
    
    /// <summary>
    /// Timeout for individual agent activation in milliseconds
    /// </summary>
    public int AgentActivationTimeoutMs { get; set; } = 5000;
    
    /// <summary>
    /// Maximum retry attempts for failed agent activations
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Retry delay in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;
    
    /// <summary>
    /// MongoDB rate limiting configuration
    /// </summary>
    public MongoDbRateLimitConfiguration MongoDbRateLimit { get; set; } = new();
    
    /// <summary>
    /// Automatic agent discovery configuration
    /// </summary>
    public AutoDiscoveryConfiguration AutoDiscovery { get; set; } = new();
    
    /// <summary>
    /// Default strategy configuration
    /// </summary>
    public DefaultStrategyConfiguration DefaultStrategy { get; set; } = new();
    
    /// <summary>
    /// MongoDB integration configuration
    /// </summary>
    public MongoDbIntegrationConfiguration MongoDbIntegration { get; set; } = new();
}

/// <summary>
/// Configuration for MongoDB rate limiting
/// </summary>
public class MongoDbRateLimitConfiguration
{
    /// <summary>
    /// Maximum operations per second
    /// </summary>
    public int MaxOperationsPerSecond { get; set; } = 50;
    
    /// <summary>
    /// Burst allowance for short spikes
    /// </summary>
    public int BurstAllowance { get; set; } = 10;
    
    /// <summary>
    /// Time window for rate limiting in milliseconds
    /// </summary>
    public int TimeWindowMs { get; set; } = 1000;
}

/// <summary>
/// Configuration for automatic agent discovery
/// </summary>
public class AutoDiscoveryConfiguration
{
    /// <summary>
    /// Whether automatic discovery is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Base types that agents must inherit from or implement
    /// </summary>
    public List<Type> BaseTypes { get; set; } = new();
    
    /// <summary>
    /// Required attributes that agents must have
    /// </summary>
    public List<string> RequiredAttributes { get; set; } = new() { "StorageProvider" };
    
    /// <summary>
    /// Storage provider name that agents must use
    /// </summary>
    public string StorageProviderName { get; set; } = "PubSubStore";
    
    /// <summary>
    /// Agent types to exclude from discovery
    /// </summary>
    public List<string> ExcludedAgentTypes { get; set; } = new();
    
    /// <summary>
    /// Assemblies to include in discovery (empty = all loaded assemblies)
    /// </summary>
    public List<string> IncludedAssemblies { get; set; } = new();
    
    /// <summary>
    /// Whether to cache discovered types
    /// </summary>
    public bool CacheDiscoveredTypes { get; set; } = true;
}

/// <summary>
/// Configuration for the default strategy
/// </summary>
public class DefaultStrategyConfiguration
{
    /// <summary>
    /// Whether the default strategy is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Source for agent identifiers (MongoDB, Predefined, Range)
    /// </summary>
    public string IdentifierSource { get; set; } = "MongoDB";
    
    /// <summary>
    /// Maximum identifiers per agent type
    /// </summary>
    public int MaxIdentifiersPerType { get; set; } = 1000;
    
    /// <summary>
    /// Priority for the default strategy
    /// </summary>
    public int Priority { get; set; } = 0;
}

/// <summary>
/// Configuration for MongoDB integration
/// </summary>
public class MongoDbIntegrationConfiguration
{
    /// <summary>
    /// MongoDB connection string (empty = use Orleans storage configuration)
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Database name (empty = use Orleans storage configuration)
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;
    
    /// <summary>
    /// Collection prefix to prepend to all collection names (matches Orleans storage configuration)
    /// </summary>
    public string CollectionPrefix { get; set; } = string.Empty;
    
    /// <summary>
    /// Collection naming strategy (FullTypeName, TypeName, Custom)
    /// </summary>
    public string CollectionNamingStrategy { get; set; } = "FullTypeName";
    
    /// <summary>
    /// Batch size for MongoDB queries
    /// </summary>
    public int BatchSize { get; set; } = 100;
    
    /// <summary>
    /// Query timeout in milliseconds
    /// </summary>
    public int QueryTimeoutMs { get; set; } = 30000;
} 