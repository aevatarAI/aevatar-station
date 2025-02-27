using Aevatar.Core.Abstractions.Extensions;
using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Plugins.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using Volo.Abp.PermissionManagement;

namespace PluginGAgent.Silo;

public class PluginGAgentTestHostedService : IHostedService
{
    private readonly IAbpApplicationWithExternalServiceProvider _application;
    private readonly IServiceProvider _serviceProvider;

    public PluginGAgentTestHostedService(
        IAbpApplicationWithExternalServiceProvider application,
        IServiceProvider serviceProvider)
    {
        _application = application;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _application.InitializeAsync(_serviceProvider);
        var tenantId = "test".ToGuid();
        var pluginManager = _serviceProvider.GetRequiredService<IPluginGAgentManager>();
        var plugins = await pluginManager.GetPluginAssembliesAsync(tenantId);
        Console.WriteLine("Plugins:");
        foreach (var plugin in plugins)
        {
            Console.WriteLine(plugin.FullName);
        }
        var permissionManager = _serviceProvider.GetRequiredService<IPermissionManager>();
        var userId = "TestUser".ToGuid().ToString();
        await permissionManager.SetAsync("DoSomething", "User", userId, true);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _application.Shutdown();
        return Task.CompletedTask;
    }
}