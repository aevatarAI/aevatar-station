using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Agents.Creator;
using Aevatar.Application.Grains.Agents.Creator;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS;
using Aevatar.CQRS.Provider;
using Aevatar.Cqrs.Tests.Cqrs.Dto;
using Aevatar.GAgent.Dto;
using Aevatar.Query;
using Aevatar.Station.Feature.CreatorGAgent;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Orleans.Runtime;
using Shouldly;
using Xunit;

namespace Aevatar.Cqrs.Tests;

/// <summary>
/// Integration tests for AevatarStateProjector using real service injection
/// Similar to CqrsProviderTest.cs - uses actual dependencies instead of mocks
/// </summary>
public class AevatarStateProjectorIntegrationTests : AevatarTestBase<AevatarCqrsTestModule>
{
    private readonly AevatarStateProjector _stateProjector;
    private readonly ICQRSProvider _cqrsProvider;
    private readonly IIndexingService _indexingService;

    public AevatarStateProjectorIntegrationTests()
    {
        _stateProjector = GetRequiredService<AevatarStateProjector>();
        _cqrsProvider = GetRequiredService<ICQRSProvider>();
        _indexingService = GetRequiredService<IIndexingService>();
    }

    [Fact]
    public async Task ProjectAsync_LegacyStateWrapper_ShouldSaveToElastic()
    {
        // Arrange
        var grainId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var state = new CqrsTestAgentState
        {
            Id = grainId,
            AgentName = "Test Agent Legacy",
            AgentCount = 10,
            GroupId = groupId.ToString(),
            AgentIds = new List<string> { "agent1", "agent2" },
            AgentTypeDictionary = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            }
        };

        var wrapper = new StateWrapper<CqrsTestAgentState>(
            GrainId.Create("test", grainId.ToString("N")), 
            state, 
            1);

        // Act
        await _stateProjector.ProjectAsync(wrapper);
        await _stateProjector.FlushAsync();

        // Wait for indexing
        await Task.Delay(500);

        // Assert - Query to verify the state was saved
        var result = await _cqrsProvider.QueryStateAsync(
            nameof(CqrsTestAgentState),
            q => q.Term(t => t.Field("id").Value(grainId.ToString())),
            0,
            10
        );

