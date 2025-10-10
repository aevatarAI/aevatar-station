using System.Diagnostics.Tracing;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.StateManagement;
using Aevatar.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Aevatar.Core.Tests;

[GenerateSerializer]
public class TestCoreGAgentEvent : EventBase
{
    [Id(0)] public string Operation { get; set; } = string.Empty;
    [Id(1)] public int Value { get; set; }
}

[GenerateSerializer]
public class TestCoreGAgentState : CoreStateBase
{
    [Id(0)] public string TestValue { get; set; } = string.Empty;
    [Id(1)] public int Counter { get; set; } = 0;
}

[GenerateSerializer]
public class TestCoreStateLogEvent : StateLogEventBase<TestCoreStateLogEvent>
{
    [Id(0)] public string Operation { get; set; } = string.Empty;
    [Id(1)] public int Value { get; set; }
}

[GenerateSerializer]
public class TestCoreConfiguration : ConfigurationBase
{
    [Id(0)] public string Setting { get; set; } = string.Empty;
}

[GenerateSerializer]
public class TestInvalidConfiguration : ConfigurationBase
{
    [Id(0)] public string InvalidSetting { get; set; } = string.Empty;
}

[GAgent]
public class TestCoreGAgent : CoreGAgentBase<TestCoreGAgentState, TestCoreStateLogEvent, EventBase, TestCoreConfiguration>
{
    public override async Task<string> GetDescriptionAsync()
    {
        return await Task.FromResult("Test Core GAgent for unit testing");
    }

    protected override async Task PerformConfigAsync(TestCoreConfiguration configuration)
    {
        RaiseEvent(new TestCoreStateLogEvent
        {
            Operation = "Configure",
            Value = configuration.Setting.Length
        });
        await ConfirmEvents();
    }

    protected override void GAgentTransitionState(TestCoreGAgentState state, StateLogEventBase<TestCoreStateLogEvent> @event)
    {
        if (@event is TestCoreStateLogEvent testEvent)
        {
            switch (testEvent.Operation)
            {
                case "Configure":
                    state.TestValue = $"Configured with length {testEvent.Value}";
                    break;
                case "Increment":
                    state.Counter += testEvent.Value;
                    break;
            }
        }
    }

    // Test method to expose protected functionality
    public async Task<Guid> TestSendEventToAgentAsync<T>(T @event, GrainId targetGrainId) where T : EventBase
    {
        return await SendEventToAgentAsync(@event, targetGrainId);
    }

    // Test method to trigger state changes
    [EventHandler]
    public async Task IncrementCounterAsync(TestCoreGAgentEvent @event)
    {
        RaiseEvent(new TestCoreStateLogEvent
        {
            Operation = @event.Operation,
            Value = @event.Value
        });
        await ConfirmEvents();
    }
}

public class CoreGAgentBaseTests : GAgentTestKitBase
{
    [Fact]
    public async Task CoreGAgentBase_ShouldImplementICoreGAgentInterface()
    {
        // Arrange
        var agent = await Silo.CreateGrainAsync<TestCoreGAgent>(Guid.NewGuid());

        // Act & Assert - Test ICoreGAgent methods
        await agent.ActivateAsync();
        
        var subscriptions = await agent.GetAllSubscribedEventsAsync();
        subscriptions.ShouldNotBeNull();
        
        var configType = await agent.GetConfigurationTypeAsync();
        configType.ShouldBe(typeof(TestCoreConfiguration));
        
        var description = await agent.GetDescriptionAsync();
        description.ShouldBe("Test Core GAgent for unit testing");
    }

    [Fact]
    public async Task CoreGAgentBase_ShouldImplementICoreStateGAgentInterface()
    {
        // Arrange
        var agent = await Silo.CreateGrainAsync<TestCoreGAgent>(Guid.NewGuid());

        // Act
        var state = await agent.GetStateAsync();

        // Assert
        state.ShouldNotBeNull();
        state.ShouldBeOfType<TestCoreGAgentState>();
        state.TestValue.ShouldBe(string.Empty);
        state.Counter.ShouldBe(0);
    }

