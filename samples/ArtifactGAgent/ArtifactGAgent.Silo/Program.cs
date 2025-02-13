using Aevatar.Core.Abstractions;
using Aevatar.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp.PermissionManagement;

var builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(silo =>
    {
        silo.AddMemoryGrainStorage("Default")
            .AddMemoryStreams(AevatarCoreConstants.StreamProvider)
            .AddMemoryGrainStorage("PubSubStore")
            .AddLogStorageBasedLogConsistencyProvider("LogStorage")
            .UseLocalhostClustering()
            .UseAevatar()
            .ConfigureLogging(logging => logging.AddConsole());
    })
    .UseConsoleLifetime();

using var host = builder.Build();

var permissionManager = host.Services.GetRequiredService<IPermissionManager>();

await host.RunAsync();