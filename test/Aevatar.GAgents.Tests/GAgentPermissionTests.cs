using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestGAgents;
using Aevatar.PermissionManagement;
using Aevatar.Plugins.Extensions;
using Shouldly;
using Volo.Abp.PermissionManagement;

namespace Aevatar.GAgents.Tests;

public sealed class GAgentPermissionTests : AevatarGAgentsTestBase, IAsyncLifetime
{
    private readonly IPermissionManager _permissionManager;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IPermissionInfoProvider _permissionInfoProvider;

    public GAgentPermissionTests()
    {
        _permissionManager = GetRequiredService<IPermissionManager>();
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
        _permissionInfoProvider = GetRequiredService<IPermissionInfoProvider>();
    }

    [Fact]
    public async Task PermissionInfosProviderTest()
    {
        var allPermissionInfos = _permissionInfoProvider.GetAllPermissionInfos();
        allPermissionInfos.ShouldContain(i =>
            i.Name == "DoSomething"
            && i.DisplayName == "Only for testing."
            && i.GrainType == "Aevatar.Core.Tests.TestGAgents/PermissionGAgent"
            && i.Type == "Aevatar.Core.Tests.TestGAgents.PermissionGAgent"
        );
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
        var userId = "TestUser".ToGuid().ToString();
        await _permissionManager.SetAsync("DoSomething", "User", userId, true);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}