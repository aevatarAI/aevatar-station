using Aevatar.GAgents.Tests;

namespace Aevatar.AI.AIGAgent.Tests;

public class AIGAgentBaseTests : AevatarGAgentsTestBase
{
    protected readonly IGrainFactory _grainFactory;

    public AIGAgentBaseTests()
    {
        _grainFactory = GetRequiredService<IGrainFactory>();
    }

    [Fact]
    public async Task ComplicatedEventHandleTest()
    {
        
    }
}