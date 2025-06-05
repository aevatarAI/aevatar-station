using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aevatar.Silo.AgentWarmup;

/// <summary>
/// Orchestrator for managing strategy execution order and agent type assignment
/// </summary>
/// <typeparam name="TIdentifier">The identifier type used by all agents (e.g., Guid, string, long)</typeparam>
public interface IAgentWarmupOrchestrator<TIdentifier>
{
    /// <summary>
    /// Plans warmup execution based on discovered agents and registered strategies
    /// </summary>
    /// <param name="agentTypes">The agent types to warm up</param>
    /// <param name="strategies">The available strategies</param>
    /// <returns>The execution plan</returns>
    WarmupExecutionPlan CreateExecutionPlan(
        IEnumerable<Type> agentTypes, 
        IEnumerable<IAgentWarmupStrategy> strategies);
    
    /// <summary>
    /// Executes the warmup plan
    /// </summary>
    /// <param name="plan">The execution plan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the execution</returns>
    Task ExecuteWarmupPlanAsync(WarmupExecutionPlan plan, CancellationToken cancellationToken = default);
} 