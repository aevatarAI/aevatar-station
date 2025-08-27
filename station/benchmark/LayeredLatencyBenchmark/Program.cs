using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Streams;
using Orleans.Streams.Kafka.Config;
using Aevatar.Core.Streaming.Extensions;
using Aevatar.Core.Abstractions;
using Aevatar.Core;
using LayeredLatencyBenchmark;

/// <summary>
/// Main entry point for the layered latency benchmark
/// </summary>
internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Setup cancellation support for Ctrl+C
        using var cts = new CancellationTokenSource();
        var cancellationRequested = false;
        
        Console.CancelKeyPress += (sender, e) =>
        {
            if (!cancellationRequested)
            {
                cancellationRequested = true;
                e.Cancel = true; // Prevent immediate termination
                Console.WriteLine();
                Console.WriteLine("🛑 Cancellation requested... Stopping benchmark gracefully.");
                Console.WriteLine("   Press Ctrl+C again to force exit.");
                cts.Cancel();
            }
            else
            {
                Console.WriteLine("💥 Force exit requested!");
                Environment.Exit(1);
            }
        };

        try
        {
            var result = await Parser.Default.ParseArguments<LayeredBenchmarkConfig>(args)
                .WithParsedAsync(async config =>
                {
                    await RunBenchmarkAsync(config, cts.Token);
                });

            return result.Tag == ParserResultType.Parsed ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Benchmark failed: {ex.Message}");
            Console.WriteLine($"🔍 Stack trace: {ex.StackTrace}");
            return 1;
        }
    }

    private static async Task RunBenchmarkAsync(LayeredBenchmarkConfig config, CancellationToken cancellationToken)
    {
        // Create host builder (copied from LatencyBenchmark project)
        var hostBuilder = Host.CreateDefaultBuilder()
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

                        var partitions = 8; // Multiple partitions for load distribution
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
                    });
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                // Enable debug logging in debug mode or when verbose is explicitly set
                var enableDebugLogging = config.Debug || config.Verbose;
                logging.SetMinimumLevel(enableDebugLogging ? LogLevel.Debug : LogLevel.Information);
            })
            .ConfigureServices(services =>
            {
                // Add configuration
                services.AddSingleton(config);

                // Add Orleans agent client (and required dependencies)
                services.AddSingleton<IGAgentFactory, GAgentFactory>();
                services.AddSingleton<OrleansAgentClient>();

                // Add services
                services.AddTransient<LayeredBenchmarkRunner>();
            })
            .UseConsoleLifetime();

        using var host = hostBuilder.Build();
        
        // Start the host
        await host.StartAsync();
        
        // Get services
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var orleansClient = host.Services.GetRequiredService<OrleansAgentClient>();

        var runner = new LayeredBenchmarkRunner(
            config,
            orleansClient,
            host.Services.GetRequiredService<ILogger<LayeredBenchmarkRunner>>());

        // Display configuration
        logger.LogInformation("🎯 Starting Layered Latency Benchmark");
        logger.LogInformation("Leader count: {LeaderCount}", config.LeaderCount);
        logger.LogInformation("Sub-agent range: {BaseSubAgents}-{MaxSubAgents}", config.BaseSubAgents, config.MaxSubAgents);
        logger.LogInformation("Scale factor: {ScaleFactor}", config.ScaleFactor);
        logger.LogInformation("Duration: {Duration}s", config.Duration);
        logger.LogInformation("Events per second: {EventsPerSecond}", config.EventsPerSecond);
        logger.LogInformation("Output file: {OutputFile}", config.OutputFile);
        logger.LogInformation("Verbose: {Verbose}", config.Verbose);
        logger.LogInformation("Debug mode: {Debug}", config.Debug);
        logger.LogInformation("Target daily events: {TargetDailyEvents:N0}", config.TargetDailyEvents);
        
        if (config.Debug)
        {
            logger.LogInformation("🔍 Debug mode: Testing with 1 leader + 1 sub-agent");
        }
        else
        {
            var levels = config.GetSubAgentConcurrencyLevels();
            logger.LogInformation("🔢 Testing concurrency levels: {Levels}", string.Join(", ", levels));
        }
        
        if (config.Debug)
        {
            logger.LogInformation("🔍 DEBUG MODE: Will create exactly 1 leader and 1 sub-agent, send 1 event.");
            logger.LogInformation("  This helps diagnose layered agent communication flow and event routing.");
        }
        else
        {
            logger.LogInformation("📈 Expected Results:");
            logger.LogInformation("   - Published Events: {PublishedEvents}", config.EventsPerSecond * config.Duration);
            // Calculate total expected events: published events * fan-out factor
            var publishedEvents = config.EventsPerSecond * config.Duration;
            var maxSubAgents = config.MaxSubAgents;
            var totalExpectedEvents = publishedEvents * maxSubAgents;
            logger.LogInformation("   - Total Received Events: {TotalReceived:N0} (with max fan-out)", totalExpectedEvents);
            logger.LogInformation("   - Estimated Duration: {EstimatedDuration}s", config.Duration + config.WarmupDuration + 10);
        }
        
        var action = config.Debug ? "debug trace" : "benchmark";
        logger.LogInformation("Starting {Action}...", action);
        logger.LogInformation("");

        // Run benchmark
        var results = await runner.RunAsync(cancellationToken);
        
        // Save results
        results.SaveToFile(config.OutputFile);
        logger.LogInformation("💾 Results saved to {OutputFile}", config.OutputFile);

        // Display summary
        DisplaySummary(results, logger);
    }

    private static void DisplaySummary(BenchmarkResults results, ILogger logger)
    {
        Console.WriteLine();
        Console.WriteLine("🎯 Layered Agent Communication Results");
        Console.WriteLine("=====================================");

        if (results.Config.Debug)
        {
            DisplayDebugResults(results, logger);
            return;
        }

        if (results.ConcurrencyResults.Count == 0)
        {
            Console.WriteLine("No concurrency level results to display.");
            return;
        }

        // Overall summary
        Console.WriteLine();
        Console.WriteLine("📊 Overall Summary");
        Console.WriteLine("=================");
        Console.WriteLine($"Total execution time: {results.TotalElapsedTime:mm\\:ss}");
        Console.WriteLine($"Concurrency levels tested: {results.ConcurrencyResults.Count}");
        Console.WriteLine($"Target daily events: {results.Config.TargetDailyEvents:N0}");
        Console.WriteLine($"Required sub-agents for target: {results.Config.CalculateRequiredSubAgents()}");

        // Concurrency scaling table
        Console.WriteLine();
        Console.WriteLine("🔢 Concurrency Scaling Results");
        Console.WriteLine("==============================");
        Console.WriteLine();

        // Table header
        Console.WriteLine("Sub-Agents | Events/Sec | Target/Sec |   Achievement   | Avg Latency | P95 Latency | P99 Latency | Status");
        Console.WriteLine("-----------|------------|------------|-----------------|-------------|-------------|-------------|--------");

        // Table rows
        foreach (var kvp in results.ConcurrencyResults.OrderBy(x => x.Key))
        {
            var subAgentCount = kvp.Key;
            var result = kvp.Value;
            
            var achievement = result.ThroughputAchieved ? "✅ ACHIEVED" : "❌ MISSED";
            var status = result.Success ? "✅ OK" : "❌ FAIL";
            
            Console.WriteLine($"{subAgentCount,10} | {result.ActualEventsPerSecond,10:F1} | {result.TargetEventsPerSecond,10:F1} | {achievement,15} | {result.AverageLatencyMs,11:F2}ms | {result.P95LatencyMs,11:F2}ms | {result.P99LatencyMs,11:F2}ms | {status,6}");
        }

        // Agent-Type Performance Breakdown
        Console.WriteLine();
        Console.WriteLine("🎭 Agent-Type Performance Breakdown");
        Console.WriteLine("===================================");
        Console.WriteLine();
        
        Console.WriteLine("Sub-Agents | Leader Events | Leader Fwd | Sub-Agent Events | Client→Leader | Leader→Sub | Total Latency");
        Console.WriteLine("-----------|---------------|------------|------------------|---------------|------------|---------------");
        
        foreach (var kvp in results.ConcurrencyResults.OrderBy(x => x.Key))
        {
            var subAgentCount = kvp.Key;
            var result = kvp.Value;
            
            // Extract Client→Leader latency from leader metrics
            var clientLeaderLatency = result.LeaderMetrics?.AvgLatencyMs ?? 0;
            
            // Extract Leader→Sub latency from sub-agent metrics  
            var leaderSubLatency = result.SubAgentMetrics.Any() ? result.SubAgentMetrics.Average(m => m.AvgLatencyMs) : 0;
            
            // Calculate total latency (Client→Leader + Leader→Sub)
            var totalLatency = clientLeaderLatency + leaderSubLatency;
            
            Console.WriteLine($"{subAgentCount,10} | {result.LeaderMetrics?.EventsReceived ?? 0,13} | {result.LeaderMetrics?.EventsForwarded ?? 0,10} | {result.SubAgentMetrics.Sum(m => m.EventsReceived),16} | {clientLeaderLatency,11:F2}ms | {leaderSubLatency,8:F2}ms | {totalLatency,11:F2}ms");
        }

        // Performance assessment
        Console.WriteLine();
        Console.WriteLine("📈 Performance Assessment");
        Console.WriteLine("========================");
        
        var bestThroughput = results.ConcurrencyResults.Values.Max(r => r.ActualEventsPerSecond);
        var bestLatency = results.ConcurrencyResults.Values.Where(r => r.Success).Min(r => r.AverageLatencyMs);
        var achievedTargets = results.ConcurrencyResults.Values.Count(r => r.ThroughputAchieved);
        var totalLevels = results.ConcurrencyResults.Count;
        var targetDailyEventsPerSecond = results.Config.CalculateMinEventsPerSecond();
        
        Console.WriteLine($"Best throughput: {bestThroughput:F1} events/sec");
        Console.WriteLine($"Best latency: {bestLatency:F2}ms");
        Console.WriteLine($"Target achievement: {achievedTargets}/{totalLevels} levels");
        Console.WriteLine($"Daily target feasible: {(bestThroughput >= targetDailyEventsPerSecond ? "✅ YES" : "❌ NO")}");
        
        // Find optimal sub-agent count
        var optimalResult = results.ConcurrencyResults.Values
            .Where(r => r.Success && r.ThroughputAchieved)
            .OrderBy(r => r.AverageLatencyMs)
            .FirstOrDefault();
            
        if (optimalResult != null)
        {
            var optimalSubAgents = results.ConcurrencyResults.First(kvp => kvp.Value == optimalResult).Key;
            Console.WriteLine($"Optimal configuration: {optimalSubAgents} sub-agents (lowest latency while achieving target)");
        }

        Console.WriteLine();
        Console.WriteLine("✅ Analysis Complete");
    }

    private static void DisplayDebugResults(BenchmarkResults results, ILogger logger)
    {
        Console.WriteLine();
        Console.WriteLine("🔍 Debug Mode Results");
        Console.WriteLine("====================");
        
        if (results.ConcurrencyResults.Count == 0 || !results.ConcurrencyResults.ContainsKey(1))
        {
            Console.WriteLine("No debug results available.");
            return;
        }

        // Debug mode should have results stored with key 1 (1 sub-agent)
        var debugResult = results.ConcurrencyResults[1];
        
        Console.WriteLine($"✅ Single Event Test: {(debugResult.Success ? "SUCCESS" : "FAILED")}");
        Console.WriteLine($"📊 Execution time: {debugResult.ExecutionTime.TotalSeconds:F1}s");
        Console.WriteLine($"📤 Events sent: {debugResult.EventsSent}");
        Console.WriteLine($"📥 Events received: {debugResult.TotalEventsReceived}");
        Console.WriteLine($"🔄 Events forwarded: {debugResult.TotalEventsForwarded}");
        
        if (debugResult.Success && debugResult.AverageLatencyMs > 0)
        {
            Console.WriteLine($"⚡ End-to-end latency: {debugResult.AverageLatencyMs:F2}ms");
            
            // Performance assessment for debug
            string assessment = debugResult.AverageLatencyMs switch
            {
                < 100 => "🟢 EXCELLENT",
                < 500 => "🟡 GOOD", 
                < 1000 => "🟠 ACCEPTABLE",
                _ => "🔴 POOR"
            };
            Console.WriteLine($"📈 Performance: {assessment}");
            
            // Detailed latency breakdown if available
            if (debugResult.SubAgentMetrics.Count > 0 && debugResult.SubAgentMetrics[0].RawMeasurements.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("📊 Detailed Latency Analysis");
                Console.WriteLine("============================");
                Console.WriteLine($"Min latency: {debugResult.MinLatencyMs:F2}ms");
                Console.WriteLine($"Max latency: {debugResult.MaxLatencyMs:F2}ms");
                Console.WriteLine($"P95 latency: {debugResult.P95LatencyMs:F2}ms");
                Console.WriteLine($"P99 latency: {debugResult.P99LatencyMs:F2}ms");
            }
        }
        else if (!debugResult.Success)
        {
            Console.WriteLine($"❌ Error: {debugResult.ErrorMessage}");
        }
        
        // Communication flow breakdown
        Console.WriteLine();
        Console.WriteLine("🔄 Communication Flow Analysis");
        Console.WriteLine("==============================");
        if (debugResult.LeaderMetrics != null)
        {
            Console.WriteLine($"👑 Leader: Received {debugResult.LeaderMetrics.EventsReceived}, Forwarded {debugResult.LeaderMetrics.EventsForwarded}");
        }
        if (debugResult.SubAgentMetrics.Count > 0)
        {
            var subAgent = debugResult.SubAgentMetrics[0];
            Console.WriteLine($"👥 Sub-agent: Received {subAgent.EventsReceived}");
        }
        
        // Success indicators
        var flowComplete = debugResult.TotalEventsForwarded > 0 && debugResult.TotalEventsReceived >= 2; // Leader + Sub-agent
        Console.WriteLine($"🔗 Communication flow: {(flowComplete ? "✅ COMPLETE" : "❌ INCOMPLETE")}");
        
        if (debugResult.Success)
        {
            Console.WriteLine();
            Console.WriteLine("✅ Debug Analysis: Layered agent communication is working correctly!");
            Console.WriteLine("   Events successfully flow: Publisher → Leader → Sub-agent");
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("❌ Debug Analysis: Issues detected in layered communication");
            Console.WriteLine("   Check logs above for specific failure points");
        }
    }

    private static double CalculatePercentile(List<double> values, double percentile)
    {
        if (!values.Any()) return 0;
        
        var sorted = values.OrderBy(x => x).ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }
} 