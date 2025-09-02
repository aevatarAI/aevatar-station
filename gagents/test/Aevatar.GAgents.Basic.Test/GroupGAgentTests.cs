using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.TestBase;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.GAgents.Basic.Test;

/// <summary>
/// Unit tests for GroupGAgent
/// </summary>
[Collection(ClusterCollection.Name)]
public sealed class GroupGAgentTests : AevatarBasicTestBase<AevatarBasicTestModule>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public GroupGAgentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task GroupGAgent_GetDescription_ShouldReturnCorrectDescription()
    {
        // Arrange
        var groupAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());

        // Act
        var description = await groupAgent.GetDescriptionAsync();

        // Assert
        description.ShouldBe("An agent to inform other agents when a social event is published.");
        _testOutputHelper.WriteLine($"Description: {description}");
    }

    [Fact]
    public async Task GroupGAgent_InitialState_ShouldHaveZeroRegisteredAgents()
    {
        // Arrange & Act
        var groupAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());

        // Assert
        var state = await groupAgent.GetStateAsync();
        state.ShouldNotBeNull();
        state.RegisteredGAgents.ShouldBe(0);

        _testOutputHelper.WriteLine($"Initial registered agents count: {state.RegisteredGAgents}");
    }

    [Fact]
    public async Task GroupGAgent_PublishEvent_ShouldPublishSuccessfully()
    {
        // Arrange
        var groupAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var socialEvent = new SocialTestEvent { Content = "Test social event", UserId = "user123" };

        // Act
        await groupAgent.PublishEventAsync(socialEvent);

        // Assert - If no exception is thrown, the publish was successful
        _testOutputHelper.WriteLine($"Successfully published social event: {socialEvent.Content}");
    }

    [Fact]
    public async Task GroupGAgent_PublishEvent_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Arrange
        var groupAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await groupAgent.PublishEventAsync<SocialTestEvent>(null!);
        });

        exception.Message.ShouldContain("Parameter 'event'");
        _testOutputHelper.WriteLine($"Correctly threw exception: {exception.Message}");
    }

    [Fact]
    public async Task GroupGAgent_AgentRegistration_ShouldIncreaseCount()
    {
        // Arrange
        var groupAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var childAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());

        // Act
        await groupAgent.RegisterAsync(childAgent);

        // Allow some time for the registration to process
        await Task.Delay(100);

        // Assert
        var state = await groupAgent.GetStateAsync();
        state.RegisteredGAgents.ShouldBe(1);

        _testOutputHelper.WriteLine($"Registered agents count after registration: {state.RegisteredGAgents}");
    }

    [Fact]
    public async Task GroupGAgent_AgentUnregistration_ShouldDecreaseCount()
    {
        // Arrange
        var groupAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var childAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());

        // Register first
        await groupAgent.RegisterAsync(childAgent);
        await Task.Delay(100);

        // Verify registration
        var stateAfterRegister = await groupAgent.GetStateAsync();
        stateAfterRegister.RegisteredGAgents.ShouldBe(1);

        // Act - Unregister
        await groupAgent.UnregisterAsync(childAgent);
        await Task.Delay(100);

        // Assert
        var stateAfterUnregister = await groupAgent.GetStateAsync();
        stateAfterUnregister.RegisteredGAgents.ShouldBe(0);

        _testOutputHelper.WriteLine(
            $"Registered agents count after unregistration: {stateAfterUnregister.RegisteredGAgents}");
    }

    [Fact]
    public async Task GroupGAgent_MultipleAgentRegistration_ShouldTrackCorrectCount()
    {
        // Arrange
        var groupAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var childAgent1 = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var childAgent2 = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var childAgent3 = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());

        // Act - Register multiple agents
        await groupAgent.RegisterAsync(childAgent1);
        await groupAgent.RegisterAsync(childAgent2);
        await groupAgent.RegisterAsync(childAgent3);

        await Task.Delay(200); // Allow time for all registrations

        // Assert
        var state = await groupAgent.GetStateAsync();
        state.RegisteredGAgents.ShouldBe(3);

        _testOutputHelper.WriteLine($"Registered agents count after multiple registrations: {state.RegisteredGAgents}");
    }

    [Fact]
    public async Task GroupGAgent_MixedRegistrationUnregistration_ShouldTrackCorrectly()
    {
        // Arrange
        var groupAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var childAgent1 = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var childAgent2 = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());

        // Act - Register two agents
        await groupAgent.RegisterAsync(childAgent1);
        await groupAgent.RegisterAsync(childAgent2);
        await Task.Delay(100);

        var stateAfterRegistration = await groupAgent.GetStateAsync();
        stateAfterRegistration.RegisteredGAgents.ShouldBe(2);

        // Unregister one agent
        await groupAgent.UnregisterAsync(childAgent1);
        await Task.Delay(100);

        // Assert
        var finalState = await groupAgent.GetStateAsync();
        finalState.RegisteredGAgents.ShouldBe(1);

        _testOutputHelper.WriteLine($"Final registered agents count: {finalState.RegisteredGAgents}");
    }

    [Fact]
    public async Task GroupGAgent_EventPublishingWithRegisteredAgents_ShouldWorkCorrectly()
    {
        // Arrange
        var groupAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var childAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());

        // Register child agent
        await groupAgent.RegisterAsync(childAgent);
        await Task.Delay(100);

        // Verify registration
        var state = await groupAgent.GetStateAsync();
        state.RegisteredGAgents.ShouldBe(1);

        // Act - Publish event
        var socialEvent = new SocialTestEvent { Content = "Group event", UserId = "groupUser" };
        await groupAgent.PublishEventAsync(socialEvent);

        // Assert - Event should be published successfully even with registered agents
        _testOutputHelper.WriteLine("Successfully published event with registered agents");
    }

    [Fact]
    public async Task GroupGAgent_ConcurrentRegistrationUnregistration_ShouldHandleCorrectly()
    {
        // Arrange
        var groupAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var childAgents = new List<IGroupGAgent>();

        for (int i = 0; i < 5; i++)
        {
            childAgents.Add(await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid()));
        }

        // Act - Concurrent registrations
        var registrationTasks = childAgents.Select(agent => groupAgent.RegisterAsync(agent));
        await Task.WhenAll(registrationTasks);
        await Task.Delay(200);

        var stateAfterRegistrations = await groupAgent.GetStateAsync();
        stateAfterRegistrations.RegisteredGAgents.ShouldBe(5);

        // Concurrent unregistrations
        var unregistrationTasks = childAgents.Take(3).Select(agent => groupAgent.UnregisterAsync(agent));
        await Task.WhenAll(unregistrationTasks);
        await Task.Delay(200);

        // Assert
        var finalState = await groupAgent.GetStateAsync();
        finalState.RegisteredGAgents.ShouldBe(2);

        _testOutputHelper.WriteLine($"Final count after concurrent operations: {finalState.RegisteredGAgents}");
    }

    [Fact]
    public async Task GroupGAgent_DifferentEventTypes_ShouldPublishCorrectly()
    {
        // Arrange
        var groupAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var socialEvent = new SocialTestEvent { Content = "Social content", UserId = "social_user" };
        var notificationEvent = new NotificationTestEvent { Title = "Notification", Message = "Test notification" };

        // Act
        await groupAgent.PublishEventAsync(socialEvent);
        await groupAgent.PublishEventAsync(notificationEvent);

        // Assert - Both events should be published successfully
        _testOutputHelper.WriteLine("Successfully published different event types through GroupGAgent");
    }
}

/// <summary>
/// Test event for social functionality testing
/// </summary>
[GenerateSerializer]
public class SocialTestEvent : EventBase
{
    [Id(0)] public string Content { get; set; } = string.Empty;
    [Id(1)] public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// Test event for notification functionality testing
/// </summary>
[GenerateSerializer]
public class NotificationTestEvent : EventBase
{
    [Id(0)] public string Title { get; set; } = string.Empty;
    [Id(1)] public string Message { get; set; } = string.Empty;
}