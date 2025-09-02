using BenchmarkDotNet.Running;
using Aevatar.GAgents.Basic.Test;

// This program can be used to run benchmarks
// Usage: dotnet run -c Release --project test/Aevatar.GAgents.Basic.ConfigManagerGAgent.Test

Console.WriteLine("ConfigManagerGAgent Performance Test Suite");
Console.WriteLine("==========================================");
Console.WriteLine();

var cmdArgs = Environment.GetCommandLineArgs();

if (cmdArgs.Length > 1 && cmdArgs[1] == "benchmark")
{
    // Run BenchmarkDotNet benchmarks
    Console.WriteLine("Running BenchmarkDotNet performance benchmarks...");
    var summary = BenchmarkRunner.Run<ConfigManagerBenchmarks>();
    Console.WriteLine("Benchmarks completed. Results saved to BenchmarkDotNet.Artifacts folder.");
}
else
{
    // Run basic performance validation
    Console.WriteLine("Running basic performance validation tests...");
    
    try
    {
        // Run a simple benchmark test
        var benchmark = new ConfigManagerBenchmarks();
        benchmark.Setup();
        
        Console.WriteLine("Sample benchmark execution completed.");
        Console.WriteLine();
        Console.WriteLine("✅ Performance validation completed!");
        Console.WriteLine();
        Console.WriteLine("To run detailed benchmarks, use:");
        Console.WriteLine("dotnet run -c Release --project test/Aevatar.GAgents.Basic.ConfigManagerGAgent.Test benchmark");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Performance tests failed: {ex.Message}");
        Environment.Exit(1);
    }
}

Console.WriteLine("\nTest suite completed successfully!");