// ABOUTME: This file contains integration tests using the real TestMetaDataAgent with Orleans
// ABOUTME: Tests demonstrate proper implementation of IMetaDataStateGAgent interface with event sourcing

using Aevatar.Core.Abstractions;
using Aevatar.MetaData.Enums;
using Aevatar.TestBase;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.MetaData.Tests;

/// <summary>
/// Real integration tests that use TestMetaDataAgent with full Orleans integration.
/// These tests demonstrate how to properly implement IMetaDataStateGAgent interface.
/// </summary>
[Collection(ClusterCollection.Name)]
public class IMetaDataStateGAgentRealIntegrationTests : AevatarMetaDataTestBase
{
    private readonly ITestOutputHelper _outputHelper;

    public IMetaDataStateGAgentRealIntegrationTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task TestMetaDataAgent_Should_ImplementIMetaDataStateGAgent_Correctly()
    {
        // Arrange - Get a real TestMetaDataAgent grain from Orleans
        var agentId = Guid.NewGuid();
        var agent = await GetGrainAsync<ITestMetaDataAgent>(agentId);
        
        // Act - Test that the agent implements IMetaDataStateGAgent
        // Since TestMetaDataAgent now inherits from IMetaDataStateGAgent<TestMetaDataAgentState>,
        // we can cast it and use the metadata operations
        
        // Assert - Verify the agent is accessible
        agent.ShouldNotBeNull();
        
        // Verify the agent can provide description
        var description = await agent.GetDescriptionAsync();
        description.ShouldNotBeNullOrEmpty();
        description.ShouldBe("Test agent for metadata operations with Orleans integration testing");
        
        // Verify the agent can access state
        var state = await agent.GetStateAsync();
        state.ShouldNotBeNull();
        state.ShouldBeOfType<TestMetaDataAgentState>();
    }

