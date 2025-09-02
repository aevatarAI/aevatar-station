using System.ComponentModel;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.AIGAgent.Test.Mocks;

// Test state for executor tests
[GenerateSerializer]
public class MockExecutorGAgentState : StateBase
{
    [Id(0)] public string LastProcessedEvent { get; set; } = string.Empty;
    [Id(1)] public int EventCount { get; set; }
}

// Test state log event
[GenerateSerializer]
public class MockExecutorGAgentStateLogEvent : StateLogEventBase<MockExecutorGAgentStateLogEvent>;

// Test event
[GenerateSerializer]
[Description("Test event for mock executor GAgent")]
public class MockExecutorTestEvent : EventBase
{
    [Id(0)] public string Message { get; set; } = string.Empty;
}

// Test response event
[GenerateSerializer]
[Description("Response event from mock executor GAgent")]
public class MockExecutorTestResponseEvent : EventBase
{
    [Id(0)] public string Result { get; set; } = string.Empty;
}

// Mock GAgent interface
public interface IMockExecutorGAgent : IStateGAgent<MockExecutorGAgentState>;

// Mock GAgent implementation
[GAgent("mock_executor", "test")]
public class MockExecutorGAgent : GAgentBase<MockExecutorGAgentState, MockExecutorGAgentStateLogEvent>,
    IMockExecutorGAgent
{
    private readonly ILogger<MockExecutorGAgent> _logger;

    public MockExecutorGAgent(ILogger<MockExecutorGAgent> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Mock GAgent for testing GAgentExecutor");
    }

    [EventHandler]
    public async Task HandleMockExecutorTestEvent(MockExecutorTestEvent @event)
    {
        _logger.LogInformation("Handling MockExecutorTestEvent with message: {Message}", @event.Message);

        // Update state
        RaiseEvent(new MockExecutorGAgentStateLogEvent());
        State.LastProcessedEvent = @event.Message;
        State.EventCount++;
        await ConfirmEvents();

        // Return a response
        await PublishAsync(new MockExecutorTestResponseEvent
        {
            Result = $"Processed: {@event.Message} - Count: {State.EventCount}"
        });
    }

    [EventHandler]
    public async Task HandleMockExecutorTimeoutEvent(MockExecutorTimeoutEvent @event)
    {
        _logger.LogInformation("Handling MockExecutorTimeoutEvent - will not send response");

        // Update state but don't send response (to test timeout)
        RaiseEvent(new MockExecutorGAgentStateLogEvent());
        State.LastProcessedEvent = "Timeout Event";
        State.EventCount++;
        await ConfirmEvents();

        // Intentionally not publishing any response event
    }

    protected override void GAgentTransitionState(MockExecutorGAgentState state,
        StateLogEventBase<MockExecutorGAgentStateLogEvent> @event)
    {
        // State transitions are handled directly in the event handlers for this simple case
    }
}

// Event that won''t generate a response (for timeout testing)
// Moved from GAgentExecutorTests.cs
[GenerateSerializer]
[Description("Event that causes timeout for testing")]
public class MockExecutorTimeoutEvent : EventBase
{
    [Id(0)] public string Data { get; set; } = string.Empty;
}