using Aevatar.Core.Abstractions;

namespace Aevatar.Core.Tests.TestGAgents;

public interface IStateTrackingTestGAgent : IStateGAgent<StateTrackingTestGAgentState>
{
    Task UpdateTestDataAsync(string data);
    Task<int> GetCurrentVersionAsync();
}

[GenerateSerializer]
public class StateTrackingTestGAgentState : StateBase
{
    [Id(0)] public int HandleStateChangedCallCount { get; set; }
    [Id(1)] public List<int> ProcessedVersions { get; set; } = [];
    [Id(2)] public DateTime LastStateChangeTime { get; set; }
    [Id(3)] public string TestData { get; set; } = string.Empty;
}

[GenerateSerializer]
public class StateTrackingTestStateLogEvent : StateLogEventBase<StateTrackingTestStateLogEvent>
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string Data { get; set; } = string.Empty;
}

[GAgent("stateTrackingTest")]
public class StateTrackingTestGAgent : GAgentBase<StateTrackingTestGAgentState, StateTrackingTestStateLogEvent>, IStateTrackingTestGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Test GAgent for tracking state changes and version filtering");
    }

    protected override async Task HandleStateChangedAsync()
    {
        await base.HandleStateChangedAsync();
        
        // Track the call
        State.HandleStateChangedCallCount++;
        State.ProcessedVersions.Add(Version);
        State.LastStateChangeTime = DateTime.UtcNow;
    }

    // Method to trigger a state change for testing
    public async Task UpdateTestDataAsync(string data)
    {
        var stateLogEvent = new StateTrackingTestStateLogEvent
        {
            Id = Guid.NewGuid(),
            Data = data
        };
        
        RaiseEvent(stateLogEvent);
        await ConfirmEvents();
    }

    // Override GAgentTransitionState to handle the state change properly
    protected override void GAgentTransitionState(StateTrackingTestGAgentState state, StateLogEventBase<StateTrackingTestStateLogEvent> @event)
    {
        if (@event is StateTrackingTestStateLogEvent stateLogEvent)
        {
            state.TestData = stateLogEvent.Data;
        }
        base.GAgentTransitionState(state, @event);
    }

    // Method to get current version for testing
    public Task<int> GetCurrentVersionAsync()
    {
        return Task.FromResult(Version);
    }
} 