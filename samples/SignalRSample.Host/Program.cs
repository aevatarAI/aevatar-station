using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Extensions;
using Aevatar.PermissionManagement.Extensions;
using Aevatar.SignalR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Streams.Kafka.Config;
using SignalRSample.Host;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.secrets.json", optional: true)
    .Build();

builder.Host.UseOrleans(silo =>
{
    silo.AddMemoryGrainStorage("PubSubStore")
        .AddLogStorageBasedLogConsistencyProvider("LogStorage")
        .UseLocalhostClustering()
        .ConfigureServices(services =>
        {
            services.AddTransient<IGAgentFactory, GAgentFactory>();
        })
        .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Information).AddConsole(); })
        .UseAevatar()
        .UseSignalR()
        .RegisterHub<AevatarSignalRHub>();
    silo.Services.AddSingleton<IStateDispatcher, StateDispatcher>();
    silo.Services.AddSingleton<IStateProjector, TestStateProjector>();
    silo.AddLogStorageBasedLogConsistencyProvider("LogStorage");
    var streamProvider = configuration.GetSection("OrleansStream:Provider").Get<string>();
    if (streamProvider == "Kafka")
    {
        silo.AddKafka("Aevatar")
            .WithOptions(options =>
            {
                options.BrokerList = configuration.GetSection("OrleansStream:Brokers").Get<List<string>>();
                options.ConsumerGroupId = "Aevatar";
                options.ConsumeMode = ConsumeMode.LastCommittedMessage;

                var partitions = configuration.GetSection("OrleansStream:Partitions").Get<int>();
                var replicationFactor =
                    configuration.GetSection("OrleansStream:ReplicationFactor").Get<short>();
                var topic = configuration.GetSection("OrleansStream:Topic").Get<string>();
                topic = topic.IsNullOrEmpty() ? "Aevatar" : topic;

                var topics = new List<string>
                    { "SERVER_DISCONNECT_STREAM", "SERVER_STREAM", "CLIENT_DISCONNECT_STREAM", "ALL_STREAM", topic };
                foreach (var tpc in topics)
                {
                    options.AddTopic(tpc, new TopicCreationConfig
                    {
                        AutoCreate = true,
                        Partitions = partitions,
                        ReplicationFactor = replicationFactor
                    });
                }
            })
            .AddJson()
            .AddLoggingTracker()
            .Build();
    }
    else
    {
        silo.AddMemoryStreams("Aevatar");
    }
});

builder.WebHost.UseKestrel((_, kestrelOptions) =>
{
    kestrelOptions.ListenAnyIP( 5001);
});

builder.Services.AddSignalR().AddOrleans();

builder.Services.AddHostedService<HostedService>();

var app = builder.Build();

app.UseRouting();
app.UseAuthorization();
app.MapHub<AevatarSignalRHub>("/aevatarHub");
await app.RunAsync();