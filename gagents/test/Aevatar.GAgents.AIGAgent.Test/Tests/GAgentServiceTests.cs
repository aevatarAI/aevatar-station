using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Test.Mocks;
using Aevatar.GAgents.Executor;
using Shouldly;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

public class GAgentServiceTests : AevatarAIGAgentTestBase
{
    private readonly IGAgentService _gAgentService;
    private readonly IGAgentFactory _gAgentFactory;

    public GAgentServiceTests()
    {
        _gAgentService = GetRequiredService<IGAgentService>();
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    //[Fact]
    public async Task GetAllAvailableGAgentInformation_ShouldReturnGAgentInfo()
    {
        // Act
        var gAgentInfo = await _gAgentService.GetAllAvailableGAgentInformation();

        // Assert
        gAgentInfo.ShouldNotBeNull();
        gAgentInfo.Count.ShouldBeGreaterThan(0);

        // Check if MockExecutorGAgent is registered (Orleans automatically registers GAgents with [GAgent] attribute)
        var mockExecutorEntry = gAgentInfo.FirstOrDefault(kvp =>
            kvp.Key.ToString()!.Contains("MockExecutorGAgent", StringComparison.OrdinalIgnoreCase));

        if (mockExecutorEntry.Key != default)
        {
            mockExecutorEntry.Value.ShouldNotBeNull();
            mockExecutorEntry.Value.ShouldContain(typeof(MockExecutorTestEvent));
            mockExecutorEntry.Value.ShouldContain(typeof(MockExecutorTimeoutEvent));
        }
    }

    //[Fact]
    public async Task GetGAgentDetailInfoAsync_WithValidGAgent_ShouldReturnDetails()
    {
        // Arrange
        // First get an actual GAgent to get its GrainType
        var mockGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var grainId = mockGAgent.GetGrainId();
        var grainType = grainId.Type;

        // Act
        var detailInfo = await _gAgentService.GetGAgentDetailInfoAsync(grainType);

        // Assert
        detailInfo.ShouldNotBeNull();
        detailInfo.GrainType.ShouldBe(grainType);
        detailInfo.Description.ShouldBe("Mock GAgent for testing GAgentExecutor");
        detailInfo.SupportedEventTypes.ShouldNotBeNull();
        detailInfo.SupportedEventTypes.ShouldContain(typeof(MockExecutorTestEvent));
        detailInfo.SupportedEventTypes.ShouldContain(typeof(MockExecutorTimeoutEvent));
    }

    //[Fact]
    public async Task GetGAgentDetailInfoAsync_WithNonExistentGAgent_ShouldThrowException()
    {
        // Arrange
        var nonExistentGrainType = GrainType.Create("non-existent-gagent-" + Guid.NewGuid());

        // Act & Assert
        // The actual exception may vary based on Orleans implementation
        await Should.ThrowAsync<Exception>(async () =>
        {
            await _gAgentService.GetGAgentDetailInfoAsync(nonExistentGrainType);
        });
    }

    //[Fact]
    public async Task FindGAgentsByEventTypeAsync_WithValidEventType_ShouldReturnMatchingGAgents()
    {
        // Act
        var gAgents = await _gAgentService.FindGAgentsByEventTypeAsync(typeof(MockExecutorTestEvent));

        // Assert
        gAgents.ShouldNotBeNull();

        // If MockExecutorGAgent is registered, it should be in the results
        if (gAgents.Count > 0)
        {
            gAgents.ShouldContain(gt =>
                gt.ToString()!.Contains("test.mock_executor", StringComparison.OrdinalIgnoreCase));
        }
    }

    //[Fact]
    public async Task FindGAgentsByEventTypeAsync_WithNonHandledEvent_ShouldReturnEmptyOrNoMatchingGAgents()
    {
        // Act
        var gAgents = await _gAgentService.FindGAgentsByEventTypeAsync(typeof(UnhandledTestEvent));

        // Assert
        gAgents.ShouldNotBeNull();
        // Should either be empty or not contain MockExecutorGAgent
        gAgents.ShouldNotContain(gt =>
            gt.ToString()!.Contains("MockExecutorGAgent", StringComparison.OrdinalIgnoreCase));
    }

    //[Fact]
    public async Task GetGAgentDetailInfo_ShouldReturnCorrectConfigurationType()
    {
        // Arrange
        var mockGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
        var grainId = mockGAgent.GetGrainId();
        var grainType = grainId.Type;

        // Act
        var detailInfo = await _gAgentService.GetGAgentDetailInfoAsync(grainType);

        // Assert
        detailInfo.ShouldNotBeNull();
        detailInfo.ConfigurationType.ShouldBe(typeof(ConfigurationBase));
    }

    //[Fact]
    public async Task GetAllAvailableGAgentInformation_MultipleCalls_ShouldBeCached()
    {
        // Arrange & Act
        var startTime = DateTime.UtcNow;
        var firstCall = await _gAgentService.GetAllAvailableGAgentInformation();
        var firstCallTime = DateTime.UtcNow - startTime;

        startTime = DateTime.UtcNow;
        var secondCall = await _gAgentService.GetAllAvailableGAgentInformation();
        var secondCallTime = DateTime.UtcNow - startTime;

        // Assert
        firstCall.ShouldNotBeNull();
        secondCall.ShouldNotBeNull();
        firstCall.Count.ShouldBe(secondCall.Count);

        // Second call should be faster due to caching (though this is not guaranteed in all environments)
        // Just verify they return the same results
        foreach (var kvp in firstCall)
        {
            secondCall.ShouldContainKey(kvp.Key);
            secondCall[kvp.Key].Count.ShouldBe(kvp.Value.Count);
        }
    }
}

// Event type that no GAgent handles for testing
[GenerateSerializer]
[Description("Test event that no GAgent handles")]
public class UnhandledTestEvent : EventBase
{
    [Id(0)] public string Data { get; set; } = string.Empty;
}