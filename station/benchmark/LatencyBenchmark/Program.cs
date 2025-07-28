using CommandLine;
using Microsoft.Extensions.Logging;
using Serilog;
using LatencyBenchmark;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Setup global logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

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
            Console.WriteLine("🚀 Orleans Latency Benchmark Tool");
            Console.WriteLine("==================================");
            Console.WriteLine();

            return await Parser.Default.ParseArguments<BenchmarkConfig>(args)
                .MapResult(
                    async (BenchmarkConfig config) => await RunBenchmarkAsync(config, cts.Token),
                    errors => Task.FromResult(1));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Benchmark failed: {ex.Message}");
            Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static async Task<int> RunBenchmarkAsync(BenchmarkConfig config, CancellationToken cancellationToken)
    {
        // Validate configuration
        if (!ValidateConfig(config))
        {
            return 1;
        }

        // Display configuration
        await DisplayConfiguration(config);

        // Setup logging
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
            if (config.Verbose)
            {
                builder.SetMinimumLevel(LogLevel.Debug);
            }
            else
            {
                builder.SetMinimumLevel(LogLevel.Information);
            }
        });

        var logger = loggerFactory.CreateLogger<Program>();

        try
        {
            // Create and run benchmark
            using var benchmarkRunner = new LatencyBenchmarkRunner(config, loggerFactory.CreateLogger<LatencyBenchmarkRunner>());
            
            logger.LogInformation("Starting latency benchmark...");
            var results = await benchmarkRunner.RunBenchmarkAsync(cancellationToken);

            // Display summary
            DisplaySummary(results, config);

            logger.LogInformation("Benchmark completed successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Benchmark execution failed");
            return 1;
        }
        finally
        {
            loggerFactory.Dispose();
        }
    }

    private static bool ValidateConfig(BenchmarkConfig config)
    {
        try
        {
            config.Validate();
            return true;
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine("❌ Configuration error:");
            Console.WriteLine($"  - {ex.Message}");
            Console.WriteLine();
            return false;
        }
    }

    private static async Task DisplayConfiguration(BenchmarkConfig config)
    {
        Console.WriteLine("📋 Benchmark Configuration:");
        Console.WriteLine($"  Concurrency Range: {config.BaseConcurrency} - {config.MaxConcurrency} publishers");
        Console.WriteLine($"  Scale Factor: {config.ScaleFactor}");
        Console.WriteLine($"  Events Per Second: {config.EventsPerSecond} per publisher");
        Console.WriteLine($"  Duration: {config.Duration} seconds per test");
        Console.WriteLine($"  Warmup Duration: {config.WarmupDuration} seconds");
        Console.WriteLine($"  Target Daily Events: {config.TargetDailyEvents:N0}");
        Console.WriteLine($"  Output File: {config.OutputFile}");
        Console.WriteLine($"  Verbose Logging: {config.Verbose}");
        Console.WriteLine($"  Debug Mode: {config.Debug}");
        
        // Display range filtering information
        if (config.StartFromLevel.HasValue || config.StopAtLevel.HasValue)
        {
            Console.WriteLine();
            Console.WriteLine("🎯 Range Filtering:");
            if (config.StartFromLevel.HasValue)
            {
                Console.WriteLine($"  Start From Level: {config.StartFromLevel.Value} (skipping lower concurrency levels)");
            }
            if (config.StopAtLevel.HasValue)
            {
                Console.WriteLine($"  Stop At Level: {config.StopAtLevel.Value} (avoiding higher concurrency levels)");
            }
        }
        
        Console.WriteLine();

        if (!config.Debug)
        {
            // Calculate and display requirements
            var requiredConcurrency = config.CalculateRequiredConcurrency();
            var minEventsPerSecond = config.CalculateMinEventsPerSecond();
            
            Console.WriteLine("📊 Calculated Requirements:");
            Console.WriteLine($"  Target Rate: {minEventsPerSecond:F1} events/second");
            Console.WriteLine($"  Required Concurrency: {requiredConcurrency} publishers");
            
            if (config.MaxConcurrency < requiredConcurrency)
            {
                Console.WriteLine($"  ⚠️  Warning: Max concurrency ({config.MaxConcurrency}) is less than required ({requiredConcurrency})");
                Console.WriteLine($"      You may not reach the target daily events rate.");
            }
            else
            {
                Console.WriteLine($"  ✅ Max concurrency ({config.MaxConcurrency}) is sufficient to reach target rate");
            }
            
            Console.WriteLine();

            // Display test sequence
            var levels = config.GetConcurrencyLevels().ToList();
            Console.WriteLine("🔢 Test Sequence:");
            Console.WriteLine($"  Concurrency Levels: {string.Join(", ", levels)}");
            Console.WriteLine($"  Total Tests: {levels.Count}");
            Console.WriteLine($"  Estimated Duration: {levels.Count * (config.Duration + config.WarmupDuration + 2):F0} seconds");
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine("🔍 DEBUG MODE: Will fire exactly 1 event to trace communication flow.");
            Console.WriteLine("  This helps diagnose why events aren't being received.");
            Console.WriteLine();
        }

        // Start immediately
        var action = config.Debug ? "debug trace" : "benchmark";
        Console.WriteLine($"Starting {action}...");
        Console.WriteLine();
    }

    private static void DisplaySummary(List<BenchmarkResult> results, BenchmarkConfig config)
    {
        Console.WriteLine();
        Console.WriteLine("🎯 Benchmark Results Summary");
        Console.WriteLine("============================");

        if (results.Count == 0)
        {
            Console.WriteLine("No results to display.");
            return;
        }

        var successfulResults = results.Where(r => r.Success).ToList();
        
        Console.WriteLine($"Total Tests: {results.Count}");
        Console.WriteLine($"Successful: {successfulResults.Count}");
        Console.WriteLine($"Failed: {results.Count - successfulResults.Count}");
        Console.WriteLine();

        if (successfulResults.Count > 0)
        {
            // Find best results
            var maxThroughput = successfulResults.OrderByDescending(r => r.ActualThroughput).First();
            var bestLatency = successfulResults.OrderBy(r => r.P95LatencyMs).First();
            
            Console.WriteLine("🏆 Best Performance:");
            Console.WriteLine($"  Max Throughput: {maxThroughput.ActualThroughput:F1} events/sec ({maxThroughput.ConcurrencyLevel} publishers)");
            Console.WriteLine($"  Best P95 Latency: {bestLatency.P95LatencyMs:F2}ms ({bestLatency.ConcurrencyLevel} publishers)");
            Console.WriteLine($"  Best P99 Latency: {successfulResults.Min(r => r.P99LatencyMs):F2}ms");
            Console.WriteLine();

            // Check if target was achieved
            var targetEventsPerSecond = config.CalculateMinEventsPerSecond();
            var targetAchieved = maxThroughput.ActualThroughput >= targetEventsPerSecond;
            
            Console.WriteLine("🎯 Target Achievement:");
            Console.WriteLine($"  Target: {targetEventsPerSecond:F1} events/sec");
            Console.WriteLine($"  Achieved: {maxThroughput.ActualThroughput:F1} events/sec");
            Console.WriteLine($"  Status: {(targetAchieved ? "✅ TARGET ACHIEVED" : "❌ TARGET NOT ACHIEVED")}");
            Console.WriteLine();

            // Performance breakdown
            Console.WriteLine("📈 Performance by Concurrency Level:");
            Console.WriteLine("  Concurrency | Throughput  | P95 Latency | P99 Latency | Success");
            Console.WriteLine("  ------------|-------------|-------------|-------------|--------");
            
            foreach (var result in results.OrderBy(r => r.ConcurrencyLevel))
            {
                var status = result.Success ? "✅" : "❌";
                var throughput = result.Success ? $"{result.ActualThroughput,9:F1}" : "        -";
                var p95 = result.Success ? $"{result.P95LatencyMs,9:F2}" : "        -";
                var p99 = result.Success ? $"{result.P99LatencyMs,9:F2}" : "        -";
                
                Console.WriteLine($"  {result.ConcurrencyLevel,10} | {throughput} | {p95} ms | {p99} ms | {status,6}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"📄 Detailed results saved to: {config.OutputFile}");
        Console.WriteLine("   Use this file for further analysis, visualization, or reporting.");
        Console.WriteLine();
    }
}
