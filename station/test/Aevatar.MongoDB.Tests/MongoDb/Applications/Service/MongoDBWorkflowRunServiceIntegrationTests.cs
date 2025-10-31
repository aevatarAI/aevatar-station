using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Workflow.Core.Configs;
using Aevatar.MongoDB;
using Aevatar.Service;
using Aevatar.WorkflowRun;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using Xunit;

namespace Aevatar.MongoDb.Applications.Service;

/// <summary>
/// Integration tests for WorkflowRunService using real service injection with MongoDB
/// </summary>
[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class MongoDBWorkflowRunServiceIntegrationTests : AevatarApplicationTestBase<AevatarMongoDbTestModule>
{
    private readonly IWorkflowRunService _workflowRunService;
    private readonly IWorkflowViewService _workflowViewService;
    private readonly IAgentService _agentService;
    private readonly IdentityUserManager _identityUserManager;
    private readonly ICurrentUser _currentUser;

    public MongoDBWorkflowRunServiceIntegrationTests()
    {
        _workflowRunService = GetRequiredService<IWorkflowRunService>();
        _workflowViewService = GetRequiredService<IWorkflowViewService>();
        _agentService = GetRequiredService<IAgentService>();
        _identityUserManager = GetRequiredService<IdentityUserManager>();
        _currentUser = GetRequiredService<ICurrentUser>();
    }



    [Fact]
    public async Task GetAllWorkflowAgents_ShouldReturnAvailableAgentTypes()
    {
        // Arrange
        await _identityUserManager.CreateAsync(
            new IdentityUser(_currentUser.Id.Value, "getagents", "getagents@test.io"));

        // Act
        var agentTypes = await _workflowRunService.GetAllWorkflowAgents();

        // Assert
        agentTypes.ShouldNotBeNull();
        // Note: The list might be empty or contain agent types depending on the test environment
        // We just verify the method executes without error
    }

    [Fact]
    public async Task RunWorkflowAsync_WithSimpleWorkflow_ShouldExecuteSuccessfully()
    {
        // Arrange
        await _identityUserManager.CreateAsync(
            new IdentityUser(_currentUser.Id.Value, "runtest1", "runtest1@test.io"));

        // Create a workflow with single InputGAgent node (using exact DefaultWorkflowProperties format)
        var node1Id = Guid.NewGuid().ToString();
        var workflowConfigJson = "{\"workflowNodeList\":[{\"agentType\":\"Aevatar.GAgents.InputGAgent.GAgent.InputGAgentPlus\",\"name\":\"MyInputGAgent\",\"extendedData\":{\"xPosition\":\"2\",\"yPosition\":\"16\"},\"jsonProperties\":\"{\\\"memberName\\\":\\\"inputGAgent1\\\",\\\"input\\\":\\\"Test workflow execution\\\"}\",\"nodeId\":\"" + node1Id + "\"}],\"workflowNodeUnitList\":[],\"name\":\"test run workflow\"}";

        var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(workflowConfigJson);
        var workflow = await _agentService.CreateAgentAsync(new CreateAgentInputDto
        {
            AgentType = "Aevatar.GAgents.Workflow.WorkflowViewGAgentPlus",
            Name = "test run workflow",
            Properties = properties
        });

        // Act
        var result = await _workflowRunService.RunWorkflowAsync(new WorkflowRunRequestDto
        {
            ViewAgentId = workflow.Id
        });

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.WorkflowId.ShouldNotBe(Guid.Empty);
        result.PublishedAgent.ShouldNotBeNull();
        result.Message.ShouldContain("successfully");
    }

    [Fact]
    public async Task RunWorkflowAsync_WithConnectedNodes_ShouldExecuteSuccessfully()
    {
        // Arrange
        await _identityUserManager.CreateAsync(
            new IdentityUser(_currentUser.Id.Value, "runtopology", "runtopology@test.io"));

        // Create a workflow with two connected InputGAgent nodes
        var node1Id = Guid.NewGuid().ToString();
        var node2Id = Guid.NewGuid().ToString();
        var workflowConfigJson = "{\"workflowNodeList\":[{\"agentType\":\"Aevatar.GAgents.InputGAgent.GAgent.InputGAgentPlus\",\"name\":\"InputAgent1\",\"extendedData\":{\"xPosition\":\"2\",\"yPosition\":\"16\"},\"jsonProperties\":\"{\\\"memberName\\\":\\\"input1\\\",\\\"input\\\":\\\"First step\\\"}\",\"nodeId\":\"" + node1Id + "\"},{\"agentType\":\"Aevatar.GAgents.InputGAgent.GAgent.InputGAgentPlus\",\"name\":\"InputAgent2\",\"extendedData\":{\"xPosition\":\"365\",\"yPosition\":\"-12\"},\"jsonProperties\":\"{\\\"memberName\\\":\\\"input2\\\",\\\"input\\\":\\\"Second step\\\"}\",\"nodeId\":\"" + node2Id + "\"}],\"workflowNodeUnitList\":[{\"nodeId\":\"" + node1Id + "\",\"nextNodeId\":\"" + node2Id + "\"}],\"name\":\"connected run workflow\"}";

        var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(workflowConfigJson);
        var workflow = await _agentService.CreateAgentAsync(new CreateAgentInputDto
        {
            AgentType = "Aevatar.GAgents.Workflow.WorkflowViewGAgentPlus",
            Name = "connected run workflow",
            Properties = properties
        });

        // Act
        var result = await _workflowRunService.RunWorkflowAsync(new WorkflowRunRequestDto
        {
            ViewAgentId = workflow.Id
        });

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.WorkflowId.ShouldNotBe(Guid.Empty);
        result.PublishedAgent.ShouldNotBeNull();
        result.PublishedAgent!.Properties.ShouldContainKey("workflowCoordinatorGAgentId");
    }

}
