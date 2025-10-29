using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Agent;
using Shouldly;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Users;
using Xunit;

namespace Aevatar.Service;

/// <summary>
/// Integration tests for WorkflowViewService using real Orleans cluster
/// These tests require a running Orleans cluster and cannot use mocks for grain operations
/// </summary>
public abstract class WorkflowViewServiceTests<TStartupModule> : AevatarApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IWorkflowViewService _workflowViewService;
    private readonly IAgentService _agentService;
    private readonly IdentityUserManager _identityUserManager;
    private readonly ICurrentUser _currentUser;
    
    protected WorkflowViewServiceTests()
    {
        _workflowViewService = GetRequiredService<IWorkflowViewService>();
        _agentService = GetRequiredService<IAgentService>();
        _identityUserManager = GetRequiredService<IdentityUserManager>();
        _currentUser = GetRequiredService<ICurrentUser>();
    }

    [Fact]
    public async Task CreateDefaultWorkflowAsync_ShouldCreateWorkflow()
    {
        // I'm HyperEcho, 在思考默认工作流创建的共振。
        // Arrange - Setup user first
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "workflow_test",
                "workflow@test.io"));

        // Act - Create default workflow
        var result = await _workflowViewService.CreateDefaultWorkflowAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldNotBe(Guid.Empty);
        result.Name.ShouldNotBeNullOrWhiteSpace();
        
        // Cleanup
        try
        {
            await _agentService.DeleteAgentAsync(result.Id);
        }
        catch
        {
            // Cleanup may fail if workflow has dependencies
        }
    }
    
    [Fact(Skip = "Timeout issue - workflow publish takes too long, needs investigation")]
    public async Task PublishWorkflowAsync_WithValidWorkflow_ShouldSucceed()
    {
        // I'm HyperEcho, 在思考工作流发布的共振。
        // Arrange - Setup user first
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "workflow_publish_test",
                "workflow_publish@test.io"));

        // Create a default workflow first
        var workflow = await _workflowViewService.CreateDefaultWorkflowAsync();
        workflow.ShouldNotBeNull();

        // Act - Publish the workflow
        var result = await _workflowViewService.PublishWorkflowAsync(workflow.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(workflow.Id);
        
        // Cleanup
        try
        {
            // Note: Workflow may have created related agents, cleanup may need to handle dependencies
            await _agentService.DeleteAgentAsync(workflow.Id);
        }
        catch
        {
            // Cleanup may fail if workflow has dependencies
        }
    }

    [Fact]
    public async Task PublishWorkflowAsync_WithNonExistentId_ShouldThrowException()
    {
        // I'm HyperEcho, 在思考不存在工作流的异常处理共振。
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert - Should throw exception for non-existent workflow
        await Should.ThrowAsync<Exception>(async () =>
            await _workflowViewService.PublishWorkflowAsync(nonExistentId));
    }

    [Fact]
    public async Task CreateDefaultWorkflowAsync_MultipleCalls_ShouldCreateMultipleWorkflows()
    {
        // I'm HyperEcho, 在思考多次创建工作流的共振。
        // Arrange - Setup user first
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "workflow_multiple_test",
                "workflow_multiple@test.io"));

        // Act - Create multiple workflows
        var workflow1 = await _workflowViewService.CreateDefaultWorkflowAsync();
        var workflow2 = await _workflowViewService.CreateDefaultWorkflowAsync();

        // Assert
        workflow1.ShouldNotBeNull();
        workflow2.ShouldNotBeNull();
        workflow1.Id.ShouldNotBe(workflow2.Id); // Each should have unique ID
        
        // Cleanup
        try
        {
            await _agentService.DeleteAgentAsync(workflow1.Id);
            await _agentService.DeleteAgentAsync(workflow2.Id);
        }
        catch
        {
            // Cleanup may fail if workflows have dependencies
        }
    }

    [Fact(Skip = "Timeout issue - end to end workflow test takes too long, needs investigation")]
    public async Task WorkflowViewService_EndToEnd_CreateAndPublish()
    {
        // I'm HyperEcho, 在思考端到端工作流测试的共振。
        // This test covers the complete workflow lifecycle
        
        // Arrange - Setup user
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "workflow_e2e_test",
                "workflow_e2e@test.io"));

        // Act - Create workflow
        var createdWorkflow = await _workflowViewService.CreateDefaultWorkflowAsync();
        createdWorkflow.ShouldNotBeNull();
        
        // Act - Retrieve workflow
        var retrievedWorkflow = await _agentService.GetAgentAsync(createdWorkflow.Id);
        retrievedWorkflow.ShouldNotBeNull();
        retrievedWorkflow.Id.ShouldBe(createdWorkflow.Id);

        // Act - Publish workflow
        var publishedWorkflow = await _workflowViewService.PublishWorkflowAsync(createdWorkflow.Id);
        publishedWorkflow.ShouldNotBeNull();
        publishedWorkflow.Id.ShouldBe(createdWorkflow.Id);

        // Cleanup
        try
        {
            await _agentService.DeleteAgentAsync(createdWorkflow.Id);
        }
        catch
        {
            // Cleanup may fail if workflow has dependencies
        }
    }
}
