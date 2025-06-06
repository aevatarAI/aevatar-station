using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Aevatar.Core.Abstractions;

namespace Aevatar.Silo.AgentWarmup;

/// <summary>
/// Orchestrator for managing strategy execution order and agent type assignment
/// </summary>
/// <typeparam name="TIdentifier">The identifier type used by all agents (e.g., Guid, string, long)</typeparam>
public class AgentWarmupOrchestrator<TIdentifier> : IAgentWarmupOrchestrator<TIdentifier>
{
    private readonly IGrainFactory _agentFactory;
    private readonly ILogger<AgentWarmupOrchestrator<TIdentifier>> _logger;
    private readonly AgentWarmupConfiguration _config;

    public AgentWarmupOrchestrator(
        IGrainFactory agentFactory,
        IOptions<AgentWarmupConfiguration> options,
        ILogger<AgentWarmupOrchestrator<TIdentifier>> logger)
    {
        _agentFactory = agentFactory;
        _config = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Plans warmup execution based on discovered agents and registered strategies
    /// </summary>
    /// <param name="agentTypes">The agent types to warm up</param>
    /// <param name="strategies">The available strategies</param>
    /// <returns>The execution plan</returns>
    public WarmupExecutionPlan CreateExecutionPlan(
        IEnumerable<Type> agentTypes, 
        IEnumerable<IAgentWarmupStrategy> strategies)
    {
        var plan = new WarmupExecutionPlan();
        var agentTypesList = agentTypes.ToList();
        var strategiesList = strategies.ToList();

        _logger.LogInformation("Creating warmup execution plan for {AgentCount} agent types with {StrategyCount} strategies", 
            agentTypesList.Count, strategiesList.Count);

        // Track which agent types have been assigned to strategies
        var assignedAgentTypes = new HashSet<Type>();

        // Sort strategies by priority (higher priority first)
        var sortedStrategies = strategiesList
            .OrderByDescending(s => s.Priority)
            .ThenBy(s => s.Name)
            .ToList();

        foreach (var strategy in sortedStrategies)
        {
            var strategyExecution = new StrategyExecution
            {
                Strategy = strategy,
                Priority = strategy.Priority
            };

            // Determine which agent types this strategy should handle
            foreach (var agentType in agentTypesList)
            {
                // Skip if already assigned to a higher priority strategy
                if (assignedAgentTypes.Contains(agentType))
                    continue;

                // Check if strategy applies to this agent type
                if (strategy.AppliesTo(agentType))
                {
                    strategyExecution.TargetAgentTypes.Add(agentType);
                    assignedAgentTypes.Add(agentType);
                }
            }

            // Only add strategy execution if it has target agent types
            if (strategyExecution.TargetAgentTypes.Any())
            {
                plan.StrategyExecutions.Add(strategyExecution);
                
                _logger.LogInformation("Strategy {StrategyName} (Priority: {Priority}) assigned {AgentCount} agent types: {AgentTypes}", 
                    strategy.Name, strategy.Priority, strategyExecution.TargetAgentTypes.Count,
                    string.Join(", ", strategyExecution.TargetAgentTypes.Select(t => t.Name)));
            }
        }

        // Track unassigned agent types
        plan.UnassignedAgentTypes = agentTypesList
            .Where(gt => !assignedAgentTypes.Contains(gt))
            .ToList();

        if (plan.UnassignedAgentTypes.Any())
        {
            _logger.LogWarning("Found {Count} unassigned agent types: {AgentTypes}", 
                plan.UnassignedAgentTypes.Count,
                string.Join(", ", plan.UnassignedAgentTypes.Select(t => t.Name)));
        }

        _logger.LogInformation("Execution plan created with {StrategyCount} strategy executions", 
            plan.StrategyExecutions.Count);

        return plan;
    }

    /// <summary>
    /// Executes the warmup plan
    /// </summary>
    /// <param name="plan">The execution plan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the execution</returns>
    public async Task ExecuteWarmupPlanAsync(WarmupExecutionPlan plan, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting execution of warmup plan with {StrategyCount} strategies", 
            plan.StrategyExecutions.Count);

        var totalAgentTypes = plan.StrategyExecutions.Sum(se => se.TargetAgentTypes.Count);
        var processedAgentTypes = 0;

        foreach (var strategyExecution in plan.StrategyExecutions)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Warmup execution cancelled");
                break;
            }

