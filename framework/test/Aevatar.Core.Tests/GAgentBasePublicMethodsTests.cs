// ABOUTME: This file tests the new public RaiseEvent and ConfirmEvents methods in GAgentBase
// ABOUTME: Validates that the public wrapper methods work correctly and maintain Orleans compatibility

using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestGAgents;
using Aevatar.Core.Tests.TestStateLogEvents;
using Orleans;
using Shouldly;

namespace Aevatar.Core.Tests;

/// <summary>
/// Unit tests for the new public methods added to GAgentBase:
/// - public void RaiseEvent(StateLogEventBase&lt;TStateLogEvent&gt; @event)
/// - public new Task ConfirmEvents()
/// These methods are designed to be used by helper interfaces like IMetaDataStateGAgent
/// to interact with the Orleans event sourcing system.
/// </summary>
public sealed class GAgentBasePublicMethodsTests : GAgentTestKitBase
{
    [Fact(DisplayName = "Public RaiseEvent method should raise event successfully")]
    public async Task PublicRaiseEvent_ShouldRaiseEventSuccessfully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = await Silo.CreateGrainAsync<EventHandlerTestGAgent>(agentId);
        
        // Get initial state count
        var initialState = await testAgent.GetStateAsync();
        var initialContentCount = initialState.Content?.Count ?? 0;
        
        var testEvent = new EventHandlerTestStateLogEvent();

        // Act - Use the public RaiseEvent method
        await testAgent.TestPublicRaiseEvent(testEvent);

        // Assert - Verify the event was processed through event sourcing (not event handlers)
        // The public RaiseEvent method should work without throwing exceptions
        // and the state should remain consistent (event sourcing events don't trigger [EventHandler] methods)
        var updatedState = await testAgent.GetStateAsync();
        updatedState.ShouldNotBeNull("State should be accessible after raising event");
        updatedState.Content.Count.ShouldBe(initialContentCount, "Content should not change as state log events don't trigger event handlers");
    }

    [Fact(DisplayName = "Public RaiseEvent followed by ConfirmEvents should work together")]
    public async Task PublicRaiseEventAndConfirm_ShouldWorkTogether()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = await Silo.CreateGrainAsync<EventHandlerTestGAgent>(agentId);
        var initialState = await testAgent.GetStateAsync();
        var initialContentCount = initialState.Content?.Count ?? 0;
        
        var testEvent = new EventHandlerTestStateLogEvent();

        // Act - Use the public RaiseEvent and ConfirmEvents methods together
        await testAgent.TestPublicRaiseEventAndConfirm(testEvent);

        // Assert - Verify both methods worked correctly without throwing exceptions
        // State log events are processed through Orleans event sourcing, not event handlers
        var state = await testAgent.GetStateAsync();
        state.ShouldNotBeNull("State should be accessible after raising and confirming events");
        state.Content.Count.ShouldBe(initialContentCount, "Content should not change as state log events don't trigger event handlers");
    }

    [Fact(DisplayName = "Public RaiseEvent should handle null gracefully")]
    public async Task PublicRaiseEvent_WithNull_ShouldHandleGracefully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = await Silo.CreateGrainAsync<EventHandlerTestGAgent>(agentId);
        var initialState = await testAgent.GetStateAsync();
        var initialMessageCount = initialState.Messages?.Count ?? 0;

        // Act - Should handle null without throwing unexpected exceptions
        await testAgent.TestPublicRaiseEventWithNull();
        
        // Assert - Verify no state changes occurred with null event
        var finalState = await testAgent.GetStateAsync();
        var finalMessageCount = finalState.Messages?.Count ?? 0;
        finalMessageCount.ShouldBe(initialMessageCount, "Message count should not change when null event is handled");
    }

    [Fact(DisplayName = "Public RaiseEvent should handle multiple events in sequence")]
    public async Task PublicRaiseEvent_MultipleEvents_ShouldHandleSequentially()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = await Silo.CreateGrainAsync<EventHandlerTestGAgent>(agentId);
        var initialState = await testAgent.GetStateAsync();
        var initialContentCount = initialState.Content?.Count ?? 0;
        
        var events = new[]
        {
            new EventHandlerTestStateLogEvent(),
            new EventHandlerTestStateLogEvent(),
            new EventHandlerTestStateLogEvent()
        };

        // Act - Raise multiple events using the public method
        await testAgent.TestPublicRaiseMultipleEvents(events);

        // Assert - Verify all events were processed without exceptions
        // State log events are processed through Orleans event sourcing, not event handlers
        var state = await testAgent.GetStateAsync();
        state.ShouldNotBeNull("State should be accessible after raising multiple events");
        state.Content.Count.ShouldBe(initialContentCount, "Content should not change as state log events don't trigger event handlers");
    }

    [Fact(DisplayName = "Public methods should allow state modifications when used properly")]
    public async Task PublicMethods_ShouldAllowStateModifications()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = await Silo.CreateGrainAsync<EventHandlerTestGAgent>(agentId);

        // Act - Use a test method that directly modifies state and uses public methods
        await testAgent.TestDirectStateModification("Test Content");

        // Assert
        var state = await testAgent.GetStateAsync();
        state.Content.ShouldContain("Test Content");
    }

    [Fact(DisplayName = "Public ConfirmEvents method should complete without error")]
    public async Task PublicConfirmEvents_ShouldCompleteWithoutError()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = await Silo.CreateGrainAsync<EventHandlerTestGAgent>(agentId);
        var initialState = await testAgent.GetStateAsync();
        var initialMessageCount = initialState.Messages?.Count ?? 0;

        // Act - Public ConfirmEvents should complete successfully
        await testAgent.TestPublicConfirmEventsOnly();
        
        // Assert - Verify ConfirmEvents completed successfully without changing state (no events to confirm)
        var finalState = await testAgent.GetStateAsync();
        var finalMessageCount = finalState.Messages?.Count ?? 0;
        finalMessageCount.ShouldBe(initialMessageCount, "Message count should not change when there are no events to confirm");
    }

    [Fact(DisplayName = "Public ConfirmEvents should return completed Task")]
    public async Task PublicConfirmEvents_ShouldReturnCompletedTask()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = await Silo.CreateGrainAsync<EventHandlerTestGAgent>(agentId);

        // Act
        var confirmTask = await testAgent.TestPublicConfirmEventsReturnValue();

        // Assert
        confirmTask.ShouldNotBeNull();
        await confirmTask; // Should complete without error
    }
}