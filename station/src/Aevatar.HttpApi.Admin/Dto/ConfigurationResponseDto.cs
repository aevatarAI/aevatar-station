// ABOUTME: This file defines response DTOs for configuration management operations
// ABOUTME: Includes validation results, operation status, and response data structures

using System;
using System.Collections.Generic;

namespace Aevatar.Admin.Dto;

/// <summary>
/// Configuration operation response
/// </summary>
public class ConfigurationOperationResponseDto
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Operation result message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Configuration ID if applicable
    /// </summary>
    public string ConfigurationId { get; set; }

    /// <summary>
    /// Validation result if validation was performed
    /// </summary>
    public ValidationResultDto ValidationResult { get; set; }

    /// <summary>
    /// Operation timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata about the operation
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Configuration validation result
/// </summary>
public class ValidationResultDto
{
    /// <summary>
    /// Whether the configuration is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<ValidationErrorDto> Errors { get; set; } = new();

    /// <summary>
    /// Validation warnings
    /// </summary>
    public List<ValidationWarningDto> Warnings { get; set; } = new();

    /// <summary>
    /// Overall validation score (0-100)
    /// </summary>
    public int ValidationScore { get; set; }

    /// <summary>
    /// Validation summary
    /// </summary>
    public string Summary { get; set; }

    /// <summary>
    /// Detailed validation report
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Validation error details
/// </summary>
public class ValidationErrorDto
{
    /// <summary>
    /// Error code
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Configuration path where error occurred
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Error severity level
    /// </summary>
    public ValidationSeverity Severity { get; set; }

    /// <summary>
    /// Suggested fix for the error
    /// </summary>
    public string SuggestedFix { get; set; }

    /// <summary>
    /// Additional error context
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Validation warning details
/// </summary>
public class ValidationWarningDto
{
    /// <summary>
    /// Warning code
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Warning message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Configuration path where warning occurred
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Warning category
    /// </summary>
    public ValidationCategory Category { get; set; }

    /// <summary>
    /// Recommended action
    /// </summary>
    public string RecommendedAction { get; set; }
}

/// <summary>
/// Configuration list response
/// </summary>
public class ConfigurationListResponseDto
{
    /// <summary>
    /// List of configurations
    /// </summary>
    public List<ConfigurationSummaryDto> Configurations { get; set; } = new();

    /// <summary>
    /// Total count of configurations
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Whether there are more pages
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Filter criteria used
    /// </summary>
    public Dictionary<string, object> Filters { get; set; } = new();
}

/// <summary>
/// Configuration summary for listing
/// </summary>
public class ConfigurationSummaryDto
{
    /// <summary>
    /// Configuration ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Host ID
    /// </summary>
    public string HostId { get; set; }

    /// <summary>
    /// Configuration name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Configuration description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Configuration scope
    /// </summary>
    public ConfigurationScope Scope { get; set; }

    /// <summary>
    /// Configuration version
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Whether configuration is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Created by user
    /// </summary>
    public string CreatedBy { get; set; }

    /// <summary>
    /// Configuration tags
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Configuration status
    /// </summary>
    public ConfigurationStatus Status { get; set; }
}

/// <summary>
/// Configuration deployment status response
/// </summary>
public class ConfigurationDeploymentStatusDto
{
    /// <summary>
    /// Host ID
    /// </summary>
    public string HostId { get; set; }

    /// <summary>
    /// Deployment status
    /// </summary>
    public DeploymentStatus Status { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Deployment progress percentage
    /// </summary>
    public int ProgressPercent { get; set; }

    /// <summary>
    /// Current deployment phase
    /// </summary>
    public string CurrentPhase { get; set; }

    /// <summary>
    /// Deployment start time
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Expected completion time
    /// </summary>
    public DateTime? EstimatedCompletionAt { get; set; }

    /// <summary>
    /// Deployment history
    /// </summary>
    public List<DeploymentPhaseDto> Phases { get; set; } = new();

    /// <summary>
    /// Any deployment errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Deployment metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Deployment phase information
/// </summary>
public class DeploymentPhaseDto
{
    /// <summary>
    /// Phase name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Phase status
    /// </summary>
    public DeploymentPhaseStatus Status { get; set; }

    /// <summary>
    /// Phase start time
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Phase completion time
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Phase duration
    /// </summary>
    public TimeSpan? Duration => CompletedAt?.Subtract(StartedAt);

    /// <summary>
    /// Phase message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Phase-specific details
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Configuration template response
/// </summary>
public class ConfigurationTemplateDto
{
    /// <summary>
    /// Template ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Template name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Template description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Configuration type this template is for
    /// </summary>
    public ConfigurationType Type { get; set; }

    /// <summary>
    /// Template content (JSON)
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Available placeholder keys
    /// </summary>
    public string[] PlaceholderKeys { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Default values for placeholders
    /// </summary>
    public Dictionary<string, object> DefaultValues { get; set; } = new();

    /// <summary>
    /// Template schema for validation
    /// </summary>
    public Dictionary<string, object> Schema { get; set; } = new();

    /// <summary>
    /// Template version
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Whether template is system-provided
    /// </summary>
    public bool IsSystemTemplate { get; set; }
}

// Enumerations

/// <summary>
/// Configuration validation severity levels
/// </summary>
public enum ValidationSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Configuration validation categories
/// </summary>
public enum ValidationCategory
{
    Syntax,
    Schema,
    Performance,
    Security,
    Compatibility,
    BestPractice
}

/// <summary>
/// Configuration status
/// </summary>
public enum ConfigurationStatus
{
    Draft,
    Active,
    Pending,
    Deployed,
    Failed,
    Archived
}

/// <summary>
/// Deployment status
/// </summary>
public enum DeploymentStatus
{
    NotDeployed,
    Pending,
    InProgress,
    Completed,
    Failed,
    RolledBack,
    Cancelled
}

/// <summary>
/// Deployment phase status
/// </summary>
public enum DeploymentPhaseStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped
}

/// <summary>
/// Configuration type enumeration
/// </summary>
public enum ConfigurationType
{
    BusinessConfiguration,
    SystemConfiguration,
    SiloTemplate,
    ClientTemplate,
    WebhookTemplate
}