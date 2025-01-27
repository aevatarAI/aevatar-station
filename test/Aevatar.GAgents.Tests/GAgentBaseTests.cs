using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestEvents;
using Aevatar.Core.Tests.TestGAgents;
using Aevatar.Core.Tests.TestStates;
using Shouldly;

namespace Aevatar.GAgents.Tests;

public sealed class GAgentBaseTests : AevatarGAgentsTestBase
{
    private readonly IGrainFactory _grainFactory;

    public GAgentBaseTests()
    {
        _grainFactory = GetRequiredService<IGrainFactory>();
    }

    [Fact]
    public async Task ComplicatedEventHandleTest()
    {
        var guid = Guid.NewGuid();
        // Arrange.
        var marketingLeader = _grainFactory.GetGrain<IMarketingLeaderTestGAgent>(guid);
        var developingLeader = _grainFactory.GetGrain<IDevelopingLeaderTestGAgent>(guid);

        var developer1 = _grainFactory.GetGrain<IDeveloperTestGAgent>(guid);
        var developer2 = _grainFactory.GetGrain<IDeveloperTestGAgent>(Guid.NewGuid());
        var developer3 = _grainFactory.GetGrain<IDeveloperTestGAgent>(Guid.NewGuid());
        await developingLeader.RegisterAsync(developer1);
        await developingLeader.RegisterAsync(developer2);
        await developingLeader.RegisterAsync(developer3);

        var investor1 = _grainFactory.GetGrain<IStateGAgent<InvestorTestGAgentState>>(guid);
        var investor2 = _grainFactory.GetGrain<IStateGAgent<InvestorTestGAgentState>>(Guid.NewGuid());
        await marketingLeader.RegisterAsync(investor1);
        await marketingLeader.RegisterAsync(investor2);

        var groupGAgent = _grainFactory.GetGrain<IStateGAgent<GroupGAgentState>>(guid);
        await groupGAgent.RegisterAsync(marketingLeader);
        await groupGAgent.RegisterAsync(developingLeader);
        var publishingGAgent = _grainFactory.GetGrain<IPublishingGAgent>(guid);
        await publishingGAgent.RegisterAsync(groupGAgent);

        // Act.
        await publishingGAgent.PublishEventAsync(new NewDemandTestEvent
        {
            Description = "New demand from customer."
        });

        await TestHelper.WaitUntilAsync(_ => CheckState(investor1), TimeSpan.FromSeconds(20));

        var groupState = await groupGAgent.GetStateAsync();
        groupState.RegisteredGAgents.ShouldBe(2);

        var investorState = await investor1.GetStateAsync();
        investorState.Content.Count.ShouldBe(2);
    }
    
    private async Task<bool> CheckState(IStateGAgent<InvestorTestGAgentState> investor1)
    {
        var state = await investor1.GetStateAsync();
        return !state.Content.IsNullOrEmpty() && state.Content.Count == 2;
    }
}