            try
            {
                _logger.LogInformation("Executing strategy {StrategyName} for {AgentCount} agent types", 
                    strategyExecution.Strategy.Name, strategyExecution.TargetAgentTypes.Count);

                await ExecuteStrategyAsync(strategyExecution, cancellationToken);

                processedAgentTypes += strategyExecution.TargetAgentTypes.Count;
                
                _logger.LogInformation("Completed strategy {StrategyName}. Progress: {Processed}/{Total} agent types", 
                    strategyExecution.Strategy.Name, processedAgentTypes, totalAgentTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing strategy {StrategyName}", strategyExecution.Strategy.Name);
                // Continue with other strategies even if one fails
            }
        }

        _logger.LogInformation("Warmup plan execution completed. Processed {Processed}/{Total} agent types", 
            processedAgentTypes, totalAgentTypes);
    }

    /// <summary>
    /// Executes a single strategy for its assigned agent types
    /// </summary>
    private async Task ExecuteStrategyAsync(StrategyExecution strategyExecution, CancellationToken cancellationToken)
    {
        var strategy = strategyExecution.Strategy;
        
        // Since we're generic, we can directly work with TIdentifier
        if (strategy is not IAgentWarmupStrategy<TIdentifier> genericStrategy)
        {
            _logger.LogWarning("Strategy {StrategyName} does not support identifier type {IdentifierType}", 
                strategy.Name, typeof(TIdentifier).Name);
            return;
        }

        foreach (var agentType in strategyExecution.TargetAgentTypes)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await ExecuteStrategyForAgentTypeAsync(genericStrategy, agentType, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing strategy {StrategyName} for agent type {AgentType}", 
                    strategy.Name, agentType.Name);
                // Continue with other agent types
            }
        }
    }

    /// <summary>
    /// Executes a strategy for a specific agent type with true concurrent processing
    /// </summary>
    private async Task ExecuteStrategyForAgentTypeAsync(
        IAgentWarmupStrategy<TIdentifier> strategy, 
        Type agentType, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Executing strategy {StrategyName} for agent type {AgentType}", 
            strategy.Name, agentType.Name);

        var agentCount = 0;
        var failedCount = 0;

        // Get the agent interface for this agent type
        var agentInterface = GetAgentInterface(agentType);
        _logger.LogDebug("Using agent interface {AgentInterface} for agent type {AgentType}", 
            agentInterface.Name, agentType.Name);

        // Create semaphore for concurrency control
        using var semaphore = new SemaphoreSlim(_config.MaxConcurrency, _config.MaxConcurrency);
        var activationTasks = new List<Task<bool>>();
        var batchCount = 0;
        var currentBatchSize = _config.InitialBatchSize;

        await foreach (var identifier in strategy.GenerateAgentIdentifiersAsync(agentType, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            // Start activation task without awaiting (true concurrency)
            var activationTask = ActivateAgentConcurrentlyAsync(agentInterface, identifier, semaphore, cancellationToken);
            activationTasks.Add(activationTask);

            batchCount++;

            // Process batch when reaching current batch size
            if (batchCount >= currentBatchSize)
            {
                // Wait for current batch to complete
                var completedTasks = await Task.WhenAll(activationTasks);
                
                // Count successes and failures
                var batchSuccesses = completedTasks.Count(success => success);
                var batchFailures = completedTasks.Count(success => !success);
                
                agentCount += batchSuccesses;
                failedCount += batchFailures;

                _logger.LogDebug("Batch completed for {AgentType}: {Successes} successes, {Failures} failures", 
                    agentType.Name, batchSuccesses, batchFailures);

                // Clear completed tasks
                activationTasks.Clear();
                batchCount = 0;

                // Progressive batch size increase
                if (currentBatchSize < _config.MaxBatchSize)
                {
                    currentBatchSize = Math.Min(_config.MaxBatchSize, 
                        (int)(currentBatchSize * _config.BatchSizeIncreaseFactor));
                }

                // Delay between batches
                if (_config.DelayBetweenBatchesMs > 0)
                {
                    await Task.Delay(_config.DelayBetweenBatchesMs, cancellationToken);
                }
            }
        }

        // Process remaining tasks
        if (activationTasks.Any())
        {
            var completedTasks = await Task.WhenAll(activationTasks);
            var remainingSuccesses = completedTasks.Count(success => success);
            var remainingFailures = completedTasks.Count(success => !success);
            
            agentCount += remainingSuccesses;
            failedCount += remainingFailures;
        }

        _logger.LogInformation("Strategy {StrategyName} completed for agent type {AgentType}: {SuccessCount} activated, {FailedCount} failed", 
            strategy.Name, agentType.Name, agentCount, failedCount);
    }

    /// <summary>
    /// Activates a single agent concurrently with proper semaphore management
    /// </summary>
    private async Task<bool> ActivateAgentConcurrentlyAsync(
        Type agentInterface, 
        TIdentifier identifier, 
        SemaphoreSlim semaphore, 
        CancellationToken cancellationToken)
    {
        // Wait for semaphore slot
        await semaphore.WaitAsync(cancellationToken);
        
        try
        {
            var retryCount = 0;
            while (retryCount <= _config.MaxRetryAttempts)
            {
                try
                {
                    // Create agent reference using agent interface (required by Orleans)
                    var agent = CreateAgentReference(agentInterface, identifier);
                    
                    // Activate with timeout by calling ActivateAsync
                    using var timeoutCts = new CancellationTokenSource(_config.AgentActivationTimeoutMs);
                    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                    
                    // Properly activate the grain
                    await agent.ActivateAsync();
                    
                    return true; // Success
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Overall cancellation requested
                    return false;
                }
                catch (Exception ex) when (retryCount < _config.MaxRetryAttempts)
                {
                    // Retry on failure
                    retryCount++;
                    _logger.LogDebug(ex, "Agent activation failed (attempt {Attempt}/{MaxAttempts}) for identifier {Identifier}, retrying...", 
                        retryCount, _config.MaxRetryAttempts + 1, identifier);
                    
                    if (_config.RetryDelayMs > 0)
                    {
                        await Task.Delay(_config.RetryDelayMs, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    // Final failure after all retries
                    _logger.LogWarning(ex, "Failed to activate agent with identifier {Identifier} after {Attempts} attempts", 
                        identifier, _config.MaxRetryAttempts + 1);
                    return false;
                }
            }
            
            return false; // Should not reach here
        }
        finally
        {
            // Always release semaphore
            semaphore.Release();
        }
    }

    /// <summary>
    /// Creates a agent reference using IGrainFactory.GetGrain with the specified agent interface and identifier
    /// </summary>
    private IGAgent CreateAgentReference(Type agentInterface, TIdentifier identifier) 
    {
        // Use the appropriate IGrainFactory.GetGrain method based on identifier type
        var agentResult = identifier switch
        {
            Guid guidId => _agentFactory.GetGrain(agentInterface, guidId),
            string stringId => _agentFactory.GetGrain(agentInterface, stringId),
            long longId => _agentFactory.GetGrain(agentInterface, longId),
            int intId => _agentFactory.GetGrain(agentInterface, (long)intId),
            _ => throw new ArgumentException($"Unsupported identifier type: {typeof(TIdentifier).Name}")
        };
        
        return (IGAgent)agentResult;
    }

    /// <summary>
    /// Gets the agent interface for a agent type
    /// </summary>
    private Type GetAgentInterface(Type agentType)
    {
        var interfaces = agentType.GetInterfaces();
        
        // Filter to Orleans agent interfaces (exclude system interfaces)
        var agentInterfaces = interfaces
            .Where(i => i.IsAssignableTo(typeof(IGAgent)))
            .ToList();

        // Look for specific agent interface that matches "I" + agent type name pattern
        // e.g., TestDbGAgent -> ITestDbGAgent
        var expectedInterfaceName = "I" + agentType.Name;
        var specificInterface = agentInterfaces
            .FirstOrDefault(i => i.Name == expectedInterfaceName);
        
        if (specificInterface != null)
        {
            _logger.LogDebug("Found specific agent interface {Interface} for {AgentType}", 
                specificInterface.Name, agentType.Name);
            return specificInterface;
        }

        // Look for IStateGAgent<TState> with specific state type
        var stateInterface = agentInterfaces
            .FirstOrDefault(i => i.IsGenericType && 
                               i.GetGenericTypeDefinition() == typeof(IStateGAgent<>));
        
        if (stateInterface != null)
        {
            _logger.LogDebug("Found state agent interface {Interface} for {AgentType}", 
                stateInterface.Name, agentType.Name);
            return stateInterface;
        }

        // Log warning if we have to fall back to IGAgent (this will likely cause resolution conflicts)
        if (typeof(IGAgent).IsAssignableFrom(agentType))
        {
            _logger.LogWarning("No specific agent interface found for {AgentType}, falling back to IGAgent (may cause resolution conflicts)", agentType.Name);
            return typeof(IGAgent);
        }

        // Final fallback to IGrainBase
        _logger.LogWarning("No specific agent interface found for {AgentType}, using IGrainBase", agentType.Name);
        return typeof(IGrainBase);
    }
}