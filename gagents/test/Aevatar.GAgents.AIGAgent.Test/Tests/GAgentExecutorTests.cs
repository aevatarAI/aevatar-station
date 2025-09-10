using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Test.Mocks;
using Aevatar.GAgents.Executor;
using Shouldly;
using System.Text.Json;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

public sealed class GAgentExecutorTests : AevatarAIGAgentTestBase
{
    private readonly IGAgentExecutor _executor;
    private readonly IGAgentFactory _gAgentFactory;

    public GAgentExecutorTests()
    {
        _executor = GetRequiredService<IGAgentExecutor>();
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    //[Fact]
    public async Task ExecuteGAgentEventHandler_WithIGAgent_ShouldExecuteSuccessfully()
    {
        // Arrange
        var mockGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var testEvent = new MockExecutorTestEvent { Message = "Test Message 1" };

        // Act
        var result = await _executor.ExecuteGAgentEventHandler(mockGAgent, testEvent);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: Test Message 1");
        result.ShouldContain("Count: 1");
    }

    //[Fact]
    public async Task ExecuteGAgentEventHandler_WithGrainId_ShouldExecuteSuccessfully()
    {
        // Arrange
        var mockExecutorGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var grainId = mockExecutorGAgent.GetGrainId();
        var testEvent = new MockExecutorTestEvent { Message = "Test Message 2" };

        // Act
        var result = await _executor.ExecuteGAgentEventHandler(grainId, testEvent);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: Test Message 2");
    }

    //[Fact]
    public async Task ExecuteGAgentEventHandler_WithGrainType_ShouldExecuteSuccessfully()
    {
        // Arrange
        var mockExecutorGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var grainId = mockExecutorGAgent.GetGrainId();
        var grainType = grainId.Type;
        var testEvent = new MockExecutorTestEvent { Message = "Test Message 3" };

        // Act
        var result = await _executor.ExecuteGAgentEventHandler(grainType, testEvent);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: Test Message 3");
    }

    [Fact(Skip = "Wait to long.")]
    public async Task ExecuteGAgentEventHandler_ShouldThrowTimeoutException_WhenNoResponseReceived()
    {
        // Arrange
        var mockGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var testEvent = new MockExecutorTimeoutEvent(); // Event that won't generate a response

        // Act & Assert
        await Should.ThrowAsync<TimeoutException>(async () =>
        {
            await _executor.ExecuteGAgentEventHandler(mockGAgent, testEvent);
        });
    }

    //[Fact]
    public async Task ExecuteGAgentEventHandler_ShouldHandleMultipleConcurrentExecutions()
    {
        // Arrange
        var tasks = new List<Task<string>>();

        // Act
        for (var i = 0; i < 3; i++)
        {
            var mockGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
            var testEvent = new MockExecutorTestEvent { Message = $"Concurrent Message {i}" };
            tasks.Add(_executor.ExecuteGAgentEventHandler(mockGAgent, testEvent));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Length.ShouldBe(3);
        for (var i = 0; i < 3; i++)
        {
            results[i].ShouldNotBeNullOrEmpty();
            results[i].ShouldContain("Processed:");
        }
    }

    //[Fact]
    public async Task ExecuteGAgentEventHandler_WithFixedExecutionFlow_ShouldSubscribeCorrectly()
    {
        // Arrange
        var targetGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var testEvent = new MockExecutorTestEvent { Message = "Fixed Flow Test" };

        // Act
        var result = await _executor.ExecuteGAgentEventHandler(targetGAgent, testEvent);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: Fixed Flow Test");

        // Verify that the ResultGAgent subscribed directly to targetGAgent
        // (not through PublishingGAgent)
    }

    //[Fact]
    public async Task ExecuteGAgentEventHandler_WithExpectedResultType_ShouldFilterResults()
    {
        // Arrange
        var mockGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var testEvent = new MockExecutorTestEvent { Message = "Expected Result Type Test" };

        // Act
        var result =
            await _executor.ExecuteGAgentEventHandler(mockGAgent, testEvent, typeof(MockExecutorTestResponseEvent));

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: Expected Result Type Test");
    }

    //[Fact]
    public async Task ExecuteGAgentEventHandler_WithGrainIdAndExpectedResultType_ShouldExecuteSuccessfully()
    {
        // Arrange
        var mockExecutorGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var grainId = mockExecutorGAgent.GetGrainId();
        var testEvent = new MockExecutorTestEvent { Message = "GrainId Expected Result Test" };

        // Act
        var result =
            await _executor.ExecuteGAgentEventHandler(grainId, testEvent, typeof(MockExecutorTestResponseEvent));

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: GrainId Expected Result Test");
    }

    //[Fact]
    public async Task ExecuteGAgentEventHandler_WithGrainTypeAndExpectedResultType_ShouldExecuteSuccessfully()
    {
        // Arrange
        var mockExecutorGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var grainId = mockExecutorGAgent.GetGrainId();
        var grainType = grainId.Type;
        var testEvent = new MockExecutorTestEvent { Message = "GrainType Expected Result Test" };

        // Act
        var result =
            await _executor.ExecuteGAgentEventHandler(grainType, testEvent, typeof(MockExecutorTestResponseEvent));

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: GrainType Expected Result Test");
    }

    //[Fact]
    public async Task ExecuteGAgentEventHandler_WithEventTypeNameAndJson_ShouldExecuteSuccessfully()
    {
        // Arrange
        var mockGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var testEvent = new MockExecutorTestEvent { Message = "Dynamic Event Test" };
        var eventJson = JsonSerializer.Serialize(testEvent);

        // Act
        var result = await _executor.ExecuteGAgentEventHandler(mockGAgent, "MockExecutorTestEvent", eventJson);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: Dynamic Event Test");
    }

    //[Fact]
    public async Task ExecuteGAgentEventHandler_WithGrainIdEventTypeNameAndJson_ShouldExecuteSuccessfully()
    {
        // Arrange
        var mockExecutorGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var grainId = mockExecutorGAgent.GetGrainId();
        var testEvent = new MockExecutorTestEvent { Message = "GrainId Dynamic Event Test" };
        var eventJson = JsonSerializer.Serialize(testEvent);

        // Act
        var result = await _executor.ExecuteGAgentEventHandler(grainId, "MockExecutorTestEvent", eventJson);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: GrainId Dynamic Event Test");
    }

    //[Fact]
    public async Task ExecuteGAgentEventHandler_WithGrainTypeEventTypeNameAndJson_ShouldExecuteSuccessfully()
    {
        // Arrange
        var mockExecutorGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var grainId = mockExecutorGAgent.GetGrainId();
        var grainType = grainId.Type;
        var testEvent = new MockExecutorTestEvent { Message = "GrainType Dynamic Event Test" };
        var eventJson = JsonSerializer.Serialize(testEvent);

        // Act
        var result = await _executor.ExecuteGAgentEventHandler(grainType, "MockExecutorTestEvent", eventJson);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: GrainType Dynamic Event Test");
    }

    //[Fact]
    public async Task ExecuteGAgentEventHandler_WithEventTypeNameJsonAndExpectedResultType_ShouldExecuteSuccessfully()
    {
        // Arrange
        var mockGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var testEvent = new MockExecutorTestEvent { Message = "Dynamic Event with Expected Result Test" };
        var eventJson = JsonSerializer.Serialize(testEvent);

        // Act
        var result = await _executor.ExecuteGAgentEventHandler(mockGAgent, "MockExecutorTestEvent", eventJson,
            typeof(MockExecutorTestResponseEvent));

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: Dynamic Event with Expected Result Test");
    }

    //[Fact]
    public async Task
        ExecuteGAgentEventHandler_WithGrainIdEventTypeNameJsonAndExpectedResultType_ShouldExecuteSuccessfully()
    {
        // Arrange
        var mockExecutorGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var grainId = mockExecutorGAgent.GetGrainId();
        var testEvent = new MockExecutorTestEvent { Message = "GrainId Dynamic Event with Expected Result Test" };
        var eventJson = JsonSerializer.Serialize(testEvent);

        // Act
        var result = await _executor.ExecuteGAgentEventHandler(grainId, "MockExecutorTestEvent", eventJson,
            typeof(MockExecutorTestResponseEvent));

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: GrainId Dynamic Event with Expected Result Test");
    }

    //[Fact]
    public async Task
        ExecuteGAgentEventHandler_WithGrainTypeEventTypeNameJsonAndExpectedResultType_ShouldExecuteSuccessfully()
    {
        // Arrange
        var mockExecutorGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var grainId = mockExecutorGAgent.GetGrainId();
        var grainType = grainId.Type;
        var testEvent = new MockExecutorTestEvent { Message = "GrainType Dynamic Event with Expected Result Test" };
        var eventJson = JsonSerializer.Serialize(testEvent);

        // Act
        var result = await _executor.ExecuteGAgentEventHandler(grainType, "MockExecutorTestEvent", eventJson,
            typeof(MockExecutorTestResponseEvent));

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: GrainType Dynamic Event with Expected Result Test");
    }

    // Error handling test cases
    //[Fact]
    public async Task ExecuteGAgentEventHandler_WithInvalidEventTypeName_ShouldThrowException()
    {
        // Arrange
        var mockGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var eventJson = "{\"Message\":\"Test\"}";

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _executor.ExecuteGAgentEventHandler(mockGAgent, "NonExistentEventType", eventJson);
        });
    }

