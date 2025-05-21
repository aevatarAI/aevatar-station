using System.Diagnostics;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Core.Tests.TestArtifacts;
using Aevatar.Core.Tests.TestGAgents;
using Aevatar.Extensions;
using Aevatar.PermissionManagement;
using Aevatar.Plugins;
using Aevatar.Plugins.Extensions;
using E2E.Grains;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using PluginGAgent.Grains;

var builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.UseLocalhostClustering()
            .UseMongoDBClient("mongodb://localhost:27017/?maxPoolSize=555")
            .AddMemoryStreams(AevatarCoreConstants.StreamProvider)
            .UseAevatar();
    })
    .ConfigureLogging(logging => logging.AddConsole())
    .UseConsoleLifetime();

using var host = builder.Build();
await host.StartAsync();

var pluginManager = host.Services.GetRequiredService<IPluginGAgentManager>();
var gAgentFactory = host.Services.GetRequiredService<IGAgentFactory>();
var gAgentManager = host.Services.GetRequiredService<IGAgentManager>();
var client = host.Services.GetRequiredService<IClusterClient>();

Console.WriteLine("Select an option:");
Console.WriteLine("0. Add plugin code");
Console.WriteLine("1. Try Execute Plugin GAgents");
Console.WriteLine("2. Try Get Artifact GAgent");
Console.WriteLine("3. Show all candidate GAgents");
Console.WriteLine("4. Call PermissionGAgent");
var choice = Console.ReadLine();

switch (choice)
{
    case "0":
        await AddCodeAsync(pluginManager);
        break;
    case "1":
        await PerformCommandAsync(gAgentFactory);
        break;
    case "2":
        await TryArtifactGAgentAsync(gAgentFactory);
        break;
    case "3":
        ListCandidateGAgents(gAgentManager);
        break;
    case "4":
        await CallPermissionGAgent(gAgentFactory);
        break;
    case "5":
        await VerifyTest(client);
        break;
    default:
        Console.WriteLine("Invalid choice.");
        break;
}

async Task AddCodeAsync(IPluginGAgentManager pluginGAgentManager)
{
    var plugins = PluginLoader.LoadPlugins("plugins");

    var tenantId = "test".ToGuid();
    foreach (var code in plugins)
    {
        await pluginGAgentManager.AddPluginAsync(new AddPluginDto
        {
            Code = code,
            TenantId = tenantId
        });
    }
}

async Task PerformCommandAsync(IGAgentFactory factory)
{
    var publishingGAgent = await factory.GetGAgentAsync<IPublishingGAgent>();
    var grainId = GrainId.Create("pluginTest.commander", Guid.NewGuid().ToString("N"));
    var commander = await factory.GetGAgentAsync(grainId);
    var worker = await factory.GetGAgentAsync("worker", "pluginTest");
    await publishingGAgent.RegisterAsync(commander);
    await commander.RegisterAsync(worker);
    await publishingGAgent.PublishEventAsync(new Command { Content = "test" });
}

async Task TryArtifactGAgentAsync(IGAgentFactory factory)
{
    var myArtifactGAgent =
        await factory.GetArtifactGAgentAsync<MyArtifact, MyArtifactGAgentState, MyArtifactStateLogEvent>();
    var description = await myArtifactGAgent.GetDescriptionAsync();
    Console.WriteLine(description);
}

void ListCandidateGAgents(IGAgentManager manager)
{
    Console.WriteLine("Available GAgent GrainTypes:");
    var grainTypes = manager.GetAvailableGAgentGrainTypes();
    foreach (var grainType in grainTypes)
    {
        Console.WriteLine(grainType.ToString());
    }

    Console.WriteLine("Available Types:");
    var types = manager.GetAvailableGAgentTypes();
    foreach (var type in types)
    {
        Console.WriteLine(type.FullName);
    }
}

async Task CallPermissionGAgent(IGAgentFactory factory)
{
    RequestContext.Set("CurrentUser", new UserContext
    {
        UserId = "TestUser".ToGuid(),
        Roles = ["admin"]
    });
    var permissionGAgent = await factory.GetGAgentAsync<IPermissionGAgent>();
    await permissionGAgent.DoSomething1Async();
}

async Task VerifyTest(IClusterClient client)
{
    const int subscriberCount = 1;

    var sw = new Stopwatch();
    sw.Start();
// Create a new grain instance for the sub-agent
    var subAgents = new List<ITestDbGAgent>();
    for (var i = 0; i < subscriberCount; ++i)
    {
        var sws = new Stopwatch();
        //var subAgentId = Guid.NewGuid();
        var subAgentId = Guid.Parse("abd8518a9c134fae9694d182fa327944");
        Console.WriteLine("subAgent Guid: {0:N}", subAgentId);
        var subAgent = client.GetGrain<ITestDbGAgent>(subAgentId);
        // Console.WriteLine("subAgent count: {0}", await subAgent.GetCount());

        sws.Start();
        await subAgent.ActivateAsync();
        sws.Stop();
        Console.WriteLine("Time taken to create agent-{0}: {1} ms", i, sws.ElapsedMilliseconds);
        subAgents.Add(subAgent);
    }

    sw.Stop();
    Console.WriteLine("Time taken to create {0} sub-agents: {1} ms", subscriberCount, sw.ElapsedMilliseconds);
// Create a new grain instance for the publisher agent
// var pubAgentId = Guid.NewGuid();
//subagent bc8aeb04bb0043008b49e5caf4d86fe7
    var pubAgentId = Guid.Parse("48733802aa084964bccb978f720c0486");

    var pubAgent = client.GetGrain<ITestDbScheduleGAgent>(pubAgentId);

    Console.WriteLine("pubAgent: {0:N}", pubAgentId);
    if (subscriberCount > 0)
    {
        Console.WriteLine("init: subAgent-{0} Count {1}", subscriberCount, await subAgents[subscriberCount - 1].GetCount());
    }

    var TestDbEvent = new TestDbEvent
    {
        Number = 100,
        CorrelationId = Guid.NewGuid(),
        PublisherGrainId = pubAgent.GetGrainId(),
    };

    await pubAgent.BroadCastEventAsync("TestDbScheduleGAgent", TestDbEvent);

    await Task.Delay(1000);

    var count = 0;
    for (var i = 0; i < subscriberCount; ++i)
    {
        if (await subAgents[i].GetCount() != 100)
        {
            Console.WriteLine("after: subAgent-{0} Count {1}", i, await subAgents[i].GetCount());
            count++;
        }
    }

    if (count > 0)
    {
        Console.WriteLine("Total missing is {0}", count);
    }

    if (subscriberCount > 0)
    {
        Console.WriteLine("subAgent-{0} Count {1}", subscriberCount, await subAgents[subscriberCount - 1].GetCount());
    }

// Console.WriteLine("pubAgent: {0} subAgent-{1} Count {2}, subAgent-{3} Count {4}",pubAgentId.ToString("N"),  subscriberCount-2,await subAgents[subscriberCount-2].GetCount(), subscriberCount-1,await subAgents[subscriberCount-1].GetCount());

    Console.WriteLine("Press any key to exit...");
}