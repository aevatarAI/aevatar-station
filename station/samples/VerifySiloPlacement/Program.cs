using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;


using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Streams.Kafka.Config;
using E2E.Grains;
using System.Diagnostics;

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        //client.UseLocalhostClustering();
        var hostId = "Aevatar";
        client.UseMongoDBClient("mongodb://localhost:27017/?maxPoolSize=15000")
            .UseMongoDBClustering(options =>
            {
                options.DatabaseName = "AevatarDb";
                options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                options.CollectionPrefix = hostId.IsNullOrEmpty() ? "OrleansAevatar" : $"Orleans{hostId}";
            })
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "AevatarSiloCluster";
                options.ServiceId = "AevatarBasicService";
            })
            .AddActivityPropagation()
            // .AddMemoryStreams(AevatarCoreConstants.StreamProvider);
            .AddKafka("Aevatar")
            .WithOptions(options =>
            {
                options.BrokerList = new List<string> { "localhost:9092" };  // BrokerList expects List<string>
                options.ConsumerGroupId = "Aevatar";
                options.ConsumeMode = ConsumeMode.LastCommittedMessage;

                var partitions = 1;
                var replicationFactor = (short)1;  // ReplicationFactor should be short
                var topics = "Aevatar,AevatarStateProjection,AevatarBroadcast";
                foreach (var topic in topics.Split(','))
                {
                    options.AddTopic(topic.Trim(), new TopicCreationConfig
                    {
                        AutoCreate = true,
                        Partitions = partitions,
                        ReplicationFactor = replicationFactor
                    });
                }
            })
            .AddJson()  // Add logging tracker for better observability
            .Build();
    })
    .ConfigureLogging(logging => logging.AddConsole())
    .UseConsoleLifetime();

using IHost host = builder.Build();
await host.StartAsync();

var client = host.Services.GetRequiredService<IClusterClient>();

//create a new schedule agent
var scheduleAgent = client.GetGrain<ITestDbScheduleGAgent>(Guid.NewGuid());
var scheResult = await scheduleAgent.GetDescriptionAsync();
Console.WriteLine(scheResult);

//create a new user agent
var userAgent = client.GetGrain<ITestDbGAgent>(Guid.NewGuid());
var userResult = await userAgent.GetDescriptionAsync();
Console.WriteLine(userResult);

await host.StopAsync();