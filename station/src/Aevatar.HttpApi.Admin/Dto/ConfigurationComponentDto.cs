// ABOUTME: This file defines component-specific DTOs for configuration management
// ABOUTME: Includes logging, database, Orleans, and observability configuration DTOs

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Admin.Dto;

/// <summary>
/// Logging configuration data transfer object
/// </summary>
public class LoggingConfigurationDto
{
    /// <summary>
    /// Minimum log level
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Log output configuration
    /// </summary>
    public LogOutputDto[] Outputs { get; set; } = System.Array.Empty<LogOutputDto>();

    /// <summary>
    /// Custom log filters
    /// </summary>
    public Dictionary<string, LogLevel> Filters { get; set; } = new();

    /// <summary>
    /// Whether to include scopes in log output
    /// </summary>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>
    /// Log format template
    /// </summary>
    public string Template { get; set; }

    /// <summary>
    /// Custom properties to include in logs
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new();
}

/// <summary>
/// Log output configuration
/// </summary>
public class LogOutputDto
{
    /// <summary>
    /// Output type (Console, File, Elasticsearch, etc.)
    /// </summary>
    [Required]
    public string Type { get; set; }

    /// <summary>
    /// Output-specific configuration
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Minimum log level for this output
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
}

/// <summary>
/// Database configuration data transfer object
/// </summary>
public class DatabaseConfigurationDto
{
    /// <summary>
    /// MongoDB connection configuration
    /// </summary>
    public MongoDbConfigurationDto MongoDB { get; set; }

    /// <summary>
    /// Redis connection configuration
    /// </summary>
    public RedisConfigurationDto Redis { get; set; }

    /// <summary>
    /// Elasticsearch configuration
    /// </summary>
    public ElasticsearchConfigurationDto Elasticsearch { get; set; }

    /// <summary>
    /// Connection pool settings
    /// </summary>
    public ConnectionPoolDto ConnectionPool { get; set; }

    /// <summary>
    /// Database health check settings
    /// </summary>
    public DatabaseHealthCheckDto HealthCheck { get; set; }
}

/// <summary>
/// MongoDB configuration
/// </summary>
public class MongoDbConfigurationDto
{
    /// <summary>
    /// MongoDB connection string
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    public string DatabaseName { get; set; }

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum connection pool size
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 100;

    /// <summary>
    /// Write concern configuration
    /// </summary>
    public WriteConcernDto WriteConcern { get; set; }
}

/// <summary>
/// Redis configuration
/// </summary>
public class RedisConfigurationDto
{
    /// <summary>
    /// Redis connection string
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// Redis database number
    /// </summary>
    public int Database { get; set; } = 0;

    /// <summary>
    /// Connection timeout in milliseconds
    /// </summary>
    public int ConnectionTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Key prefix for Redis keys
    /// </summary>
    public string KeyPrefix { get; set; }
}

/// <summary>
/// Elasticsearch configuration
/// </summary>
public class ElasticsearchConfigurationDto
{
    /// <summary>
    /// Elasticsearch URLs
    /// </summary>
    public string[] Urls { get; set; } = System.Array.Empty<string>();

    /// <summary>
    /// Default index name
    /// </summary>
    public string DefaultIndex { get; set; }

    /// <summary>
    /// Authentication configuration
    /// </summary>
    public ElasticsearchAuthDto Authentication { get; set; }

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Orleans configuration data transfer object
/// </summary>
public class OrleansConfigurationDto
{
    /// <summary>
    /// Cluster configuration
    /// </summary>
    public OrleansClusterDto Cluster { get; set; }

    /// <summary>
    /// Silo configuration
    /// </summary>
    public OrleansSiloDto Silo { get; set; }

    /// <summary>
    /// Client configuration
    /// </summary>
    public OrleansClientDto Client { get; set; }

    /// <summary>
    /// Stream provider configuration
    /// </summary>
    public OrleansStreamDto Streams { get; set; }

    /// <summary>
    /// Storage provider configuration
    /// </summary>
    public OrleansStorageDto Storage { get; set; }
}

/// <summary>
/// Orleans cluster configuration
/// </summary>
public class OrleansClusterDto
{
    /// <summary>
    /// Cluster ID
    /// </summary>
    public string ClusterId { get; set; }

    /// <summary>
    /// Service ID
    /// </summary>
    public string ServiceId { get; set; }

    /// <summary>
    /// Clustering provider configuration
    /// </summary>
    public Dictionary<string, object> Provider { get; set; } = new();
}

/// <summary>
/// Orleans silo configuration
/// </summary>
public class OrleansSiloDto
{
    /// <summary>
    /// Silo name pattern
    /// </summary>
    public string NamePattern { get; set; }

    /// <summary>
    /// Advertised IP address
    /// </summary>
    public string AdvertisedIP { get; set; }

    /// <summary>
    /// Silo port
    /// </summary>
    public int SiloPort { get; set; }

