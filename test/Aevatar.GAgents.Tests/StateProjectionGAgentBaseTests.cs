using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestGAgents;
using Shouldly;

namespace Aevatar.GAgents.Tests;

public class StateProjectionGAgentBaseTests : AevatarGAgentsTestBase
{
    private readonly IGAgentFactory _gAgentFactory;

    public StateProjectionGAgentBaseTests()
    {
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task CallStateHandlerTest()
    {
        var groupGAgent = await _gAgentFactory.GetGAgentAsync<IStateGAgent<GroupGAgentState>>();
        var testGAgent = await _gAgentFactory.GetGAgentAsync<ITestStateProjectionGAgent>();
        await groupGAgent.RegisterAsync(testGAgent);
        await TestHelper.WaitUntilAsync(_ => CheckState(groupGAgent), TimeSpan.FromSeconds(20));
        var state = await testGAgent.GetStateAsync();
        state.StateHandlerCalled.ShouldBeTrue();
    }

    private async Task<bool> CheckState(IStateGAgent<GroupGAgentState> groupGAgent)
    {
        var state = await groupGAgent.GetStateAsync();
        return state.RegisteredGAgents > 0;
    }
}