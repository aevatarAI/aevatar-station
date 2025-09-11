using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace Aevatar.Application.Contracts.MCPGateway;

/// <summary>
/// MCP Adapter data transfer object
/// </summary>
public class MCPAdapterDto : AuditedEntityDto<string>
{
    /// <summary>
    /// Adapter name (unique identifier)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Docker image name
    /// </summary>
    public string ImageName { get; set; } = string.Empty;

    /// <summary>
    /// Image version/tag
    /// </summary>
    public string ImageVersion { get; set; } = string.Empty;

    /// <summary>
    /// Adapter description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Environment variables
    /// </summary>
    public Dictionary<string, string> Environment { get; set; } = new();

    /// <summary>
    /// Current status of the adapter
    /// </summary>
    public MCPAdapterStatus Status { get; set; }

    /// <summary>
    /// Number of active connections
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Last health check timestamp
    /// </summary>
    public DateTime? LastHealthCheck { get; set; }

    /// <summary>
    /// Health check status
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Resource usage statistics
    /// </summary>
    public MCPResourceUsageDto? ResourceUsage { get; set; }

    /// <summary>
    /// Tags for categorization
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Create MCP Adapter DTO
/// </summary>
public class CreateMCPAdapterDto
{
    /// <summary>
    /// Adapter name (unique identifier)
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [RegularExpression(@"^[a-z0-9]([a-z0-9-]*[a-z0-9])?$", ErrorMessage = "Name must be lowercase alphanumeric with hyphens")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Docker image name
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string ImageName { get; set; } = string.Empty;

    /// <summary>
    /// Image version/tag
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string ImageVersion { get; set; } = string.Empty;

    /// <summary>
    /// Adapter description
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Environment variables
    /// </summary>
    public Dictionary<string, string> Environment { get; set; } = new();

    /// <summary>
    /// Tags for categorization
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Resource limits
    /// </summary>
    public MCPResourceLimitsDto? ResourceLimits { get; set; }
}

/// <summary>
/// Update MCP Adapter DTO
/// </summary>
public class UpdateMCPAdapterDto
{
    /// <summary>
    /// Docker image name
    /// </summary>
    [StringLength(200)]
    public string? ImageName { get; set; }

    /// <summary>
    /// Image version/tag
    /// </summary>
    [StringLength(50)]
    public string? ImageVersion { get; set; }

    /// <summary>
    /// Adapter description
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Environment variables
    /// </summary>
    public Dictionary<string, string>? Environment { get; set; }

    /// <summary>
    /// Tags for categorization
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Resource limits
    /// </summary>
    public MCPResourceLimitsDto? ResourceLimits { get; set; }
}

/// <summary>
/// Get adapters input parameters
/// </summary>
public class GetAdaptersInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Filter by adapter status
    /// </summary>
    public MCPAdapterStatus? Status { get; set; }

    /// <summary>
    /// Filter by tags
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Search by name or description
    /// </summary>
    [StringLength(100)]
    public string? Search { get; set; }

    /// <summary>
    /// Filter by health status
    /// </summary>
    public bool? IsHealthy { get; set; }
}

/// <summary>
/// MCP Adapter status enumeration
/// </summary>
public enum MCPAdapterStatus
{
    /// <summary>
    /// Adapter is being created
    /// </summary>
    Creating = 0,

    /// <summary>
    /// Adapter is running and available
    /// </summary>
    Running = 1,

    /// <summary>
    /// Adapter is stopped
    /// </summary>
    Stopped = 2,

    /// <summary>
    /// Adapter has failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Adapter is being updated
    /// </summary>
    Updating = 4,

    /// <summary>
    /// Adapter is being deleted
    /// </summary>
    Deleting = 5
}

/// <summary>
/// Resource usage statistics
/// </summary>
public class MCPResourceUsageDto
{
    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// Memory usage in MB
    /// </summary>
    public long MemoryUsageMB { get; set; }

    /// <summary>
    /// Network I/O statistics
    /// </summary>
    public MCPNetworkIODto NetworkIO { get; set; } = new();

    /// <summary>
    /// Request statistics
    /// </summary>
    public MCPRequestStatsDto RequestStats { get; set; } = new();
}

/// <summary>
/// Resource limits configuration
/// </summary>
public class MCPResourceLimitsDto
{
    /// <summary>
    /// CPU limit (cores)
    /// </summary>
    [Range(0.1, 32.0)]
    public double? CpuLimit { get; set; }

    /// <summary>
    /// Memory limit in MB
    /// </summary>
    [Range(64, 32768)]
    public int? MemoryLimitMB { get; set; }

    /// <summary>
    /// Maximum number of concurrent connections
    /// </summary>
    [Range(1, 10000)]
    public int? MaxConnections { get; set; }

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    [Range(1, 300)]
    public int? RequestTimeoutSeconds { get; set; }
}

/// <summary>
/// Network I/O statistics
/// </summary>
public class MCPNetworkIODto
{
    /// <summary>
    /// Bytes received
    /// </summary>
    public long BytesReceived { get; set; }

    /// <summary>
    /// Bytes sent
    /// </summary>
    public long BytesSent { get; set; }

    /// <summary>
    /// Packets received
    /// </summary>
    public long PacketsReceived { get; set; }

    /// <summary>
    /// Packets sent
    /// </summary>
    public long PacketsSent { get; set; }
}

/// <summary>
/// Request statistics
/// </summary>
public class MCPRequestStatsDto
{
    /// <summary>
    /// Total requests processed
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Successful requests
    /// </summary>
    public long SuccessfulRequests { get; set; }

    /// <summary>
    /// Failed requests
    /// </summary>
    public long FailedRequests { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Requests per second
    /// </summary>
    public double RequestsPerSecond { get; set; }
}