    /// <summary>
    /// Gateway port
    /// </summary>
    public int GatewayPort { get; set; }

    /// <summary>
    /// Dashboard configuration
    /// </summary>
    public OrleansDashboardDto Dashboard { get; set; }
}

/// <summary>
/// Orleans client configuration
/// </summary>
public class OrleansClientDto
{
    /// <summary>
    /// Connection retry configuration
    /// </summary>
    public OrleansRetryDto Retry { get; set; }

    /// <summary>
    /// Gateway provider configuration
    /// </summary>
    public Dictionary<string, object> GatewayProvider { get; set; } = new();
}

/// <summary>
/// Observability configuration data transfer object
/// </summary>
public class ObservabilityConfigurationDto
{
    /// <summary>
    /// OpenTelemetry configuration
    /// </summary>
    public OpenTelemetryDto OpenTelemetry { get; set; }

    /// <summary>
    /// Metrics configuration
    /// </summary>
    public MetricsConfigurationDto Metrics { get; set; }

    /// <summary>
    /// Tracing configuration
    /// </summary>
    public TracingConfigurationDto Tracing { get; set; }

    /// <summary>
    /// Health check configuration
    /// </summary>
    public HealthCheckConfigurationDto HealthChecks { get; set; }
}

/// <summary>
/// Resource limits configuration
/// </summary>
public class ResourceLimitsDto
{
    /// <summary>
    /// CPU limit (in millicores)
    /// </summary>
    public int CpuLimitMillicores { get; set; } = 1000;

    /// <summary>
    /// Memory limit (in MB)
    /// </summary>
    public int MemoryLimitMB { get; set; } = 512;

    /// <summary>
    /// CPU request (in millicores)
    /// </summary>
    public int CpuRequestMillicores { get; set; } = 100;

    /// <summary>
    /// Memory request (in MB)
    /// </summary>
    public int MemoryRequestMB { get; set; } = 128;
}

/// <summary>
/// Scaling configuration
/// </summary>
public class ScalingConfigurationDto
{
    /// <summary>
    /// Minimum number of replicas
    /// </summary>
    public int MinReplicas { get; set; } = 1;

    /// <summary>
    /// Maximum number of replicas
    /// </summary>
    public int MaxReplicas { get; set; } = 10;

    /// <summary>
    /// Target CPU utilization percentage
    /// </summary>
    public int TargetCpuPercent { get; set; } = 70;

    /// <summary>
    /// Target memory utilization percentage
    /// </summary>
    public int TargetMemoryPercent { get; set; } = 80;
}

/// <summary>
/// Health check configuration
/// </summary>
public class HealthCheckConfigurationDto
{
    /// <summary>
    /// HTTP health check path
    /// </summary>
    public string Path { get; set; } = "/health";

    /// <summary>
    /// Health check port
    /// </summary>
    public int Port { get; set; } = 8080;

    /// <summary>
    /// Initial delay before first health check (seconds)
    /// </summary>
    public int InitialDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Interval between health checks (seconds)
    /// </summary>
    public int IntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Timeout for health check (seconds)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Number of consecutive failures before marking unhealthy
    /// </summary>
    public int FailureThreshold { get; set; } = 3;
}

// Supporting DTOs
public class WriteConcernDto
{
    public string W { get; set; } = "majority";
    public bool Journal { get; set; } = true;
    public int WTimeoutMs { get; set; } = 10000;
}

public class ConnectionPoolDto
{
    public int MaxSize { get; set; } = 100;
    public int MinSize { get; set; } = 5;
    public int MaxIdleTimeMs { get; set; } = 300000;
}

public class DatabaseHealthCheckDto
{
    public bool Enabled { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public int IntervalSeconds { get; set; } = 60;
}

public class ElasticsearchAuthDto
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string ApiKey { get; set; }
}

public class OrleansStreamDto
{
    public string Provider { get; set; } = "Kafka";
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class OrleansStorageDto
{
    public string DefaultProvider { get; set; } = "MongoDB";
    public Dictionary<string, object> Providers { get; set; } = new();
}

public class OrleansDashboardDto
{
    public string IP { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 8080;
    public bool Enabled { get; set; } = true;
}

public class OrleansRetryDto
{
    public int MaxAttempts { get; set; } = 3;
    public int InitialBackoffMs { get; set; } = 1000;
    public int MaxBackoffMs { get; set; } = 30000;
}

public class OpenTelemetryDto
{
    public bool Enabled { get; set; } = true;
    public string Endpoint { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class MetricsConfigurationDto
{
    public bool Enabled { get; set; } = true;
    public string ExportEndpoint { get; set; }
    public int ExportIntervalSeconds { get; set; } = 60;
}

public class TracingConfigurationDto
{
    public bool Enabled { get; set; } = true;
    public double SamplingRatio { get; set; } = 1.0;
    public string ExportEndpoint { get; set; }
}

/// <summary>
/// Log level enumeration
/// </summary>
public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical,
    None
}