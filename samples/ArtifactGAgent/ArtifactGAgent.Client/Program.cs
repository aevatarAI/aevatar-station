using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Orleans.Streams;

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.UseLocalhostClustering()
            .AddMemoryStreams(AevatarCoreConstants.StreamProvider);
        client.Services.AddSingleton<IGAgentFactory, GAgentFactory>();
        client.Services.AddSingleton<IGAgentManager, GAgentManager>();
    })
    .ConfigureLogging(logging => logging.AddConsole())
    .UseConsoleLifetime();

using IHost host = builder.Build();
await host.StartAsync();

var gAgentFactory = host.Services.GetRequiredService<IGAgentFactory>();
var gAgentManager = host.Services.GetRequiredService<IGAgentManager>();
var allGAgents = gAgentManager.GetAvailableGAgentTypes();
var myArtifactGAgent = await gAgentFactory.GetGAgentAsync(nameof(MyArtifact));
var description = await myArtifactGAgent.GetDescriptionAsync();
;