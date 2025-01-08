using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestGAgents;
using Aevatar.Core.Tests.TestInitializeDtos;
using Aevatar.Core.Tests.TestStates;
using Shouldly;

namespace Aevatar.GAgents.Tests;

public class GAgentFactoryTests : AevatarGAgentsTestBase
{
    protected readonly IGAgentFactory _gAgentFactory;

    public GAgentFactoryTests()
    {
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task CreateNormalGAgentTest()
    {
        var gAgent = await _gAgentFactory.GetGAgentAsync<IStateGAgent<GroupGAgentState>>(Guid.NewGuid());
        gAgent.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateGAgentWithInitializeMethodTest()
    {
        var gAgent = await _gAgentFactory.GetGAgentAsync<IStateGAgent<NaiveTestGAgentState>>(Guid.NewGuid(),
            new NaiveGAgentInitializeDto
            {
                InitialGreeting = "Test"
            });
        await TestHelper.WaitUntilAsync(_ => CheckState(gAgent), TimeSpan.FromSeconds(20));
        var gAgentState = await gAgent.GetStateAsync();
        gAgentState.Content.Count.ShouldBe(1);
    }

    private async Task<bool> CheckState(IStateGAgent<NaiveTestGAgentState> gAgent)
    {
        var state = await gAgent.GetStateAsync();
        return !state.Content.IsNullOrEmpty();
    }
}