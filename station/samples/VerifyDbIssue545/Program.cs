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

        // client.UseLocalhostClustering(gatewayPort: 20001)
            // .AddMemoryStreams(AevatarCoreConstants.StreamProvider);
            .AddKafka("Aevatar")
            .WithOptions(options =>
            {
                options.BrokerList = new List<string> { "localhost:9092" };  // BrokerList expects List<string>
                options.ConsumerGroupId = "Aevatar";
                options.ConsumeMode = ConsumeMode.LastCommittedMessage;

                var partitions = 1;
                var replicationFactor = (short)1;  // ReplicationFactor should be short
                var topics = "Aevatar,AevatarStateProjection,AevatarBroadCast";
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
const int subscriberCount = 1;

var sw = new Stopwatch();
sw.Start();
// Create a new grain instance for the sub-agent
var subAgents = new List<ITestDbGAgent>();
for (var i = 0; i < subscriberCount; ++i)
{
    var sws = new Stopwatch();
    // var subAgentId = Guid.NewGuid();
    //6bb4d647cd034339ac655b34f0ef4d9f working
    var subAgentId = Guid.Parse("6bb4d647cd034339ac655b34f0ef4d9f");
    Console.WriteLine("subAgent Guid: {0}", subAgentId.ToString("N"));
    var subAgent = client.GetGrain<ITestDbGAgent>(subAgentId);
    
    sws.Start();
    await subAgent.ActivateAsync();
    sws.Stop();
    Console.WriteLine("Time taken to create agent-{0}: {1} ms", i, sws.ElapsedMilliseconds);
    subAgents.Add(subAgent);
}
sw.Stop();
Console.WriteLine("Time taken to create {0} sub-agents: {1} ms", subscriberCount, sw.ElapsedMilliseconds);
// Create a new grain instance for the publisher agent
// var pubAgentId = Guid.NewGuid();
var pubAgentId = Guid.Parse("8de50e3952884d928bcff15b68123eab");

var pubAgent = client.GetGrain<ITestDbScheduleGAgent>(pubAgentId);

Console.WriteLine("pubAgent: {0}", pubAgentId.ToString("N"));
if (subscriberCount > 0)
{
    Console.WriteLine("subAgent-{0} Count {1}", subscriberCount, await subAgents[subscriberCount - 1].GetCount());
}

var TestDbEvent = new TestDbEvent
{
    Number = 100,
    CorrelationId = Guid.NewGuid(),
    PublisherGrainId = pubAgent.GetGrainId(),
};

await pubAgent.BroadCastEventAsync("TestDbScheduleGAgent", TestDbEvent);

// Wait for the event to be processed
await Task.Delay(5000);

var count = 0;
for (var i = 0; i < subscriberCount; ++i)
{
    if (await subAgents[i].GetCount() != 100)
    {
        Console.WriteLine("subAgent-{0} Count {1}", i+1, await subAgents[i].GetCount());
        count++;
    }
}

if (count > 0)
{
    Console.WriteLine("Total missing is {0}", count);
}

if (subscriberCount > 0)
{
    Console.WriteLine("subAgent-{0} Count {1}", subscriberCount, await subAgents[subscriberCount - 1].GetCount());
}

// await pubAgent.BroadCastEventAsync("TestDbScheduleGAgent", TestDbEvent);

// Wait for the event to be processed
// await Task.Delay(1000);

// count = 0;
// for (var i = 0; i < subscriberCount; ++i)
// {
//     if (await subAgents[i].GetCount() != 200)
//     {
//        count++;
//     }
// }

// if (count > 0)
// {
//     Console.WriteLine("Total missing is {0}", count);
// }

// if (subscriberCount > 0)
// {
//     Console.WriteLine("subAgent-{0} Count {1}", subscriberCount, await subAgents[subscriberCount - 1].GetCount());
// }

Console.WriteLine("Press any key to exit...");

await host.StopAsync();