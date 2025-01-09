using Aevatar.CQRS.Provider;
using Aevatar.GAgent.Dto;
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
    
   
}