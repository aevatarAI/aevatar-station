using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aevatar.Silo.GrainWarmup;

/// <summary>
/// Status of grain warmup operation
/// </summary>
public class GrainWarmupStatus
{
    public bool IsRunning { get; set; }
    public int TotalStrategies { get; set; }
    public int CompletedStrategies { get; set; }
    public int TotalGrains { get; set; }
    public int WarmedUpGrains { get; set; }
    public int FailedGrains { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration => EndTime.HasValue && StartTime.HasValue ? EndTime - StartTime : null;
    public string? CurrentStrategy { get; set; }
    public double ProgressPercentage => TotalGrains > 0 ? (double)(WarmedUpGrains + FailedGrains) / TotalGrains * 100 : 0;
    public double SuccessRate => (WarmedUpGrains + FailedGrains) > 0 ? (double)WarmedUpGrains / (WarmedUpGrains + FailedGrains) * 100 : 0;
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Service for warming up Orleans grains to reduce activation latency
/// </summary>
public interface IGrainWarmupService
{
    /// <summary>
    /// Starts the warmup process for all registered strategies
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the warmup operation</returns>
    Task StartWarmupAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops the warmup process gracefully
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the stop operation</returns>
    Task StopWarmupAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current warmup status
    /// </summary>
    /// <returns>The current warmup status</returns>
    GrainWarmupStatus GetStatus();
} 