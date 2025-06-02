using Aevatar.Core.Abstractions;
using Aevatar.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .UseOrleans(silo =>
    {
        silo.AddMemoryGrainStorage("Default")
            .AddMemoryStreams(AevatarCoreConstants.StreamProvider)
            .AddMemoryGrainStorage("PubSubStore")
            .AddLogStorageBasedLogConsistencyProvider("LogStorage")
            .UseLocalhostClustering()
            .UseAevatar(includingAbpServices: true)
            .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Information).AddConsole());
    })
    .UseConsoleLifetime();

var host = builder.Build();

Console.WriteLine("Starting WeatherAgent Plugin Silo...");
Console.WriteLine("Press Ctrl+C to shut down the silo");

await host.RunAsync(); 