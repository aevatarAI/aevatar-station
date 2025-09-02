using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Test.GAgents;
using Aevatar.GAgents.Executor;
using Orleans.Streams;
using Shouldly;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

public sealed class GAgentExecutorIntegrationTests : AevatarAIGAgentBaseTestBase
{
    private readonly IGAgentExecutor _executor;

    public GAgentExecutorIntegrationTests()
    {
        _executor = GetRequiredService<IGAgentExecutor>();
        // Register test GAgents
        RegisterTestGAgents();
    }

    private void RegisterTestGAgents()
    {
        // GAgents are automatically registered via [GAgent] attribute in Orleans
        // No manual registration needed
    }

    //[Fact]
    public async Task FullExecutionFlow_ShouldCompleteSuccessfully()
    {
        // Arrange
        var mockGAgent = await GAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var testEvent = new MockExecutorTestEvent { Message = "Integration Test Message" };

        // Act
        var result = await _executor.ExecuteGAgentEventHandler(mockGAgent, testEvent);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: Integration Test Message");

        // Verify GAgent state was updated
        var state = await mockGAgent.GetStateAsync();
        state.LastProcessedEvent.ShouldBe("Integration Test Message");
        state.EventCount.ShouldBeGreaterThan(0);
    }

    //[Fact]
    public async Task ExecutionFlow_VerifyResultGAgentSubscribesToTargetDirectly()
    {
        // This test verifies the fix where ResultGAgent subscribes directly to targetGAgent
        // instead of going through PublishingGAgent

        // Arrange
        var targetGAgent = await GAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var testEvent = new MockExecutorTestEvent { Message = "Direct Subscription Test" };

        // Act
        var result = await _executor.ExecuteGAgentEventHandler(targetGAgent, testEvent);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: Direct Subscription Test");

        // The result should come directly from the targetGAgent''s response event
        // not from an intermediate PublishingGAgent
    }

    //[Fact]
    public async Task ConcurrentExecutions_ShouldHandleIndependently()
    {
        // Arrange
        var tasks = new List<Task<string>>();

        // Act - Execute multiple events concurrently on different GAgent instances
        for (var i = 0; i < 10; i++)
        {
            var mockExecutorGAgent = await GAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
            var grainId = mockExecutorGAgent.GetGrainId();
            var testEvent = new MockExecutorTestEvent { Message = $"Concurrent Test {i}" };
            tasks.Add(_executor.ExecuteGAgentEventHandler(grainId, testEvent));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Length.ShouldBe(10);
        for (int i = 0; i < 10; i++)
        {
            results[i].ShouldNotBeNullOrEmpty();
            results[i].ShouldContain($"Concurrent Test {i}");
        }
    }

    //[Fact]
    public async Task ExecutionWithTimeout_ShouldRespectConfiguredTimeout()
    {
        // Arrange
        var mockGAgent = await GAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var timeoutEvent = new MockExecutorTimeoutEvent { Data = "This will timeout" };

        var startTime = DateTime.UtcNow;

        // Act & Assert
        await Should.ThrowAsync<TimeoutException>(async () =>
        {
            await _executor.ExecuteGAgentEventHandler(mockGAgent, timeoutEvent);
        });

        var elapsedTime = DateTime.UtcNow - startTime;
        elapsedTime.ShouldBeLessThan(AevatarGAgentExecutorConstants.GAgentExecutorTimeout.Add(TimeSpan.FromSeconds(1)));
    }

    //[Fact]
    public async Task StreamCommunication_ShouldWorkCorrectly()
    {
        // Arrange
        var executionId = Guid.NewGuid().ToString();
        var streamProvider = Cluster.Client.GetStreamProvider(AevatarCoreConstants.StreamProvider);
        var stream = streamProvider.GetStream<ExecutionCompletedEvent>(
            AevatarGAgentExecutorConstants.GAgentExecutorStreamNamespace, executionId);

        var receivedEvents = new List<ExecutionCompletedEvent>();
        var subscription = await stream.SubscribeAsync((evt, token) =>
        {
            receivedEvents.Add(evt);
            return Task.CompletedTask;
        });

        try
        {
            // Act - Publish an event to the stream
            await stream.OnNextAsync(new ExecutionCompletedEvent
            {
                ExecutionId = executionId,
                Result = "Test Stream Result"
            });

            // Give some time for the event to be processed
            await Task.Delay(100);

            // Assert
            receivedEvents.Count.ShouldBe(1);
            receivedEvents[0].ExecutionId.ShouldBe(executionId);
            receivedEvents[0].Result.ShouldBe("Test Stream Result");
        }
        finally
        {
            await subscription.UnsubscribeAsync();
        }
    }

    //[Fact]
    public async Task MultipleGAgentTypes_ShouldExecuteCorrectHandlers()
    {
        // Arrange
        var executorGAgent = await GAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var testEvent = new MockExecutorTestEvent { Message = "Multi-Type Test" };

        // Register multiple GAgent types that can handle the same event
        // (This tests that the executor correctly routes to the intended GAgent)

        // Act
        var result = await _executor.ExecuteGAgentEventHandler(executorGAgent, testEvent);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: Multi-Type Test");

        // Verify the correct GAgent handled the event
        var state = await executorGAgent.GetStateAsync();
        state.LastProcessedEvent.ShouldBe("Multi-Type Test");
    }
}