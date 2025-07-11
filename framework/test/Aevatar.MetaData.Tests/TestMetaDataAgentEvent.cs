// ABOUTME: This file defines the state log event class for Orleans event sourcing integration testing
// ABOUTME: Used by TestMetaDataAgent to track state changes through Orleans event sourcing

using Aevatar.Core.Abstractions;

namespace Aevatar.MetaData.Tests;

/// <summary>
/// Test state log event for Orleans integration testing with IMetaDataStateGAgent.
/// Used to track state changes in the TestMetaDataAgent.
/// </summary>
[GenerateSerializer]
public class TestMetaDataAgentEvent : StateLogEventBase<TestMetaDataAgentEvent>
{
    /// <summary>
    /// The action that occurred during this event.
    /// </summary>
    [Id(0)]
    public virtual string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional test message for verification.
    /// </summary>
    [Id(1)]
    public virtual string TestMessage { get; set; } = string.Empty;
}