using Aevatar.Agent;
using Aevatar.Agents.Creator;
using Aevatar.Application.Grains.Agents.Creator;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Provider;
using Aevatar.Cqrs.Tests.Cqrs.Dto;
using Aevatar.GAgent.Dto;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgent;

public class CqrsProviderTest : AevatarApplicationTestBase
{
    private readonly IClusterClient _clusterClient;
    private readonly ICQRSProvider _cqrsProvider;
    private readonly IStateProjector _stateProjector;
    private const string AgentType = nameof(CqrsTestCreateAgentGEvent);
    private const string User1Address = "2HxX36oXZS89Jvz7kCeUyuWWDXLTiNRkAzfx3EuXq4KSSkH62W";
    private const string User2Address = "2KxX36oXZS89Jvz7kCeUyuWWDXLTiNRkAzfx3EuXq4KSSkH62S";
    private const string IndexSuffix = "index";
    private const string IndexPrefix = "aevatar";

    public CqrsProviderTest(ITestOutputHelper output)
    {
        _clusterClient = GetRequiredService<IClusterClient>();
        _cqrsProvider = GetRequiredService<ICQRSProvider>();
        _stateProjector = GetRequiredService<IStateProjector>();
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
            new StateWrapper<CqrsTestAgentState>(GrainId.Create("test", grainId.ToString()), cqrsTestAgentState, 0));

        //query state index query by eventId
        var indexName = IndexPrefix + nameof(CqrsTestAgentState).ToLower() + IndexSuffix;
        await Task.Delay(1000);

        var result = await _cqrsProvider.QueryStateAsync(indexName,
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
            new StateWrapper<CqrsTestAgentState>(GrainId.Create("test", grainId2.ToString()), cqrsTestAgentState, 0));
        await Task.Delay(1000);
        var result2 = await _cqrsProvider.QueryStateAsync(indexName,
            q => q.Term(t => t.Field("id").Value(grainId2.ToString())),
            0,
            10
        );
        result2.ShouldNotBe("");
        var stateDto2 = JsonConvert.DeserializeObject<List<CqrsTestAgentStateDto>>(result2);
        stateDto2.Count.ShouldBe(1);
        stateDto2[0].Id.ShouldBe(grainId2.ToString());
        stateDto2[0].GroupId.ShouldBe(groupId.ToString());

        var resultGroup = await _cqrsProvider.QueryStateAsync(indexName,
            q => q.Term(t => t.Field("groupId").Value(groupId.ToString())),
            0,
            10
        );
        resultGroup.ShouldNotBe("");
        var stateDtoList = JsonConvert.DeserializeObject<List<CqrsTestAgentStateDto>>(resultGroup);
        stateDtoList.Count.ShouldBe(2);
        stateDtoList[0].Id.ShouldBe(grainId.ToString());
        stateDtoList[0].GroupId.ShouldBe(groupId.ToString());
        stateDtoList[1].Id.ShouldBe(grainId2.ToString());
        stateDtoList[1].GroupId.ShouldBe(groupId.ToString());
    }


    [Fact]
    public async Task QueryUserInstallAgentTest()
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
                    GrainId.Create(nameof(CreatorGAgent).ToLower(), item.Id.ToString().Replace("-", "")), item, 0));
        }

        var indexName = IndexPrefix + nameof(CreatorGAgentState).ToLower() + IndexSuffix;
        var stationList = await _cqrsProvider.GetUserInstanceAgent<CreatorGAgentState, AgentInstanceDto>(userId, 0, 10);
        stationList.Item1.ShouldNotBe(0);
    }
}