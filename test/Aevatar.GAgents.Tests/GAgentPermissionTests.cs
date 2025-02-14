using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestGAgents;
using Aevatar.Plugins.Extensions;
using Volo.Abp.PermissionManagement;

namespace Aevatar.GAgents.Tests;

public sealed class GAgentPermissionTests : AevatarGAgentsTestBase, IAsyncLifetime
{
    private readonly IPermissionManager _permissionManager;
    private readonly IGAgentFactory _gAgentFactory;

    public GAgentPermissionTests()
    {
        _permissionManager = GetRequiredService<IPermissionManager>();
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }
    
    [Fact]
    public async Task CallMethodTest()
    {
        RequestContext.Set("CurrentUser", new UserContext
        {
            UserId = "TestUser".ToGuid(),
            Role = "User"
        });
        var permissionGAgent = await _gAgentFactory.GetGAgentAsync<IPermissionGAgent>();
        await permissionGAgent.DoSomethingAsync();
    }

    public async Task InitializeAsync()
    {
        await _permissionManager.SetAsync("DoSomething", "User", "TestUser".ToGuid().ToString(), true);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}