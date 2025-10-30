using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Workflow;
using Aevatar.GAgents.Workflow.Core;
using Aevatar.GAgents.Workflow.Core.Configs;
using Aevatar.GAgents.Workflow.Core.Events;
using Aevatar.GAgents.Workflow.Core.Models;
using Aevatar.GAgents.Workflow.Core.States;
using Aevatar.GAgents.Core;
using Orleans;
using Orleans.Providers;
using Shouldly;
using Xunit;
using Aevatar.GAgents.TestBase;

namespace Aevatar.GAgents.Workflow.Test.Tests;

/// <summary>
/// Integration tests for WorkflowCoordinatorGAgentPlus using Orleans TestCluster infrastructure.
/// Tests the BusinessAgentBase-based workflow coordinator functionality.
/// </summary>
[Collection(ClusterCollection.Name)]
public class WorkflowCoordinatorGAgentPlusIntegrationTest : AevatarWorkflowTestBase
{
    private readonly IGrainFactory _grainFactory;

    public WorkflowCoordinatorGAgentPlusIntegrationTest()
    {
        _grainFactory = GetRequiredService<IGrainFactory>();
    }

    #region Configuration Tests

    [Fact]
    public async Task GetDescriptionAsync_Should_ReturnCorrectDescription()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowCoordinatorGAgentPlus>(agentId);

        // Act
        var description = await agent.GetDescriptionAsync();

