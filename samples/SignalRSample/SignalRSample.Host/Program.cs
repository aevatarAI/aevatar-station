using Aevatar.Core.Abstractions;
using Aevatar.Extensions;
using Aevatar.SignalR;
using Microsoft.AspNetCore.Builder;
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

builder.Services
    .AddSignalR()
    .AddOrleans();

var app = builder.Build();
app.UseStaticFiles();
app.UseDefaultFiles();

app.MapHub<AevatarSignalRHub>("/aevatarHub");
await app.RunAsync();