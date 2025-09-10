// ABOUTME: This file defines DTOs for simplified business configuration management API
// ABOUTME: Supports uploading business configuration as JSON strings through REST endpoints

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Aevatar.Enum;

namespace Aevatar.Admin.Dto;

/// <summary>
/// Data transfer object for business configuration management
/// </summary>
public class BusinessConfigurationDto
{
    /// <summary>
    /// Host identifier for the configuration
    /// </summary>
    [Required]
    public string HostId { get; set; }

    /// <summary>
    /// Configuration version
    /// </summary>
    public string Version { get; set; } = "1";

    /// <summary>
    /// Business-specific configuration data as JSON string
    /// </summary>
    [Required]
    public string ConfigurationJson { get; set; } = "{}";

    /// <summary>
    /// Configuration scope (Global, Host, Environment)
    /// </summary>
    public ConfigurationScope Scope { get; set; } = ConfigurationScope.Host;

    /// <summary>
    /// When the configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the configuration was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Who created the configuration
    /// </summary>
    public string CreatedBy { get; set; }

    /// <summary>
    /// Optional description of the configuration
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Whether this configuration is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Configuration tags for categorization
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Data transfer object for system configuration overrides
/// </summary>
public class SystemConfigurationDto
{
    /// <summary>
    /// Host identifier for the configuration
    /// </summary>
    [Required]
    public string HostId { get; set; }

    /// <summary>
    /// Logging configuration overrides
    /// </summary>
    public LoggingConfigurationDto Logging { get; set; }

    /// <summary>
    /// Database configuration overrides
    /// </summary>
    public DatabaseConfigurationDto Database { get; set; }

    /// <summary>
    /// Orleans-specific configuration overrides
    /// </summary>
    public OrleansConfigurationDto Orleans { get; set; }

    /// <summary>
    /// Observability configuration overrides
    /// </summary>
    public ObservabilityConfigurationDto Observability { get; set; }

    /// <summary>
    /// Custom system settings
    /// </summary>
    public Dictionary<string, string> CustomSettings { get; set; } = new();

    /// <summary>
    /// Environment variables to set
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
}

/// <summary>
/// Data transfer object for host creation with configuration
/// </summary>
public class CreateHostWithConfigurationDto
{
    /// <summary>
    /// Client ID for the host
    /// </summary>
    [Required]
    public string ClientId { get; set; }

    /// <summary>
    /// CORS URLs for the host
    /// </summary>
    public string CorsUrls { get; set; }

    /// <summary>
    /// Business configuration for the host
    /// </summary>
    public BusinessConfigurationDto BusinessConfiguration { get; set; }

    /// <summary>
    /// System configuration overrides for the host
    /// </summary>
    public SystemConfigurationDto SystemConfiguration { get; set; }

    /// <summary>
    /// Whether to deploy immediately after creation
    /// </summary>
    public bool DeployImmediately { get; set; } = true;

    /// <summary>
    /// Host creation options
    /// </summary>
    public HostCreationOptionsDto Options { get; set; } = new();
}

/// <summary>
/// Data transfer object for applying configuration changes
/// </summary>
public class ApplyConfigurationDto
{
    /// <summary>
    /// Whether to restart pods after applying configuration
    /// </summary>
    public bool RestartPods { get; set; } = true;

    /// <summary>
    /// Whether to validate configuration before applying
    /// </summary>
    public bool ValidateConfiguration { get; set; } = true;

    /// <summary>
    /// Services affected by the configuration change
    /// </summary>
    public string[] AffectedServices { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Whether to create backup before applying changes
    /// </summary>
    public bool CreateBackup { get; set; } = true;

    /// <summary>
    /// Optional rollback configuration
    /// </summary>
    public RollbackOptionsDto RollbackOptions { get; set; }
}

/// <summary>
/// Data transfer object for updating Docker image with configuration
/// </summary>
public class UpdateDockerImageWithConfigurationDto
{
    /// <summary>
    /// Host type for the image update
    /// </summary>
    [Required]
    public HostTypeEnum HostType { get; set; }

