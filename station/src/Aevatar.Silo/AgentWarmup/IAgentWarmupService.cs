using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aevatar.Silo.AgentWarmup;

/// <summary>
/// Status of agent warmup operation
/// </summary>
public class AgentWarmupStatus
{
    public bool IsRunning { get; set; }
    public int TotalStrategies { get; set; }
    public int CompletedStrategies { get; set; }
    public int TotalAgents { get; set; }
    public int WarmedUpAgents { get; set; }
    public int FailedAgents { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration => EndTime.HasValue && StartTime.HasValue ? EndTime - StartTime : null;
    public string? CurrentStrategy { get; set; }
    public double ProgressPercentage => TotalAgents > 0 ? (double)(WarmedUpAgents + FailedAgents) / TotalAgents * 100 : 0;
    public double SuccessRate => (WarmedUpAgents + FailedAgents) > 0 ? (double)WarmedUpAgents / (WarmedUpAgents + FailedAgents) * 100 : 0;
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Service for warming up Orleans agents to reduce activation latency
/// </summary>
public interface IAgentWarmupService
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
    AgentWarmupStatus GetStatus();
} 