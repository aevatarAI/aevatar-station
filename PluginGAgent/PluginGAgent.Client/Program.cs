using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Plugins;
using Aevatar.Plugins.Extensions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.UseLocalhostClustering()
            .AddMemoryStreams(AevatarCoreConstants.StreamProvider);
    })
    .ConfigureLogging(logging => logging.AddConsole())
    .UseConsoleLifetime()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IGAgentFactory, GAgentFactory>();
        services.AddSingleton<ApplicationPartManager>();
        services.AddSingleton<IPluginGAgentManager, PluginGAgentManager>();
    });

using var host = builder.Build();
await host.StartAsync();

var gAgentFactory = host.Services.GetRequiredService<IGAgentFactory>();
var pluginManager = host.Services.GetRequiredService<IPluginGAgentManager>();

var plugins = await PluginLoader.LoadPluginsAsync("plugins");

var tenantId = "test".ToGuid();
foreach (var code in plugins.Values)
{
    await pluginManager.AddPluginGAgentAsync(new AddPluginGAgentDto
    {
        Code = code,
        TenantId = tenantId
    });
}

