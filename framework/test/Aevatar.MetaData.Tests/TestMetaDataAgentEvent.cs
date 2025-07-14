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
    [Id(2)]
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional test message for verification.
    /// </summary>
    [Id(3)]
    public string TestMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// Override base class Id to avoid Orleans serialization conflicts
    /// </summary>
    [Id(0)]
    public override Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Override base class Ctime to avoid Orleans serialization conflicts
    /// </summary>
    [Id(1)]
    public new DateTime Ctime { get; set; } = DateTime.UtcNow;
}