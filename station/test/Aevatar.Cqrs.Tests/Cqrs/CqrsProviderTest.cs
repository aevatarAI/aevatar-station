using Aevatar.Agent;
using Aevatar.Agents.Creator;
using Aevatar.Application.Grains.Agents.Creator;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS;
using Aevatar.CQRS.Provider;
using Aevatar.Cqrs.Tests;
using Aevatar.Cqrs.Tests.Cqrs.Dto;
using Aevatar.GAgent.Dto;
using Aevatar.Mock;
using Aevatar.Query;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using Aevatar.Station.Feature.CreatorGAgent;

namespace Aevatar.GAgent;

public class CqrsProviderTest : AevatarTestBase<AevatarCqrsTestModule>
{
    private readonly IIndexingService _elasticService;
    private readonly ICQRSProvider _cqrsProvider;
    private readonly AevatarStateProjector _stateProjector;
    private const string AgentType = nameof(CqrsTestCreateAgentGEvent);
    private const string User1Address = "2HxX36oXZS89Jvz7kCeUyuWWDXLTiNRkAzfx3EuXq4KSSkH62W";
    private const string User2Address = "2KxX36oXZS89Jvz7kCeUyuWWDXLTiNRkAzfx3EuXq4KSSkH62S";
    private const string IndexSuffix = "index";
    private const string IndexPrefix = "aevatar";

    public CqrsProviderTest(ITestOutputHelper output)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIndexingService, MockElasticIndexingService>();
        _cqrsProvider = GetRequiredService<ICQRSProvider>();
        _stateProjector = GetRequiredService<AevatarStateProjector>();
        _elasticService = GetRequiredService<IIndexingService>();
    }

    [Fact]
    public async Task SendStateCommandTest()
    {
        var grainId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var cqrsTestAgentState = new CqrsTestAgentState()
        {
            Id = grainId,
            AgentName = "test",
            AgentCount = 10,
            GroupId = groupId.ToString(),
            AgentIds = new List<string>() { "agent1", "agent2" },
            AgentTypeDictionary = new Dictionary<string, string>()
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }
            }
        };
        await _stateProjector.ProjectAsync(
            new StateWrapper<CqrsTestAgentState>(GrainId.Create("test", grainId.ToString("N")), cqrsTestAgentState, 0));
        await _stateProjector.FlushAsync();
        //query state index query by eventId
        var result = await _cqrsProvider.QueryStateAsync(nameof(CqrsTestAgentState),
            q => q.Term(t => t.Field("id").Value(grainId.ToString())),
            0,
            10
        );
        result.ShouldNotBe("");

        var stateDto = JsonConvert.DeserializeObject<List<CqrsTestAgentStateDto>>(result);
        stateDto.Count.ShouldBe(1);
        stateDto[0].Id.ShouldBe(grainId.ToString());
        stateDto[0].GroupId.ShouldBe(groupId.ToString());

        //query state index query by groupId
        var grainId2 = Guid.NewGuid();
        cqrsTestAgentState.Id = grainId2;
        await _stateProjector.ProjectAsync(
            new StateWrapper<CqrsTestAgentState>(GrainId.Create("test", grainId2.ToString("N")), cqrsTestAgentState,
                0));
        await _stateProjector.FlushAsync();
        var resultGroup = await _cqrsProvider.QueryStateAsync(nameof(CqrsTestAgentState),
            q => q.Term(t => t.Field("groupId").Value(groupId.ToString())),
            0,
            10
        );
        resultGroup.ShouldNotBe("");
        var stateDtoList = JsonConvert.DeserializeObject<List<CqrsTestAgentStateDto>>(resultGroup);
        stateDtoList.Count.ShouldBe(2);
        stateDtoList[0].GroupId.ShouldBe(groupId.ToString());
        stateDtoList[1].Id.ShouldBe(grainId2.ToString());
        stateDtoList[1].GroupId.ShouldBe(groupId.ToString());
    }


    [Fact]
    public async Task QueryAgentStateAsyncTest()
    {
        var userId = Guid.NewGuid();
        var userIdString = userId.ToString();
        List<CreatorGAgentState> creatorList = new List<CreatorGAgentState>();
        for (var i = 0; i < 3; i++)
        {
            creatorList.Add(new CreatorGAgentState()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AgentType = "TestAgent",
                Name = "TestAgentName",
                Properties = JsonConvert.SerializeObject(new Dictionary<string, object>() { { "Name", "you" } }),
                BusinessAgentGrainId = GrainId.Create(nameof(creatorList), Guid.NewGuid().ToString().Replace("-", "")),
            });
        }

        foreach (var item in creatorList)
        {
            await _stateProjector.ProjectAsync(
                new StateWrapper<CreatorGAgentState>(
                    GrainId.Create(nameof(CreatorGAgent), item.Id.ToString().Replace("-", "")), item, 0));
        }

        await _stateProjector.FlushAsync();
        var stationList = await _cqrsProvider.QueryAgentStateAsync(nameof(CreatorGAgentState), creatorList[0].Id);
        stationList.ShouldNotBeEmpty();
        var pagedResultDto = await _elasticService.QueryWithLuceneAsync(new LuceneQueryDto
        {
            StateName = nameof(CreatorGAgentState),
            PageIndex = 0,
            PageSize = 2,
        });

        pagedResultDto.TotalCount.ShouldBe(3);
        pagedResultDto.Items.Count.ShouldBe(2);
    }
}