// ABOUTME: This file contains TDD tests for IMetaDataStateGAgent interface default methods
// ABOUTME: Following RED-GREEN-REFACTOR cycle with failing tests written first

using Aevatar.MetaData.Enums;
using Aevatar.MetaData.Events;
using Shouldly;
using Xunit;

namespace Aevatar.MetaData.Tests;

/// <summary>
/// TDD tests for IMetaDataStateGAgent interface default method behavior.
/// Tests are written FIRST (RED phase) before implementation is complete.
/// </summary>
public class IMetaDataStateGAgentDefaultMethodTests
{
    private readonly TestMetaDataState _state;
    private readonly MockEventRaiser _eventRaiser;
    private readonly IMetaDataStateGAgent<TestMetaDataState> _eventRaiserInterface;
    private readonly Guid _testAgentId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();

    public IMetaDataStateGAgentDefaultMethodTests()
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
        @event.ActivityType.ShouldBe(string.Empty);
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
        var keyToKeep = "keepMe";
        _state.Properties[keyToRemove] = "someValue"; // Add property first
        _state.Properties[keyToKeep] = "keepValue"; // Add another property to keep

        // Act
        await _eventRaiserInterface.RemovePropertyAsync(keyToRemove);

        // Assert
        _eventRaiser.RaisedEvents.ShouldHaveSingleItem();
        var @event = _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        @event.AgentId.ShouldBe(_testAgentId);
        @event.UserId.ShouldBe(_testUserId);
        @event.UpdatedProperties.ShouldContainKeyAndValue(keyToKeep, "keepValue");
        @event.UpdatedProperties.ShouldNotContainKey(keyToRemove);
        @event.RemovedProperties.ShouldHaveSingleItem();
        @event.RemovedProperties[0].ShouldBe(keyToRemove);
        @event.WasMerged.ShouldBeFalse();
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
        _eventRaiser.RaisedEvents.Count.ShouldBe(1); // Status only
        var statusEvent = _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentStatusChangedEvent>();
        statusEvent.NewStatus.ShouldBe(newStatus);
        statusEvent.Reason.ShouldBe(reason);
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
        _eventRaiser.RaisedEvents.Count.ShouldBe(1); // Properties only
        var propertiesEvent = _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        propertiesEvent.UpdatedProperties.ShouldBe(properties);
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
        _eventRaiser.RaisedEvents.Count.ShouldBe(2); // Status + Properties
        _eventRaiser.RaisedEvents[0].ShouldBeOfType<AgentStatusChangedEvent>();
        _eventRaiser.RaisedEvents[1].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
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
        _eventRaiser.RaisedEvents.ShouldBeEmpty();
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
        _eventRaiser.RaisedEvents.ShouldBeEmpty(); // No events for empty properties
    }
}