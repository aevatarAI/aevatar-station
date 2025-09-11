using System;
using System.Collections.Generic;

namespace Aevatar.Application.Contracts.MCPGateway;

/// <summary>
/// MCP Adapter status information
/// </summary>
public class MCPAdapterStatusDto
{
    /// <summary>
    /// Adapter name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Current status
    /// </summary>
    public MCPAdapterStatus Status { get; set; }

    /// <summary>
    /// Health check status
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Last health check timestamp
    /// </summary>
    public DateTime? LastHealthCheck { get; set; }

    /// <summary>
    /// Health check error message (if any)
    /// </summary>
    public string? HealthCheckError { get; set; }

    /// <summary>
    /// Number of active connections
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Uptime in seconds
    /// </summary>
    public long UptimeSeconds { get; set; }

    /// <summary>
    /// Resource usage statistics
    /// </summary>
    public MCPResourceUsageDto? ResourceUsage { get; set; }

    /// <summary>
    /// Recent error messages
    /// </summary>
    public List<MCPErrorLogDto> RecentErrors { get; set; } = new();

    /// <summary>
    /// Performance metrics
    /// </summary>
    public MCPPerformanceMetricsDto? PerformanceMetrics { get; set; }
}

/// <summary>
/// MCP Gateway health information
/// </summary>
public class MCPGatewayHealthDto
{
    /// <summary>
    /// Overall gateway health status
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Gateway version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gateway uptime in seconds
    /// </summary>
    public long UptimeSeconds { get; set; }

    /// <summary>
    /// Total number of adapters
    /// </summary>
    public int TotalAdapters { get; set; }

    /// <summary>
    /// Number of healthy adapters
    /// </summary>
    public int HealthyAdapters { get; set; }

    /// <summary>
    /// Total active connections across all adapters
    /// </summary>
    public int TotalActiveConnections { get; set; }

    /// <summary>
    /// System resource usage
    /// </summary>
    public MCPResourceUsageDto? SystemResourceUsage { get; set; }

    /// <summary>
    /// Recent system errors
    /// </summary>
    public List<MCPErrorLogDto> RecentErrors { get; set; } = new();

    /// <summary>
    /// Health check timestamp
    /// </summary>
    public DateTime CheckedAt { get; set; }
}

/// <summary>
/// Connection test result
/// </summary>
public class MCPConnectionTestResultDto
{
    /// <summary>
    /// Test success status
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Connection latency in milliseconds
    /// </summary>
    public double LatencyMs { get; set; }

    /// <summary>
    /// Error message (if test failed)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Test timestamp
    /// </summary>
    public DateTime TestedAt { get; set; }

    /// <summary>
    /// Additional test details
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Adapter metrics and statistics
/// </summary>
public class MCPAdapterMetricsDto
{
    /// <summary>
    /// Adapter name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Metrics time range
    /// </summary>
    public DateTime FromTime { get; set; }

    /// <summary>
    /// Metrics time range
    /// </summary>
    public DateTime ToTime { get; set; }

    /// <summary>
    /// Request statistics
    /// </summary>
    public MCPRequestStatsDto RequestStats { get; set; } = new();

    /// <summary>
    /// Performance metrics
    /// </summary>
    public MCPPerformanceMetricsDto PerformanceMetrics { get; set; } = new();

    /// <summary>
    /// Error statistics
    /// </summary>
    public MCPErrorStatsDto ErrorStats { get; set; } = new();

    /// <summary>
    /// Resource usage over time
    /// </summary>
    public List<MCPResourceUsageTimeSeriesDto> ResourceUsageTimeSeries { get; set; } = new();
}

/// <summary>
/// Error log entry
/// </summary>
public class MCPErrorLogDto
{
    /// <summary>
    /// Error timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Error level (Error, Warning, Info)
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error source/component
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Stack trace (if available)
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Additional error context
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Performance metrics
/// </summary>
public class MCPPerformanceMetricsDto
{
    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// 50th percentile response time
    /// </summary>
    public double P50ResponseTimeMs { get; set; }

    /// <summary>
    /// 95th percentile response time
    /// </summary>
    public double P95ResponseTimeMs { get; set; }

    /// <summary>
    /// 99th percentile response time
    /// </summary>
    public double P99ResponseTimeMs { get; set; }

    /// <summary>
    /// Requests per second
    /// </summary>
    public double RequestsPerSecond { get; set; }

    /// <summary>
    /// Throughput in bytes per second
    /// </summary>
    public long ThroughputBytesPerSecond { get; set; }
}

/// <summary>
/// Error statistics
/// </summary>
public class MCPErrorStatsDto
{
    /// <summary>
    /// Total error count
    /// </summary>
    public long TotalErrors { get; set; }

    /// <summary>
    /// Error rate (errors per request)
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Errors by type
    /// </summary>
    public Dictionary<string, long> ErrorsByType { get; set; } = new();

    /// <summary>
    /// Most common error message
    /// </summary>
    public string? MostCommonError { get; set; }
}

/// <summary>
/// Resource usage time series data point
/// </summary>
public class MCPResourceUsageTimeSeriesDto
{
    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// Memory usage in MB
    /// </summary>
    public long MemoryUsageMB { get; set; }

    /// <summary>
    /// Active connections count
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Requests per second at this time
    /// </summary>
    public double RequestsPerSecond { get; set; }
}
