// ABOUTME: This file defines the interface for Orleans-based TestMetaDataAgent grain
// ABOUTME: Used for integration testing with Orleans cluster and event sourcing

using Aevatar.Core.Abstractions;
using Aevatar.MetaData;
using Aevatar.MetaData.Enums;

namespace Aevatar.MetaData.Tests;

/// <summary>
/// Orleans grain interface for TestMetaDataAgent.
/// Provides Orleans-compatible testing interface for metadata operations.
/// </summary>
public interface ITestMetaDataAgent : IGAgent
{
    /// <summary>
    /// Handles test events for Orleans integration testing.
    /// </summary>
    /// <param name="event">The test event to handle.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleTestEventAsync(TestMetaDataAgentEvent @event);
    
    /// <summary>
    /// Gets the current test event count for verification.
    /// </summary>
    /// <returns>The current test event count.</returns>
    Task<int> GetTestEventCountAsync();
    
    /// <summary>
    /// Gets the test messages for verification.
    /// </summary>
    /// <returns>The list of test messages.</returns>
    Task<List<string>> GetTestMessagesAsync();
    
    /// <summary>
    /// Clears test data for cleanup.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearTestDataAsync();
    
    /// <summary>
    /// Gets the current state asynchronously for Orleans compatibility.
    /// </summary>
    /// <returns>The current state.</returns>
    Task<TestMetaDataAgentState> GetStateAsync();
    
}