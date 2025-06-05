using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aevatar.Silo.GrainWarmup;

/// <summary>
/// Orchestrator for managing strategy execution order and grain type assignment
/// </summary>
/// <typeparam name="TIdentifier">The identifier type used by all grains (e.g., Guid, string, long)</typeparam>
public interface IGrainWarmupOrchestrator<TIdentifier>
{
    /// <summary>
    /// Plans warmup execution based on discovered grains and registered strategies
    /// </summary>
    /// <param name="grainTypes">The grain types to warm up</param>
    /// <param name="strategies">The available strategies</param>
    /// <returns>The execution plan</returns>
    WarmupExecutionPlan CreateExecutionPlan(
        IEnumerable<Type> grainTypes, 
        IEnumerable<IGrainWarmupStrategy> strategies);
    
    /// <summary>
    /// Executes the warmup plan
    /// </summary>
    /// <param name="plan">The execution plan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the execution</returns>
    Task ExecuteWarmupPlanAsync(WarmupExecutionPlan plan, CancellationToken cancellationToken = default);
} 