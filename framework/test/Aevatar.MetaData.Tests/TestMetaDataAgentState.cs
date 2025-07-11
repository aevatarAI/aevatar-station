// ABOUTME: This file defines the state class for integration testing with Orleans event sourcing
// ABOUTME: Extends MetaDataStateBase to provide Orleans-compatible state with metadata capabilities

using Aevatar.Core.Abstractions;
using Aevatar.MetaData;

namespace Aevatar.MetaData.Tests;

/// <summary>
/// Test state class for Orleans integration testing with IMetaDataStateEventRaiser.
/// Extends MetaDataStateBase to provide full metadata state functionality.
/// </summary>
[GenerateSerializer]
public class TestMetaDataAgentState : MetaDataStateBase
{
    /// <summary>
    /// Additional test-specific property for verifying Orleans event sourcing.
    /// </summary>
    [Id(100)]
    public virtual int TestEventCount { get; set; }
    
    /// <summary>
    /// Test messages for verifying event processing.
    /// </summary>
    [Id(101)]
    public virtual List<string> TestMessages { get; set; } = new();
}