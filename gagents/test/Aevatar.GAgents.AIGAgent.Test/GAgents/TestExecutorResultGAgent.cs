using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Test.Mocks;
using Aevatar.GAgents.Executor;
using Microsoft.Extensions.Logging;
using Orleans.Streams;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents;

[GAgent("executor_result", "test")]
public class TestExecutorResultGAgent : GAgentBase<ResultGAgentState, ResultGAgentStateLogEvent>, IResultGAgent
{
    private readonly ILogger<TestExecutorResultGAgent> _logger;

    public TestExecutorResultGAgent(ILogger<TestExecutorResultGAgent> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Test ResultGAgent for collecting execution results");
    }

    public async Task SetExecutionContextAsync(string executionId, string streamProvider, string streamNamespace)
    {
        _logger.LogInformation("Setting execution context: {ExecutionId}", executionId);

        RaiseEvent(new SetExecutionContextGAgentStateLogEvent
        {
            ExecutionId = executionId,
            StreamProvider = streamProvider,
            StreamNamespace = streamNamespace
        });

        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleMockExecutorTestResponseEvent(MockExecutorTestResponseEvent @event)
    {
        _logger.LogInformation("Received response: {Result}", @event.Result);

        // Update state
        RaiseEvent(new ResultArrivedStateLogEvent
        {
            Result = @event.Result
        });

        await ConfirmEvents();

        // Publish result to stream
        if (!string.IsNullOrEmpty(State.ExecutionId) &&
            !string.IsNullOrEmpty(State.StreamProvider) &&
            !string.IsNullOrEmpty(State.StreamNamespace))
        {
            var clusterClient = this.GetStreamProvider(State.StreamProvider).GetStream<ExecutionCompletedEvent>(
                State.StreamNamespace, State.ExecutionId);

            await clusterClient.OnNextAsync(new ExecutionCompletedEvent
            {
                ExecutionId = State.ExecutionId,
                Result = @event.Result
            });
        }
    }

    protected override void GAgentTransitionState(ResultGAgentState state,
        StateLogEventBase<ResultGAgentStateLogEvent> @event)
    {
        // State transitions are handled directly in the event handlers for this simple case
    }
}