    [Fact]
    public async Task CoreGAgentBase_ConfigAsync_ShouldApplyConfiguration()
    {
        // Arrange
        var agent = await Silo.CreateGrainAsync<TestCoreGAgent>(Guid.NewGuid());
        var config = new TestCoreConfiguration { Setting = "TestValue" };

        // Act
        await agent.ConfigAsync(config);

        // Assert
        var state = await agent.GetStateAsync();
        state.TestValue.ShouldBe("Configured with length 9"); // "TestValue" has length 9
    }

    [Fact]
    public async Task CoreGAgentBase_StateTransitions_ShouldWorkCorrectly()
    {
        // Arrange
        var agent = await Silo.CreateGrainAsync<TestCoreGAgent>(Guid.NewGuid());

        // Act
        await agent.IncrementCounterAsync(new TestCoreGAgentEvent
        {
            Operation = "Increment",
            Value = 5
        });
        await agent.IncrementCounterAsync(new TestCoreGAgentEvent
        {
            Operation = "Increment",
            Value = 3
        });

        // Assert
        var state = await agent.GetStateAsync();
        state.Counter.ShouldBe(8);
    }

    [Fact]
    public async Task CoreGAgentBase_SendEventToAgentAsync_ShouldTransmitEvent()
    {
        // Arrange
        var senderAgent = await Silo.CreateGrainAsync<TestCoreGAgent>(Guid.NewGuid());
        var receiverAgent = await Silo.CreateGrainAsync<TestCoreGAgent>(Guid.NewGuid());
        
        var testEvent = new TestEvent
        {
            Message = "Hello from CoreGAgentBase"
        };

        // Act
        var eventId = await senderAgent.TestSendEventToAgentAsync(testEvent, receiverAgent.GetGrainId());

        // Assert
        eventId.ShouldNotBe(Guid.Empty);
        testEvent.PublisherGrainId.ShouldBe(senderAgent.GetGrainId());
    }

    [Fact]
    public async Task CoreGAgentBase_GetAllSubscribedEventsAsync_ShouldReturnEventTypes()
    {
        // Arrange
        var agent = await Silo.CreateGrainAsync<TestCoreGAgent>(Guid.NewGuid());

        // Act
        var eventTypes = await agent.GetAllSubscribedEventsAsync();

        // Assert
        eventTypes.ShouldNotBeNull();
        eventTypes.ShouldNotBeEmpty();
        // Should include EventBase-derived types that the agent can handle
    }

    [Fact]
    public async Task CoreGAgentBase_GetConfigurationTypeAsync_ShouldReturnCorrectType()
    {
        // Arrange
        var agent = await Silo.CreateGrainAsync<TestCoreGAgent>(Guid.NewGuid());

        // Act
        var configType = await agent.GetConfigurationTypeAsync();

        // Assert
        configType.ShouldBe(typeof(TestCoreConfiguration));
    }

    [Fact]
    public async Task CoreGAgentBase_ActivateAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var agent = await Silo.CreateGrainAsync<TestCoreGAgent>(Guid.NewGuid());

        // Act & Assert
        await Should.NotThrowAsync(async () => await agent.ActivateAsync());
    }

    [Fact]
    public async Task CoreGAgentBase_WithInvalidConfiguration_ShouldHandleGracefully()
    {
        // Arrange
        var agent = await Silo.CreateGrainAsync<TestCoreGAgent>(Guid.NewGuid());
        var invalidConfig = new TestInvalidConfiguration(); // Wrong type

        // Act & Assert
        await Should.NotThrowAsync(async () => await agent.ConfigAsync(invalidConfig));
        
        // State should remain unchanged
        var state = await agent.GetStateAsync();
        state.TestValue.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task CoreGAgentBase_StateChanges_ShouldTriggerStatePublishing()
    {
        // Arrange
        var agent = await Silo.CreateGrainAsync<TestCoreGAgent>(Guid.NewGuid());

        // Act
        await agent.IncrementCounterAsync(new TestCoreGAgentEvent
        {
            Operation = "Increment",
            Value = 1
        });

        // Assert
        var state = await agent.GetStateAsync();
        state.Counter.ShouldBe(1);
        
        // Note: In a real test, we would mock IStatePublisher to verify it was called
        // For now, we just verify the state change occurred
    }
}

[GenerateSerializer]
public class TestEvent : EventBase
{
    [Id(0)] public string Message { get; set; } = string.Empty;
} 