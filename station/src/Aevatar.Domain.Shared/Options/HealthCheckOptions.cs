// ABOUTME: This file defines configuration options for Orleans health check endpoints
// ABOUTME: Provides configurable port and path settings for health check infrastructure

namespace Aevatar.Options;

/// <summary>
/// Configuration options for Orleans health check endpoints
/// </summary>
public class HealthCheckOptions
{
    /// <summary>
    /// The port number for health check endpoints
    /// Default: 8081
    /// </summary>
    public int Port { get; set; } = 8081;
    
    /// <summary>
    /// Whether health check endpoints are enabled
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// The path for the general health endpoint
    /// Default: "/health"
    /// </summary>
    public string HealthPath { get; set; } = "/health";
    
    /// <summary>
    /// The path for the liveness probe endpoint
    /// Default: "/health/live"
    /// </summary>
    public string LivenessPath { get; set; } = "/health/live";
    
    /// <summary>
    /// The path for the readiness probe endpoint
    /// Default: "/health/ready"
    /// </summary>
    public string ReadinessPath { get; set; } = "/health/ready";
    
    /// <summary>
    /// Timeout in seconds for health check operations
    /// Default: 30 seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Gets the full URL pattern for the health check endpoints
    /// </summary>
    public string GetUrl() => $"http://*:{Port}";
}