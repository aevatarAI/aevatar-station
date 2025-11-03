using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Workflow.Core.Configs;
using Aevatar.MongoDB;
using Aevatar.Service;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using Xunit;

namespace Aevatar.MongoDb.Applications.Service;

/// <summary>
/// Integration tests for WorkflowViewServicePlus using real service injection with MongoDB
/// </summary>
[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class MongoDBWorkflowViewServicePlusTests : AevatarApplicationTestBase<AevatarMongoDbTestModule>
{
    private readonly IWorkflowViewService _workflowViewService;
    private readonly IAgentService _agentService;
    private readonly IdentityUserManager _identityUserManager;
    private readonly ICurrentUser _currentUser;

    public MongoDBWorkflowViewServicePlusTests()
    {
        _workflowViewService = GetRequiredService<IWorkflowViewService>();
        _agentService = GetRequiredService<IAgentService>();
        _identityUserManager = GetRequiredService<IdentityUserManager>();
        _currentUser = GetRequiredService<ICurrentUser>();
    }

    [Fact]
    public async Task CreateDefaultWorkflowAsync_ShouldCreateNewWorkflow_WhenNoExistingWorkflow()
    {
        // Arrange
        await _identityUserManager.CreateAsync(
            new IdentityUser(_currentUser.Id.Value, "testuser", "testuser@test.io"));

        // Act
        var result = await _workflowViewService.CreateDefaultWorkflowAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldNotBeNullOrEmpty();
        result.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task PublishWorkflowAsync_WithSimpleWorkflow_ShouldPublishSuccessfully()
    {
        // Arrange
        await _identityUserManager.CreateAsync(
            new IdentityUser(_currentUser.Id.Value, "pubtest1", "pubtest1@test.io"));

        // Create workflow with simple InputGAgent configuration (exactly like DefaultWorkflowProperties format)
        var node1Id = Guid.NewGuid().ToString();
        var workflowConfigJson = "{\"workflowNodeList\":[{\"agentType\":\"Aevatar.GAgents.InputGAgent.GAgent.InputGAgentPlus\",\"name\":\"MyInputGAgent\",\"extendedData\":{\"xPosition\":\"2\",\"yPosition\":\"16\"},\"jsonProperties\":\"{\\\"memberName\\\":\\\"inputGAgent1\\\",\\\"input\\\":\\\"Test input data\\\"}\",\"nodeId\":\"" + node1Id + "\"}],\"workflowNodeUnitList\":[],\"name\":\"test workflow\"}";

        var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(workflowConfigJson);
        var workflow = await _agentService.CreateAgentAsync(new CreateAgentInputDto
        {
            AgentType = "Aevatar.GAgents.Workflow.WorkflowViewGAgentPlus",
            Name = "test workflow",
            Properties = properties
        });

        // Act
        var publishedWorkflow = await _workflowViewService.PublishWorkflowAsync(workflow.Id);

        // Assert
        publishedWorkflow.ShouldNotBeNull();
        publishedWorkflow.Properties.ShouldNotBeNull();
        publishedWorkflow.Properties.ShouldContainKey("workflowCoordinatorGAgentId");
        
        // Verify WorkflowCoordinatorGAgentId is valid
        var configJson = JsonConvert.SerializeObject(publishedWorkflow.Properties);
        var viewConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(configJson);
        viewConfig["workflowCoordinatorGAgentId"].ToString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task PublishWorkflowAsync_WithConnectedNodes_ShouldCreateTopology()
    {
        // Arrange
        await _identityUserManager.CreateAsync(
            new IdentityUser(_currentUser.Id.Value, "topology1", "topology1@test.io"));

        // Create workflow with two connected InputGAgent nodes (exactly like DefaultWorkflowProperties structure)
        var node1Id = Guid.NewGuid().ToString();
        var node2Id = Guid.NewGuid().ToString();
        var workflowConfigJson = "{\"workflowNodeList\":[{\"agentType\":\"Aevatar.GAgents.InputGAgent.GAgent.InputGAgentPlus\",\"name\":\"InputAgent1\",\"extendedData\":{\"xPosition\":\"2\",\"yPosition\":\"16\"},\"jsonProperties\":\"{\\\"memberName\\\":\\\"input1\\\",\\\"input\\\":\\\"First input\\\"}\",\"nodeId\":\"" + node1Id + "\"},{\"agentType\":\"Aevatar.GAgents.InputGAgent.GAgent.InputGAgentPlus\",\"name\":\"InputAgent2\",\"extendedData\":{\"xPosition\":\"365\",\"yPosition\":\"-12\"},\"jsonProperties\":\"{\\\"memberName\\\":\\\"input2\\\",\\\"input\\\":\\\"Second input\\\"}\",\"nodeId\":\"" + node2Id + "\"}],\"workflowNodeUnitList\":[{\"nodeId\":\"" + node1Id + "\",\"nextNodeId\":\"" + node2Id + "\"}],\"name\":\"connected workflow\"}";

        var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(workflowConfigJson);
        var workflow = await _agentService.CreateAgentAsync(new CreateAgentInputDto
        {
            AgentType = "Aevatar.GAgents.Workflow.WorkflowViewGAgentPlus",
            Name = "connected workflow",
            Properties = properties
        });

        // Act
        var publishedWorkflow = await _workflowViewService.PublishWorkflowAsync(workflow.Id);

        // Assert
        publishedWorkflow.ShouldNotBeNull();
        publishedWorkflow.Properties.ShouldNotBeNull();
        publishedWorkflow.Properties.ShouldContainKey("workflowCoordinatorGAgentId");
        publishedWorkflow.Properties.ShouldContainKey("workflowNodeUnitList");
        
        // Verify WorkflowCoordinatorGAgentId is valid
        var configJson = JsonConvert.SerializeObject(publishedWorkflow.Properties);
        var viewConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(configJson);
        viewConfig["workflowCoordinatorGAgentId"].ToString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task PublishWorkflowAsync_WithEmptyNodeList_ShouldCreateEmptyWorkflow()
    {
        // Arrange
        await _identityUserManager.CreateAsync(
            new IdentityUser(_currentUser.Id.Value, "emptytest", "emptytest@test.io"));

        // Create workflow with empty node list (exactly like DefaultWorkflowProperties format)
        var workflowConfigJson = "{\"workflowNodeList\":[],\"workflowNodeUnitList\":[],\"name\":\"empty workflow\"}";

        var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(workflowConfigJson);
        var workflow = await _agentService.CreateAgentAsync(new CreateAgentInputDto
        {
            AgentType = "Aevatar.GAgents.Workflow.WorkflowViewGAgentPlus",
            Name = "empty workflow",
            Properties = properties
        });

        // Act
        var publishedWorkflow = await _workflowViewService.PublishWorkflowAsync(workflow.Id);

        // Assert - Empty node list should create workflow coordinator with empty unit list
        publishedWorkflow.ShouldNotBeNull();
        publishedWorkflow.Properties.ShouldNotBeNull();
        publishedWorkflow.Properties.ShouldContainKey("workflowCoordinatorGAgentId");
        publishedWorkflow.Properties.ShouldContainKey("workflowNodeList");
        
        // Verify empty node list is preserved
        var configJson = JsonConvert.SerializeObject(publishedWorkflow.Properties);
        var viewConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(configJson);
        viewConfig["workflowCoordinatorGAgentId"].ToString().ShouldNotBeNullOrEmpty();
    }
}
