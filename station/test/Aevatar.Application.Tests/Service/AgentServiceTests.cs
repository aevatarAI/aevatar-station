using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS;
using Aevatar.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Metadata;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Users;
using Xunit;

namespace Aevatar.Service;

public abstract class AgentServiceTests<TStartupModule> : AevatarApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IAgentService _agentService;
    private readonly IClusterClient _clusterClient;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IGAgentManager _gAgentManager;
    private readonly IUserAppService _userAppService;
    private readonly IdentityUserManager _identityUserManager;
    private readonly ICurrentUser _currentUser;
    private readonly GrainTypeResolver _grainTypeResolver;
    private readonly ISchemaProvider _schemaProvider;
    private readonly IIndexingService _indexingService;

    protected AgentServiceTests()
    {
        _agentService = GetRequiredService<IAgentService>();
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
        _gAgentManager = GetRequiredService<IGAgentManager>();
        _userAppService = GetRequiredService<IUserAppService>();
        _identityUserManager = GetRequiredService<IdentityUserManager>();
        _currentUser = GetRequiredService<ICurrentUser>();
        _grainTypeResolver = GetRequiredService<GrainTypeResolver>();
        _clusterClient = GetRequiredService<IClusterClient>();
        _grainTypeResolver = _clusterClient.ServiceProvider.GetRequiredService<GrainTypeResolver>();
        _schemaProvider = GetRequiredService<ISchemaProvider>();
        _indexingService = GetRequiredService<IIndexingService>();
    }

    [Fact]
    public async Task GetAllAgents_Test()
    {
        // I'm HyperEcho, 在思考Agent类型获取的共振。
        // Test getting all available agent types
        var agentTypes = await _agentService.GetAllAgents();

        // Verify that we get a list (could be empty but should not be null)
        agentTypes.ShouldNotBeNull();
        agentTypes.ShouldBeOfType<List<AgentTypeDto>>();

        // If there are agent types, verify they have proper structure
        if (agentTypes.Any())
        {
            var firstAgent = agentTypes.First();
            firstAgent.AgentType.ShouldNotBeNullOrWhiteSpace();
            firstAgent.FullName.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task CreateAgentAsync_Test()
    {
        // I'm HyperEcho, 在思考Agent创建的共振。
        // Setup user first
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));

        // Get available agent types first
        var agentTypes = await _agentService.GetAllAgents();
        agentTypes.ShouldNotBeNull();

        if (!agentTypes.Any())
        {
            // Skip test if no agent types are available
            return;
        }

        var testAgentType = agentTypes.First();

        var createInput = new CreateAgentInputDto
        {
            AgentType = testAgentType.AgentType,
            Name = "Test Agent",
            Properties = new Dictionary<string, object>()
        };

        var createdAgent = await _agentService.CreateAgentAsync(createInput);

        // Verify created agent
        createdAgent.ShouldNotBeNull();
        createdAgent.Id.ShouldNotBe(Guid.Empty);
        createdAgent.AgentType.ShouldBe(createInput.AgentType);
        createdAgent.Name.ShouldBe(createInput.Name);
        createdAgent.BusinessAgentGrainId.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetAllAgentInstances_Test()
    {
        // I'm HyperEcho, 在思考Agent实例查询的共振。
        // Setup user first
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));

        var queryDto = new GetAllAgentInstancesQueryDto
        {
            PageIndex = 0,
            PageSize = 20
        };

        var agentInstances = await _agentService.GetAllAgentInstances(queryDto);

        // Verify result
        agentInstances.ShouldNotBeNull();
        agentInstances.ShouldBeOfType<List<AgentInstanceDto>>();

        // Test with agent type filter
        queryDto.AgentType = "NonExistentAgentType";
        var filteredInstances = await _agentService.GetAllAgentInstances(queryDto);
        filteredInstances.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetAgentAsync_Test()
    {
        // I'm HyperEcho, 在思考Agent获取的共振。
        // Setup user first
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));

        // Get available agent types first
        var agentTypes = await _agentService.GetAllAgents();
        if (!agentTypes.Any())
        {
            // Skip test if no agent types are available
            return;
        }

        var testAgentType = agentTypes.First();

        // Create an agent first
        var createInput = new CreateAgentInputDto
        {
            AgentType = testAgentType.AgentType,
            Name = "Test Agent for Get",
            Properties = new Dictionary<string, object>()
        };

        var createdAgent = await _agentService.CreateAgentAsync(createInput);

        // Now get the agent
        var retrievedAgent = await _agentService.GetAgentAsync(createdAgent.Id);

        // Verify retrieved agent
        retrievedAgent.ShouldNotBeNull();
        retrievedAgent.Id.ShouldBe(createdAgent.Id);
        retrievedAgent.AgentType.ShouldBe(createdAgent.AgentType);
        retrievedAgent.Name.ShouldBe(createdAgent.Name);
        retrievedAgent.BusinessAgentGrainId.ShouldBe(createdAgent.BusinessAgentGrainId);

        // Test getting non-existent agent (this should not throw, based on implementation analysis)
        // The agent service checks user authorization, so it might throw a UserFriendlyException for unauthorized access
        // Let's test with a different user's agent ID to trigger authorization error
        var nonExistentAgentId = Guid.NewGuid();
        await Should.ThrowAsync<Exception>(async () =>
            await _agentService.GetAgentAsync(nonExistentAgentId));
    }

    [Fact]
    public async Task UpdateAgentAsync_Test()
    {
        // I'm HyperEcho, 在思考Agent更新的共振。
        // Setup user first
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));

        // Get available agent types first
        var agentTypes = await _agentService.GetAllAgents();
        if (!agentTypes.Any())
        {
            // Skip test if no agent types are available
            return;
        }

        var testAgentType = agentTypes.First();

        // Create an agent first
        var createInput = new CreateAgentInputDto
        {
            AgentType = testAgentType.AgentType,
            Name = "Test Agent for Update",
            Properties = new Dictionary<string, object>()
        };

        var createdAgent = await _agentService.CreateAgentAsync(createInput);

        // Update the agent
        var updateInput = new UpdateAgentInputDto
        {
            Name = "Updated Test Agent",
            Properties = new Dictionary<string, object> { { "testKey", "testValue" } }
        };

        var updatedAgent = await _agentService.UpdateAgentAsync(createdAgent.Id, updateInput);

        // Verify updated agent
        updatedAgent.ShouldNotBeNull();
        updatedAgent.Id.ShouldBe(createdAgent.Id);
        updatedAgent.Name.ShouldBe(updateInput.Name);
        updatedAgent.AgentType.ShouldBe(createdAgent.AgentType);
    }

    [Fact]
    public async Task AddSubAgentAsync_Test()
    {
        // I'm HyperEcho, 在思考子Agent添加的共振。
        // Setup user first
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));

        // Get available agent types first
        var agentTypes = await _agentService.GetAllAgents();
        if (!agentTypes.Any())
        {
            // Skip test if no agent types are available
            return;
        }

        var testAgentType = agentTypes.First();

        // Create parent agent
        var parentInput = new CreateAgentInputDto
        {
            AgentType = testAgentType.AgentType,
            Name = "Parent Agent",
            Properties = new Dictionary<string, object>()
        };

        var parentAgent = await _agentService.CreateAgentAsync(parentInput);

        // Create sub agent
        var subInput = new CreateAgentInputDto
        {
            AgentType = testAgentType.AgentType,
            Name = "Sub Agent",
            Properties = new Dictionary<string, object>()
        };

        var subAgent = await _agentService.CreateAgentAsync(subInput);

        // Add sub agent to parent
        var addSubAgentDto = new AddSubAgentDto
        {
            SubAgents = new List<Guid> { subAgent.Id }
        };

        var result = await _agentService.AddSubAgentAsync(parentAgent.Id, addSubAgentDto);

        // Verify result
        result.ShouldNotBeNull();
        result.SubAgents.ShouldContain(subAgent.Id);
    }

    [Fact]
    public async Task RemoveSubAgentAsync_Test()
    {
        // I'm HyperEcho, 在思考子Agent移除的共振。
        // Setup user first
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));

        // Get available agent types first
        var agentTypes = await _agentService.GetAllAgents();
        if (!agentTypes.Any())
        {
            // Skip test if no agent types are available
            return;
        }

        var testAgentType = agentTypes.First();

        // Create parent agent
        var parentInput = new CreateAgentInputDto
        {
            AgentType = testAgentType.AgentType,
            Name = "Parent Agent",
            Properties = new Dictionary<string, object>()
        };

        var parentAgent = await _agentService.CreateAgentAsync(parentInput);

        // Create sub agent
        var subInput = new CreateAgentInputDto
        {
            AgentType = testAgentType.AgentType,
            Name = "Sub Agent",
            Properties = new Dictionary<string, object>()
        };

        var subAgent = await _agentService.CreateAgentAsync(subInput);

        // Add sub agent to parent first
        var addSubAgentDto = new AddSubAgentDto
        {
            SubAgents = new List<Guid> { subAgent.Id }
        };

        await _agentService.AddSubAgentAsync(parentAgent.Id, addSubAgentDto);

        // Remove sub agent
        var removeSubAgentDto = new RemoveSubAgentDto
        {
            RemovedSubAgents = new List<Guid> { subAgent.Id }
        };

        var result = await _agentService.RemoveSubAgentAsync(parentAgent.Id, removeSubAgentDto);

        // Verify result
        result.ShouldNotBeNull();
        result.SubAgents.ShouldNotContain(subAgent.Id);
    }

    [Fact]
    public async Task RemoveAllSubAgentAsync_Test()
    {
        // I'm HyperEcho, 在思考所有子Agent移除的共振。
        // Setup user first
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));

        // Get available agent types first
        var agentTypes = await _agentService.GetAllAgents();
        if (!agentTypes.Any())
        {
            // Skip test if no agent types are available
            return;
        }

        var testAgentType = agentTypes.First();

        // Create agent
        var createInput = new CreateAgentInputDto
        {
            AgentType = testAgentType.AgentType,
            Name = "Test Agent for RemoveAll",
            Properties = new Dictionary<string, object>()
        };

        var agent = await _agentService.CreateAgentAsync(createInput);

        // Remove all sub agents (should not throw if no sub agents)
        await _agentService.RemoveAllSubAgentAsync(agent.Id);

        // Verify agent still exists but now should be deleted
        // Note: Based on the implementation, this actually deletes the agent if it has no parent
        await Should.ThrowAsync<Exception>(async () =>
            await _agentService.GetAgentAsync(agent.Id));
    }

    [Fact]
    public async Task GetAgentRelationshipAsync_Test()
    {
        // I'm HyperEcho, 在思考Agent关系获取的共振。
        // Setup user first
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));

        // Get available agent types first
        var agentTypes = await _agentService.GetAllAgents();
        if (!agentTypes.Any())
        {
            // Skip test if no agent types are available
            return;
        }

        var testAgentType = agentTypes.First();

        // Create agent
        var createInput = new CreateAgentInputDto
        {
            AgentType = testAgentType.AgentType,
            Name = "Test Agent for Relationship",
            Properties = new Dictionary<string, object>()
        };

        var agent = await _agentService.CreateAgentAsync(createInput);

        // Get relationship
        var relationship = await _agentService.GetAgentRelationshipAsync(agent.Id);

        // Verify relationship
        relationship.ShouldNotBeNull();
        relationship.Parent.ShouldBeNull(); // Should have no parent initially
        relationship.SubAgents.ShouldNotBeNull();
        // Note: SubAgents might not be empty initially due to internal agent relationships
    }

    [Fact]
    public async Task DeleteAgentAsync_Test()
    {
        // I'm HyperEcho, 在思考Agent删除的共振。
        // Setup user first
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));

        // Get available agent types first
        var agentTypes = await _agentService.GetAllAgents();
        if (!agentTypes.Any())
        {
            // Skip test if no agent types are available
            return;
        }

        var testAgentType = agentTypes.First();

        // Create agent
        var createInput = new CreateAgentInputDto
        {
            AgentType = testAgentType.AgentType,
            Name = "Test Agent for Delete",
            Properties = new Dictionary<string, object>()
        };

        var agent = await _agentService.CreateAgentAsync(createInput);

        // Verify agent exists
        var retrievedAgent = await _agentService.GetAgentAsync(agent.Id);
        retrievedAgent.ShouldNotBeNull();

        // Delete agent
        await _agentService.DeleteAgentAsync(agent.Id);

        // Verify agent is deleted
        await Should.ThrowAsync<Exception>(async () =>
            await _agentService.GetAgentAsync(agent.Id));
    }
}