using CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using BroadcastLatencyBenchmark;
using E2E.Grains;

// Display usage information
void ShowUsage()
{
    Console.WriteLine("Usage: BroadcastLatencyBenchmark [options]");
    Console.WriteLine("Options:");
    Console.WriteLine("  -s, --subscriber-count <count>    Number of subscriber agents to create (default: 10)");
    Console.WriteLine("  -p, --publisher-count <count>     Number of publisher agents to create (default: 1)");
    Console.WriteLine("  -d, --duration <seconds>          Duration in seconds for each test (default: 5)");
    Console.WriteLine("  -r, --events-per-second <count>   Target events per second per publisher (default: 1)");
    Console.WriteLine("  -o, --output-file <path>          Output file for results (default: broadcast-latency-results.json)");
    Console.WriteLine("  -v, --verbose                     Enable verbose logging");
    Console.WriteLine("  -w, --warmup-duration <seconds>   Warmup duration in seconds (default: 10)");
    Console.WriteLine("  --use-stored-ids                  Use stored agent IDs from broadcast_agent_ids.json (default: true)");
    Console.WriteLine("  --debug                           Debug mode: Send only 1 event to trace communication flow");
    Console.WriteLine("  --event-number <number>           Number to use in broadcast events (default: 100)");
    Console.WriteLine("  --completion-timeout <seconds>    Maximum time to wait for completion (default: 60)");
    Console.WriteLine("  --completion-check-interval <s>   Completion check interval in seconds (default: 1)");
    Console.WriteLine("  --help                            Show this help message");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  BroadcastLatencyBenchmark");
    Console.WriteLine("  BroadcastLatencyBenchmark --subscriber-count 100 --publisher-count 2 --duration 30");
    Console.WriteLine("  BroadcastLatencyBenchmark --use-stored-ids --subscriber-count 50 --debug");
    Console.WriteLine("  BroadcastLatencyBenchmark --verbose --events-per-second 5 --event-number 250");
    Console.WriteLine();
    Console.WriteLine("Description:");
    Console.WriteLine("  This benchmark tests broadcast latency in agent-to-agent communication.");
    Console.WriteLine("  Publisher agents send broadcast events to all subscriber agents.");
    Console.WriteLine("  Latency is measured from event creation to event processing.");
    Console.WriteLine("  Similar to VerifyDbIssue545 but with comprehensive latency metrics.");
}

// Parse command line arguments manually (fallback if CommandLineParser fails)
BroadcastBenchmarkConfig ParseArguments(string[] args)
{
    var config = new BroadcastBenchmarkConfig();
    
    for (int i = 0; i < args.Length; i++)
    {
        string arg = args[i].ToLower();
        
        switch (arg)
        {
            case "--subscriber-count":
            case "-s":
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out int subscriberCount) && subscriberCount > 0)
                {
                    config.SubscriberCount = subscriberCount;
                    i++;
                }
                else
                {
                    Console.WriteLine($"Error: {arg} requires a positive integer value");
                    ShowUsage();
                    Environment.Exit(1);
                }
                break;
                
            case "--publisher-count":
            case "-p":
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out int publisherCount) && publisherCount > 0)
                {
                    config.PublisherCount = publisherCount;
                    i++;
                }
                else
                {
                    Console.WriteLine($"Error: {arg} requires a positive integer value");
                    ShowUsage();
                    Environment.Exit(1);
                }
                break;
                
            case "--duration":
            case "-d":
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out int duration) && duration > 0)
                {
                    config.Duration = duration;
                    i++;
                }
                else
                {
                    Console.WriteLine($"Error: {arg} requires a positive integer value");
                    ShowUsage();
                    Environment.Exit(1);
                }
                break;
                
            case "--events-per-second":
            case "-r":
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out int eventsPerSecond) && eventsPerSecond > 0)
                {
                    config.EventsPerSecond = eventsPerSecond;
                    i++;
                }
                else
                {
                    Console.WriteLine($"Error: {arg} requires a positive integer value");
                    ShowUsage();
                    Environment.Exit(1);
                }
                break;
                
            case "--output-file":
            case "-o":
                if (i + 1 < args.Length)
                {
                    config.OutputFile = args[i + 1];
                    i++;
                }
                else
                {
                    Console.WriteLine($"Error: {arg} requires a file path");
                    ShowUsage();
                    Environment.Exit(1);
                }
                break;
                
            case "--verbose":
            case "-v":
                config.Verbose = true;
                break;
                
            case "--warmup-duration":
            case "-w":
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out int warmupDuration) && warmupDuration >= 0)
                {
                    config.WarmupDuration = warmupDuration;
                    i++;
                }
                else
                {
                    Console.WriteLine($"Error: {arg} requires a non-negative integer value");
                    ShowUsage();
                    Environment.Exit(1);
                }
                break;
                
            case "--use-stored-ids":
                config.UseStoredIds = true;
                break;
                
            case "--debug":
                config.Debug = true;
                break;
                
            case "--event-number":
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out int eventNumber) && eventNumber > 0)
                {
                    config.EventNumber = eventNumber;
                    i++;
                }
                else
                {
                    Console.WriteLine($"Error: {arg} requires a positive integer value");
                    ShowUsage();
                    Environment.Exit(1);
                }
                break;
                
            case "--completion-timeout":
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out int completionTimeout) && completionTimeout > 0)
                {
                    config.CompletionTimeout = completionTimeout;
                    i++;
                }
                else
                {
                    Console.WriteLine($"Error: {arg} requires a positive integer value");
                    ShowUsage();
                    Environment.Exit(1);
                }
                break;
                
            case "--completion-check-interval":
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out int completionCheckInterval) && completionCheckInterval > 0)
                {
                    config.CompletionCheckInterval = completionCheckInterval;
                    i++;
                }
                else
                {
                    Console.WriteLine($"Error: {arg} requires a positive integer value");
                    ShowUsage();
                    Environment.Exit(1);
                }
                break;
                
            case "--help":
            case "-h":
                ShowUsage();
                Environment.Exit(0);
                break;
                
            default:
                Console.WriteLine($"Error: Unknown argument '{args[i]}'");
                ShowUsage();
                Environment.Exit(1);
                break;
        }
    }
    
    return config;
}

