namespace Aevatar.Core.Abstractions;

/// <summary>
/// Base class for core agent state without layered communication fields.
/// Provides fundamental state functionality for agents that don't require parent-child relationships.
/// </summary>
[GenerateSerializer]
public abstract class CoreStateBase
{
    [Id(0)] public GrainId GrainId { get; private set; }
    /// <summary>
    /// Applies a state log event to update the state.
    /// This method can be overridden by derived classes to handle specific state transitions.
    /// </summary>
    /// <param name="stateLogEvent">The state log event to apply</param>
    public virtual void Apply(StateLogEventBase stateLogEvent)
    {
        // Base implementation - can be overridden by derived classes
    }
} 