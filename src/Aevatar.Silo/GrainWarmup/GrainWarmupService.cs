using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Aevatar.Silo.GrainWarmup;

/// <summary>
/// Background service that manages grain warmup operations
/// </summary>
/// <typeparam name="TIdentifier">The identifier type used by all grains (e.g., Guid, string, long)</typeparam>
public class GrainWarmupService<TIdentifier> : BackgroundService, IGrainWarmupService
{
    private readonly IGrainDiscoveryService _discoveryService;
    private readonly IGrainWarmupOrchestrator<TIdentifier> _orchestrator;
    private readonly GrainWarmupConfiguration _config;
    private readonly ILogger<GrainWarmupService<TIdentifier>> _logger;
    private readonly IEnumerable<IGrainWarmupStrategy> _strategies;
    private readonly GrainWarmupStatus _status = new();
    private readonly object _lock = new();

    public GrainWarmupService(
        IGrainDiscoveryService discoveryService,
        IGrainWarmupOrchestrator<TIdentifier> orchestrator,
        IEnumerable<IGrainWarmupStrategy> strategies,
        IOptions<GrainWarmupConfiguration> options,
        ILogger<GrainWarmupService<TIdentifier>> logger)
    {
        _discoveryService = discoveryService;
        _orchestrator = orchestrator;
        _strategies = strategies;
        _config = options.Value;
        _logger = logger;
        
        _logger.LogInformation("GrainWarmupService<{IdentifierType}> initialized with {Count} strategies", 
            typeof(TIdentifier).Name, _strategies.Count());
    }

    /// <summary>
    /// Starts the warmup process for all registered strategies
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the warmup operation</returns>
    public async Task StartWarmupAsync(CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("Grain warmup is disabled");
            return;
        }

        lock (_lock)
        {
            if (_status.IsRunning)
            {
                _logger.LogWarning("Warmup is already running");
                return;
            }

            _status.IsRunning = true;
            _status.StartTime = DateTime.UtcNow;
            _status.EndTime = null;
            _status.TotalGrains = 0;
            _status.WarmedUpGrains = 0;
            _status.FailedGrains = 0;
        }

        try
        {
            _logger.LogInformation("Starting grain warmup process...");

            // Discover grain types
            var grainTypes = _discoveryService.DiscoverWarmupEligibleGrainTypes().ToList();
            _logger.LogInformation("Discovered {Count} warmup-eligible grain types", grainTypes.Count);

            if (!grainTypes.Any())
            {
                _logger.LogInformation("No grain types found for warmup");
                return;
            }

            // Create execution plan
            var strategies = _strategies.ToList();
            var plan = _orchestrator.CreateExecutionPlan(grainTypes, strategies);

            // Execute warmup plan
            await _orchestrator.ExecuteWarmupPlanAsync(plan, cancellationToken);

            _logger.LogInformation("Grain warmup process completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during grain warmup process");
            throw;
        }
        finally
        {
            lock (_lock)
            {
                _status.IsRunning = false;
                _status.EndTime = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Stops the warmup process gracefully
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the stop operation</returns>
    public Task StopWarmupAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_status.IsRunning)
            {
                _logger.LogInformation("Warmup is not currently running");
                return Task.CompletedTask;
            }

            _status.IsRunning = false;
            _status.EndTime = DateTime.UtcNow;
            _logger.LogInformation("Warmup process stopped");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current warmup status
    /// </summary>
    /// <returns>The current warmup status</returns>
    public GrainWarmupStatus GetStatus()
    {
        lock (_lock)
        {
            return new GrainWarmupStatus
            {
                IsRunning = _status.IsRunning,
                TotalGrains = _status.TotalGrains,
                WarmedUpGrains = _status.WarmedUpGrains,
                FailedGrains = _status.FailedGrains,
                StartTime = _status.StartTime,
                EndTime = _status.EndTime
            };
        }
    }

    /// <summary>
    /// Background service execution
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    /// <returns>Task representing the background execution</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("Grain warmup is disabled, background service will not start");
            return;
        }

        // Wait a bit for the silo to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        try
        {
            await StartWarmupAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Grain warmup background service was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in grain warmup background service");
        }
    }
}