        result.ShouldNotBeNullOrEmpty();
        var stateDto = JsonConvert.DeserializeObject<List<CqrsTestAgentStateDto>>(result);
        stateDto.ShouldNotBeNull();
        stateDto.Count.ShouldBeGreaterThan(0);
        stateDto[0].Id.ShouldBe(grainId.ToString());
        stateDto[0].AgentName.ShouldBe("Test Agent Legacy");
    }

    [Fact]
    public async Task ProjectAsync_PlusStateWrapper_ShouldSaveToElastic()
    {
        // Arrange
        var grainId = Guid.NewGuid();
        var state = new CreatorGAgentState
        {
            Id = grainId,
            Name = "Test Creator Agent Plus",
            UserId = Guid.NewGuid(),
            AgentType = "TestAgentType",
            Properties = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                { "Name", "Test Configuration" }
            }),
            BusinessAgentGrainId = GrainId.Create("TestGrain", Guid.NewGuid().ToString("N"))
        };

        // Note: CreatorGAgentState extends GroupAgentState which extends StateBase, not CoreStateBase
        var wrapper = new StateWrapper<CreatorGAgentState>(
            GrainId.Create("CreatorGAgent", grainId.ToString("N")), 
            state, 
            1);

        // Act
        await _stateProjector.ProjectAsync(wrapper);
        await _stateProjector.FlushAsync(); // Use FlushAsync instead of FlushPlusAsync

        // Wait for indexing
        await Task.Delay(500);

        // Assert - Query to verify the state was saved
        var result = await _cqrsProvider.QueryStateAsync(
            nameof(CreatorGAgentState),
            q => q.Term(t => t.Field("id").Value(grainId.ToString())),
            0,
            10
        );

        result.ShouldNotBeNullOrEmpty();
        // Just verify we got results back - CreatorGAgentState is complex
        result.ShouldContain(grainId.ToString());
    }

    [Fact]
    public async Task ProjectAsync_MultipleStates_ShouldBatchAndSave()
    {
        // Arrange
        var groupId = Guid.NewGuid(); // Single groupId for all states
        var states = new List<StateWrapper<CqrsTestAgentState>>();
        for (int i = 0; i < 5; i++)
        {
            var grainId = Guid.NewGuid();
            var state = new CqrsTestAgentState
            {
                Id = grainId,
                AgentName = $"Batch Test Agent {i}",
                AgentCount = i + 1,
                GroupId = groupId.ToString() // Use same groupId
            };

            states.Add(new StateWrapper<CqrsTestAgentState>(
                GrainId.Create("test", grainId.ToString("N")), 
                state, 
                i + 1));
        }

        // Act
        foreach (var wrapper in states)
        {
            await _stateProjector.ProjectAsync(wrapper);
        }
        await _stateProjector.FlushAsync();

        // Wait for indexing
        await Task.Delay(1000);

        // Assert - Query to verify all states were saved
        var result = await _cqrsProvider.QueryStateAsync(
            nameof(CqrsTestAgentState),
            q => q.Term(t => t.Field("groupId").Value(groupId.ToString())),
            0,
            10
        );

        result.ShouldNotBeNullOrEmpty();
        var stateDtoList = JsonConvert.DeserializeObject<List<CqrsTestAgentStateDto>>(result);
        stateDtoList.ShouldNotBeNull();
        stateDtoList.Count.ShouldBe(5);
        stateDtoList.All(s => s.GroupId == groupId.ToString()).ShouldBeTrue();
    }

    [Fact]
    public async Task ProjectAsync_SameGrainMultipleVersions_ShouldKeepLatestVersion()
    {
        // Arrange
        var grainId = Guid.NewGuid();
        
        var state1 = new CqrsTestAgentState
        {
            Id = grainId,
            AgentName = "Version 1",
            AgentCount = 1
        };
        var wrapper1 = new StateWrapper<CqrsTestAgentState>(
            GrainId.Create("test", grainId.ToString("N")), 
            state1, 
            1);

        var state2 = new CqrsTestAgentState
        {
            Id = grainId,
            AgentName = "Version 2",
            AgentCount = 2
        };
        var wrapper2 = new StateWrapper<CqrsTestAgentState>(
            GrainId.Create("test", grainId.ToString("N")), 
            state2, 
            2);

        // Act
        await _stateProjector.ProjectAsync(wrapper1);
        await _stateProjector.ProjectAsync(wrapper2);
        await _stateProjector.FlushAsync();

        // Wait for indexing
        await Task.Delay(500);

        // Assert - Query to verify only the latest version is saved
        var result = await _cqrsProvider.QueryStateAsync(
            nameof(CqrsTestAgentState),
            q => q.Term(t => t.Field("id").Value(grainId.ToString())),
            0,
            10
        );

        result.ShouldNotBeNullOrEmpty();
        var stateDto = JsonConvert.DeserializeObject<List<CqrsTestAgentStateDto>>(result);
        stateDto.ShouldNotBeNull();
        stateDto.Count.ShouldBe(1);
        stateDto[0].AgentName.ShouldBe("Version 2");
        stateDto[0].AgentCount.ShouldBe(2);
    }

    [Fact]
    public async Task ProjectAsync_ConcurrentProjections_ShouldHandleCorrectly()
    {
        // Arrange
        var groupId = Guid.NewGuid(); // Single groupId for all concurrent states
        var tasks = new List<Task>();
        for (int i = 0; i < 20; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var grainId = Guid.NewGuid();
                var state = new CqrsTestAgentState
                {
                    Id = grainId,
                    AgentName = $"Concurrent Agent {index}",
                    AgentCount = index,
                    GroupId = groupId.ToString() // Use same groupId
                };

                var wrapper = new StateWrapper<CqrsTestAgentState>(
                    GrainId.Create("test", grainId.ToString("N")), 
                    state, 
                    1);

                await _stateProjector.ProjectAsync(wrapper);
            }));
        }

        // Act
        await Task.WhenAll(tasks);
        await _stateProjector.FlushAsync();

        // Wait for indexing
        await Task.Delay(1500); // Slightly longer wait for concurrent operations

        // Assert - Verify no exceptions and data was saved
        var result = await _cqrsProvider.QueryStateAsync(
            nameof(CqrsTestAgentState),
            q => q.Term(t => t.Field("groupId").Value(groupId.ToString())),
            0,
            25
        );

        result.ShouldNotBeNullOrEmpty();
        var stateDtoList = JsonConvert.DeserializeObject<List<CqrsTestAgentStateDto>>(result);
        stateDtoList.ShouldNotBeNull();
        stateDtoList.Count.ShouldBe(20);
        stateDtoList.All(s => s.GroupId == groupId.ToString()).ShouldBeTrue();
    }

    [Fact]
    public async Task FlushAsync_EmptyQueue_ShouldCompleteWithoutError()
    {
        // Act & Assert - Should not throw
        await _stateProjector.FlushAsync();
        await _stateProjector.FlushPlusAsync();
    }

    [Fact]
    public async Task ProjectAsync_WithComplexState_ShouldSerializeCorrectly()
    {
        // Arrange
        var grainId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var state = new CqrsTestAgentState
        {
            Id = grainId,
            AgentName = "Complex State Agent",
            AgentCount = 100,
            GroupId = groupId.ToString(),
            AgentIds = new List<string> 
            { 
                "agent1", "agent2", "agent3", "agent4", "agent5" 
            },
            AgentTypeDictionary = new Dictionary<string, string>
            {
                { "type1", "value1" },
                { "type2", "value2" },
                { "type3", "value3" },
                { "type4", "value4" },
                { "type5", "value5" }
            }
        };

        var wrapper = new StateWrapper<CqrsTestAgentState>(
            GrainId.Create("test", grainId.ToString("N")), 
            state, 
            1);

        // Act
        await _stateProjector.ProjectAsync(wrapper);
        await _stateProjector.FlushAsync();

        // Wait for indexing
        await Task.Delay(500);

        // Assert - Query and verify complex data was serialized correctly
        var result = await _cqrsProvider.QueryStateAsync(
            nameof(CqrsTestAgentState),
            q => q.Term(t => t.Field("id").Value(grainId.ToString())),
            0,
            10
        );

        result.ShouldNotBeNullOrEmpty();
        var stateDto = JsonConvert.DeserializeObject<List<CqrsTestAgentStateDto>>(result);
        stateDto.ShouldNotBeNull();
        stateDto.ShouldNotBeEmpty();
        stateDto[0].AgentName.ShouldNotBeNullOrEmpty();
        stateDto[0].AgentIds.ShouldNotBeNullOrEmpty();
        stateDto[0].AgentTypeDictionary.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProjectAsync_QueryByGroupId_ShouldReturnMultipleStates()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var states = new List<StateWrapper<CqrsTestAgentState>>();
        
        for (int i = 0; i < 3; i++)
        {
            var grainId = Guid.NewGuid();
            var state = new CqrsTestAgentState
            {
                Id = grainId,
                AgentName = $"Group Agent {i}",
                AgentCount = i,
                GroupId = groupId.ToString()
            };

            states.Add(new StateWrapper<CqrsTestAgentState>(
                GrainId.Create("test", grainId.ToString("N")), 
                state, 
                1));
        }

        // Act
        foreach (var wrapper in states)
        {
            await _stateProjector.ProjectAsync(wrapper);
        }
        await _stateProjector.FlushAsync();

        // Wait for indexing
        await Task.Delay(1000);

        // Assert - Query by GroupId should return all 3 states
        var result = await _cqrsProvider.QueryStateAsync(
            nameof(CqrsTestAgentState),
            q => q.Term(t => t.Field("groupId").Value(groupId.ToString())),
            0,
            10
        );

        result.ShouldNotBeNullOrEmpty();
        var stateDtoList = JsonConvert.DeserializeObject<List<CqrsTestAgentStateDto>>(result);
        stateDtoList.ShouldNotBeNull();
        stateDtoList.Count.ShouldBe(3);
        stateDtoList.All(s => s.GroupId == groupId.ToString()).ShouldBeTrue();
    }
}

