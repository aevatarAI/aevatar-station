using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Aevatar.Core.Abstractions;

namespace Aevatar.Silo.GrainWarmup;

/// <summary>
/// Orchestrator for managing strategy execution order and grain type assignment
/// </summary>
/// <typeparam name="TIdentifier">The identifier type used by all grains (e.g., Guid, string, long)</typeparam>
public class GrainWarmupOrchestrator<TIdentifier> : IGrainWarmupOrchestrator<TIdentifier>
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<GrainWarmupOrchestrator<TIdentifier>> _logger;

    public GrainWarmupOrchestrator(
        IGrainFactory grainFactory,
        ILogger<GrainWarmupOrchestrator<TIdentifier>> logger)
    {
        _grainFactory = grainFactory;
        _logger = logger;
    }

    /// <summary>
    /// Plans warmup execution based on discovered grains and registered strategies
    /// </summary>
    /// <param name="grainTypes">The grain types to warm up</param>
    /// <param name="strategies">The available strategies</param>
    /// <returns>The execution plan</returns>
    public WarmupExecutionPlan CreateExecutionPlan(
        IEnumerable<Type> grainTypes, 
        IEnumerable<IGrainWarmupStrategy> strategies)
    {
        var plan = new WarmupExecutionPlan();
        var grainTypesList = grainTypes.ToList();
        var strategiesList = strategies.ToList();

        _logger.LogInformation("Creating warmup execution plan for {GrainCount} grain types with {StrategyCount} strategies", 
            grainTypesList.Count, strategiesList.Count);

        // Track which grain types have been assigned to strategies
        var assignedGrainTypes = new HashSet<Type>();

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

            // Determine which grain types this strategy should handle
            foreach (var grainType in grainTypesList)
            {
                // Skip if already assigned to a higher priority strategy
                if (assignedGrainTypes.Contains(grainType))
                    continue;

                // Check if strategy applies to this grain type
                if (strategy.AppliesTo(grainType))
                {
                    strategyExecution.TargetGrainTypes.Add(grainType);
                    assignedGrainTypes.Add(grainType);
                }
            }

            // Only add strategy execution if it has target grain types
            if (strategyExecution.TargetGrainTypes.Any())
            {
                plan.StrategyExecutions.Add(strategyExecution);
                
                _logger.LogInformation("Strategy {StrategyName} (Priority: {Priority}) assigned {GrainCount} grain types: {GrainTypes}", 
                    strategy.Name, strategy.Priority, strategyExecution.TargetGrainTypes.Count,
                    string.Join(", ", strategyExecution.TargetGrainTypes.Select(t => t.Name)));
            }
        }

        // Track unassigned grain types
        plan.UnassignedGrainTypes = grainTypesList
            .Where(gt => !assignedGrainTypes.Contains(gt))
            .ToList();

        if (plan.UnassignedGrainTypes.Any())
        {
            _logger.LogWarning("Found {Count} unassigned grain types: {GrainTypes}", 
                plan.UnassignedGrainTypes.Count,
                string.Join(", ", plan.UnassignedGrainTypes.Select(t => t.Name)));
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

        var totalGrainTypes = plan.StrategyExecutions.Sum(se => se.TargetGrainTypes.Count);
        var processedGrainTypes = 0;

        foreach (var strategyExecution in plan.StrategyExecutions)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Warmup execution cancelled");
                break;
            }

            try
            {
                _logger.LogInformation("Executing strategy {StrategyName} for {GrainCount} grain types", 
                    strategyExecution.Strategy.Name, strategyExecution.TargetGrainTypes.Count);

                await ExecuteStrategyAsync(strategyExecution, cancellationToken);

                processedGrainTypes += strategyExecution.TargetGrainTypes.Count;
                
                _logger.LogInformation("Completed strategy {StrategyName}. Progress: {Processed}/{Total} grain types", 
                    strategyExecution.Strategy.Name, processedGrainTypes, totalGrainTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing strategy {StrategyName}", strategyExecution.Strategy.Name);
                // Continue with other strategies even if one fails
            }
        }

        _logger.LogInformation("Warmup plan execution completed. Processed {Processed}/{Total} grain types", 
            processedGrainTypes, totalGrainTypes);
    }

    /// <summary>
    /// Executes a single strategy for its assigned grain types
    /// </summary>
    private async Task ExecuteStrategyAsync(StrategyExecution strategyExecution, CancellationToken cancellationToken)
    {
        var strategy = strategyExecution.Strategy;
        
        // Since we're generic, we can directly work with TIdentifier
        if (strategy is not IGrainWarmupStrategy<TIdentifier> genericStrategy)
        {
            _logger.LogWarning("Strategy {StrategyName} does not support identifier type {IdentifierType}", 
                strategy.Name, typeof(TIdentifier).Name);
            return;
        }

        foreach (var grainType in strategyExecution.TargetGrainTypes)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await ExecuteStrategyForGrainTypeAsync(genericStrategy, grainType, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing strategy {StrategyName} for grain type {GrainType}", 
                    strategy.Name, grainType.Name);
                // Continue with other grain types
            }
        }
    }

    /// <summary>
    /// Executes a strategy for a specific grain type
    /// </summary>
    private async Task ExecuteStrategyForGrainTypeAsync(
        IGrainWarmupStrategy<TIdentifier> strategy, 
        Type grainType, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Executing strategy {StrategyName} for grain type {GrainType}", 
            strategy.Name, grainType.Name);

        var grainCount = 0;

        // Get the grain interface for this grain type
        var grainInterface = GetGrainInterface(grainType);
        _logger.LogDebug("Using grain interface {GrainInterface} for grain type {GrainType}", 
            grainInterface.Name, grainType.Name);

        await foreach (var identifier in strategy.GenerateGrainIdentifiersAsync(grainType, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // Create grain reference using grain interface (required by Orleans)
                var grain = CreateGrainReference(grainInterface, identifier);
                await grain.ActivateAsync();
                
                grainCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to warm up grain {GrainType} with identifier {Identifier}", 
                    grainType.Name, identifier);
            }
        }

        _logger.LogInformation("Strategy {StrategyName} warmed up {Count} grains of type {GrainType}", 
            strategy.Name, grainCount, grainType.Name);
    }

    /// <summary>
    /// Creates a grain reference using IGrainFactory.GetGrain with the specified grain interface and identifier
    /// </summary>
    private IGAgent CreateGrainReference(Type grainInterface, TIdentifier identifier) 
    {
        // Use the appropriate IGrainFactory.GetGrain method based on identifier type
        var grainResult = identifier switch
        {
            Guid guidId => _grainFactory.GetGrain(grainInterface, guidId),
            string stringId => _grainFactory.GetGrain(grainInterface, stringId),
            long longId => _grainFactory.GetGrain(grainInterface, longId),
            int intId => _grainFactory.GetGrain(grainInterface, (long)intId),
            _ => throw new ArgumentException($"Unsupported identifier type: {typeof(TIdentifier).Name}")
        };
        
        return (IGAgent)grainResult;
    }

    /// <summary>
    /// Gets the grain interface for a grain type
    /// </summary>
    private Type GetGrainInterface(Type grainType)
    {
        var interfaces = grainType.GetInterfaces();
        
        // Filter to Orleans grain interfaces (exclude system interfaces)
        var grainInterfaces = interfaces
            .Where(i => i.IsAssignableTo(typeof(IGAgent)))
            .ToList();

        // Look for specific grain interface that matches "I" + grain type name pattern
        // e.g., TestDbGAgent -> ITestDbGAgent
        var expectedInterfaceName = "I" + grainType.Name;
        var specificInterface = grainInterfaces
            .FirstOrDefault(i => i.Name == expectedInterfaceName);
        
        if (specificInterface != null)
        {
            _logger.LogDebug("Found specific grain interface {Interface} for {GrainType}", 
                specificInterface.Name, grainType.Name);
            return specificInterface;
        }

        // Look for IStateGAgent<TState> with specific state type
        var stateInterface = grainInterfaces
            .FirstOrDefault(i => i.IsGenericType && 
                               i.GetGenericTypeDefinition() == typeof(IStateGAgent<>));
        
        if (stateInterface != null)
        {
            _logger.LogDebug("Found state grain interface {Interface} for {GrainType}", 
                stateInterface.Name, grainType.Name);
            return stateInterface;
        }

        // Log warning if we have to fall back to IGAgent (this will likely cause resolution conflicts)
        if (typeof(IGAgent).IsAssignableFrom(grainType))
        {
            _logger.LogWarning("No specific grain interface found for {GrainType}, falling back to IGAgent (may cause resolution conflicts)", grainType.Name);
            return typeof(IGAgent);
        }

        // Final fallback to IGrainBase
        _logger.LogWarning("No specific grain interface found for {GrainType}, using IGrainBase", grainType.Name);
        return typeof(IGrainBase);
    }
}