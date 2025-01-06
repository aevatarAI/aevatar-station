using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestGAgents;
using Aevatar.Core.Tests.TestInitializeDtos;
using Aevatar.Core.Tests.TestStates;
using Shouldly;

namespace Aevatar.Core.Tests;

[Trait("Category", "BVT")]
public class GAgentFactoryTests : GAgentTestKitBase, IAsyncLifetime
{
    private IGAgentFactory _gAgentFactory;
    
    [Fact(DisplayName = "Implementation of GetAvailableGAgentTypes works.")]
    public async Task GetAvailableGAgentTypesTest()
    {
        var gAgentTypes = _gAgentFactory.GetAvailableGAgentTypes();
        gAgentTypes.Count.ShouldBeGreaterThan(20);
    }
    
    [Fact(DisplayName = "Implementation of GetGAgentAsync works.")]
    public async Task GetGAgentAsyncTest()
    {
        {
            var gAgent = await _gAgentFactory.GetGAgentAsync<IStateGAgent<GroupGAgentState>>(Guid.NewGuid());
            gAgent.ShouldNotBeNull();
        }

        {
            var gAgent = await _gAgentFactory.GetGAgentAsync<IStateGAgent<NaiveTestGAgentState>>(Guid.NewGuid(), new NaiveGAgentInitializeDto
            {
                InitialGreeting = "Test"
            });
            gAgent.ShouldNotBeNull();
        }
    }

    public Task InitializeAsync()
    {
        _gAgentFactory = new GAgentFactory(Silo.GrainFactory);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}