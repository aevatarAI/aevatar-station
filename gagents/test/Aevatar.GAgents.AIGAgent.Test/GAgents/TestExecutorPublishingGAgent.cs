using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.PublishGAgent;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents;

[GenerateSerializer]
public class TestExecutorPublishingGAgentState : StateBase
{
    [Id(0)] public int PublishedEventCount { get; set; }
}

[GenerateSerializer]
public class TestExecutorPublishingGAgentStateLogEvent : StateLogEventBase<TestExecutorPublishingGAgentStateLogEvent>;

[GAgent("executor_publishing", "test")]
public class TestExecutorPublishingGAgent : GAgentBase<TestExecutorPublishingGAgentState, TestExecutorPublishingGAgentStateLogEvent>, Aevatar.GAgents.Basic.PublishGAgent.IPublishingGAgent
{
    private readonly ILogger<TestExecutorPublishingGAgent> _logger;
    
    public TestExecutorPublishingGAgent(ILogger<TestExecutorPublishingGAgent> logger)
    {
        _logger = logger;
    }
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Test PublishingGAgent for forwarding events");
    }
    
    public async Task PublishEventAsync<T>(T @event) where T : EventBase
    {
        _logger.LogInformation("Publishing event of type: {EventType}", @event.GetType().Name);
        
        // Update state
        RaiseEvent(new TestExecutorPublishingGAgentStateLogEvent());
        State.PublishedEventCount++;
        await ConfirmEvents();
        
        // Simply publish the event
        await PublishAsync(@event);
    }
    
    protected override void GAgentTransitionState(TestExecutorPublishingGAgentState state, StateLogEventBase<TestExecutorPublishingGAgentStateLogEvent> @event)
    {
        // State transitions are handled directly in the event handlers for this simple case
    }
}