    //[Fact]
    public async Task ExecuteGAgentEventHandler_WithInvalidEventJson_ShouldThrowException()
    {
        // Arrange
        var mockGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var invalidJson = "{invalid json}";

        // Act & Assert
        await Should.ThrowAsync<JsonException>(async () =>
        {
            await _executor.ExecuteGAgentEventHandler(mockGAgent, "MockExecutorTestEvent", invalidJson);
        });
    }

    //[Fact]
    public async Task ExecuteGAgentEventHandler_WithInvalidGrainType_ShouldThrowException()
    {
        // Arrange
        var invalidGrainType = GrainType.Create("NonExistentGAgent");
        var testEvent = new MockExecutorTestEvent { Message = "Test" };
        var eventJson = JsonSerializer.Serialize(testEvent);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _executor.ExecuteGAgentEventHandler(invalidGrainType, "MockExecutorTestEvent", eventJson);
        });
    }

    //[Fact]
    public async Task ExecuteGAgentEventHandler_WithCaseInsensitiveEventTypeName_ShouldExecuteSuccessfully()
    {
        // Arrange
        var mockGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var testEvent = new MockExecutorTestEvent { Message = "Case Insensitive Test" };
        var eventJson = JsonSerializer.Serialize(testEvent);

        // Act - Test with lowercase event type name
        var result = await _executor.ExecuteGAgentEventHandler(mockGAgent, "mockexecutortestevent", eventJson);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: Case Insensitive Test");
    }

    //[Fact]
    public async Task ExecuteGAgentEventHandler_WithFullEventTypeName_ShouldExecuteSuccessfully()
    {
        // Arrange
        var mockGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var testEvent = new MockExecutorTestEvent { Message = "Full Type Name Test" };
        var eventJson = JsonSerializer.Serialize(testEvent);
        var fullEventTypeName = typeof(MockExecutorTestEvent).FullName!;

        // Act
        var result = await _executor.ExecuteGAgentEventHandler(mockGAgent, fullEventTypeName, eventJson);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: Full Type Name Test");
    }

    //[Fact]
    public async Task ExecuteGAgentEventHandler_WithTimeoutEventAndExpectedResultType_ShouldThrowTimeoutException()
    {
        // Arrange
        var mockGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var timeoutEvent = new MockExecutorTimeoutEvent { Data = "Timeout Test" };
        var eventJson = JsonSerializer.Serialize(timeoutEvent);

        // Act & Assert
        await Should.ThrowAsync<TimeoutException>(async () =>
        {
            await _executor.ExecuteGAgentEventHandler(mockGAgent, "MockExecutorTimeoutEvent", eventJson,
                typeof(MockExecutorTestResponseEvent));
        });
    }
    
    //[Fact]
    public async Task TestEventHandlerExecutorGAgent()
    {
        var executorGAgent = await _gAgentFactory.GetGAgentAsync<IEventHandlerExecutorGAgent>();
        // Arrange
        var mockExecutorGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var grainId = mockExecutorGAgent.GetGrainId();
        var grainType = grainId.Type;
        var testEvent = new MockExecutorTestEvent { Message = "Test Message 3" };

        // Act
        var result = await executorGAgent.ExecuteGAgentEventHandler(grainType, testEvent);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Processed: Test Message 3");
    }
}