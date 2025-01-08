using Aevatar.CQRS.Provider;
using Aevatar.GAgent.Dto;
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
    
    public CqrsProviderTest(ITestOutputHelper output)
    {
        _clusterClient = GetRequiredService<IClusterClient>();
        _cqrsProvider = GetRequiredService<ICQRSProvider>();
    }
    [Fact]
    public async Task SendStateCommandTest()
    {
        var grainId = Guid.NewGuid();
        var cqrsTestAgentState = new CqrsTestAgentState()
        {
            Id = Guid.NewGuid(),
            AgentName = "test",
            AgentCount = 10,
            GroupId = Guid.NewGuid().ToString(),
            AgentIds = new List<string>(){"agent1","agent2"},
            AgentTypeDictionary = new Dictionary<string, string>(){
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }}
        };
        await _cqrsProvider.PublishAsync(cqrsTestAgentState,grainId.ToString());
    }
    
    [Fact]
    public async Task SendGeventCommandTest()
    {
        var eventId1 = Guid.NewGuid();
        var agentGrainId = Guid.NewGuid();


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
        await _cqrsProvider.PublishAsync(eventId1, agentGrainId, AgentType, cqrsTestCreateAgentGEvent);
        
        //query gEvent index query by eventId
        await Task.Delay(1000);
        var tuple = await _cqrsProvider.QueryGEventAsync(eventId1.ToString(), new List<string>(){}, 1, 10);
        tuple.Item1.ShouldBe(1);
        tuple.Item2.Count.ShouldBe(1);
        tuple.Item2.FirstOrDefault().Id.ShouldBe(eventId1);
        tuple.Item2.FirstOrDefault().GrainId.ShouldBe(agentGrainId);
        tuple.Item2.FirstOrDefault().EventJson.ShouldContain(User1Address);
        
        //query gEvent index query by grainId
        var eventId2 = Guid.NewGuid();
        cqrsTestCreateAgentGEvent.UserAddress = User2Address;
        await _cqrsProvider.PublishAsync(eventId2, agentGrainId, AgentType, cqrsTestCreateAgentGEvent);
        
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