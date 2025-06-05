using System;
using System.Collections.Generic;

namespace Aevatar.Silo.GrainWarmup;

/// <summary>
/// Represents a plan for executing grain warmup strategies
/// </summary>
public class WarmupExecutionPlan
{
    /// <summary>
    /// List of strategy executions in order of priority
    /// </summary>
    public List<StrategyExecution> StrategyExecutions { get; set; } = new();
    
    /// <summary>
    /// Grain types that were not assigned to any strategy
    /// </summary>
    public List<Type> UnassignedGrainTypes { get; set; } = new();
}

/// <summary>
/// Represents the execution of a single strategy for specific grain types
/// </summary>
public class StrategyExecution
{
    /// <summary>
    /// The strategy to execute
    /// </summary>
    public IGrainWarmupStrategy Strategy { get; set; } = null!;
    
    /// <summary>
    /// The grain types this strategy will handle
    /// </summary>
    public List<Type> TargetGrainTypes { get; set; } = new();
    
    /// <summary>
    /// Priority of this strategy execution
    /// </summary>
    public int Priority { get; set; }
} 