Console.WriteLine("🚀 Broadcast Latency Benchmark Starting...");

// Parse command line arguments
var config = ParseArguments(args);

// Validate configuration
try
{
    config.Validate();
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Configuration error: {ex.Message}");
    ShowUsage();
    Environment.Exit(1);
}

// Display configuration
Console.WriteLine($"Configuration:");
Console.WriteLine($"  Subscriber count: {config.SubscriberCount}");
Console.WriteLine($"  Publisher count: {config.PublisherCount}");
Console.WriteLine($"  Duration: {config.Duration} seconds");
Console.WriteLine($"  Events per second: {config.EventsPerSecond}");
Console.WriteLine($"  Event number: {config.EventNumber}");
Console.WriteLine($"  Output file: {config.OutputFile}");
Console.WriteLine($"  Verbose logging: {config.Verbose}");
Console.WriteLine($"  Warmup duration: {config.WarmupDuration} seconds");
Console.WriteLine($"  Use stored IDs: {config.UseStoredIds}");
Console.WriteLine($"  Debug mode: {config.Debug}");
Console.WriteLine($"  Completion timeout: {config.CompletionTimeout} seconds");
Console.WriteLine($"  Completion check interval: {config.CompletionCheckInterval} seconds");
Console.WriteLine();

// Calculate expected metrics
var totalEventsToPublish = config.CalculateTotalEventsToPublish();
var totalExpectedEvents = config.CalculateTotalExpectedEvents();
Console.WriteLine($"Expected metrics:");
Console.WriteLine($"  Total events to publish: {totalEventsToPublish:N0}");
Console.WriteLine($"  Total expected events (all subscribers): {totalExpectedEvents:N0}");
Console.WriteLine($"  Expected broadcast fan-out: 1-to-{config.SubscriberCount}");
Console.WriteLine();

// Create host for logging
var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(config.Verbose ? LogLevel.Debug : LogLevel.Information);
    })
    .Build();

var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger<BroadcastBenchmarkRunner>();

// Run benchmark
var stopwatch = Stopwatch.StartNew();
var runner = new BroadcastBenchmarkRunner(config, logger);

try
{
    // Handle Ctrl+C gracefully
    var cancellationTokenSource = new CancellationTokenSource();
    Console.CancelKeyPress += (sender, e) =>
    {
        e.Cancel = true;
        logger.LogInformation("🛑 Cancellation requested...");
        cancellationTokenSource.Cancel();
    };

    var results = await runner.RunBenchmarkAsync(cancellationTokenSource.Token);
    
    stopwatch.Stop();
    
    // Display final completion summary
    Console.WriteLine();
    Console.WriteLine("🎉 Broadcast Latency Benchmark Completed!");
    Console.WriteLine($"⏱️  Total runtime: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
    Console.WriteLine($"📊 Results saved to: {config.OutputFile}");
    
    if (results.Count > 0)
    {
        var result = results[0];
        var expectedEvents = result.TotalEventsSent * result.SubscriberCount;
        var successRate = expectedEvents > 0 ? (double)result.TotalEventsProcessed / expectedEvents * 100 : 0;
        var overallStatus = result.Success && successRate >= 95 ? "✅ SUCCESS" : "❌ ISSUES";
        
        Console.WriteLine();
        Console.WriteLine($"🏆 Final Status: {overallStatus}");
        Console.WriteLine($"📈 Quick Summary: {result.TotalEventsSent:N0} events → {result.TotalEventsProcessed:N0} processed ({successRate:F1}%)");
        Console.WriteLine($"⚡ Latency: {result.AverageLatencyMs:F2}ms avg, {result.P95LatencyMs:F2}ms P95");
        
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            Console.WriteLine($"❌ Error: {result.ErrorMessage}");
        }
    }
    
    Console.WriteLine();
    Console.WriteLine("💡 Tip: Run with --help to see all available options.");
}
catch (Exception ex)
{
    logger.LogError(ex, "❌ Benchmark failed");
    Console.WriteLine($"❌ Benchmark failed: {ex.Message}");
    Environment.Exit(1);
}
finally
{
    runner.Dispose();
    await host.StopAsync();
}

Console.WriteLine("👋 Goodbye!");
Environment.Exit(0); 