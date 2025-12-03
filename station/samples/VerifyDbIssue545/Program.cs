using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.IO;

using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Streams.Kafka.Config;
using E2E.Grains;
using System.Diagnostics;
using VerifyDbIssue545;
using Aevatar.Core.Streaming.Extensions;

// Parse command line arguments
bool useStoredIds = true; // Default to using stored IDs
int subscriberCount = 1; // Default value

// Display usage information
void ShowUsage()
{
    Console.WriteLine("Usage: VerifyDbIssue545 [options]");
    Console.WriteLine("Options:");
    Console.WriteLine("  --use-stored-ids              Use stored agent IDs from agent_ids.json");
    Console.WriteLine("  --subscribers <count>         Number of subscriber agents to create (default: 1)");
    Console.WriteLine("  --subscriber-count <count>    Alias for --subscribers");
    Console.WriteLine("  --help                        Show this help message");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  VerifyDbIssue545");
    Console.WriteLine("  VerifyDbIssue545 --subscribers 5000");
    Console.WriteLine("  VerifyDbIssue545 --use-stored-ids --subscribers 1000");
}

// Parse command line arguments
for (int i = 0; i < args.Length; i++)
{
    string arg = args[i].ToLower();
    
    switch (arg)
    {
        case "--use-stored-ids":
            useStoredIds = true;
            break;
            
        case "--subscribers":
        case "--subscriber-count":
            if (i + 1 < args.Length && int.TryParse(args[i + 1], out int subscriberCountValue) && subscriberCountValue > 0)
            {
                subscriberCount = subscriberCountValue;
                i++; // Skip the next argument as it's the count value
            }
            else
            {
                Console.WriteLine($"Error: {arg} requires a positive integer value");
                ShowUsage();
                return;
            }
            break;
            
        case "--help":
        case "-h":
            ShowUsage();
            return;
            
        default:
            Console.WriteLine($"Error: Unknown argument '{args[i]}'");
            ShowUsage();
            return;
    }
}

Console.WriteLine($"Using stored agent IDs: {useStoredIds}");
Console.WriteLine($"Subscriber count: {subscriberCount}");

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        //client.UseLocalhostClustering();
        var hostId = "Aevatar";
        client.UseMongoDBClient("mongodb://localhost:27017")
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
            .AddAevatarKafkaStreaming("Aevatar", options =>
            {
                options.BrokerList = new List<string> { "localhost:9092" };
                options.ConsumerGroupId = "Aevatar";
                options.ConsumeMode = ConsumeMode.LastCommittedMessage;

                var partitions = 8; // Multiple partitions for load distribution
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
            });
            // .WithOptions(options =>
            // {
                
            // })
            // .AddJson()  // Add logging tracker for better observability
            // .Build();
    })
    .ConfigureLogging(logging => logging.AddConsole())
    .UseConsoleLifetime();

using IHost host = builder.Build();
await host.StartAsync();

var client = host.Services.GetRequiredService<IClusterClient>();
var agentIdsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "agent_ids.json");

// Function to save agent IDs to JSON file
async Task SaveAgentIdsAsync(AgentIds agentIds)
{
    try
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(agentIds, options);
        await File.WriteAllTextAsync(agentIdsFilePath, jsonString);
        Console.WriteLine($"Agent IDs saved to {agentIdsFilePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error saving agent IDs: {ex.Message}");
    }
}

// Function to load agent IDs from JSON file
async Task<AgentIds> LoadAgentIdsAsync()
{
    try
    {
        if (!File.Exists(agentIdsFilePath))
        {
            Console.WriteLine($"No agent IDs file found at {agentIdsFilePath}");
            return new AgentIds();
        }

        string jsonString = await File.ReadAllTextAsync(agentIdsFilePath);
        var agentIds = JsonSerializer.Deserialize<AgentIds>(jsonString);
        if (agentIds == null)
        {
            Console.WriteLine("Failed to deserialize agent IDs, creating new ones");
            return new AgentIds();
        }
        Console.WriteLine($"Loaded {agentIds.SubAgentIds.Count} sub-agent IDs and pub-agent ID from {agentIdsFilePath}");
        return agentIds;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading agent IDs: {ex.Message}");
        return new AgentIds();
    }
}

var sw = new Stopwatch();
sw.Start();
// Create a new grain instance for the sub-agent
var subAgents = new List<ITestDbGAgent>();
var agentIds = new AgentIds();

// Create a dictionary to store initial counts for each agent
var initialCounts = new Dictionary<int, int>();

