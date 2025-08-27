using System;
using System.Collections.Generic;

namespace Aevatar.Silo.AgentWarmup;

/// <summary>
/// Represents a plan for executing agent warmup strategies
/// </summary>
public class WarmupExecutionPlan
{
    /// <summary>
    /// List of strategy executions in order of priority
    /// </summary>
    public List<StrategyExecution> StrategyExecutions { get; set; } = new();
    
    /// <summary>
    /// Agent types that were not assigned to any strategy
    /// </summary>
    public List<Type> UnassignedAgentTypes { get; set; } = new();
}

/// <summary>
/// Represents the execution of a single strategy for specific agent types
/// </summary>
public class StrategyExecution
{
    /// <summary>
    /// The strategy to execute
    /// </summary>
    public IAgentWarmupStrategy Strategy { get; set; } = null!;
    
    /// <summary>
    /// The agent types this strategy will handle
    /// </summary>
    public List<Type> TargetAgentTypes { get; set; } = new();
    
    /// <summary>
    /// Priority of this strategy execution
    /// </summary>
    public int Priority { get; set; }
} 