using BenchmarkDotNet.Running;
using OrleansServiceDiscoveryBenchmark;
using Serilog;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Setup global logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

try
{
            // Check if running in demo mode
        if (args.Length > 0 && args[0] == "--demo")
        {
            Console.WriteLine("🎯 Orleans ZooKeeper Demo Mode");
            Console.WriteLine("Ensure ZooKeeper service is running (localhost:2181)");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Console.WriteLine();

            var demo = new ZooKeeperDemo();
            await demo.RunAsync();
            
            Console.WriteLine();
            Console.WriteLine("Demo completed, press any key to exit...");
            Console.ReadKey();
            return 0;
        }
        
        // Check if running quick test
        if (args.Length > 0 && args[0] == "--quick")
        {
            await QuickTest.RunAsync();
            return 0;
        }

    Console.WriteLine("Orleans Service Discovery Benchmark");
    Console.WriteLine("===================================");
    Console.WriteLine();
    Console.WriteLine("This benchmark compares MongoDB vs ZooKeeper as Orleans service discovery providers.");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run                    - Run benchmarks");
    Console.WriteLine("  dotnet run -- --demo         - Run ZooKeeper demo");
    Console.WriteLine();
    Console.WriteLine("Prerequisites:");
    Console.WriteLine("1. Docker installed (for ZooKeeper)");
    Console.WriteLine("2. MongoDB running locally or via EphemeralMongo");
    Console.WriteLine();
    Console.WriteLine("Starting benchmarks...");
    Console.WriteLine();

    var summary = BenchmarkRunner.Run<ServiceDiscoveryBenchmark>();
    
    Console.WriteLine();
    Console.WriteLine("Benchmark completed successfully!");
    Console.WriteLine($"Results saved to: {summary.ResultsDirectoryPath}");
    
    return 0;
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
} 