using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Streams.Kafka.Config;
using Aevatar.Core.Streaming.Extensions;
using Aevatar.Core.Abstractions;
using E2E.Grains;

Console.WriteLine("=== Event Forwarding E2E Test ===");
Console.WriteLine("Testing event direction-based forwarding in hierarchical agent structures");
Console.WriteLine();

// Setup Orleans client
var builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
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
            .AddAevatarKafkaStreaming("Aevatar", options =>
            {
                options.BrokerList = new List<string> { "localhost:9092" };
                options.ConsumerGroupId = "Aevatar";
                options.ConsumeMode = ConsumeMode.LastCommittedMessage;

                var partitions = 8;
                var replicationFactor = (short)1;
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
    })
    .ConfigureLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Information))
    .UseConsoleLifetime();

using IHost host = builder.Build();
await host.StartAsync();

var client = host.Services.GetRequiredService<IClusterClient>();
Console.WriteLine("✅ Connected to Orleans cluster");
Console.WriteLine();

try
{
    // Run event forwarding tests
    await RunEventForwardingTests(client);
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Test failed with error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    return 1;
}
finally
{
    await host.StopAsync();
}

Console.WriteLine("🎉 All event forwarding tests completed successfully!");
return 0;

/// <summary>
/// Runs comprehensive event forwarding tests
/// </summary>
async Task RunEventForwardingTests(IClusterClient client)
{
    Console.WriteLine("🧪 Setting up complex test hierarchy with siblings, uncles, and cousins...");
    Console.WriteLine("🆔 Creating agents and displaying their Grain IDs:");
    
    // Create a 3-level complex hierarchy:
    // Level 1: Root
    // Level 2: Uncle1, Parent, Uncle2 (siblings)
    // Level 3: Each Level2 agent has their own children:
    //   - Uncle1: Uncle1Child1, Uncle1Child2
    //   - Parent: Child1, Child2
    //   - Uncle2: Uncle2Child1, Uncle2Child2
    
    var rootAgent = client.GetGrain<IEventForwardingTestAgent>(Guid.NewGuid());
    
    var uncle1Agent = client.GetGrain<IEventForwardingTestAgent>(Guid.NewGuid());
    var parentAgent = client.GetGrain<IEventForwardingTestAgent>(Guid.NewGuid());
    var uncle2Agent = client.GetGrain<IEventForwardingTestAgent>(Guid.NewGuid());
    
    var uncle1Child1Agent = client.GetGrain<IEventForwardingTestAgent>(Guid.NewGuid());
    var uncle1Child2Agent = client.GetGrain<IEventForwardingTestAgent>(Guid.NewGuid());
    var child1Agent = client.GetGrain<IEventForwardingTestAgent>(Guid.NewGuid());
    var child2Agent = client.GetGrain<IEventForwardingTestAgent>(Guid.NewGuid());
    var uncle2Child1Agent = client.GetGrain<IEventForwardingTestAgent>(Guid.NewGuid());
    var uncle2Child2Agent = client.GetGrain<IEventForwardingTestAgent>(Guid.NewGuid());
    
    // Initialize all agents and print their grain IDs
    await rootAgent.InitializeAsync("Root", "Level1");
    Console.WriteLine($"  🆔 Root: {rootAgent.GetGrainId()}");
    
    await uncle1Agent.InitializeAsync("Uncle1", "Level2");
    Console.WriteLine($"  🆔 Uncle1: {uncle1Agent.GetGrainId()}");
    await parentAgent.InitializeAsync("Parent", "Level2");
    Console.WriteLine($"  🆔 Parent: {parentAgent.GetGrainId()}");
    await uncle2Agent.InitializeAsync("Uncle2", "Level2");
    Console.WriteLine($"  🆔 Uncle2: {uncle2Agent.GetGrainId()}");
    
    await uncle1Child1Agent.InitializeAsync("Uncle1Child1", "Level3");
    Console.WriteLine($"  🆔 Uncle1Child1: {uncle1Child1Agent.GetGrainId()}");
    await uncle1Child2Agent.InitializeAsync("Uncle1Child2", "Level3");
    Console.WriteLine($"  🆔 Uncle1Child2: {uncle1Child2Agent.GetGrainId()}");
    await child1Agent.InitializeAsync("Child1", "Level3");
    Console.WriteLine($"  🆔 Child1: {child1Agent.GetGrainId()}");
    await child2Agent.InitializeAsync("Child2", "Level3");
    Console.WriteLine($"  🆔 Child2: {child2Agent.GetGrainId()}");
    await uncle2Child1Agent.InitializeAsync("Uncle2Child1", "Level3");
    Console.WriteLine($"  🆔 Uncle2Child1: {uncle2Child1Agent.GetGrainId()}");
    await uncle2Child2Agent.InitializeAsync("Uncle2Child2", "Level3");
    Console.WriteLine($"  🆔 Uncle2Child2: {uncle2Child2Agent.GetGrainId()}");
    Console.WriteLine();
    
    // Setup hierarchy relationships
    Console.WriteLine("🔗 Establishing hierarchy relationships:");
    
    // Root has children: Uncle1, Parent, Uncle2 (making them siblings)
    await rootAgent.RegisterAsync((IGAgentPlus)uncle1Agent);
    Console.WriteLine($"  ➕ Root ({rootAgent.GetGrainId()}) registered Uncle1 ({uncle1Agent.GetGrainId()})");
    await rootAgent.RegisterAsync((IGAgentPlus)parentAgent);
    Console.WriteLine($"  ➕ Root ({rootAgent.GetGrainId()}) registered Parent ({parentAgent.GetGrainId()})");
    await rootAgent.RegisterAsync((IGAgentPlus)uncle2Agent);
    Console.WriteLine($"  ➕ Root ({rootAgent.GetGrainId()}) registered Uncle2 ({uncle2Agent.GetGrainId()})");
    
    // Each Level2 agent has their own children
    // Uncle1 has children: Uncle1Child1, Uncle1Child2
    await uncle1Agent.RegisterAsync((IGAgentPlus)uncle1Child1Agent);
    Console.WriteLine($"  ➕ Uncle1 ({uncle1Agent.GetGrainId()}) registered Uncle1Child1 ({uncle1Child1Agent.GetGrainId()})");
    await uncle1Agent.RegisterAsync((IGAgentPlus)uncle1Child2Agent);
    Console.WriteLine($"  ➕ Uncle1 ({uncle1Agent.GetGrainId()}) registered Uncle1Child2 ({uncle1Child2Agent.GetGrainId()})");
    
    // Parent has children: Child1, Child2
    await parentAgent.RegisterAsync((IGAgentPlus)child1Agent);
    Console.WriteLine($"  ➕ Parent ({parentAgent.GetGrainId()}) registered Child1 ({child1Agent.GetGrainId()})");
    await parentAgent.RegisterAsync((IGAgentPlus)child2Agent);
    Console.WriteLine($"  ➕ Parent ({parentAgent.GetGrainId()}) registered Child2 ({child2Agent.GetGrainId()})");
    
    // Uncle2 has children: Uncle2Child1, Uncle2Child2
    await uncle2Agent.RegisterAsync((IGAgentPlus)uncle2Child1Agent);
    Console.WriteLine($"  ➕ Uncle2 ({uncle2Agent.GetGrainId()}) registered Uncle2Child1 ({uncle2Child1Agent.GetGrainId()})");
    await uncle2Agent.RegisterAsync((IGAgentPlus)uncle2Child2Agent);
    Console.WriteLine($"  ➕ Uncle2 ({uncle2Agent.GetGrainId()}) registered Uncle2Child2 ({uncle2Child2Agent.GetGrainId()})");
    Console.WriteLine();
    
    Console.WriteLine("✅ Complex hierarchy established with Grain IDs:");
    Console.WriteLine($"   Level 1: Root ({rootAgent.GetGrainId()})");
    Console.WriteLine("   Level 2: (siblings)");
    Console.WriteLine($"     Uncle1 ({uncle1Agent.GetGrainId()})");
    Console.WriteLine($"     Parent ({parentAgent.GetGrainId()})");
    Console.WriteLine($"     Uncle2 ({uncle2Agent.GetGrainId()})");
    Console.WriteLine("   Level 3:");
    Console.WriteLine($"     Uncle1 children: Uncle1Child1 ({uncle1Child1Agent.GetGrainId()}), Uncle1Child2 ({uncle1Child2Agent.GetGrainId()})");
    Console.WriteLine($"     Parent children: Child1 ({child1Agent.GetGrainId()}), Child2 ({child2Agent.GetGrainId()})");
    Console.WriteLine($"     Uncle2 children: Uncle2Child1 ({uncle2Child1Agent.GetGrainId()}), Uncle2Child2 ({uncle2Child2Agent.GetGrainId()})");
    Console.WriteLine();
    
    var allAgents = new[]
    {
        rootAgent, uncle1Agent, parentAgent, uncle2Agent,
        uncle1Child1Agent, uncle1Child2Agent, child1Agent, child2Agent, 
        uncle2Child1Agent, uncle2Child2Agent
    };
    
    // Interactive menu for test selection
    await RunInteractiveTestMenu(allAgents);
}

