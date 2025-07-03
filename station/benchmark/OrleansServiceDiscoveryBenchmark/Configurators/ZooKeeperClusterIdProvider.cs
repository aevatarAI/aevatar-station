namespace OrleansServiceDiscoveryBenchmark.Configurators;

/// <summary>
/// Provides shared ZooKeeper cluster ID to ensure silo and client use the same cluster ID within the same benchmark test
/// </summary>
public static class ZooKeeperClusterIdProvider
{
    private static readonly object _lock = new object();
    private static string? _currentClusterId;
    
    /// <summary>
    /// Gets or generates the current cluster ID
    /// </summary>
    public static string GetClusterId()
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(_currentClusterId))
            {
                _currentClusterId = $"zk-test-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}";
            }
            return _currentClusterId;
        }
    }
    
    /// <summary>
    /// Resets cluster ID for new test runs
    /// </summary>
    public static void ResetClusterId()
    {
        lock (_lock)
        {
            _currentClusterId = null;
        }
    }
} 