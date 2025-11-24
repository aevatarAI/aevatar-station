using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Speech;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(silo =>
    {
        silo.AddMemoryGrainStorage("Default")
            .AddMemoryStreams(AevatarCoreConstants.StreamProvider)
            .AddMemoryGrainStorage("PubSubStore")
            .AddLogStorageBasedLogConsistencyProvider("LogStorage")
            .UseLocalhostClustering()
            .ConfigureLogging(logging => logging.AddConsole());
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<SpeechOptions>(context.Configuration.GetSection("Speech"));
    })
    .UseConsoleLifetime();

using var host = builder.Build();

await host.RunAsync();