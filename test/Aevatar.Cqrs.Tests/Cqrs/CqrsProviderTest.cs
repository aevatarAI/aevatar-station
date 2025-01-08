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
        var eventId = Guid.NewGuid();
        var agentGrainId = Guid.NewGuid();
        var agentType = nameof(CqrsTestCreateAgentGEvent);

        //save gEvent index
        var cqrsTestCreateAgentGEvent = new CqrsTestCreateAgentGEvent
        {
            Id = eventId,
            UserAddress = "2HxX36oXZS89Jvz7kCeUyuWWDXLTiNRkAzfx3EuXq4KSSkH62W",
            Type = "twitter",
            Name = agentType,
            BusinessAgentId = agentGrainId.ToString(),
            Properties = "create"
        };
        await _cqrsProvider.PublishAsync(eventId, agentGrainId, agentType, cqrsTestCreateAgentGEvent);
        
        //query gEvent index query by eventId
        //    Task<string> QueryGEventAsync(string eventId, List<string> grainIds, int pageNumber, int pageSize);

        var documents = await _cqrsProvider.QueryGEventAsync("3e64ce58-929b-41d6-b54b-1290f568c768", new List<string>(){"a064a05a-2b60-4955-adb5-81d435d4736b"}, 1, 10);
        documents.ShouldBe("");
    }
    
   
}