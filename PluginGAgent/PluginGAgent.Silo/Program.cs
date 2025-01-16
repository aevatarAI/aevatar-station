using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.EventSourcing.MongoDB.Hosting;
using Aevatar.Plugins;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

var builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(silo =>
    {
        silo.AddMemoryGrainStorage("Default")
            .AddMemoryStreams(AevatarCoreConstants.StreamProvider)
            .AddMemoryGrainStorage("PubSubStore")
            .AddMongoDbStorageBasedLogConsistencyProvider("LogStorage", options =>
            {
                options.ClientSettings =
                    MongoClientSettings.FromConnectionString("mongodb://localhost:27017/?maxPoolSize=555");
                options.Database = "Aevatar";
            })
            .UseLocalhostClustering()
            .ConfigureLogging(logging => logging.AddConsole())
            .ConfigureServices(services =>
            {
                services.AddSingleton<IGAgentFactory, GAgentFactory>();
                services.AddSingleton<ApplicationPartManager>();
                services.AddSingleton<PluginGAgentManager>();
                services.AddSingleton<ILifecycleParticipant<ISiloLifecycle>>(sp => sp.GetRequiredService<PluginGAgentManager>());
            });
    })
    .UseConsoleLifetime();

using var host = builder.Build();

await host.RunAsync();