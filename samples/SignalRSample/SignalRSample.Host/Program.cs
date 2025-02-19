using Aevatar.Core.Abstractions;
using Aevatar.Extensions;
using Aevatar.SignalR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SignalRSample.Host;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(silo =>
{
    silo.AddMemoryStreams(AevatarCoreConstants.StreamProvider)
        .AddMemoryGrainStorage("PubSubStore")
        .AddLogStorageBasedLogConsistencyProvider("LogStorage")
        .UseLocalhostClustering()
        .UseAevatar()
        .UseSignalR()
        .RegisterHub<AevatarSignalRHub>();
});

builder.WebHost.UseKestrel((_, kestrelOptions) =>
{
    kestrelOptions.ListenLocalhost(5001);
});

builder.Services.AddSignalR().AddOrleans();

builder.Services.AddHostedService<HostedService>();

var app = builder.Build();

app.UseRouting();
app.UseAuthorization();
app.MapHub<AevatarSignalRHub>("/aevatarHub");
await app.RunAsync();