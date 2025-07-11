// ABOUTME: This file contains TDD tests for IMetaDataStateEventRaiser interface default methods
// ABOUTME: Following RED-GREEN-REFACTOR cycle with failing tests written first

using Aevatar.MetaData.Enums;
using Aevatar.MetaData.Events;
using Shouldly;
using Xunit;

namespace Aevatar.MetaData.Tests;

/// <summary>
/// TDD tests for IMetaDataStateEventRaiser interface default method behavior.
/// Tests are written FIRST (RED phase) before implementation is complete.
/// </summary>
public class IMetaDataStateEventRaiserTests
{
    private readonly TestMetaDataState _state;
    private readonly MockEventRaiser _eventRaiser;
    private readonly IMetaDataStateEventRaiser<TestMetaDataState> _eventRaiserInterface;
    private readonly Guid _testAgentId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();

    public IMetaDataStateEventRaiserTests()
    {
        _state = new TestMetaDataState();
        _eventRaiser = new MockEventRaiser(_state);
        _eventRaiserInterface = _eventRaiser; // Cast to interface to access default methods
    }

    [Fact]
    public async Task CreateAgentAsync_Should_RaiseAgentCreatedEvent_WithCorrectProperties()
    {
        // Arrange
        var name = "Test Agent";
        var agentType = "TestAgent";
        var properties = new Dictionary<string, string> { { "key1", "value1" } };

        // Act
        await _eventRaiserInterface.CreateAgentAsync(_testAgentId, _testUserId, name, agentType, properties);

        // Assert
        _eventRaiser.RaisedEvents.ShouldHaveSingleItem();
        var @event = _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentCreatedEvent>();
        @event.AgentId.ShouldBe(_testAgentId);
        @event.UserId.ShouldBe(_testUserId);
        @event.Name.ShouldBe(name);
        @event.AgentType.ShouldBe(agentType);
        @event.Properties.ShouldBe(properties);
        @event.InitialStatus.ShouldBe(AgentStatus.Creating);
        _eventRaiser.ConfirmEventsTimes.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task CreateAgentAsync_Should_HandleNullProperties()
    {
        // Arrange & Act
        await _eventRaiserInterface.CreateAgentAsync(_testAgentId, _testUserId, "Test", "TestAgent", null);

        // Assert
        var @event = _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentCreatedEvent>();
        @event.Properties.ShouldNotBeNull();
        @event.Properties.ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateStatusAsync_Should_RaiseAgentStatusChangedEvent()
    {
        // Arrange
        _state.Status = AgentStatus.Active;
        _state.Id = _testAgentId;
        _state.UserId = _testUserId;
        var newStatus = AgentStatus.Paused;
        var reason = "Manual pause";

        // Act
        await _eventRaiserInterface.UpdateStatusAsync(newStatus, reason);

        // Assert
        _eventRaiser.RaisedEvents.ShouldHaveSingleItem();
        var @event = _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentStatusChangedEvent>();
        @event.AgentId.ShouldBe(_testAgentId);
        @event.UserId.ShouldBe(_testUserId);
        @event.OldStatus.ShouldBe(AgentStatus.Active);
        @event.NewStatus.ShouldBe(newStatus);
        @event.Reason.ShouldBe(reason);
        _eventRaiser.ConfirmEventsTimes.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task UpdateStatusAsync_Should_HandleNullReason()
    {
        // Arrange
        _state.Status = AgentStatus.Active;
        _state.Id = _testAgentId;
        _state.UserId = _testUserId;

        // Act
        await _eventRaiserInterface.UpdateStatusAsync(AgentStatus.Paused);

        // Assert
        var @event = _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentStatusChangedEvent>();
        @event.Reason.ShouldBeNull();
    }

    [Fact]
    public async Task UpdatePropertiesAsync_Should_RaiseAgentPropertiesUpdatedEvent_WithMerge()
    {
        // Arrange
        _state.Id = _testAgentId;
        _state.UserId = _testUserId;
        var properties = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } };

        // Act
        await _eventRaiserInterface.UpdatePropertiesAsync(properties, merge: true);

        // Assert
        _eventRaiser.RaisedEvents.ShouldHaveSingleItem();
        var @event = _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        @event.AgentId.ShouldBe(_testAgentId);
        @event.UserId.ShouldBe(_testUserId);
        @event.UpdatedProperties.ShouldBe(properties);
        @event.RemovedProperties.ShouldBeEmpty();
        @event.WasMerged.ShouldBeTrue();
        _eventRaiser.ConfirmEventsTimes.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task UpdatePropertiesAsync_Should_RaiseAgentPropertiesUpdatedEvent_WithoutMerge()
    {
        // Arrange
        _state.Id = _testAgentId;
        _state.UserId = _testUserId;
        var properties = new Dictionary<string, string> { { "key1", "value1" } };

        // Act
        await _eventRaiserInterface.UpdatePropertiesAsync(properties, merge: false);

        // Assert
        var @event = _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        @event.WasMerged.ShouldBeFalse();
    }

    [Fact]
    public async Task RecordActivityAsync_Should_RaiseAgentActivityUpdatedEvent_WithActivityType()
    {
        // Arrange
        _state.Id = _testAgentId;
        _state.UserId = _testUserId;
        var activityType = "heartbeat";

        // Act
        await _eventRaiserInterface.RecordActivityAsync(activityType);

        // Assert
        _eventRaiser.RaisedEvents.ShouldHaveSingleItem();
        var @event = _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentActivityUpdatedEvent>();
        @event.AgentId.ShouldBe(_testAgentId);
        @event.UserId.ShouldBe(_testUserId);
        @event.ActivityType.ShouldBe(activityType);
        _eventRaiser.ConfirmEventsTimes.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task RecordActivityAsync_Should_UseDefaultActivityType_WhenNull()
    {
        // Arrange
        _state.Id = _testAgentId;
        _state.UserId = _testUserId;

        // Act
        await _eventRaiserInterface.RecordActivityAsync(null);

        // Assert
        var @event = _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentActivityUpdatedEvent>();
        @event.ActivityType.ShouldBe("activity");
    }

    [Fact]
    public async Task SetPropertyAsync_Should_CallUpdatePropertiesAsync_WithSingleProperty()
    {
        // Arrange
        _state.Id = _testAgentId;
        _state.UserId = _testUserId;
        var key = "testKey";
        var value = "testValue";

        // Act
        await _eventRaiserInterface.SetPropertyAsync(key, value);

        // Assert
        _eventRaiser.RaisedEvents.ShouldHaveSingleItem();
        var @event = _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        @event.UpdatedProperties.ShouldContainKeyAndValue(key, value);
        @event.WasMerged.ShouldBeTrue();
    }

    [Fact]
    public async Task RemovePropertyAsync_Should_RaiseAgentPropertiesUpdatedEvent_WithRemovedProperty()
    {
        // Arrange
        _state.Id = _testAgentId;
        _state.UserId = _testUserId;
        var keyToRemove = "removeMe";

        // Act
        await _eventRaiserInterface.RemovePropertyAsync(keyToRemove);

        // Assert
        _eventRaiser.RaisedEvents.ShouldHaveSingleItem();
        var @event = _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        @event.AgentId.ShouldBe(_testAgentId);
        @event.UserId.ShouldBe(_testUserId);
        @event.UpdatedProperties.ShouldBeEmpty();
        @event.RemovedProperties.ShouldHaveSingleItem();
        @event.RemovedProperties[0].ShouldBe(keyToRemove);
        @event.WasMerged.ShouldBeTrue();
    }

    [Fact]
    public async Task BatchUpdateAsync_Should_UpdateStatusOnly_WhenOnlyStatusProvided()
    {
        // Arrange
        _state.Status = AgentStatus.Active;
        _state.Id = _testAgentId;
        _state.UserId = _testUserId;
        var newStatus = AgentStatus.Paused;
        var reason = "Batch pause";

        // Act
        await _eventRaiserInterface.BatchUpdateAsync(newStatus: newStatus, statusReason: reason);

        // Assert
        _eventRaiser.RaisedEvents.Count.ShouldBe(2); // Status + Activity
        var statusEvent = _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentStatusChangedEvent>();
        statusEvent.NewStatus.ShouldBe(newStatus);
        statusEvent.Reason.ShouldBe(reason);
        
        var activityEvent = _eventRaiser.RaisedEvents[1].ShouldBeOfType<AgentActivityUpdatedEvent>();
        activityEvent.ActivityType.ShouldBe("batch_update");
    }

    [Fact]
    public async Task BatchUpdateAsync_Should_UpdatePropertiesOnly_WhenOnlyPropertiesProvided()
    {
        // Arrange
        _state.Id = _testAgentId;
        _state.UserId = _testUserId;
        var properties = new Dictionary<string, string> { { "key1", "value1" } };

        // Act
        await _eventRaiserInterface.BatchUpdateAsync(properties: properties);

        // Assert
        _eventRaiser.RaisedEvents.Count.ShouldBe(2); // Properties + Activity
        var propertiesEvent = _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        propertiesEvent.UpdatedProperties.ShouldBe(properties);
        
        var activityEvent = _eventRaiser.RaisedEvents[1].ShouldBeOfType<AgentActivityUpdatedEvent>();
        activityEvent.ActivityType.ShouldBe("batch_update");
    }

    [Fact]
    public async Task BatchUpdateAsync_Should_UpdateBothStatusAndProperties()
    {
        // Arrange
        _state.Status = AgentStatus.Active;
        _state.Id = _testAgentId;
        _state.UserId = _testUserId;
        var newStatus = AgentStatus.Paused;
        var properties = new Dictionary<string, string> { { "key1", "value1" } };

        // Act
        await _eventRaiserInterface.BatchUpdateAsync(newStatus: newStatus, properties: properties);

        // Assert
        _eventRaiser.RaisedEvents.Count.ShouldBe(3); // Status + Properties + Activity
        _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentStatusChangedEvent>();
        _eventRaiser.RaisedEvents[1].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        _eventRaiser.RaisedEvents[2].ShouldBeOfType<AgentActivityUpdatedEvent>();
    }

    [Fact]
    public async Task BatchUpdateAsync_Should_OnlyRecordActivity_WhenNoUpdatesProvided()
    {
        // Arrange
        _state.Id = _testAgentId;
        _state.UserId = _testUserId;

        // Act
        await _eventRaiserInterface.BatchUpdateAsync();

        // Assert
        _eventRaiser.RaisedEvents.ShouldHaveSingleItem();
        var activityEvent = _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentActivityUpdatedEvent>();
        activityEvent.ActivityType.ShouldBe("batch_update");
    }

    [Fact]
    public async Task BatchUpdateAsync_Should_HandleEmptyPropertiesDictionary()
    {
        // Arrange
        _state.Id = _testAgentId;
        _state.UserId = _testUserId;
        var emptyProperties = new Dictionary<string, string>();

        // Act
        await _eventRaiserInterface.BatchUpdateAsync(properties: emptyProperties);

        // Assert
        _eventRaiser.RaisedEvents.ShouldHaveSingleItem(); // Only activity, no properties event
        var activityEvent = _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentActivityUpdatedEvent>();
        activityEvent.ActivityType.ShouldBe("batch_update");
    }
}