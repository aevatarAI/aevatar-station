using Aevatar.CQRS.Provider;
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
    private const string AgentType = nameof(CqrsTestCreateAgentGEvent);
    private const string User1Address = "2HxX36oXZS89Jvz7kCeUyuWWDXLTiNRkAzfx3EuXq4KSSkH62W";
    private const string User2Address = "2KxX36oXZS89Jvz7kCeUyuWWDXLTiNRkAzfx3EuXq4KSSkH62S";
    private const string IndexSuffix = "index";
    public CqrsProviderTest(ITestOutputHelper output)
    {
        _clusterClient = GetRequiredService<IClusterClient>();
        _cqrsProvider = GetRequiredService<ICQRSProvider>();
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
            AgentIds = new List<string>(){"agent1","agent2"},
            AgentTypeDictionary = new Dictionary<string, string>(){
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }}
        };
        await _cqrsProvider.PublishAsync(cqrsTestAgentState,grainId.ToString());
        
        //query state index query by eventId
        var indexName = nameof(CqrsTestAgentState).ToLower() + IndexSuffix;
        await Task.Delay(1000);

        var result = await _cqrsProvider.QueryStateAsync(indexName,
            q => q.Term(t => t.Field("id").Value(grainId)), 
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
        await _cqrsProvider.PublishAsync(cqrsTestAgentState,grainId2.ToString());
        await Task.Delay(1000);
        var result2 = await _cqrsProvider.QueryStateAsync(indexName,
            q => q.Term(t => t.Field("id").Value(grainId2)), 
            0,
            10
        );
        result2.ShouldNotBe("");
        var stateDto2 = JsonConvert.DeserializeObject<List<CqrsTestAgentStateDto>>(result2);
        stateDto2.Count.ShouldBe(1);
        stateDto2[0].Id.ShouldBe(grainId2.ToString());
        stateDto2[0].GroupId.ShouldBe(groupId.ToString());

        var resultGroup = await _cqrsProvider.QueryStateAsync(indexName,
            q => q.Term(t => t.Field("groupId").Value(groupId)), 
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
    public async Task SendGeventCommandTest()
    {
        var eventId1 = Guid.NewGuid();
        var agentGrainId = Guid.NewGuid();
       
       var grainType = GrainType.Create(AgentType);
       var primaryKey = IdSpan.Create(agentGrainId.ToString());

        var grainId = GrainId.Create(grainType,primaryKey);
        //save gEvent index
        var cqrsTestCreateAgentGEvent = new CqrsTestCreateAgentGEvent
        {
            Id = eventId1,
            UserAddress = User1Address,
            Type = "twitter",
            Name = AgentType,
            BusinessAgentId = agentGrainId.ToString(),
            Properties = "create"
        };
        await _cqrsProvider.PublishAsync(eventId1, grainId, cqrsTestCreateAgentGEvent);

        await Task.Delay(1000);
        //query gEvent index query by eventId
        var tuple = await _cqrsProvider.QueryGEventAsync(eventId1.ToString(), new List<string>(){}, 1, 10);
        tuple.Item1.ShouldBe(1);
        tuple.Item2.Count.ShouldBe(1);
        tuple.Item2.FirstOrDefault().Id.ShouldBe(eventId1);
        tuple.Item2.FirstOrDefault().GrainId.ShouldBe(agentGrainId);
        tuple.Item2.FirstOrDefault().EventJson.ShouldContain(User1Address);

        //query gEvent index query by grainId
        var eventId2 = Guid.NewGuid();
        cqrsTestCreateAgentGEvent.UserAddress = User2Address;
        await _cqrsProvider.PublishAsync(eventId2, grainId, cqrsTestCreateAgentGEvent);
        await Task.Delay(1000);

        var tupleResult = await _cqrsProvider.QueryGEventAsync("", new List<string>(){agentGrainId.ToString()}, 1, 10);
        tupleResult.Item1.ShouldBe(2);
        tupleResult.Item2.Count.ShouldBe(2);
        tupleResult.Item2[0].Id.ShouldBe(eventId1);
        tupleResult.Item2[1].Id.ShouldBe(eventId2);
        tupleResult.Item2[0].GrainId.ShouldBe(agentGrainId);
        tupleResult.Item2[1].GrainId.ShouldBe(agentGrainId);
        tupleResult.Item2[0].EventJson.ShouldContain(User1Address);
        tupleResult.Item2[1].EventJson.ShouldContain(User2Address);

    }
    
   
}