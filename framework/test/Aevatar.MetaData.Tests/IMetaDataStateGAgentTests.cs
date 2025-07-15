// ABOUTME: This file contains unit tests for IMetaDataStateGAgent interface default methods
// ABOUTME: Tests verify the behavior of all default method implementations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.MetaData.Enums;
using Aevatar.MetaData.Events;
using Orleans.Runtime;
using Shouldly;
using Xunit;

namespace Aevatar.MetaData.Tests;

public class IMetaDataStateGAgentTests
{
    private readonly TestMetaDataStateGAgent _testAgent;
    private readonly IMetaDataStateGAgent<TestMetaDataState> _agent;
    private readonly Guid _testAgentId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();
    
    public IMetaDataStateGAgentTests()
    {
        _testAgent = new TestMetaDataStateGAgent(_testAgentId, _testUserId);
        _agent = _testAgent;
    }
    
    #region CreateAgentAsync Tests
    
    [Fact]
    public async Task CreateAgentAsync_WithValidParameters_RaisesCorrectEvent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var name = "Test Agent";
        var agentType = "TestType";
        
        // Act
        await _agent.CreateAgentAsync(id, userId, name, agentType);
        
        // Assert
        _testAgent.RaisedEvents.Count.ShouldBe(1);
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentCreatedEvent>();
        @event.AgentId.ShouldBe(id);
        @event.UserId.ShouldBe(userId);
        @event.Name.ShouldBe(name);
        @event.AgentType.ShouldBe(agentType);
        @event.Properties.ShouldNotBeNull();
        @event.Properties.Count.ShouldBe(0);
        @event.AgentGrainId.ShouldBe(_testAgent.TestGrainId);
        @event.InitialStatus.ShouldBe(AgentStatus.Creating);
        _testAgent.ConfirmEventsCalled.ShouldBe(1);
    }
    
    [Fact]
    public async Task CreateAgentAsync_WithProperties_IncludesPropertiesInEvent()
    {
        // Arrange
        var properties = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };
        
        // Act
        await _agent.CreateAgentAsync(Guid.NewGuid(), Guid.NewGuid(), "Agent", "Type", properties);
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentCreatedEvent>();
        @event.Properties.ShouldBe(properties);
    }
    
    [Fact]
    public async Task CreateAgentAsync_WithNullProperties_CreatesEmptyDictionary()
    {
        // Act
        await _agent.CreateAgentAsync(Guid.NewGuid(), Guid.NewGuid(), "Agent", "Type", null);
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentCreatedEvent>();
        @event.Properties.ShouldNotBeNull();
        @event.Properties.Count.ShouldBe(0);
    }
    
    [Fact]
    public async Task CreateAgentAsync_WithEmptyName_AllowsEmptyName()
    {
        // Act
        await _agent.CreateAgentAsync(Guid.NewGuid(), Guid.NewGuid(), "", "Type");
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentCreatedEvent>();
        @event.Name.ShouldBe("");
    }
    
    [Fact]
    public async Task CreateAgentAsync_WithSpecialCharactersInProperties_PreservesCharacters()
    {
        // Arrange
        var properties = new Dictionary<string, string>
        {
            ["special"] = "!@#$%^&*()_+-=[]{}|;':\",./<>?"
        };
        
        // Act
        await _agent.CreateAgentAsync(Guid.NewGuid(), Guid.NewGuid(), "Agent", "Type", properties);
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentCreatedEvent>();
        @event.Properties["special"].ShouldBe("!@#$%^&*()_+-=[]{}|;':\",./<>?");
    }
    
    #endregion
    
    #region UpdateStatusAsync Tests
    
    [Fact]
    public async Task UpdateStatusAsync_WithReason_RaisesEventWithAllFields()
    {
        // Arrange
        var newStatus = AgentStatus.Active;
        var reason = "Activation complete";
        var timeBefore = DateTime.UtcNow;
        
        // Act
        await _agent.UpdateStatusAsync(newStatus, reason);
        var timeAfter = DateTime.UtcNow;
        
        // Assert
        _testAgent.RaisedEvents.Count.ShouldBe(1);
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentStatusChangedEvent>();
        @event.AgentId.ShouldBe(_testAgentId);
        @event.UserId.ShouldBe(_testUserId);
        @event.OldStatus.ShouldBe(AgentStatus.Creating);
        @event.NewStatus.ShouldBe(newStatus);
        @event.Reason.ShouldBe(reason);
        @event.StatusChangeTime.ShouldBeInRange(timeBefore, timeAfter);
        _testAgent.ConfirmEventsCalled.ShouldBe(1);
    }
    
    [Fact]
    public async Task UpdateStatusAsync_WithoutReason_RaisesEventWithNullReason()
    {
        // Act
        await _agent.UpdateStatusAsync(AgentStatus.Paused);
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentStatusChangedEvent>();
        @event.Reason.ShouldBeNull();
    }
    
    [Fact]
    public async Task UpdateStatusAsync_CapturesCurrentStateStatus()
    {
        // Arrange
        _testAgent.TestState.Status = AgentStatus.Active;
        
        // Act
        await _agent.UpdateStatusAsync(AgentStatus.Paused);
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentStatusChangedEvent>();
        @event.OldStatus.ShouldBe(AgentStatus.Active);
    }
    
    [Fact]
    public async Task UpdateStatusAsync_WithVeryLongReason_PreservesFullReason()
    {
        // Arrange
        var longReason = new string('a', 10000);
        
        // Act
        await _agent.UpdateStatusAsync(AgentStatus.Active, longReason);
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentStatusChangedEvent>();
        @event.Reason.ShouldBe(longReason);
        @event.Reason.Length.ShouldBe(10000);
    }
    
    #endregion
    
    #region UpdatePropertiesAsync Tests
    
    [Fact]
    public async Task UpdatePropertiesAsync_WithMergeTrue_OnlyIncludesNewProperties()
    {
        // Arrange
        _testAgent.TestState.Properties = new Dictionary<string, string>
        {
            ["existing"] = "value",
            ["toUpdate"] = "oldValue"
        };
        var newProperties = new Dictionary<string, string>
        {
            ["new"] = "newValue",
            ["toUpdate"] = "newValue"
        };
        
        // Act
        await _agent.UpdatePropertiesAsync(newProperties, merge: true);
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        @event.UpdatedProperties.ShouldBe(newProperties);
        @event.RemovedProperties.ShouldBeEmpty();
        @event.WasMerged.ShouldBeTrue();
    }
    
    [Fact]
    public async Task UpdatePropertiesAsync_WithMergeFalse_IncludesRemovedProperties()
    {
        // Arrange
        _testAgent.TestState.Properties = new Dictionary<string, string>
        {
            ["toRemove1"] = "value1",
            ["toRemove2"] = "value2",
            ["toKeep"] = "keepValue"
        };
        var newProperties = new Dictionary<string, string>
        {
            ["toKeep"] = "keepValue",
            ["new"] = "newValue"
        };
        
        // Act
        await _agent.UpdatePropertiesAsync(newProperties, merge: false);
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        @event.UpdatedProperties.ShouldBe(newProperties);
        @event.RemovedProperties.ShouldContain("toRemove1");
        @event.RemovedProperties.ShouldContain("toRemove2");
        @event.RemovedProperties.Length.ShouldBe(2);
        @event.WasMerged.ShouldBeFalse();
    }
    
    [Fact]
    public async Task UpdatePropertiesAsync_WithEmptyDictionary_HandlesCorrectly()
    {
        // Arrange
        _testAgent.TestState.Properties = new Dictionary<string, string> { ["key"] = "value" };
        
        // Act
        await _agent.UpdatePropertiesAsync(new Dictionary<string, string>(), merge: false);
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        @event.UpdatedProperties.ShouldBeEmpty();
        @event.RemovedProperties.ShouldContain("key");
    }
    
    [Fact]
    public async Task UpdatePropertiesAsync_SetsUpdateTime()
    {
        // Arrange
        var timeBefore = DateTime.UtcNow;
        
        // Act
        await _agent.UpdatePropertiesAsync(new Dictionary<string, string> { ["key"] = "value" });
        var timeAfter = DateTime.UtcNow;
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        @event.UpdateTime.ShouldBeInRange(timeBefore, timeAfter);
    }
    
    #endregion
    
    #region RecordActivityAsync Tests
    
    [Fact]
    public async Task RecordActivityAsync_WithActivityType_SetsCorrectFields()
    {
        // Arrange
        var activityType = "UserAction";
        var timeBefore = DateTime.UtcNow;
        
        // Act
        await _agent.RecordActivityAsync(activityType);
        var timeAfter = DateTime.UtcNow;
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentActivityUpdatedEvent>();
        @event.AgentId.ShouldBe(_testAgentId);
        @event.UserId.ShouldBe(_testUserId);
        @event.ActivityType.ShouldBe(activityType);
        @event.ActivityTime.ShouldBeInRange(timeBefore, timeAfter);
        _testAgent.ConfirmEventsCalled.ShouldBe(1);
    }
    
    [Fact]
    public async Task RecordActivityAsync_WithNullActivityType_SetsEmptyString()
    {
        // Act
        await _agent.RecordActivityAsync(null);
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentActivityUpdatedEvent>();
        @event.ActivityType.ShouldBe(string.Empty);
    }
    
    [Fact]
    public async Task RecordActivityAsync_WithoutParameter_SetsEmptyString()
    {
        // Act
        await _agent.RecordActivityAsync();
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentActivityUpdatedEvent>();
        @event.ActivityType.ShouldBe(string.Empty);
    }
    
    #endregion
    
    #region SetPropertyAsync Tests
    
    [Fact]
    public async Task SetPropertyAsync_CallsUpdatePropertiesWithMergeTrue()
    {
        // Arrange
        var key = "testKey";
        var value = "testValue";
        
        // Act
        await _agent.SetPropertyAsync(key, value);
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        @event.UpdatedProperties.Count.ShouldBe(1);
        @event.UpdatedProperties[key].ShouldBe(value);
        @event.WasMerged.ShouldBeTrue();
    }
    
    [Fact]
    public async Task SetPropertyAsync_WithEmptyKey_AllowsEmptyKey()
    {
        // Act
        await _agent.SetPropertyAsync("", "value");
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        @event.UpdatedProperties.ContainsKey("").ShouldBeTrue();
    }
    
    [Fact]
    public async Task SetPropertyAsync_WithSpecialCharacters_PreservesExactly()
    {
        // Arrange
        var key = "key!@#$";
        var value = "value%^&*()";
        
        // Act
        await _agent.SetPropertyAsync(key, value);
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        @event.UpdatedProperties[key].ShouldBe(value);
    }
    
    #endregion
    
    #region RemovePropertyAsync Tests
    
    [Fact]
    public async Task RemovePropertyAsync_ExistingProperty_CallsUpdateWithoutProperty()
    {
        // Arrange
        _testAgent.TestState.Properties = new Dictionary<string, string>
        {
            ["toRemove"] = "value",
            ["toKeep"] = "keepValue"
        };
        
        // Act
        await _agent.RemovePropertyAsync("toRemove");
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        @event.UpdatedProperties.ContainsKey("toRemove").ShouldBeFalse();
        @event.UpdatedProperties.ContainsKey("toKeep").ShouldBeTrue();
        @event.UpdatedProperties["toKeep"].ShouldBe("keepValue");
        @event.WasMerged.ShouldBeFalse();
    }
    
    [Fact]
    public async Task RemovePropertyAsync_NonExistentProperty_StillCallsUpdate()
    {
        // Arrange
        _testAgent.TestState.Properties = new Dictionary<string, string>
        {
            ["existing"] = "value"
        };
        
        // Act
        await _agent.RemovePropertyAsync("nonExistent");
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        @event.UpdatedProperties.Count.ShouldBe(1);
        @event.UpdatedProperties["existing"].ShouldBe("value");
    }
    
    #endregion
    
    #region BatchUpdateAsync Tests
    
    [Fact]
    public async Task BatchUpdateAsync_StatusOnly_RaisesOnlyStatusEvent()
    {
        // Act
        await _agent.BatchUpdateAsync(newStatus: AgentStatus.Active, statusReason: "Batch activation");
        
        // Assert
        _testAgent.RaisedEvents.Count.ShouldBe(1);
        _testAgent.RaisedEvents[0].ShouldBeOfType<AgentStatusChangedEvent>();
        _testAgent.ConfirmEventsCalled.ShouldBe(1);
    }
    
    [Fact]
    public async Task BatchUpdateAsync_PropertiesOnly_RaisesOnlyPropertiesEvent()
    {
        // Arrange
        var properties = new Dictionary<string, string> { ["key"] = "value" };
        
        // Act
        await _agent.BatchUpdateAsync(properties: properties);
        
        // Assert
        _testAgent.RaisedEvents.Count.ShouldBe(1);
        _testAgent.RaisedEvents[0].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
    }
    
    [Fact]
    public async Task BatchUpdateAsync_BothStatusAndProperties_RaisesBothEvents()
    {
        // Arrange
        var properties = new Dictionary<string, string> { ["key"] = "value" };
        
        // Act
        await _agent.BatchUpdateAsync(
            newStatus: AgentStatus.Active, 
            properties: properties,
            statusReason: "Batch update");
        
        // Assert
        _testAgent.RaisedEvents.Count.ShouldBe(2);
        _testAgent.RaisedEvents[0].ShouldBeOfType<AgentStatusChangedEvent>();
        _testAgent.RaisedEvents[1].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        _testAgent.ConfirmEventsCalled.ShouldBe(1); // Only one confirm for batch
    }
    
    [Fact]
    public async Task BatchUpdateAsync_AllNull_StillCallsConfirmEvents()
    {
        // Act
        await _agent.BatchUpdateAsync();
        
        // Assert
        _testAgent.RaisedEvents.Count.ShouldBe(0);
        _testAgent.ConfirmEventsCalled.ShouldBe(1);
    }
    
    [Fact]
    public async Task BatchUpdateAsync_EmptyProperties_DoesNotRaisePropertiesEvent()
    {
        // Act
        await _agent.BatchUpdateAsync(properties: new Dictionary<string, string>());
        
        // Assert
        _testAgent.RaisedEvents.Count.ShouldBe(0);
    }
    
    [Fact]
    public async Task BatchUpdateAsync_PropertiesWithMergeFalse_SetsCorrectly()
    {
        // Arrange
        _testAgent.TestState.Properties = new Dictionary<string, string>
        {
            ["old"] = "oldValue"
        };
        var newProps = new Dictionary<string, string>
        {
            ["new"] = "newValue"
        };
        
        // Act
        await _agent.BatchUpdateAsync(properties: newProps, mergeProperties: false);
        
        // Assert
        var @event = _testAgent.RaisedEvents[0].ShouldBeOfType<AgentPropertiesUpdatedEvent>();
        @event.WasMerged.ShouldBeFalse();
        @event.RemovedProperties.ShouldContain("old");
    }
    
    #endregion
    
    // Test implementation class
    private class TestMetaDataStateGAgent : IMetaDataStateGAgent<TestMetaDataState>
    {
        public List<MetaDataStateLogEvent> RaisedEvents { get; } = new();
        public int ConfirmEventsCalled { get; private set; }
        public TestMetaDataState TestState { get; }
        public GrainId TestGrainId { get; }
        
        public TestMetaDataStateGAgent(Guid agentId, Guid userId)
        {
            TestState = new TestMetaDataState
            {
                Id = agentId,
                UserId = userId,
                Status = AgentStatus.Creating,
                Properties = new Dictionary<string, string>(),
                CreateTime = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            };
            TestGrainId = GrainId.Parse($"agent/{agentId}");
        }
        
        public void RaiseEvent(MetaDataStateLogEvent @event)
        {
            RaisedEvents.Add(@event);
        }
        
        public Task ConfirmEvents()
        {
            ConfirmEventsCalled++;
            return Task.CompletedTask;
        }
        
        public TestMetaDataState GetState() => TestState;
        
        IMetaDataState IMetaDataStateGAgent.GetState() => TestState;
        
        public GrainId GetGrainId() => TestGrainId;
    }
    
    private class TestMetaDataState : IMetaDataState
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string AgentType { get; set; }
        public AgentStatus Status { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public GrainId AgentGrainId { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime LastActivity { get; set; }
    }
}