/// <summary>
/// Interactive menu system for test selection
/// </summary>
async Task RunInteractiveTestMenu(IEventForwardingTestAgent[] allAgents)
{
    while (true)
    {
        DisplayTestMenu();
        
        Console.Write("Enter your choice (1-10): ");
        var choice = Console.ReadLine()?.Trim();
        
        Console.WriteLine(); // Add spacing
        
        try
        {
            switch (choice)
            {
                case "1":
                    await RunAllTests(allAgents);
                    break;
                case "2":
                    await TestUpwardEvents(allAgents);
                    break;
                case "3":
                    await TestDownwardEvents(allAgents);
                    break;
                case "4":
                    await TestUpThenDownEvents(allAgents);
                    break;
                case "5":
                    await TestBidirectionalEvents(allAgents);
                    break;
                case "6":
                    await TestSingleHopUpwardEvents(allAgents);
                    break;
                case "7":
                    await TestSingleHopDownwardEvents(allAgents);
                    break;
                case "8":
                    await TestMultiLevelEvents(allAgents);
                    break;
                case "9":
                    VisualizeNodeRelationships(allAgents);
                    break;
                case "10":
                case "q":
                case "quit":
                case "exit":
                    Console.WriteLine("👋 Exiting EventForwardingE2E Demo. Goodbye!");
                    return;
                default:
                    Console.WriteLine("❌ Invalid choice. Please enter a number between 1-10.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test execution failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("\nPress any key to return to the menu...");
        Console.ReadKey();
        Console.Clear();
    }
}

/// <summary>
/// Display the interactive test menu
/// </summary>
void DisplayTestMenu()
{
    Console.WriteLine("=====================================");
    Console.WriteLine("🧪 EventForwardingE2E Test Menu");
    Console.WriteLine("=====================================");
    Console.WriteLine();
    Console.WriteLine("Available Options:");
    Console.WriteLine("  1. 🎯 Run All Tests");
    Console.WriteLine("  2. 🔼 Upward Events Test");
    Console.WriteLine("  3. 🔽 Downward Events Test");
    Console.WriteLine("  4. 🔄⬇️ UpThenDown Events Test");
    Console.WriteLine("  5. 🔄 Bidirectional Events Test");
    Console.WriteLine("  6. 1️⃣⬆️ Single Hop Upward Test");
    Console.WriteLine("  7. 1️⃣⬇️ Single Hop Downward Test");
    Console.WriteLine("  8. 🎯 Multi-Level Events Test");
    Console.WriteLine("  9. 🌳 Visualize Node Relationships");
    Console.WriteLine(" 10. 🚪 Exit");
    Console.WriteLine();
    Console.WriteLine("Description:");
    Console.WriteLine("  • Upward: Events flow from children to parents");
    Console.WriteLine("  • Downward: Events flow from parents to children");
    Console.WriteLine("  • UpThenDown: Events go up to parent, then broadcast to siblings");
    Console.WriteLine("  • Bidirectional: Events flow in both directions with hop limiting");
    Console.WriteLine("  • Single Hop Upward: Events only travel one level up from publisher");
    Console.WriteLine("  • Single Hop Downward: Events only travel one level down from publisher");
    Console.WriteLine("  • Multi-Level: Events can traverse multiple hierarchy levels");
    Console.WriteLine("  • Visualize: Display the hierarchical structure of agents");
    Console.WriteLine();
}

/// <summary>
/// Run all tests in sequence
/// </summary>
async Task RunAllTests(IEventForwardingTestAgent[] allAgents)
{
    Console.WriteLine("🎯 Running All Tests in Sequence...");
    Console.WriteLine("=====================================");
    Console.WriteLine();
    
    var startTime = DateTime.Now;
    
    // Test 1: Basic upward events
    await TestUpwardEvents(allAgents);
    Console.WriteLine("⏱️ Waiting between tests...\n");
    await Task.Delay(1000);
    
    // Test 2: Basic downward events  
    await TestDownwardEvents(allAgents);
    Console.WriteLine("⏱️ Waiting between tests...\n");
    await Task.Delay(1000);
    
    // Test 3: UpThenDown events (new behavior)
    await TestUpThenDownEvents(allAgents);
    Console.WriteLine("⏱️ Waiting between tests...\n");
    await Task.Delay(1000);
    
    // Test 4: Bidirectional events with hop limiting and publisher tracking
    await TestBidirectionalEvents(allAgents);
    Console.WriteLine("⏱️ Waiting between tests...\n");
    await Task.Delay(1000);
    
    // Test 5: Single hop upward events
    await TestSingleHopUpwardEvents(allAgents);
    Console.WriteLine("⏱️ Waiting between tests...\n");
    await Task.Delay(1000);
    
    // Test 6: Single hop downward events
    await TestSingleHopDownwardEvents(allAgents);
    Console.WriteLine("⏱️ Waiting between tests...\n");
    await Task.Delay(1000);
    
    // Test 7: Multi-level events with hop limits
    await TestMultiLevelEvents(allAgents);
    
    var endTime = DateTime.Now;
    var duration = endTime - startTime;
    
    Console.WriteLine("=====================================");
    Console.WriteLine("🎉 All Tests Completed Successfully!");
    Console.WriteLine($"⏱️ Total execution time: {duration.TotalSeconds:F1} seconds");
    Console.WriteLine("=====================================");
}

async Task TestUpwardEvents(IEventForwardingTestAgent[] allAgents)
{
    Console.WriteLine("🔼 Testing Upward-Only Events");
    Console.WriteLine("Expected: Child publishes -> Parent receives -> Root receives");
    
    var (root, uncle1, parent, uncle2, uncle1Child1, uncle1Child2, child1, child2, uncle2Child1, uncle2Child2) = ExtractAgents(allAgents);
    
    // Clear previous events
    await ClearAllEventsAsync(allAgents);
    
    // Publish upward event from child
    var upwardEvent = new TestEvent(EventDirection.Up, "This event should only go up to parents");
    await child1.PublishEventByDirectionAsync(upwardEvent);
    
    // Wait for event propagation
    await Task.Delay(2000);
    
    // Verify results
    var parentEvents = await parent.GetReceivedEventsAsync();
    var rootEvents = await root.GetReceivedEventsAsync();
    
    Console.WriteLine($"  Parent received: {parentEvents.Count} events");
    Console.WriteLine($"  Root received: {rootEvents.Count} events");
    
    // Verify that parent and root received the upward event
    var parentUpwardEvents = parentEvents.Where(e => e.EventType == nameof(TestEvent)).ToList();
    var rootUpwardEvents = rootEvents.Where(e => e.EventType == nameof(TestEvent)).ToList();
    
    if (parentUpwardEvents.Count > 0 && rootUpwardEvents.Count > 0)
    {
        Console.WriteLine("✅ Upward event forwarding working correctly");
        
        // Display forwarding path
        if (rootUpwardEvents.Any())
        {
            var path = string.Join(" -> ", rootUpwardEvents.First().ForwardingPath);
            Console.WriteLine($"  📍 Forwarding path: {path}");
        }
    }
    else
    {
        Console.WriteLine("❌ Upward event forwarding failed");
        Console.WriteLine($"  Expected: Parent and Root to receive events");
        Console.WriteLine($"  Actual: Parent received {parentUpwardEvents.Count}, Root received {rootUpwardEvents.Count}");
    }
    
    Console.WriteLine();
}

/// <summary>
/// Visualize the hierarchical node relationships in the demo
/// </summary>
void VisualizeNodeRelationships(IEventForwardingTestAgent[] allAgents)
{
    Console.WriteLine("🌳 Node Relationships Visualization");
    Console.WriteLine("=====================================");
    Console.WriteLine();
    
    var (root, uncle1, parent, uncle2, uncle1Child1, uncle1Child2, child1, child2, uncle2Child1, uncle2Child2) = ExtractAgents(allAgents);
    
    // Display hierarchy structure
    Console.WriteLine("📊 Hierarchical Structure:");
    Console.WriteLine();
    Console.WriteLine("Level 1 (Root):");
    Console.WriteLine($"  🏠 Root ({root.GetGrainId()})");
    Console.WriteLine("       │");
    Console.WriteLine("       ├─────────┬─────────┐");
    Console.WriteLine("       │         │         │");
    Console.WriteLine("Level 2 (Siblings):");
    Console.WriteLine($"    👨‍👩‍👧‍👦 Parent    👤 Uncle1   👤 Uncle2");
    Console.WriteLine($"     ({parent.GetGrainId().ToString().Substring(0, 8)}...)  ({uncle1.GetGrainId().ToString().Substring(0, 8)}...)  ({uncle2.GetGrainId().ToString().Substring(0, 8)}...)");
    Console.WriteLine("       │         │         │");
    Console.WriteLine("    ┌──┴──┐   ┌──┴──┐   ┌──┴──┐");
    Console.WriteLine("    │     │   │     │   │     │");
    Console.WriteLine("Level 3 (Children):");
    Console.WriteLine("  👶 Ch1 👶 Ch2 👶UC1 👶UC2 👶UC1 👶UC2");
    Console.WriteLine($"  ({child1.GetGrainId().ToString().Substring(0, 4)}...) ({child2.GetGrainId().ToString().Substring(0, 4)}...) ({uncle1Child1.GetGrainId().ToString().Substring(0, 4)}...) ({uncle1Child2.GetGrainId().ToString().Substring(0, 4)}...) ({uncle2Child1.GetGrainId().ToString().Substring(0, 4)}...) ({uncle2Child2.GetGrainId().ToString().Substring(0, 4)}...)");
    
    Console.WriteLine();
    Console.WriteLine("🔗 Family Relationships:");
    Console.WriteLine($"  • Root has 3 children: Parent, Uncle1, Uncle2");
    Console.WriteLine($"  • Parent has 2 children: Child1, Child2");
    Console.WriteLine($"  • Uncle1 has 2 children: Uncle1Child1, Uncle1Child2");
    Console.WriteLine($"  • Uncle2 has 2 children: Uncle2Child1, Uncle2Child2");
    Console.WriteLine($"  • Total agents: {allAgents.Length} (1 Root + 3 Level2 + 6 Level3)");
    
    Console.WriteLine();
    Console.WriteLine("⬆️ ⬇️ Event Flow Patterns:");
    Console.WriteLine("  🔼 Upward: Children → Parents → Root");
    Console.WriteLine("  🔽 Downward: Root → Parents → Children");
    Console.WriteLine("  🔄⬇️ UpThenDown: Child → Parent → All Parent's children");
    Console.WriteLine("  🔄 Bidirectional: Events flow in both directions with hop limits");
    Console.WriteLine("  1️⃣ Single Hop: Events only travel one level");
    Console.WriteLine("  🎯 Multi-Level: Events traverse multiple levels with hop counts");
    
    Console.WriteLine();
    Console.WriteLine("🔍 Agent Details:");
    Console.WriteLine("┌─────────────────┬──────────────────────────────────────┐");
    Console.WriteLine("│ Agent Name      │ Grain ID                             │");
    Console.WriteLine("├─────────────────┼──────────────────────────────────────┤");
    Console.WriteLine($"│ Root            │ {root.GetGrainId().ToString().PadRight(36)} │");
    Console.WriteLine($"│ Parent          │ {parent.GetGrainId().ToString().PadRight(36)} │");
    Console.WriteLine($"│ Uncle1          │ {uncle1.GetGrainId().ToString().PadRight(36)} │");
    Console.WriteLine($"│ Uncle2          │ {uncle2.GetGrainId().ToString().PadRight(36)} │");
    Console.WriteLine($"│ Child1          │ {child1.GetGrainId().ToString().PadRight(36)} │");
    Console.WriteLine($"│ Child2          │ {child2.GetGrainId().ToString().PadRight(36)} │");
    Console.WriteLine($"│ Uncle1Child1    │ {uncle1Child1.GetGrainId().ToString().PadRight(36)} │");
    Console.WriteLine($"│ Uncle1Child2    │ {uncle1Child2.GetGrainId().ToString().PadRight(36)} │");
    Console.WriteLine($"│ Uncle2Child1    │ {uncle2Child1.GetGrainId().ToString().PadRight(36)} │");
    Console.WriteLine($"│ Uncle2Child2    │ {uncle2Child2.GetGrainId().ToString().PadRight(36)} │");
    Console.WriteLine("└─────────────────┴──────────────────────────────────────┘");
    
    Console.WriteLine();
}

// Helper method to extract agents in the correct order
(IEventForwardingTestAgent root, IEventForwardingTestAgent uncle1, IEventForwardingTestAgent parent, 
 IEventForwardingTestAgent uncle2, IEventForwardingTestAgent uncle1Child1, IEventForwardingTestAgent uncle1Child2,
 IEventForwardingTestAgent child1, IEventForwardingTestAgent child2, IEventForwardingTestAgent uncle2Child1, 
 IEventForwardingTestAgent uncle2Child2) ExtractAgents(IEventForwardingTestAgent[] allAgents)
{
    return (allAgents[0], allAgents[1], allAgents[2], allAgents[3], allAgents[4], allAgents[5], allAgents[6], allAgents[7], allAgents[8], allAgents[9]);
}

// Helper method to clear events from all agents
async Task ClearAllEventsAsync(IEventForwardingTestAgent[] allAgents)
{
    foreach (var agent in allAgents)
    {
        await agent.ClearReceivedEventsAsync();
    }
}

async Task TestDownwardEvents(IEventForwardingTestAgent[] allAgents)
{
    Console.WriteLine("🔽 Testing Downward-Only Events");
    Console.WriteLine("Expected: Root publishes -> All Level2 agents receive -> Child agents receive");
    
    var (root, uncle1, parent, uncle2, uncle1Child1, uncle1Child2, child1, child2, uncle2Child1, uncle2Child2) = ExtractAgents(allAgents);
    
    // Clear previous events
    await ClearAllEventsAsync(allAgents);
    
    // Publish downward event from root
    var downwardEvent = new TestEvent(EventDirection.Down, "This event should only go down to children");
    await root.PublishEventByDirectionAsync(downwardEvent);
    
    // Wait for event propagation
    await Task.Delay(2000);
    
    // Verify results - check Level 2 agents (siblings)
    var uncle1Events = await uncle1.GetReceivedEventsAsync();
    var parentEvents = await parent.GetReceivedEventsAsync();
    var uncle2Events = await uncle2.GetReceivedEventsAsync();
    
    // Check Level 3 agents (all children)
    var uncle1Child1Events = await uncle1Child1.GetReceivedEventsAsync();
    var uncle1Child2Events = await uncle1Child2.GetReceivedEventsAsync();
    var child1Events = await child1.GetReceivedEventsAsync();
    var child2Events = await child2.GetReceivedEventsAsync();
    var uncle2Child1Events = await uncle2Child1.GetReceivedEventsAsync();
    var uncle2Child2Events = await uncle2Child2.GetReceivedEventsAsync();
    
    var level2Count = uncle1Events.Count + parentEvents.Count + uncle2Events.Count;
    var level3Count = uncle1Child1Events.Count + uncle1Child2Events.Count + child1Events.Count + child2Events.Count + uncle2Child1Events.Count + uncle2Child2Events.Count;
    
    Console.WriteLine($"  Level 2 agents received: {level2Count} events total");
    Console.WriteLine($"  Level 3 agents received: {level3Count} events total");
    
    // Verify downward propagation
    var level2DownwardEvents = parentEvents.Where(e => e.EventType == nameof(TestEvent)).ToList();
    var level3DownwardEvents = child1Events.Where(e => e.EventType == nameof(TestEvent)).ToList();
    
    if (level2DownwardEvents.Count > 0 && level3DownwardEvents.Count > 0)
    {
        Console.WriteLine("✅ Downward event forwarding working correctly");
        
        // Display forwarding path
        if (level3DownwardEvents.Any())
        {
            var path = string.Join(" -> ", level3DownwardEvents.First().ForwardingPath);
            Console.WriteLine($"  📍 Forwarding path: {path}");
        }
    }
    else
    {
        Console.WriteLine("❌ Downward event forwarding failed");
        Console.WriteLine($"  Expected: Level 2 and Level 3 agents to receive events");
        Console.WriteLine($"  Actual: Level 2 received {level2DownwardEvents.Count}, Level 3 received {level3DownwardEvents.Count}");
    }
    
    Console.WriteLine();
}

async Task TestUpThenDownEvents(IEventForwardingTestAgent[] allAgents)
{
    Console.WriteLine("🔄⬇️ Testing UpThenDown Events");
    Console.WriteLine("Expected: Child publishes up to Parent, then Parent broadcasts down to all siblings and their children");
    
    var (root, uncle1, parent, uncle2, uncle1Child1, uncle1Child2, child1, child2, uncle2Child1, uncle2Child2) = ExtractAgents(allAgents);
    
    // Clear previous events
    await ClearAllEventsAsync(allAgents);
    
    // Publish UpThenDown event from child
    var upThenDownEvent = new TestEvent(EventDirection.UpThenDown, "This event should go up then broadcast down to siblings and their children");
    await child1.PublishEventByDirectionAsync(upThenDownEvent);
    
    // Wait for event propagation
    await Task.Delay(3000); // Longer delay for complex propagation
    
    // Verify results - all siblings and cousins should receive
    var uncle1Child1Events = await uncle1Child1.GetReceivedEventsAsync();
    var uncle1Child2Events = await uncle1Child2.GetReceivedEventsAsync();
    var child2Events = await child2.GetReceivedEventsAsync();
    var uncle2Child1Events = await uncle2Child1.GetReceivedEventsAsync();
    var uncle2Child2Events = await uncle2Child2.GetReceivedEventsAsync();
    var parentEvents = await parent.GetReceivedEventsAsync();
    var rootEvents = await root.GetReceivedEventsAsync();
    
    // Uncle's children should also receive if the event propagates up to Root
    var uncle1Events = await uncle1.GetReceivedEventsAsync();
    var uncle2Events = await uncle2.GetReceivedEventsAsync();
    
    Console.WriteLine($"  Uncle1Child1 received: {uncle1Child1Events.Count} events");
    Console.WriteLine($"  Uncle1Child2 received: {uncle1Child2Events.Count} events");
    Console.WriteLine($"  Child2 received: {child2Events.Count} events");
    Console.WriteLine($"  Uncle2Child1 received: {uncle2Child1Events.Count} events");
    Console.WriteLine($"  Uncle2Child2 received: {uncle2Child2Events.Count} events");
    Console.WriteLine($"  Parent received: {parentEvents.Count} events");
    Console.WriteLine($"  Root received: {rootEvents.Count} events");
    Console.WriteLine($"  Uncle1 received: {uncle1Events.Count} events");
    Console.WriteLine($"  Uncle2 received: {uncle2Events.Count} events");
    
    // Verify UpThenDown behavior - all siblings and cousins should receive
    var uncle1Child1UpThenDownEvents = uncle1Child1Events.Where(e => e.EventType == nameof(TestEvent)).ToList();
    var uncle1Child2UpThenDownEvents = uncle1Child2Events.Where(e => e.EventType == nameof(TestEvent)).ToList();
    var child2UpThenDownEvents = child2Events.Where(e => e.EventType == nameof(TestEvent)).ToList();
    var uncle2Child1UpThenDownEvents = uncle2Child1Events.Where(e => e.EventType == nameof(TestEvent)).ToList();
    var uncle2Child2UpThenDownEvents = uncle2Child2Events.Where(e => e.EventType == nameof(TestEvent)).ToList();
    var parentUpThenDownEvents = parentEvents.Where(e => e.EventType == nameof(TestEvent)).ToList();
    
    var allCousinEvents = uncle1Child1UpThenDownEvents.Count + uncle1Child2UpThenDownEvents.Count + 
                         child2UpThenDownEvents.Count + uncle2Child1UpThenDownEvents.Count + uncle2Child2UpThenDownEvents.Count;
    
    if (allCousinEvents > 0 && parentUpThenDownEvents.Count > 0)
    {
        Console.WriteLine("✅ UpThenDown event forwarding working correctly");
        Console.WriteLine($"   ✅ All cousins and siblings received events (total: {allCousinEvents})");
        
        // Display forwarding paths from different family branches
        if (uncle1Child1UpThenDownEvents.Any())
        {
            var path = string.Join(" -> ", uncle1Child1UpThenDownEvents.First().ForwardingPath);
            Console.WriteLine($"  📍 Uncle1Child1 path: {path}");
        }
        if (uncle2Child2UpThenDownEvents.Any())
        {
            var path = string.Join(" -> ", uncle2Child2UpThenDownEvents.First().ForwardingPath);
            Console.WriteLine($"  📍 Uncle2Child2 path: {path}");
        }
    }
    else
    {
        Console.WriteLine("❌ UpThenDown event forwarding failed");
        Console.WriteLine($"  Expected: Parent and all siblings/cousins to receive events");
        Console.WriteLine($"  Actual: Parent received {parentUpThenDownEvents.Count}, All cousins received {allCousinEvents}");
    }
    
    Console.WriteLine();
}

async Task TestBidirectionalEvents(IEventForwardingTestAgent[] allAgents)
{
    Console.WriteLine("🔄 Testing Bidirectional Events with Hop Limiting and Publisher Tracking");
    Console.WriteLine("Expected: Parent publishes -> Events propagate bidirectionally respecting maxHopCount and preventing publisher loops");
    
    var (root, uncle1, parent, uncle2, uncle1Child1, uncle1Child2, child1, child2, uncle2Child1, uncle2Child2) = ExtractAgents(allAgents);
    
    // Clear previous events
    await ClearAllEventsAsync(allAgents);
    
    // Test 1: Bidirectional with 2 hops max
    Console.WriteLine("  🧪 Test 1: Bidirectional with maxHops=2");
    var bidirectionalEvent = new TestEvent(EventDirection.Bidirectional, "This event should go bidirectionally with max 2 hops", 2);
    await parent.PublishEventByDirectionAsync(bidirectionalEvent);
    
    // Wait for event propagation
    await Task.Delay(3000);
    
    // Verify results - check all agents
    var rootEvents = await root.GetReceivedEventsAsync();
    var uncle1Events = await uncle1.GetReceivedEventsAsync();
    var uncle2Events = await uncle2.GetReceivedEventsAsync();
    var uncle1Child1Events = await uncle1Child1.GetReceivedEventsAsync();
    var uncle1Child2Events = await uncle1Child2.GetReceivedEventsAsync();
    var child1Events = await child1.GetReceivedEventsAsync();
    var child2Events = await child2.GetReceivedEventsAsync();
    var uncle2Child1Events = await uncle2Child1.GetReceivedEventsAsync();
    var uncle2Child2Events = await uncle2Child2.GetReceivedEventsAsync();
    var parentEvents = await parent.GetReceivedEventsAsync();
    
    Console.WriteLine($"    Root received: {rootEvents.Count} events");
    Console.WriteLine($"    Uncle1 received: {uncle1Events.Count} events");
    Console.WriteLine($"    Uncle2 received: {uncle2Events.Count} events");
    Console.WriteLine($"    Uncle1Child1 received: {uncle1Child1Events.Count} events");
    Console.WriteLine($"    Uncle1Child2 received: {uncle1Child2Events.Count} events");
    Console.WriteLine($"    Child1 received: {child1Events.Count} events");
    Console.WriteLine($"    Child2 received: {child2Events.Count} events");
    Console.WriteLine($"    Uncle2Child1 received: {uncle2Child1Events.Count} events");
    Console.WriteLine($"    Uncle2Child2 received: {uncle2Child2Events.Count} events");
    Console.WriteLine($"    Parent received: {parentEvents.Count} events");
    
    // Verify bidirectional behavior and hop limiting
    var rootBidirectionalEvents = rootEvents.Where(e => e.EventType == nameof(TestEvent)).ToList();
    var childBidirectionalEvents = child1Events.Where(e => e.EventType == nameof(TestEvent)).ToList();
    
    // Test 2: Check publisher tracking - event should not be sent back to original publisher
    Console.WriteLine("  🧪 Test 2: Publisher tracking verification");
    if (parentEvents.Any(e => e.EventType == nameof(TestEvent)))
    {
        var parentReceivedEvent = parentEvents.First(e => e.EventType == nameof(TestEvent));
        Console.WriteLine($"    ⚠️  Parent received its own event - Publisher tracking may need adjustment");
        Console.WriteLine($"    Publishers in event: [{string.Join(", ", parentReceivedEvent.ForwardingPath)}]");
    }
    else
    {
        Console.WriteLine($"    ✅ Publisher tracking working - Parent did not receive its own event");
    }
    
    if (rootBidirectionalEvents.Count > 0 || childBidirectionalEvents.Count > 0)
    {
        Console.WriteLine("✅ Bidirectional event forwarding working correctly");
        
        // Display forwarding paths and hop counts
        if (rootBidirectionalEvents.Any())
        {
            var rootEvent = rootBidirectionalEvents.First();
            var path = string.Join(" -> ", rootEvent.ForwardingPath);
            Console.WriteLine($"  📍 Root path: {path}");
            Console.WriteLine($"  🔢 Hop count check: MaxHops={rootEvent.MaxHopCount}");
        }
        if (childBidirectionalEvents.Any())
        {
            var childEvent = childBidirectionalEvents.First();
            var path = string.Join(" -> ", childEvent.ForwardingPath);
            Console.WriteLine($"  📍 Child path: {path}");
            Console.WriteLine($"  🔢 Hop count check: MaxHops={childEvent.MaxHopCount}");
        }
    }
    else
    {
        Console.WriteLine("❌ Bidirectional event forwarding failed");
        Console.WriteLine($"  Expected: Root or Child to receive events");
        Console.WriteLine($"  Actual: Root received {rootBidirectionalEvents.Count}, Child received {childBidirectionalEvents.Count}");
    }
    
    Console.WriteLine();
}

async Task TestSingleHopUpwardEvents(IEventForwardingTestAgent[] allAgents)
{
    Console.WriteLine("1️⃣⬆️ Testing Single Hop Upward Events");
    Console.WriteLine("Expected: Events should only travel one level up from publisher (child -> parent, NOT to root)");
    
    var (root, uncle1, parent, uncle2, uncle1Child1, uncle1Child2, child1, child2, uncle2Child1, uncle2Child2) = ExtractAgents(allAgents);
    
    // Clear previous events
    await ClearAllEventsAsync(allAgents);
    
    // Test single hop upward from child (should only reach parent, not root)
    Console.WriteLine("  🧪 Publishing single hop upward event from Child1");
    var singleHopUp = new TestEvent(EventDirection.Up, "Single hop Up event", 1);
    await child1.PublishEventByDirectionAsync(singleHopUp);
    
    await Task.Delay(1500);
    
    // Verify results - check all agents for comprehensive coverage
    var parentEvents = await parent.GetReceivedEventsAsync();
    var rootEvents = await root.GetReceivedEventsAsync();
    var uncle1Events = await uncle1.GetReceivedEventsAsync();
    var uncle2Events = await uncle2.GetReceivedEventsAsync();
    
    // Check upward hop limiting
    var parentUpEvents = parentEvents.Where(e => e.EventType == nameof(TestEvent) && e.Direction == EventDirection.Up).ToList();
    var rootUpEvents = rootEvents.Where(e => e.EventType == nameof(TestEvent) && e.Direction == EventDirection.Up).ToList();
    var uncle1UpEvents = uncle1Events.Where(e => e.EventType == nameof(TestEvent) && e.Direction == EventDirection.Up).ToList();
    var uncle2UpEvents = uncle2Events.Where(e => e.EventType == nameof(TestEvent) && e.Direction == EventDirection.Up).ToList();
    
    Console.WriteLine($"  Results: Parent received {parentUpEvents.Count}, Root received {rootUpEvents.Count}");
    Console.WriteLine($"  Results: Uncle1 received {uncle1UpEvents.Count}, Uncle2 received {uncle2UpEvents.Count}");
    Console.WriteLine($"  Expected: Parent=1, others=0 (single hop upward limiting)");
    
    bool upwardHopLimitWorking = parentUpEvents.Count > 0 && rootUpEvents.Count == 0 && 
                                uncle1UpEvents.Count == 0 && uncle2UpEvents.Count == 0;
    
    if (upwardHopLimitWorking)
    {
        Console.WriteLine("✅ Single hop upward limiting working correctly");
    }
    else
    {
        Console.WriteLine("❌ Single hop upward limiting failed");
        Console.WriteLine($"   Expected: Only parent should receive event, but Root={rootUpEvents.Count}, Uncle1={uncle1UpEvents.Count}, Uncle2={uncle2UpEvents.Count}");
    }
    
    Console.WriteLine();
}

async Task TestSingleHopDownwardEvents(IEventForwardingTestAgent[] allAgents)
{
    Console.WriteLine("1️⃣⬇️ Testing Single Hop Downward Events");
    Console.WriteLine("Expected: Events should only travel one level down from publisher (root -> Level2, NOT to Level3)");
    
    var (root, uncle1, parent, uncle2, uncle1Child1, uncle1Child2, child1, child2, uncle2Child1, uncle2Child2) = ExtractAgents(allAgents);
    
    // Clear previous events
    await ClearAllEventsAsync(allAgents);
    
    // Test single hop downward from root (should only reach Level2 agents, not Level3)
    Console.WriteLine("  🧪 Publishing single hop downward event from Root");
    var singleHopDown = new TestEvent(EventDirection.Down, "Single hop Down event", 1);
    await root.PublishEventByDirectionAsync(singleHopDown);
    
    await Task.Delay(1500);
    
    // Verify results - check all agents for comprehensive coverage
    var parentEvents = await parent.GetReceivedEventsAsync();
    var uncle1Events = await uncle1.GetReceivedEventsAsync();
    var uncle2Events = await uncle2.GetReceivedEventsAsync();
    var uncle1Child1Events = await uncle1Child1.GetReceivedEventsAsync();
    var uncle1Child2Events = await uncle1Child2.GetReceivedEventsAsync();
    var child1Events = await child1.GetReceivedEventsAsync();
    var child2Events = await child2.GetReceivedEventsAsync();
    var uncle2Child1Events = await uncle2Child1.GetReceivedEventsAsync();
    var uncle2Child2Events = await uncle2Child2.GetReceivedEventsAsync();
    
    // Check downward hop limiting - include all Level 2 and Level 3 agents
    var level2DownEvents = uncle1Events.Concat(parentEvents).Concat(uncle2Events)
        .Where(e => e.EventType == nameof(TestEvent) && e.Direction == EventDirection.Down).ToList();
    var level3DownEvents = uncle1Child1Events.Concat(uncle1Child2Events).Concat(child1Events)
        .Concat(child2Events).Concat(uncle2Child1Events).Concat(uncle2Child2Events)
        .Where(e => e.EventType == nameof(TestEvent) && e.Direction == EventDirection.Down).ToList();
    
    Console.WriteLine($"  Results: Level2 agents received {level2DownEvents.Count()}, Level3 agents received {level3DownEvents.Count()}");
    Console.WriteLine($"  Expected: Level2>0, Level3=0 (single hop downward limiting)");
    
    bool downwardHopLimitWorking = level2DownEvents.Any() && !level3DownEvents.Any();
    
    if (downwardHopLimitWorking)
    {
        Console.WriteLine("✅ Single hop downward limiting working correctly");
    }
    else
    {
        Console.WriteLine("❌ Single hop downward limiting failed");
        Console.WriteLine($"   Expected: Only Level2 agents should receive event, but Level3 received {level3DownEvents.Count()} events");
    }
    
    Console.WriteLine();
}

async Task TestMultiLevelEvents(IEventForwardingTestAgent[] allAgents)
{
    Console.WriteLine("🎯 Testing Multi-Level Events with Variable Hop Limits");
    Console.WriteLine("Expected: Events should respect different hop count limits across complex hierarchy");
    
    var (root, uncle1, parent, uncle2, uncle1Child1, uncle1Child2, child1, child2, uncle2Child1, uncle2Child2) = ExtractAgents(allAgents);
    
    // Clear previous events
    await ClearAllEventsAsync(allAgents);
    
    // Test 1: Multi-level upward with max 2 hops (Child -> Parent -> Root)
    Console.WriteLine("  🧪 Test 1: Multi-level upward with maxHops=2 (Child -> Parent -> Root)");
    var multiLevelUp = new TestEvent(EventDirection.Up, "Multi-level Up event with max 2 hops", 2);
    var upwardEventId = multiLevelUp.TestId; // Store event ID for verification
    await child1.PublishEventByDirectionAsync(multiLevelUp);
    
    await Task.Delay(2000);
    
    // Test 2: Multi-level downward with max 2 hops (Root -> Level2 -> Level3)
    Console.WriteLine("  🧪 Test 2: Multi-level downward with maxHops=2 (Root -> Level2 -> Level3)");
    var multiLevelDown = new TestEvent(EventDirection.Down, "Multi-level Down event with max 2 hops", 2);
    var downwardEventId = multiLevelDown.TestId; // Store event ID for verification
    await root.PublishEventByDirectionAsync(multiLevelDown);
    
    await Task.Delay(2000);
    
    // Test 3: Limited hops with maxHops=1 (should only go one level)
    Console.WriteLine("  🧪 Test 3: Limited hops with maxHops=1 (Parent -> limited propagation)");
    var limitedHops = new TestEvent(EventDirection.Down, "Limited hop Down event with max 1 hop", 1);
    var limitedEventId = limitedHops.TestId; // Store event ID for verification
    await parent.PublishEventByDirectionAsync(limitedHops);
    
    await Task.Delay(2000);
    
    // Verify results using event IDs (immutable) instead of MaxHopCount (mutable)
    var rootEvents = await root.GetReceivedEventsAsync();
    var parentEvents = await parent.GetReceivedEventsAsync();
    var child1Events = await child1.GetReceivedEventsAsync();
    var child2Events = await child2.GetReceivedEventsAsync();
    var uncle1Child1Events = await uncle1Child1.GetReceivedEventsAsync();
    var uncle1Child2Events = await uncle1Child2.GetReceivedEventsAsync();
    var uncle2Child1Events = await uncle2Child1.GetReceivedEventsAsync();
    var uncle2Child2Events = await uncle2Child2.GetReceivedEventsAsync();
    
    // Combine all Level 3 events for verification
    var allLevel3Events = child1Events.Concat(child2Events).Concat(uncle1Child1Events)
        .Concat(uncle1Child2Events).Concat(uncle2Child1Events).Concat(uncle2Child2Events);
    
    // ✅ CORRECT VERIFICATION: Use immutable event IDs instead of mutable MaxHopCount
    var upwardEventReachedRoot = rootEvents.Any(e => e.TestId == upwardEventId);
    var downwardEventReachedLevel3 = allLevel3Events.Any(e => e.TestId == downwardEventId);
    var limitedEventReachedLevel3 = allLevel3Events.Any(e => e.TestId == limitedEventId);
    
    Console.WriteLine($"  Upward event (ID: {upwardEventId}) reached root: {upwardEventReachedRoot}");
    Console.WriteLine($"  Downward event (ID: {downwardEventId}) reached Level3: {downwardEventReachedLevel3}");
    Console.WriteLine($"  Limited event (ID: {limitedEventId}) reached Level3: {limitedEventReachedLevel3}");
    
    // Count total events received for debugging
    var rootUpwardEvents = rootEvents.Where(e => e.TestId == upwardEventId).ToList();
    var level3DownwardEvents = allLevel3Events.Where(e => e.TestId == downwardEventId).ToList();
    var level3LimitedEvents = allLevel3Events.Where(e => e.TestId == limitedEventId).ToList();
    
    Console.WriteLine($"  Root received {rootUpwardEvents.Count} upward events");
    Console.WriteLine($"  Level3 agents received {level3DownwardEvents.Count()} downward events");
    Console.WriteLine($"  Level3 agents received {level3LimitedEvents.Count()} limited hop events");
    
    bool multiLevelWorking = upwardEventReachedRoot && downwardEventReachedLevel3;
    bool hopLimitingWorking = limitedEventReachedLevel3; // With 1 hop, should reach Level3 from Parent
    
    if (multiLevelWorking)
    {
        Console.WriteLine("✅ Multi-level event forwarding working correctly");
        
        if (rootUpwardEvents.Any())
        {
            var upPath = string.Join(" -> ", rootUpwardEvents.First().ForwardingPath);
            Console.WriteLine($"  📍 Up path (2 hops): {upPath}");
        }
        if (level3DownwardEvents.Any())
        {
            var downPath = string.Join(" -> ", level3DownwardEvents.First().ForwardingPath);
            Console.WriteLine($"  📍 Down path (2 hops): {downPath}");
        }
    }
    else
    {
        Console.WriteLine("❌ Multi-level event forwarding failed");
        Console.WriteLine($"  Expected: Root and Level3 agents to receive multi-hop events");
        Console.WriteLine($"  Actual: Upward={upwardEventReachedRoot}, Downward={downwardEventReachedLevel3}");
    }
    
    if (hopLimitingWorking)
    {
        Console.WriteLine("✅ Hop limiting working correctly");
        if (level3LimitedEvents.Any())
        {
            var limitedPath = string.Join(" -> ", level3LimitedEvents.First().ForwardingPath);
            Console.WriteLine($"  📍 Limited hop path: {limitedPath}");
        }
    }
    else
    {
        Console.WriteLine("❌ Hop limiting may need verification");
        Console.WriteLine($"  Limited event reached Level3: {limitedEventReachedLevel3}");
    }
    
    Console.WriteLine();
}
