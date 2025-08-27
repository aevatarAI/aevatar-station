using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestEvents;
using Aevatar.Core.Tests.TestGAgents;
using Shouldly;

namespace Aevatar.GAgents.Tests;

public sealed class GAgentBaseTests : AevatarGAgentsTestBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly IGAgentFactory _gAgentFactory;

    public GAgentBaseTests()
    {
        _grainFactory = GetRequiredService<IGrainFactory>();
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact(DisplayName = "Can use ConfigAsync method to config GAgent.")]
    public async Task ConfigurationTest()
    {
        var guid = Guid.NewGuid();
        // Arrange.
        var configurationTestGAgent = _grainFactory.GetGrain<IStateGAgent<ConfigurationTestGAgentState>>(guid);
        var configuration = new Configuration
        {
            InitialGreeting = "Hello world"
        };

        // Act.
        await configurationTestGAgent.ConfigAsync(configuration);
        var configurationType = await configurationTestGAgent.GetConfigurationTypeAsync();

        // Assert.
        var state = await configurationTestGAgent.GetStateAsync();
        state.Content.Count.ShouldBe(1);
        state.Content[0].ShouldBe("Hello world");
        configurationType.ShouldBe(typeof(Configuration));
    }

    [Fact(DisplayName = "Simulated complicated pipeline works.")]
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

    [Fact(DisplayName = "SyncWorker should be worked and not block current GAgent.")]
    public async Task SyncWorkerTest()
    {
        var guid = Guid.NewGuid();
        // Arrange.
        var testGAgent = _grainFactory.GetGrain<IStateGAgent<LongRunTaskTestGAgentState>>(guid);
        var publishingGAgent = _grainFactory.GetGrain<IPublishingGAgent>(guid);
        await publishingGAgent.RegisterAsync(testGAgent);

        // Act.
        await publishingGAgent.PublishEventAsync(new NaiveTestEvent
        {
            Greeting = "testing with long run task."
        });
        
        // Assert: Not blocked.
        var state = await testGAgent.GetStateAsync();
        state.ShouldNotBeNull();
        var timeDiff = (state.EndTime - state.StartTime).TotalMilliseconds;
        timeDiff.ShouldBeLessThan(100);

        await Task.Delay(3000);

        // Assert: Executed.
        state = await testGAgent.GetStateAsync();
        state.Called.ShouldBe(true);
    }

    [Fact(DisplayName = "Grain activation sets _isActivated field correctly.")]
    public async Task GrainActivation_SetsIsActivatedField_Test()
    {
        var guid = Guid.NewGuid();
        // Arrange & Act - Just getting state should activate the grain
        var stateTrackingGAgent = _grainFactory.GetGrain<IStateTrackingTestGAgent>(guid);
        var stateAfterActivation = await stateTrackingGAgent.GetStateAsync();

        // Assert - No HandleStateChangedAsync should be called during simple activation
        stateAfterActivation.HandleStateChangedCallCount.ShouldBe(0);
        
        // The grain should be properly activated with initial version 0
        var currentVersion = await stateTrackingGAgent.GetCurrentVersionAsync();
        currentVersion.ShouldBe(0);
    }

    [Fact(DisplayName = "Private field _lastProcessedVersion prevents duplicate state processing.")]
    public async Task LastProcessedVersionField_PreventsDuplicateProcessing_Test()
    {
        var guid = Guid.NewGuid();
        // Arrange.
        var stateTrackingGAgent = _grainFactory.GetGrain<IStateTrackingTestGAgent>(guid);

        // Ensure grain is activated and get baseline
        await stateTrackingGAgent.GetStateAsync();
        var stateBeforeTest = await stateTrackingGAgent.GetStateAsync();
        var baselineCallCount = stateBeforeTest.HandleStateChangedCallCount;

        // Act - Trigger multiple state changes
        await stateTrackingGAgent.UpdateTestDataAsync("test1");
        await stateTrackingGAgent.UpdateTestDataAsync("test2");
        await stateTrackingGAgent.UpdateTestDataAsync("test3");

        // Assert
        var finalState = await stateTrackingGAgent.GetStateAsync();
        var totalCallsAfterUpdates = finalState.HandleStateChangedCallCount - baselineCallCount;
        
        // Each update should trigger HandleStateChangedAsync exactly once
        totalCallsAfterUpdates.ShouldBe(3);
        
        // Verify processed versions are tracked correctly
        finalState.ProcessedVersions.Count.ShouldBeGreaterThanOrEqualTo(3);
        
        // Verify no duplicate versions are processed
        var uniqueVersions = finalState.ProcessedVersions.Distinct().ToList();
        uniqueVersions.Count.ShouldBe(finalState.ProcessedVersions.Count);
    }

    private async Task<bool> CheckState(IStateGAgent<InvestorTestGAgentState> investor1)
    {
        var state = await investor1.GetStateAsync();
        return !state.Content.IsNullOrEmpty() && state.Content.Count == 2;
    }
}