using System;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Plugins;
using Shouldly;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Users;
using Xunit;

namespace Aevatar.Plugins;

public abstract class PluginServiceTests<TStartupModule> : AevatarApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IPluginService _pluginService;
    private readonly IdentityUserManager _identityUserManager;
    private readonly ICurrentUser _currentUser;
    private readonly IPluginRepository _pluginRepository;

    protected PluginServiceTests()
    {
        _pluginService = GetRequiredService<IPluginService>();
        _identityUserManager = GetRequiredService<IdentityUserManager>();
        _currentUser = GetRequiredService<ICurrentUser>();
        _pluginRepository = GetRequiredService<IPluginRepository>();
    }

    [Fact]
    public async Task Plugin_Create_Test()
    {
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));

        var projectId = Guid.NewGuid();
        var name = "Test Plugin";
        var code = new byte[] { 1, 2, 3, 4 };

        var plugin = await _pluginService.CreateAsync(projectId, name, code);
        plugin.Name.ShouldBe(name);
        plugin.ProjectId.ShouldBe(projectId);

        var plugins = await _pluginService.GetListAsync(new GetPluginDto { ProjectId = projectId });
        plugins.Items.Count.ShouldBe(1);
        plugins.Items[0].Name.ShouldBe(name);
        plugins.Items[0].CreatorName.ShouldBe("test");
    }

    [Fact]
    public async Task Plugin_Update_Test()
    {
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));

        var projectId = Guid.NewGuid();
        var name = "Test Plugin";
        var code = new byte[] { 1, 2, 3, 4 };

        var plugin = await _pluginService.CreateAsync(projectId, name, code);

        var newName = "Updated Plugin";
        var newCode = new byte[] { 5, 6, 7, 8 };

        var updatedPlugin = await _pluginService.UpdateAsync(plugin.Id, newName, newCode);
        updatedPlugin.Name.ShouldBe(newName);

        var plugins = await _pluginService.GetListAsync(new GetPluginDto { ProjectId = projectId });
        plugins.Items.Count.ShouldBe(1);
        plugins.Items[0].Name.ShouldBe(newName);
    }

    [Fact]
    public async Task Plugin_Delete_Test()
    {
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));

        var projectId = Guid.NewGuid();
        var name = "Test Plugin";
        var code = new byte[] { 1, 2, 3, 4 };

        var plugin = await _pluginService.CreateAsync(projectId, name, code);

        await _pluginService.DeleteAsync(plugin.Id);

        var plugins = await _pluginService.GetListAsync(new GetPluginDto { ProjectId = projectId });
        plugins.Items.Count.ShouldBe(0);

        await Should.ThrowAsync<EntityNotFoundException>(async () =>
            await _pluginRepository.GetAsync(plugin.Id));
    }
} 