// Load existing IDs or create new ones
if (useStoredIds && File.Exists(agentIdsFilePath))
{
    agentIds = await LoadAgentIdsAsync();
    Console.WriteLine($"Found {agentIds.SubAgentIds.Count} stored sub-agent IDs");
    
    // Create agents with stored IDs
    for (var i = 0; i < Math.Min(subscriberCount, agentIds.SubAgentIds.Count); ++i)
    {
        var sws = new Stopwatch();
        var subAgentId = Guid.Parse(agentIds.SubAgentIds[i]);
        // var subAgentId = Guid.Parse("df22c65b73fe41259082f98bce405e54");

        Console.WriteLine($"Using stored subAgent-{i}: {subAgentId.ToString("N")}");
        var subAgent = client.GetGrain<ITestDbGAgent>(subAgentId);
        
        sws.Start();
        await subAgent.ActivateAsync();
        // Store the initial count for this agent
        initialCounts[i] = await subAgent.GetCount();
        sws.Stop();
        Console.WriteLine($"Time taken to activate agent-{i}: {sws.ElapsedMilliseconds} ms, initial count: {initialCounts[i]}");
        subAgents.Add(subAgent);
    }
    
    // If we need more agents than were stored
    if (subscriberCount > agentIds.SubAgentIds.Count)
    {
        Console.WriteLine($"Need to create {subscriberCount - agentIds.SubAgentIds.Count} additional sub-agents");
        for (var i = agentIds.SubAgentIds.Count; i < subscriberCount; ++i)
        {
            var sws = new Stopwatch();
            var subAgentId = Guid.NewGuid();
            agentIds.SubAgentIds.Add(subAgentId.ToString());
            Console.WriteLine($"Created new subAgent-{i}: {subAgentId.ToString("N")}");
            var subAgent = client.GetGrain<ITestDbGAgent>(subAgentId);
            
            sws.Start();
            await subAgent.ActivateAsync();
            // Store the initial count for this agent
            initialCounts[i] = await subAgent.GetCount();
            sws.Stop();
            Console.WriteLine($"Time taken to activate agent-{i}: {sws.ElapsedMilliseconds} ms, initial count: {initialCounts[i]}");
            subAgents.Add(subAgent);
        }
    }
}
else
{
    // Create new agents with new IDs
    Console.WriteLine($"Creating {subscriberCount} new sub-agents");
    agentIds = new AgentIds();
    for (var i = 0; i < subscriberCount; ++i)
    {
        var sws = new Stopwatch();
        var subAgentId = Guid.NewGuid();
        agentIds.SubAgentIds.Add(subAgentId.ToString());
        Console.WriteLine($"Created new subAgent-{i}: {subAgentId.ToString("N")}");
        var subAgent = client.GetGrain<ITestDbGAgent>(subAgentId);
        
        sws.Start();
        await subAgent.ActivateAsync();
        // Store the initial count for this agent
        initialCounts[i] = await subAgent.GetCount();
        sws.Stop();
        Console.WriteLine($"Time taken to activate agent-{i}: {sws.ElapsedMilliseconds} ms, initial count: {initialCounts[i]}");
        subAgents.Add(subAgent);
    }
}

sw.Stop();
Console.WriteLine("Time taken to create {0} sub-agents: {1} ms", subscriberCount, sw.ElapsedMilliseconds);

// Get or create publisher agent ID
Guid pubAgentId;
if (useStoredIds && !string.IsNullOrEmpty(agentIds.PubAgentId))
{
    pubAgentId = Guid.Parse(agentIds.PubAgentId);
    Console.WriteLine($"Using stored pubAgent: {pubAgentId.ToString("N")} from {agentIdsFilePath}");
}
else
{
    pubAgentId = Guid.NewGuid();
    agentIds.PubAgentId = pubAgentId.ToString();
    Console.WriteLine($"Created new pubAgent: {pubAgentId.ToString("N")} and will save to {agentIdsFilePath}");
}

// Save all IDs to the JSON file
await SaveAgentIdsAsync(agentIds);

var pubAgent = client.GetGrain<ITestDbScheduleGAgent>(pubAgentId);

// Define the event number
int eventNumber = 100;

var TestDbEvent = new TestDbEvent
{
    Number = eventNumber, // Using the defined variable
    CorrelationId = Guid.NewGuid(),
    PublisherGrainId = pubAgent.GetGrainId(),
};

Console.WriteLine($"Broadcasting event with Number = {eventNumber}");
await pubAgent.BroadcastEventAsync("TestDbScheduleGAgent", TestDbEvent);

// Wait for the event to be processed
await Task.Delay(5000);

var count = 0;
for (var i = 0; i < subscriberCount; ++i)
{
    int currentCount = await subAgents[i].GetCount();
    int expectedCount = initialCounts[i] + eventNumber;
    
    if (currentCount != expectedCount)
    {
        Console.WriteLine($"subAgent-{i} Count: {currentCount}, Expected: {expectedCount} (Initial: {initialCounts[i]} + Event: {eventNumber})");
        count++;
    }
}

if (count > 0)
{
    Console.WriteLine($"Total agents with incorrect counts: {count}");
}
else
{
    Console.WriteLine($"All {subscriberCount} agents received the correct count increment of {eventNumber}");
}

if (subscriberCount > 0)
{
    int lastAgentIndex = subscriberCount - 1;
    int finalCount = await subAgents[lastAgentIndex].GetCount();
    int expectedFinalCount = initialCounts[lastAgentIndex] + eventNumber;
    Console.WriteLine($"Last subAgent-{lastAgentIndex} Count: {finalCount}, Expected: {expectedFinalCount} (Initial: {initialCounts[lastAgentIndex]} + Event: {eventNumber})");
}

// Add summary
if (useStoredIds && File.Exists(agentIdsFilePath))
{
    Console.WriteLine($"Test completed using stored agent IDs from {agentIdsFilePath}");
}
else
{
    Console.WriteLine($"Test completed using newly generated agent IDs, saved to {agentIdsFilePath}");
}
Console.WriteLine($"Run with --use-stored-ids to reuse the same agent IDs in future tests");

Console.WriteLine("Press any key to exit...");

await host.StopAsync();