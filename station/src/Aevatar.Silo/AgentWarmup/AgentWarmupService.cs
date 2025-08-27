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

namespace Aevatar.Silo.AgentWarmup;

/// <summary>
/// Background service that manages agent warmup operations
/// </summary>
/// <typeparam name="TIdentifier">The identifier type used by all agents (e.g., Guid, string, long)</typeparam>
public class AgentWarmupService<TIdentifier> : BackgroundService, IAgentWarmupService
{
    private readonly IAgentDiscoveryService _discoveryService;
    private readonly IAgentWarmupOrchestrator<TIdentifier> _orchestrator;
    private readonly AgentWarmupConfiguration _config;
    private readonly ILogger<AgentWarmupService<TIdentifier>> _logger;
    private readonly IEnumerable<IAgentWarmupStrategy> _strategies;
    private readonly AgentWarmupStatus _status = new();
    private readonly object _lock = new();

    public AgentWarmupService(
        IAgentDiscoveryService discoveryService,
        IAgentWarmupOrchestrator<TIdentifier> orchestrator,
        IEnumerable<IAgentWarmupStrategy> strategies,
        IOptions<AgentWarmupConfiguration> options,
        ILogger<AgentWarmupService<TIdentifier>> logger)
    {
        _discoveryService = discoveryService;
        _orchestrator = orchestrator;
        _strategies = strategies;
        _config = options.Value;
        _logger = logger;
        
        _logger.LogInformation("AgentWarmupService<{IdentifierType}> initialized with {Count} strategies", 
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
            _logger.LogInformation("Agent warmup is disabled");
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
            _status.TotalAgents = 0;
            _status.WarmedUpAgents = 0;
            _status.FailedAgents = 0;
        }

        try
        {
            _logger.LogInformation("Starting agent warmup process...");

            // Discover agent types
            var agentTypes = _discoveryService.DiscoverWarmupEligibleAgentTypes().ToList();
            _logger.LogInformation("Discovered {Count} warmup-eligible agent types", agentTypes.Count);

            if (!agentTypes.Any())
            {
                _logger.LogInformation("No agent types found for warmup");
                return;
            }

            // Create execution plan
            var strategies = _strategies.ToList();
            var plan = _orchestrator.CreateExecutionPlan(agentTypes, strategies);

            // Execute warmup plan
            await _orchestrator.ExecuteWarmupPlanAsync(plan, cancellationToken);

            _logger.LogInformation("Agent warmup process completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during agent warmup process");
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
    public AgentWarmupStatus GetStatus()
    {
        lock (_lock)
        {
            return new AgentWarmupStatus
            {
                IsRunning = _status.IsRunning,
                TotalAgents = _status.TotalAgents,
                WarmedUpAgents = _status.WarmedUpAgents,
                FailedAgents = _status.FailedAgents,
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
            _logger.LogInformation("Agent warmup is disabled, background service will not start");
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
            _logger.LogInformation("Agent warmup background service was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in agent warmup background service");
        }
    }
}