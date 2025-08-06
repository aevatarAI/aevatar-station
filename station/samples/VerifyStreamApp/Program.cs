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

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        //client.UseLocalhostClustering();
        var hostId = "Aevatar";
        client.UseMongoDBClient("mongodb://localhost:27017/?maxPoolSize=555")
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
            .AddActivityPropagation();
        client.UseLocalhostClustering(gatewayPort: 20001)
            // .AddMemoryStreams(AevatarCoreConstants.StreamProvider);
            .AddKafka("Aevatar")
            .WithOptions(options =>
            {
                options.BrokerList = new List<string> { "localhost:9092" };  // BrokerList expects List<string>
                options.ConsumerGroupId = "Aevatar";
                options.ConsumeMode = ConsumeMode.LastCommittedMessage;

                var partitions = 1;
                var replicationFactor = (short)1;  // ReplicationFactor should be short
                var topics = "Aevatar";
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


var subAgentId = Guid.NewGuid();
var subAgent = client.GetGrain<ITestDbGAgent>(subAgentId);
await subAgent.ActivateAsync();
await Task.Delay(2000);

// Create a new grain instance for the publisher agent
// test broadcast
var pubAgentId = Guid.NewGuid();
var pubAgent = client.GetGrain<ITestDbScheduleGAgent>(pubAgentId);

var demoEvent = new TestDbEvent
{
    Number = 100,
    CorrelationId = Guid.NewGuid(),
    PublisherGrainId = pubAgent.GetGrainId(),
};

await pubAgent.BroadcastEventAsync("TestDbScheduleGAgent", demoEvent);

await Task.Delay(500);

Console.WriteLine("Count is {0}", await subAgent.GetCount());

// test p2p
var p2pAgentId1 = Guid.NewGuid();
var p2pAgent1 = client.GetGrain<ITestDbGAgent>(p2pAgentId1);
await p2pAgent1.ActivateAsync();

var p2pAgentId2 = Guid.NewGuid();
var p2pAgent2 = client.GetGrain<ITestDbGAgent>(p2pAgentId2);
await p2pAgent2.ActivateAsync();

await p2pAgent1.PublishAsync(p2pAgent2.GetGrainId(), demoEvent);

await Task.Delay(500);
Console.WriteLine("Count is {0}", await p2pAgent2.GetCount());

Console.WriteLine("Press any key to exit...");

await host.StopAsync();