        // Assert
        description.ShouldContain("WorkflowCoordinatorGAgent");
        description.ShouldContain("Orchestrates complex workflow execution");
        description.ShouldContain("DAG-based task dependencies");
    }

    [Fact]
    public async Task ConfigAsync_Should_SetConfigurationCorrectly()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowCoordinatorGAgentPlus>(agentId);
        var config = new WorkflowCoordinatorConfigDto
        {
            InitContent = "Test workflow initialization",
            EnableExecutionRecord = true
        };

        // Act
        await agent.ConfigAsync(config);

        // Assert
        var state = await agent.GetStateAsync();
        state.Content.ShouldBe("Test workflow initialization");
    }

    [Fact]
    public async Task GetIsWorkflowAgentAsync_Should_ReturnTrue()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowCoordinatorGAgentPlus>(agentId);

        // Act
        var isWorkflowAgent = await agent.GetIsWorkflowAgentAsync();

        // Assert
        isWorkflowAgent.ShouldBeTrue(); // WorkflowCoordinator is a workflow agent
    }

    #endregion

    #region Workflow Lifecycle Tests

    [Fact]
    public async Task GetStartNodeAgentIdsAsync_Should_ReturnCorrectStartNodes()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowCoordinatorGAgentPlus>(agentId);
        
        // Create a workflow with clear start and end nodes
        var startNodeId = Guid.NewGuid();
        var middleNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        
        var config = new WorkflowCoordinatorConfigDto
        {
            WorkflowUnitList = new List<WorkflowUnitDto>
            {
                // Start node - has no incoming connections (not referenced as NextGrainId)
                new WorkflowUnitDto { GrainId = startNodeId.ToString(), NextGrainId = middleNodeId.ToString(), AgentName = "StartNode" },
                // Middle node - has incoming and outgoing connections
                new WorkflowUnitDto { GrainId = middleNodeId.ToString(), NextGrainId = endNodeId.ToString(), AgentName = "MiddleNode" },
                // End node - has incoming connection but no outgoing (NextGrainId is empty)
                new WorkflowUnitDto { GrainId = endNodeId.ToString(), NextGrainId = "", AgentName = "EndNode" }
            },
            InitContent = "Test workflow"
        };
        await agent.ConfigAsync(config);

        // Act
        var startNodeIds = await agent.GetStartNodeAgentIdsAsync();

        // Assert
        startNodeIds.ShouldNotBeNull();
        startNodeIds.Count.ShouldBe(1); // Should have exactly one start node
        startNodeIds[0].ShouldBe(startNodeId.ToString()); // Should be the node with no incoming connections
        
        // Verify the start node is not referenced as a NextGrainId by any other node
        var allNextGrainIds = config.WorkflowUnitList
            .Where(w => !string.IsNullOrEmpty(w.NextGrainId))
            .Select(w => w.NextGrainId)
            .ToList();
        allNextGrainIds.ShouldNotContain(startNodeId.ToString());
    }

    [Fact]
    public async Task GetStartNodeAgentIdsAsync_Should_ReturnMultipleStartNodes()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowCoordinatorGAgentPlus>(agentId);
        
        // Create a workflow with multiple start nodes (parallel workflow)
        var startNode1Id = Guid.NewGuid();
        var startNode2Id = Guid.NewGuid();
        var convergeNodeId = Guid.NewGuid();
        
        var config = new WorkflowCoordinatorConfigDto
        {
            WorkflowUnitList = new List<WorkflowUnitDto>
            {
                // Start node 1 - no incoming connections
                new WorkflowUnitDto { GrainId = startNode1Id.ToString(), NextGrainId = convergeNodeId.ToString(), AgentName = "StartNode1" },
                // Start node 2 - no incoming connections
                new WorkflowUnitDto { GrainId = startNode2Id.ToString(), NextGrainId = convergeNodeId.ToString(), AgentName = "StartNode2" },
                // Converge node - has incoming from both start nodes
                new WorkflowUnitDto { GrainId = convergeNodeId.ToString(), NextGrainId = "", AgentName = "ConvergeNode" }
            },
            InitContent = "Parallel workflow"
        };
        await agent.ConfigAsync(config);

        // Act
        var startNodeIds = await agent.GetStartNodeAgentIdsAsync();

        // Assert
        startNodeIds.ShouldNotBeNull();
        startNodeIds.Count.ShouldBe(2); // Should have exactly two start nodes
        startNodeIds.ShouldContain(startNode1Id.ToString());
        startNodeIds.ShouldContain(startNode2Id.ToString());
        startNodeIds.ShouldNotContain(convergeNodeId.ToString()); // Converge node is not a start node
    }

    [Fact]
    public async Task GetExecutionRecordIdAsync_Should_ReturnCorrectId()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowCoordinatorGAgentPlus>(agentId);
        var executionName = "TestExecution";
        
        // Act - Get execution record ID (should be empty if not found)
        var recordId = await agent.GetExecutionRecordIdAsync(executionName);

        // Assert
        recordId.ShouldBe(Guid.Empty); // No execution record exists yet
    }

    [Fact]
    public async Task HasExecutionAsync_Should_ReturnFalseForNonExistentExecution()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowCoordinatorGAgentPlus>(agentId);
        var executionName = "NonExistentExecution";
        
        // Act
        var hasExecution = await agent.HasExecutionAsync(executionName);

        // Assert
        hasExecution.ShouldBeFalse();
    }

    #endregion

    #region WorkflowCoordinator-Specific Workflow Event Handling Tests

    [Fact]
    public async Task OnBusinessAgentEventForwardingEventHandlerAsync_Should_ProcessWorkflowStartedEvent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = _grainFactory.GetGrain<ITestWorkflowCoordinatorGAgentPlus>(agentId);
        
        // Setup initial state with EnableRunRecord = true so RegisterExecutionRecordAsync returns non-empty GUID
        var setupConfig = new WorkflowCoordinatorConfigDto
        {
            WorkflowUnitList = new List<WorkflowUnitDto>(),
            InitContent = "Setup content",
            EnableExecutionRecord = true // CRITICAL: Must be true for CurrentExecutionRecordId to be set
        };
        await testAgent.ConfigAsync(setupConfig);
        
        // Agent ID and Workflow ID are the SAME GUID!
        var workflowEvent = new WorkflowEvent
        {
            WorkflowId = agentId, // Use the SAME GUID as the agent ID
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            WorkUnitAgentId = "Aevatar.GAgents.Workflow.WorkflowStartAgent/"+agentId.ToString("N"), // Required for topology discovery
            AgentName = "test", // Required for topology discovery - full type name
            Message = "Start workflow coordination",
            Metadata = new Dictionary<string, object>
            {
                ["ExecutionName"] = "TestExecution", // REQUIRED - will fail without this
                ["InitContent"] = "Test workflow content"
            }
        };

        // Act - Test WorkflowCoordinator-specific workflow event handling
        var result = await testAgent.HandleWorkflowEventAsync(workflowEvent);

        // Assert - Verify WorkflowCoordinator-specific behavior
        result.ShouldBeTrue();
        await Task.Delay(1000);
        // Validate agent state after WorkflowStarted event processing
        var state = await testAgent.GetStateAsync();
        state.ShouldNotBeNull();
        
        // State assertions based on WorkflowStartLogEvent transition:
        state.WorkflowStatus.ShouldBe(WorkflowCoordinatorStatus.InProgress); // Set by WorkflowStartLogEvent
        state.LastRunningTime.ShouldNotBe(default); // Set to DateTime.UtcNow
        state.CurrentExecutionRecordId.ShouldNotBe(Guid.Empty); // Set by RegisterExecutionRecordAsync
        state.CurrentExecutionName.ShouldBe("TestExecution"); // Set from metadata
        state.ExecutionRecords.ShouldContainKey("TestExecution"); // Added to dictionary
        state.RoundId.ShouldBeGreaterThan(0); // Incremented by 1
    }

    [Fact]
    public async Task OnBusinessAgentEventForwardingEventHandlerAsync_Should_ProcessWorkflowInProgressEvent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = _grainFactory.GetGrain<ITestWorkflowCoordinatorGAgentPlus>(agentId);
        
        // First setup some work units in the state
        var setupConfig = new WorkflowCoordinatorConfigDto
        {
            WorkflowUnitList = new List<WorkflowUnitDto>
            {
                new WorkflowUnitDto { GrainId = agentId.ToString(), NextGrainId = "" }
            },
            InitContent = "Setup content",
            EnableExecutionRecord = true
        };
        await testAgent.ConfigAsync(setupConfig);
        
        var workflowEvent = new WorkflowEvent
        {
            WorkflowId = agentId,
            WorkflowEventType = WorkflowEventType.WorkflowInProgress,
            WorkUnitAgentId = agentId.ToString(), // Must match a work unit AgentId
            Message = "Workflow in progress",
            ErrorMessage = string.Empty // No error - successful progress
        };

        // Act
        var result = await testAgent.HandleWorkflowEventAsync(workflowEvent);

        // Assert
        result.ShouldBeTrue();
        
        // Validate agent state after WorkflowInProgress event processing
        var state = await testAgent.GetStateAsync();
        state.ShouldNotBeNull();
        
        // State assertions based on FinishedWorkUnitLogEvent transition:
        // WorkflowInProgress doesn't change WorkflowStatus, just updates work unit status
        state.WorkflowStatus.ShouldBe(WorkflowCoordinatorStatus.Pending); // Unchanged
        // The work unit with matching AgentId should be marked as Finished
        var workUnit = state.CurrentWorkUnitInfos.FirstOrDefault(w => w.AgentId == agentId.ToString());
        workUnit.ShouldNotBeNull();
        workUnit.UnitStatusEnum.ShouldBe(WorkerUnitStatusEnum.Finished); // Updated by FinishedWorkUnitLogEvent
    }

    [Fact]
    public async Task OnBusinessAgentEventForwardingEventHandlerAsync_Should_ProcessWorkflowCompletedEvent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = _grainFactory.GetGrain<ITestWorkflowCoordinatorGAgentPlus>(agentId);
        
        // Setup initial state with some work units and execution tracking
        var setupConfig = new WorkflowCoordinatorConfigDto
        {
            WorkflowUnitList = new List<WorkflowUnitDto>
            {
                new WorkflowUnitDto { GrainId = agentId.ToString(), NextGrainId = "" }
            },
            InitContent = "Setup content",
            EnableExecutionRecord = true
        };
        await testAgent.ConfigAsync(setupConfig);
        
        var workflowEvent = new WorkflowEvent
        {
            WorkflowId = agentId,
            WorkflowEventType = WorkflowEventType.WorkflowCompleted,
            AgentName = "Aevatar.GAgents.Workflow.WorkflowEndAgent", // Used for logging - full type name
            Message = "Workflow completed successfully"
        };

        // Act
        var result = await testAgent.HandleWorkflowEventAsync(workflowEvent);

        // Assert
        result.ShouldBeTrue();
        
        // Validate agent state after WorkflowCompleted event processing
        var state = await testAgent.GetStateAsync();
        state.ShouldNotBeNull();
        
        // State assertions based on WorkflowFinishLogEvent transition:
        state.WorkflowStatus.ShouldBe(WorkflowCoordinatorStatus.Pending); // Reset to Pending
        state.CurrentExecutionRecordId.ShouldBe(Guid.Empty); // Cleared
        state.CurrentExecutionName.ShouldBeNull(); // Cleared
        // ExecutionRecords dictionary is preserved for historical tracking
        // Work units should be reset to Pending status
        foreach (var workUnit in state.CurrentWorkUnitInfos)
        {
            workUnit.UnitStatusEnum.ShouldBe(WorkerUnitStatusEnum.Pending);
        }
    }

    [Fact]
    public async Task OnBusinessAgentEventForwardingEventHandlerAsync_Should_ProcessWorkflowResetEvent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = _grainFactory.GetGrain<ITestWorkflowCoordinatorGAgentPlus>(agentId);
        
        // Setup initial state with work units and execution data
        var setupConfig = new WorkflowCoordinatorConfigDto
        {
            WorkflowUnitList = new List<WorkflowUnitDto>
            {
                new WorkflowUnitDto { GrainId = agentId.ToString(), NextGrainId = "" },
                new WorkflowUnitDto { GrainId = Guid.NewGuid().ToString(), NextGrainId = "" }
            },
            InitContent = "Setup content",
            EnableExecutionRecord = true
        };
        await testAgent.ConfigAsync(setupConfig);
        
        var workflowEvent = new WorkflowEvent
        {
            WorkflowId = agentId,
            WorkflowEventType = WorkflowEventType.WorkflowReset,
            Message = "Reset workflow coordination"
        };

        // Act
        var result = await testAgent.HandleWorkflowEventAsync(workflowEvent);

        // Assert
        result.ShouldBeTrue();
        
        // Validate agent state after WorkflowReset event processing
        var state = await testAgent.GetStateAsync();
        state.ShouldNotBeNull();
        
        // State assertions based on ResetWorkflowLogEvent transition:
        state.WorkflowStatus.ShouldBe(WorkflowCoordinatorStatus.Pending); // Reset to Pending
        state.CurrentWorkUnitInfos.Count.ShouldBe(0); // Cleared
        state.BackupWorkUnitInfos.Count.ShouldBe(0); // Cleared
        state.CurrentExecutionRecordId.ShouldBe(Guid.Empty); // Cleared
        state.CurrentExecutionName.ShouldBeNull(); // Cleared
        state.ExecutionRecords.Count.ShouldBe(0); // All execution history cleared
    }

    [Fact]
    public async Task OnBusinessAgentEventForwardingEventHandlerAsync_Should_ProcessWorkflowFailedEvent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var testAgent = _grainFactory.GetGrain<ITestWorkflowCoordinatorGAgentPlus>(agentId);
        
        var workflowEvent = new WorkflowEvent
        {
            WorkflowId = agentId,
            WorkflowEventType = WorkflowEventType.WorkflowFailed,
            AgentName = "Aevatar.GAgents.Workflow.WorkflowStartAgent", // Used for logging - full type name (failure during start)
            Message = "Workflow failed",
            ErrorMessage = "Test error occurred"
        };

        // Act
        var result = await testAgent.HandleWorkflowEventAsync(workflowEvent);

        // Assert
        result.ShouldBeTrue();
        
        // Validate agent state after WorkflowFailed event processing
        var state = await testAgent.GetStateAsync();
        state.ShouldNotBeNull();
        
        // State assertions based on WorkflowStartFailedLogEvent transition:
        state.WorkflowStatus.ShouldBe(WorkflowCoordinatorStatus.Failed); // Set to Failed
        state.LastRunningTime.ShouldNotBe(default); // Set to DateTime.UtcNow
        state.CurrentExecutionRecordId.ShouldBe(Guid.Empty); // Cleared
        state.CurrentExecutionName.ShouldBeNull(); // Cleared
    }


    #endregion

    #region Workflow Validation Tests

    [Fact]
    public async Task GetAllExecutionRecordsAsync_Should_ReturnEmptyDictionary()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowCoordinatorGAgentPlus>(agentId);
        
        // Act
        var executionRecords = await agent.GetAllExecutionRecordsAsync();

        // Assert
        executionRecords.ShouldNotBeNull();
        executionRecords.Count.ShouldBe(0); // No executions have been started
    }

    [Fact]
    public async Task WorkflowCoordinatorState_Should_InitializeCorrectly()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowCoordinatorGAgentPlus>(agentId);
        
        // Act
        var state = await agent.GetStateAsync();

        // Assert
        state.ShouldNotBeNull();
        state.WorkflowStatus.ShouldBe(WorkflowCoordinatorStatus.Pending);
        state.CurrentWorkUnitInfos.ShouldNotBeNull();
        state.CurrentWorkUnitInfos.Count.ShouldBe(0);
        state.ExecutionRecords.ShouldNotBeNull();
        state.ExecutionRecords.Count.ShouldBe(0);
    }

    #endregion

    #region State Management Tests

    [Fact]
    public async Task StateTransition_Should_HandleWorkflowConfiguration()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowCoordinatorGAgentPlus>(agentId);
        
        var config = new WorkflowCoordinatorConfigDto
        {
            WorkflowUnitList = new List<WorkflowUnitDto>
            {
                new WorkflowUnitDto { GrainId = Guid.NewGuid().ToString(), NextGrainId = "", AgentName = "TestNode" }
            },
            InitContent = "State transition test",
            EnableExecutionRecord = false
        };

        // Act
        await agent.ConfigAsync(config);

        // Assert
        var state = await agent.GetStateAsync();
        state.Content.ShouldBe("State transition test");
        state.EnableRunRecord.ShouldBeFalse();
    }

    [Fact]
    public async Task WorkflowCoordinatorState_Should_TrackExecutionRecords()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _grainFactory.GetGrain<IWorkflowCoordinatorGAgentPlus>(agentId);
        
        // Act - Get initial state
        var state = await agent.GetStateAsync();

        // Assert - Verify execution tracking capabilities
        state.ExecutionRecords.ShouldNotBeNull();
        state.CurrentExecutionRecordId.ShouldBe(Guid.Empty);
        state.CurrentExecutionName.ShouldBeNull();
        state.RoundId.ShouldBe(0);
    }

    #endregion

}

/// <summary>
/// Test extension interface for WorkflowCoordinatorGAgentPlus workflow event handling
/// </summary>
public interface ITestWorkflowCoordinatorGAgentPlus : IWorkflowCoordinatorGAgentPlus
{
    Task<bool> HandleWorkflowEventAsync(WorkflowEvent workflowEvent);
}

/// <summary>
/// Test extension for WorkflowCoordinatorGAgentPlus to expose protected methods for testing class-specific behavior
/// </summary>
[GAgent("test-workflow-coordinator-gagent-plus", "test")]
public class TestWorkflowCoordinatorGAgentPlus : WorkflowCoordinatorGAgentPlus, ITestWorkflowCoordinatorGAgentPlus
{
    public async Task<bool> HandleWorkflowEventAsync(WorkflowEvent workflowEvent)
    {
        await base.OnEventForwardingEventHandlerAsync(workflowEvent);
        return true;
    }
}

/// <summary>
/// Test workflow definition for integration testing
/// </summary>
public class WorkflowDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<WorkflowNode> Nodes { get; set; } = new();
}

/// <summary>
/// Test workflow node for integration testing
/// </summary>
public class WorkflowNode
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = new();
}