    /// <summary>
    /// New Docker image name
    /// </summary>
    [Required]
    public string ImageName { get; set; }

    /// <summary>
    /// Business configuration to update along with the image
    /// </summary>
    public BusinessConfigurationDto BusinessConfiguration { get; set; }

    /// <summary>
    /// System configuration to update along with the image
    /// </summary>
    public SystemConfigurationDto SystemConfiguration { get; set; }

    /// <summary>
    /// Configuration application settings
    /// </summary>
    public ApplyConfigurationDto ApplyConfigurationChanges { get; set; }

    /// <summary>
    /// Whether to validate configuration before applying
    /// </summary>
    public bool ValidateBeforeUpdate { get; set; } = true;

    /// <summary>
    /// Update options
    /// </summary>
    public ImageUpdateOptionsDto UpdateOptions { get; set; } = new();
}

/// <summary>
/// Options for Docker image updates
/// </summary>
public class ImageUpdateOptionsDto
{
    /// <summary>
    /// Whether to force pull the image even if it already exists
    /// </summary>
    public bool ForcePull { get; set; } = false;

    /// <summary>
    /// Rolling update strategy
    /// </summary>
    public RollingUpdateStrategyDto RollingUpdate { get; set; } = new();

    /// <summary>
    /// Health check configuration for the update
    /// </summary>
    public UpdateHealthCheckDto HealthCheck { get; set; } = new();

    /// <summary>
    /// Timeout for the update operation (minutes)
    /// </summary>
    public int TimeoutMinutes { get; set; } = 10;
}

/// <summary>
/// Rolling update strategy configuration
/// </summary>
public class RollingUpdateStrategyDto
{
    /// <summary>
    /// Maximum number of pods that can be unavailable during update
    /// </summary>
    public string MaxUnavailable { get; set; } = "25%";

    /// <summary>
    /// Maximum number of pods that can be created above desired number
    /// </summary>
    public string MaxSurge { get; set; } = "25%";
}

/// <summary>
/// Health check configuration for updates
/// </summary>
public class UpdateHealthCheckDto
{
    /// <summary>
    /// Whether to wait for health checks to pass before considering update successful
    /// </summary>
    public bool WaitForHealthCheck { get; set; } = true;

    /// <summary>
    /// Timeout for health checks (seconds)
    /// </summary>
    public int HealthCheckTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Number of consecutive successful health checks required
    /// </summary>
    public int RequiredSuccessfulChecks { get; set; } = 3;
}

/// <summary>
/// Host creation options
/// </summary>
public class HostCreationOptionsDto
{
    /// <summary>
    /// Resource limits for the host
    /// </summary>
    public ResourceLimitsDto ResourceLimits { get; set; }

    /// <summary>
    /// Scaling configuration
    /// </summary>
    public ScalingConfigurationDto Scaling { get; set; }

    /// <summary>
    /// Health check configuration
    /// </summary>
    public HealthCheckConfigurationDto HealthChecks { get; set; }
}

/// <summary>
/// Rollback options for configuration changes
/// </summary>
public class RollbackOptionsDto
{
    /// <summary>
    /// Whether automatic rollback is enabled
    /// </summary>
    public bool EnableAutoRollback { get; set; } = true;

    /// <summary>
    /// Timeout for rollback decision (minutes)
    /// </summary>
    public int RollbackTimeoutMinutes { get; set; } = 5;

    /// <summary>
    /// Health check criteria for rollback
    /// </summary>
    public string[] HealthCheckCriteria { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Configuration scope enumeration
/// </summary>
public enum ConfigurationScope
{
    /// <summary>
    /// Global configuration applies to all hosts
    /// </summary>
    Global,

    /// <summary>
    /// Host-specific configuration
    /// </summary>
    Host,

    /// <summary>
    /// Environment-specific configuration
    /// </summary>
    Environment
}