using Aevatar.ArtifactGAgents;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Core.Tests.TestGAgents;
using Aevatar.Plugins;
using Aevatar.Plugins.Extensions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using PluginGAgent.Grains;

var builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.UseLocalhostClustering()
            .AddActivityPropagation()
            .UseMongoDBClient("mongodb://localhost:27017/?maxPoolSize=555")
            .UseMongoDBClustering(options =>
            {
                options.DatabaseName = "AISmartDb";
                options.Strategy = MongoDBMembershipStrategy.SingleDocument;
            })
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "AISmartSiloCluster";
                options.ServiceId = "AISmartBasicService";
            })
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

Console.WriteLine("Select an option:");
Console.WriteLine("1. Add Plugin GAgents");
Console.WriteLine("2. Try Execute Plugin GAgents");
Console.WriteLine("3. Try Get Artifact GAgent");
var choice = Console.ReadLine();

switch (choice)
{
    case "1":
        await AddCodeAsync(pluginManager);
        break;
    case "2":
        await PerformCommandAsync(gAgentFactory);
        break;
    case "3":
        await TryArtifactGAgentAsync(gAgentFactory);
        break;
    default:
        Console.WriteLine("Invalid choice.");
        break;
}

async Task PerformCommandAsync(IGAgentFactory gAgentFactory1)
{
    var publishingGAgent = await gAgentFactory1.GetGAgentAsync<IPublishingGAgent>();
    var commander = await gAgentFactory1.GetGAgentAsync("commander");
    var worker = await gAgentFactory1.GetGAgentAsync("worker");
    await publishingGAgent.RegisterAsync(commander);
    await commander.RegisterAsync(worker);
    await publishingGAgent.PublishEventAsync(new Command { Content = "test" });
}

async Task AddCodeAsync(IPluginGAgentManager pluginGAgentManager)
{
    var plugins = await PluginLoader.LoadPluginsAsync("plugins");

    var tenantId = "test".ToGuid();
    foreach (var code in plugins.Values)
    {
        await pluginGAgentManager.AddPluginAsync(new AddPluginDto
        {
            Code = code,
            TenantId = tenantId
        });
    }
}

async Task TryArtifactGAgentAsync(IGAgentFactory factory)
{
    var myArtifactGAgent =
        await factory.GetGAgentAsync(typeof(MyArtifactGAgent));
    var description = await myArtifactGAgent.GetDescriptionAsync();
    Console.WriteLine(description);
}