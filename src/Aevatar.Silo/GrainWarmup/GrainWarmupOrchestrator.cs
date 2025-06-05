using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

    public AgentWarmupOrchestrator(
        IGrainFactory agentFactory,
        ILogger<AgentWarmupOrchestrator<TIdentifier>> logger)
    {
        _agentFactory = agentFactory;
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
    /// Executes a strategy for a specific agent type
    /// </summary>
    private async Task ExecuteStrategyForAgentTypeAsync(
        IAgentWarmupStrategy<TIdentifier> strategy, 
        Type agentType, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Executing strategy {StrategyName} for agent type {AgentType}", 
            strategy.Name, agentType.Name);

        var agentCount = 0;

        // Get the agent interface for this agent type
        var agentInterface = GetAgentInterface(agentType);
        _logger.LogDebug("Using agent interface {AgentInterface} for agent type {AgentType}", 
            agentInterface.Name, agentType.Name);

        await foreach (var identifier in strategy.GenerateAgentIdentifiersAsync(agentType, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // Create agent reference using agent interface (required by Orleans)
                var agent = CreateAgentReference(agentInterface, identifier);
                await agent.ActivateAsync();
                
                agentCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to warm up agent {AgentType} with identifier {Identifier}", 
                    agentType.Name, identifier);
            }
        }

        _logger.LogInformation("Strategy {StrategyName} warmed up {Count} agents of type {AgentType}", 
            strategy.Name, agentCount, agentType.Name);
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