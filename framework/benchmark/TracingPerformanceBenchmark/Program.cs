using System;
using BenchmarkDotNet.Running;
using TracingPerformanceBenchmark;

namespace TracingPerformanceBenchmark;

/// <summary>
/// Main program for tracing performance benchmarks.
/// Compares performance between Fody and no tracing.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("🚀 Starting Tracing Performance Benchmark");
        Console.WriteLine("==========================================");
        Console.WriteLine();
        Console.WriteLine("This benchmark compares two tracing approaches:");
        Console.WriteLine("1. No Tracing - Baseline performance");
        Console.WriteLine("2. Fody IL Weaving - Build-time code injection");
        Console.WriteLine();
        
        // Run all benchmarks
        var summary = BenchmarkRunner.Run<TracingPerformanceBenchmarks>();
        
        Console.WriteLine();
        Console.WriteLine("✅ Benchmark completed!");
        Console.WriteLine("Check the results above for performance comparison.");
    }
}
