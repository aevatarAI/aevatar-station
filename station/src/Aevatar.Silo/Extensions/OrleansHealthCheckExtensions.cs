// ABOUTME: This file provides Orleans health check extensions for ASP.NET Core integration
// ABOUTME: Implements elegant health checks using Orleans built-in IHealthCheckParticipant system

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Aevatar.Options;

namespace Aevatar.Silo.Extensions;

/// <summary>
/// Extension methods for Orleans health check integration with ASP.NET Core
/// </summary>
public static class OrleansHealthCheckExtensions
{
    /// <summary>
    /// Add Orleans health checks to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddOrleansHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<OrleansHealthCheck>("orleans", tags: new[] { "live", "ready" });
            
        return services;
    }
    
    /// <summary>
    /// Map health check endpoints for Kubernetes probes
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <param name="healthCheckOptions">Health check configuration options</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder MapOrleansHealthChecks(this IApplicationBuilder app, Aevatar.Options.HealthCheckOptions? healthCheckOptions = null)
    {
        // Get options from DI if not provided
        healthCheckOptions ??= app.ApplicationServices.GetService<IOptions<Aevatar.Options.HealthCheckOptions>>()?.Value ?? new Aevatar.Options.HealthCheckOptions();
        
        if (!healthCheckOptions.Enabled)
        {
            return app;
        }
        
        // Liveness probe - basic orleans health
        app.UseHealthChecks(healthCheckOptions.LivenessPath, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteMinimalPlaintext
        });
        
        // Readiness probe - orleans ready to accept traffic
        app.UseHealthChecks(healthCheckOptions.ReadinessPath, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteMinimalPlaintext
        });
        
        // General health endpoint
        app.UseHealthChecks(healthCheckOptions.HealthPath, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = WriteMinimalPlaintext
        });
        
        return app;
    }
    
    /// <summary>
    /// Write minimal plaintext response for health checks
    /// </summary>
    private static Task WriteMinimalPlaintext(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "text/plain";
        
        var status = report.Status == HealthStatus.Healthy ? "Healthy" : 
                    report.Status == HealthStatus.Degraded ? "Degraded" : "Unhealthy";
        
        return context.Response.WriteAsync(status);
    }
}

/// <summary>
/// Orleans health check implementation using built-in IHealthCheckParticipant system
/// </summary>
public class OrleansHealthCheck : IHealthCheck
{
    private readonly IEnumerable<IHealthCheckParticipant> _participants;
    
    public OrleansHealthCheck(IEnumerable<IHealthCheckParticipant> participants)
    {
        _participants = participants;
    }
    
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var lastCheckTime = DateTime.UtcNow.AddMinutes(-1);
            var unhealthyParticipants = new List<string>();
            
            foreach (var participant in _participants)
            {
                if (!participant.CheckHealth(lastCheckTime, out string reason))
                {
                    unhealthyParticipants.Add(reason ?? participant.GetType().Name);
                }
            }
            
            if (unhealthyParticipants.Any())
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Orleans unhealthy: {string.Join(", ", unhealthyParticipants)}"));
            }
            
            return Task.FromResult(HealthCheckResult.Healthy("Orleans is healthy"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Orleans health check failed", ex));
        }
    }
}