    [Fact]
    public async Task CreateAgentAsync_Should_ApplyAgentCreatedEvent_AndUpdateState()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agentName = "TestAgent";
        var agentType = "TestType";
        var properties = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };
        
        var agent = await GetGrainAsync<ITestMetaDataAgent>(agentId);
        var metadataAgent = agent as IMetaDataStateGAgent<TestMetaDataAgentState>;
        metadataAgent.ShouldNotBeNull();
        
        // Act
        await metadataAgent.CreateAgentAsync(agentId, userId, agentName, agentType, properties);
        
        // Assert
        var state = await agent.GetStateAsync();
        state.Id.ShouldBe(agentId);
        state.UserId.ShouldBe(userId);
        state.Name.ShouldBe(agentName);
        state.AgentType.ShouldBe(agentType);
        state.Properties.ShouldContainKeyAndValue("key1", "value1");
        state.Properties.ShouldContainKeyAndValue("key2", "value2");
        state.Status.ShouldBe(AgentStatus.Creating);
        state.CreateTime.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
        state.LastActivity.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
        
        // Verify test tracking
        var testEventCount = await agent.GetTestEventCountAsync();
        testEventCount.ShouldBeGreaterThan(0);
        
        var testMessages = await agent.GetTestMessagesAsync();
        testMessages.ShouldContain(msg => msg.Contains("AgentCreatedEvent"));
    }

    [Fact]
    public async Task UpdateStatusAsync_Should_ApplyAgentStatusChangedEvent_AndUpdateState()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agent = await GetGrainAsync<ITestMetaDataAgent>(agentId);
        var metadataAgent = agent as IMetaDataStateGAgent<TestMetaDataAgentState>;
        metadataAgent.ShouldNotBeNull();
        
        // First create an agent
        await metadataAgent.CreateAgentAsync(agentId, userId, "TestAgent", "TestType");
        
        // Act
        await metadataAgent.UpdateStatusAsync(AgentStatus.Active, "Started processing");
        
        // Assert
        var state = await agent.GetStateAsync();
        state.Status.ShouldBe(AgentStatus.Active);
        state.LastActivity.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
        
        // Verify test tracking
        var testMessages = await agent.GetTestMessagesAsync();
        testMessages.ShouldContain(msg => msg.Contains("AgentStatusChangedEvent"));
    }

    [Fact]
    public async Task UpdatePropertiesAsync_Should_ApplyAgentPropertiesUpdatedEvent_AndMergeProperties()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agent = await GetGrainAsync<ITestMetaDataAgent>(agentId);
        var metadataAgent = agent as IMetaDataStateGAgent<TestMetaDataAgentState>;
        metadataAgent.ShouldNotBeNull();
        
        var initialProperties = new Dictionary<string, string>
        {
            ["initial1"] = "value1",
            ["initial2"] = "value2"
        };
        
        // First create an agent with initial properties
        await metadataAgent.CreateAgentAsync(agentId, userId, "TestAgent", "TestType", initialProperties);
        
        var updatedProperties = new Dictionary<string, string>
        {
            ["initial1"] = "updatedValue1", // Update existing
            ["new1"] = "newValue1" // Add new
        };
        
        // Act - Merge properties (default behavior)
        await metadataAgent.UpdatePropertiesAsync(updatedProperties, merge: true);
        
        // Assert
        var state = await agent.GetStateAsync();
        state.Properties.ShouldContainKeyAndValue("initial1", "updatedValue1"); // Updated
        state.Properties.ShouldContainKeyAndValue("initial2", "value2"); // Preserved
        state.Properties.ShouldContainKeyAndValue("new1", "newValue1"); // Added
        state.LastActivity.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
        
        // Verify test tracking
        var testMessages = await agent.GetTestMessagesAsync();
        testMessages.ShouldContain(msg => msg.Contains("AgentPropertiesUpdatedEvent"));
    }

    [Fact]
    public async Task UpdatePropertiesAsync_Should_ApplyAgentPropertiesUpdatedEvent_AndReplaceProperties()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agent = await GetGrainAsync<ITestMetaDataAgent>(agentId);
        var metadataAgent = agent as IMetaDataStateGAgent<TestMetaDataAgentState>;
        metadataAgent.ShouldNotBeNull();
        
        var initialProperties = new Dictionary<string, string>
        {
            ["initial1"] = "value1",
            ["initial2"] = "value2"
        };
        
        // First create an agent with initial properties
        await metadataAgent.CreateAgentAsync(agentId, userId, "TestAgent", "TestType", initialProperties);
        
        var replacementProperties = new Dictionary<string, string>
        {
            ["new1"] = "newValue1",
            ["new2"] = "newValue2"
        };
        
        // Act - Replace properties
        await metadataAgent.UpdatePropertiesAsync(replacementProperties, merge: false);
        
        // Assert
        var state = await agent.GetStateAsync();
        state.Properties.ShouldNotContainKey("initial1"); // Removed
        state.Properties.ShouldNotContainKey("initial2"); // Removed
        state.Properties.ShouldContainKeyAndValue("new1", "newValue1"); // Added
        state.Properties.ShouldContainKeyAndValue("new2", "newValue2"); // Added
        state.LastActivity.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task RecordActivityAsync_Should_ApplyAgentActivityUpdatedEvent_AndUpdateLastActivity()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agent = await GetGrainAsync<ITestMetaDataAgent>(agentId);
        var metadataAgent = agent as IMetaDataStateGAgent<TestMetaDataAgentState>;
        metadataAgent.ShouldNotBeNull();
        
        // First create an agent
        await metadataAgent.CreateAgentAsync(agentId, userId, "TestAgent", "TestType");
        var stateBeforeActivity = await agent.GetStateAsync();
        
        // Wait a moment to ensure timestamp difference
        await Task.Delay(10);
        
        // Act
        await metadataAgent.RecordActivityAsync("Processing task");
        
        // Assert
        var state = await agent.GetStateAsync();
        state.LastActivity.ShouldBeGreaterThan(stateBeforeActivity.LastActivity);
        
        // Verify test tracking
        var testMessages = await agent.GetTestMessagesAsync();
        testMessages.ShouldContain(msg => msg.Contains("AgentActivityUpdatedEvent"));
    }

    [Fact]
    public async Task BatchUpdateAsync_Should_ApplyMultipleEvents_InSingleOperation()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agent = await GetGrainAsync<ITestMetaDataAgent>(agentId);
        var metadataAgent = agent as IMetaDataStateGAgent<TestMetaDataAgentState>;
        metadataAgent.ShouldNotBeNull();
        
        var initialProperties = new Dictionary<string, string>
        {
            ["initial1"] = "value1"
        };
        
        // First create an agent
        await metadataAgent.CreateAgentAsync(agentId, userId, "TestAgent", "TestType", initialProperties);
        
        var updatedProperties = new Dictionary<string, string>
        {
            ["batch1"] = "batchValue1",
            ["batch2"] = "batchValue2"
        };
        
        // Act - Batch update with both status and properties
        await metadataAgent.BatchUpdateAsync(
            newStatus: AgentStatus.Active,
            properties: updatedProperties,
            mergeProperties: true,
            statusReason: "Batch update test");
        
        // Assert
        var state = await agent.GetStateAsync();
        state.Status.ShouldBe(AgentStatus.Active);
        state.Properties.ShouldContainKeyAndValue("initial1", "value1"); // Preserved
        state.Properties.ShouldContainKeyAndValue("batch1", "batchValue1"); // Added
        state.Properties.ShouldContainKeyAndValue("batch2", "batchValue2"); // Added
        state.LastActivity.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
        
        // Verify test tracking - should have both status and properties events
        var testMessages = await agent.GetTestMessagesAsync();
        testMessages.ShouldContain(msg => msg.Contains("AgentStatusChangedEvent"));
        testMessages.ShouldContain(msg => msg.Contains("AgentPropertiesUpdatedEvent"));
    }

    [Fact]
    public async Task TestMetaDataAgent_Should_SupportMetaDataOperations_ThroughInterface()
    {
        // Arrange - Get a real TestMetaDataAgent grain
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agent = await GetGrainAsync<ITestMetaDataAgent>(agentId);
        
        // Since TestMetaDataAgent implements IMetaDataStateGAgent<TestMetaDataAgentState>,
        // we can use it through the interface for metadata operations
        var metaDataAgent = agent as IMetaDataStateGAgent<TestMetaDataAgentState>;
        metaDataAgent.ShouldNotBeNull();
        
        // Act - Test metadata operations through the interface
        await metaDataAgent.CreateAgentAsync(
            id: agentId,
            userId: userId,
            name: "Test Agent",
            agentType: "TestAgent",
            properties: new Dictionary<string, string> { { "test", "value" } }
        );
        
        // Update status
        await metaDataAgent.UpdateStatusAsync(AgentStatus.Active, "Activated for testing");
        
        // Update properties
        await metaDataAgent.SetPropertyAsync("newProperty", "newValue");
        
        // Record activity
        await metaDataAgent.RecordActivityAsync("test_activity");
        
        // Assert - Verify the operations were recorded (they're converted to test events)
        var state = await agent.GetStateAsync();
        state.ShouldNotBeNull();
        
        // The metadata operations should have generated test events
        var eventCount = await agent.GetTestEventCountAsync();
        eventCount.ShouldBeGreaterThan(0);
        
        var messages = await agent.GetTestMessagesAsync();
        messages.ShouldNotBeEmpty();
        
        // Should contain messages from metadata events
        messages.ShouldContain(m => m.Contains("AgentCreatedEvent"));
        messages.ShouldContain(m => m.Contains("AgentStatusChangedEvent"));
        messages.ShouldContain(m => m.Contains("AgentPropertiesUpdatedEvent"));
        messages.ShouldContain(m => m.Contains("AgentActivityUpdatedEvent"));
    }

    [Fact]
    public async Task TestMetaDataAgent_Should_HandleBatchUpdate_ThroughInterface()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agent = await GetGrainAsync<ITestMetaDataAgent>(agentId);
        var metaDataAgent = agent as IMetaDataStateGAgent<TestMetaDataAgentState>;
        
        // Initialize the agent first
        await metaDataAgent!.CreateAgentAsync(agentId, userId, "Batch Test Agent", "TestAgent");
        
        // Clear any initial events
        await agent.ClearTestDataAsync();
        
        // Act - Perform batch update
        var properties = new Dictionary<string, string>
        {
            { "property1", "value1" },
            { "property2", "value2" }
        };
        
        await metaDataAgent.BatchUpdateAsync(
            newStatus: AgentStatus.Active,
            properties: properties,
            statusReason: "Batch activation"
        );
        
        // Assert - Verify batch operations were recorded
        var eventCount = await agent.GetTestEventCountAsync();
        eventCount.ShouldBeGreaterThan(0);
        
        var messages = await agent.GetTestMessagesAsync();
        messages.ShouldNotBeEmpty();
        
        // Should contain messages from both status and properties events
        messages.ShouldContain(m => m.Contains("AgentStatusChangedEvent"));
        messages.ShouldContain(m => m.Contains("AgentPropertiesUpdatedEvent"));
    }

    [Fact]
    public async Task TestMetaDataAgent_Should_ProvideCorrectGrainId()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = await GetGrainAsync<ITestMetaDataAgent>(agentId);
        var metaDataAgent = agent as IMetaDataStateGAgent<TestMetaDataAgentState>;
        
        // Act - Get grain ID through the interface
        var grainId = await metaDataAgent!.GetGrainIdAsync();
        
        // Assert - Verify grain ID is valid and contains the agent ID
        grainId.ShouldNotBe(default(GrainId));
        grainId.ToString().ShouldContain(agentId.ToString());
    }

    [Fact]
    public async Task TestMetaDataAgent_Should_ProvideStateAccess_ThroughInterface()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agent = await GetGrainAsync<ITestMetaDataAgent>(agentId);
        var metaDataAgent = agent as IMetaDataStateGAgent<TestMetaDataAgentState>;
        
        // Initialize with some data
        await metaDataAgent!.CreateAgentAsync(agentId, userId, "State Test Agent", "TestAgent");
        
        // Act - Get state through the interface
        var state = metaDataAgent.GetState();
        
        // Assert - Verify state access works correctly
        state.ShouldNotBeNull();
        state.ShouldBeOfType<TestMetaDataAgentState>();
        
        // Verify we can also get it as IMetaDataState
        var baseMetaDataAgent = metaDataAgent as IMetaDataStateGAgent;
        var baseState = await baseMetaDataAgent!.GetState();
        baseState.ShouldNotBeNull();
        baseState.ShouldBeAssignableTo<IMetaDataState>();
        baseState.ShouldBeSameAs(state);
    }

    [Fact]
    public async Task TestMetaDataAgent_Should_HandlePropertyOperations_Correctly()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agent = await GetGrainAsync<ITestMetaDataAgent>(agentId);
        var metaDataAgent = agent as IMetaDataStateGAgent<TestMetaDataAgentState>;
        
        // Initialize the agent
        await metaDataAgent!.CreateAgentAsync(agentId, userId, "Property Test Agent", "TestAgent");
        await agent.ClearTestDataAsync();
        
        // Act - Test property operations
        await metaDataAgent.SetPropertyAsync("key1", "value1");
        await metaDataAgent.SetPropertyAsync("key2", "value2");
        await metaDataAgent.RemovePropertyAsync("key1");
        
        // Assert - Verify property operations were recorded as events
        var eventCount = await agent.GetTestEventCountAsync();
        eventCount.ShouldBe(3); // Three property operations
        
        var messages = await agent.GetTestMessagesAsync();
        messages.Count.ShouldBe(3);
        
        // All should be property update events
        messages.ShouldAllBe(m => m.Contains("AgentPropertiesUpdatedEvent"));
    }
}