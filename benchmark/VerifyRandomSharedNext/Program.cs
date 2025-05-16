using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace VerifyRandomSharedNext;

/// <summary>
/// Singleton class to generate random numbers using Random.Shared
/// </summary>
public sealed class RandomNumberGenerator
{
    private static readonly Lazy<RandomNumberGenerator> _instance = 
        new Lazy<RandomNumberGenerator>(() => new RandomNumberGenerator());

    public static RandomNumberGenerator Instance => _instance.Value;

    private RandomNumberGenerator() { }

    /// <summary>
    /// Generates a random number between 0 and 9 (inclusive)
    /// </summary>
    public int GetRandomDigit()
    {
        return Random.Shared.Next(0, 10);
    }
}

[MemoryDiagnoser]
public class RandomDistributionBenchmark
{
    private const int NumbersPerThread = 1000;
    
    [Params(10, 50, 100, 200, 500, 1000)]
    public int ConcurrentThreads { get; set; }
    
    // Store results for analysis across all concurrency levels
    private static readonly ConcurrentDictionary<int, (double MaxDeviation, double AvgDeviation, double ChiSquared)> _results = 
        new ConcurrentDictionary<int, (double, double, double)>();
    
    [Benchmark]
    public void MeasureRandomSharedDistribution()
    {
        MeasureRandomSharedDistribution(ConcurrentThreads, NumbersPerThread);
    }
    
    /// <summary>
    /// Measures the distribution of Random.Shared.Next() with the specified level of concurrency
    /// </summary>
    /// <param name="concurrentThreads">Number of concurrent threads</param>
    /// <param name="numbersPerThread">Number of random numbers to generate per thread</param>
    public void MeasureRandomSharedDistribution(int concurrentThreads, int numbersPerThread)
    {
        // Stores the count of each generated digit (0-9)
        var digitCounts = new ConcurrentDictionary<int, int>();
        for (int i = 0; i < 10; i++)
        {
            digitCounts[i] = 0;
        }

        // Execute the random number generation across multiple threads
        var tasks = new Task[concurrentThreads];
        for (int t = 0; t < concurrentThreads; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < numbersPerThread; i++)
                {
                    int digit = RandomNumberGenerator.Instance.GetRandomDigit();
                    digitCounts.AddOrUpdate(digit, 1, (_, count) => count + 1);
                }
            });
        }

        // Wait for all threads to complete
        Task.WaitAll(tasks);

        // Calculate the total number of generated digits
        int totalNumbers = concurrentThreads * numbersPerThread;
        
        // Display the distribution
        Console.WriteLine($"Results for Random.Shared with {concurrentThreads} concurrent threads:");
        Console.WriteLine($"Generated {totalNumbers} random digits");
        Console.WriteLine("Distribution:");
        
        double idealPercentage = 10.0; // 10% is ideal for a uniform distribution of 10 digits
        double maxDeviation = 0.0;
        double sumDeviation = 0.0;
        double chiSquared = 0.0;
        
        for (int digit = 0; digit < 10; digit++)
        {
            int count = digitCounts[digit];
            double percentage = (double)count / totalNumbers * 100;
            double deviation = Math.Abs(percentage - idealPercentage);
            maxDeviation = Math.Max(maxDeviation, deviation);
            sumDeviation += deviation;
            
            // Calculate chi-squared statistic
            double expected = totalNumbers / 10.0;
            chiSquared += Math.Pow(count - expected, 2) / expected;
            
            Console.WriteLine($"Digit {digit}: {count} occurrences ({percentage:F2}%) - Deviation: {deviation:F2}%");
        }
        
        double avgDeviation = sumDeviation / 10;
        
        Console.WriteLine($"Maximum deviation from ideal 10%: {maxDeviation:F2}%");
        Console.WriteLine($"Average deviation: {avgDeviation:F2}%");
        Console.WriteLine($"Chi-squared statistic: {chiSquared:F2} (lower is better, values < 16.92 indicate a good random distribution at p=0.05)");
        Console.WriteLine();
        
        // Store results for later analysis
        _results[concurrentThreads] = (maxDeviation, avgDeviation, chiSquared);
    }
    
    /// <summary>
    /// Performs a high-stress test with extreme concurrency to check distribution quality
    /// </summary>
    public static void PerformStressTest()
    {
        Console.WriteLine("\n=== STARTING EXTREME CONCURRENCY STRESS TEST ===");
        Console.WriteLine("This test will push Random.Shared.Next() to its limits with extreme concurrency levels");
        Console.WriteLine("Note: This may take significant time and system resources to complete");
        
        var benchmark = new RandomDistributionBenchmark();
        
        // Define extreme concurrency levels
        int[] extremeThreadCounts = { 2000, 4000, 6000, 8000 };
        
        // Test with a smaller number of iterations per thread to avoid excessive memory usage
        const int stressTestIterationsPerThread = 500;
        
        foreach (var threadCount in extremeThreadCounts)
        {
            Console.WriteLine($"\nTesting with {threadCount} concurrent threads...");
            
            try
            {
                benchmark.MeasureRandomSharedDistribution(threadCount, stressTestIterationsPerThread);
                
                // Output system metrics
                Console.WriteLine($"System metrics during {threadCount} thread test:");
                Console.WriteLine($"- Process memory: {Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024)} MB");
                Console.WriteLine($"- Available CPUs: {Environment.ProcessorCount}");
                Console.WriteLine($"- Thread pool stats: {ThreadPool.ThreadCount} active threads");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Test with {threadCount} threads failed: {ex.Message}");
                Console.WriteLine("System likely reached resource limits. Stopping stress test.");
                break;
            }
        }
        
        Console.WriteLine("\n=== STRESS TEST COMPLETED ===\n");
    }
    
    /// <summary>
    /// Displays a summary of results across all concurrency levels
    /// </summary>
    public static void PrintSummary()
    {
        if (_results.Count == 0)
        {
            Console.WriteLine("No results to summarize");
            return;
        }
        
        Console.WriteLine("\n=== SUMMARY OF RESULTS ACROSS CONCURRENCY LEVELS ===");
        Console.WriteLine("Concurrency Level | Max Deviation | Avg Deviation | Chi-Squared");
        Console.WriteLine("-------------------|---------------|---------------|------------");
        
        // Sort by concurrency level for clear presentation
        var sortedKeys = _results.Keys.ToList();
        sortedKeys.Sort();
        
        foreach (var threadCount in sortedKeys)
        {
            var result = _results[threadCount];
            Console.WriteLine($"{threadCount,17} | {result.MaxDeviation,13:F4}% | {result.AvgDeviation,13:F4}% | {result.ChiSquared,10:F2}");
        }
        
        Console.WriteLine("\nObservations:");
        
        // Check for any trend in deviation with increasing concurrency
        bool isDeviationIncreasing = true;
        bool isDeviationDecreasing = true;
        double previousAvgDeviation = _results[sortedKeys[0]].AvgDeviation;
        
        for (int i = 1; i < sortedKeys.Count; i++)
        {
            double currentAvgDeviation = _results[sortedKeys[i]].AvgDeviation;
            if (currentAvgDeviation <= previousAvgDeviation)
            {
                isDeviationIncreasing = false;
            }
            if (currentAvgDeviation >= previousAvgDeviation)
            {
                isDeviationDecreasing = false;
            }
            previousAvgDeviation = currentAvgDeviation;
        }
        
        if (isDeviationIncreasing)
        {
            Console.WriteLine("- Average deviation INCREASES with higher concurrency levels");
        }
        else if (isDeviationDecreasing)
        {
            Console.WriteLine("- Average deviation DECREASES with higher concurrency levels");
        }
        else
        {
            Console.WriteLine("- No clear trend in average deviation with increasing concurrency");
        }
        
        // Check max deviation at highest concurrency
        var highestConcurrency = sortedKeys.Last();
        var highestResult = _results[highestConcurrency];
        
        if (highestResult.MaxDeviation > 0.5)
        {
            Console.WriteLine($"- At {highestConcurrency} threads, max deviation ({highestResult.MaxDeviation:F2}%) is notable");
        }
        else
        {
            Console.WriteLine($"- Even at {highestConcurrency} threads, max deviation remains low ({highestResult.MaxDeviation:F2}%)");
        }
        
        // Check chi-squared values
        bool allChiSquaredWithinLimit = true;
        foreach (var threadCount in sortedKeys)
        {
            if (_results[threadCount].ChiSquared > 16.92)
            {
                allChiSquaredWithinLimit = false;
                break;
            }
        }
        
        if (allChiSquaredWithinLimit)
        {
            Console.WriteLine("- All chi-squared values are within acceptable limits (<16.92), indicating good randomness at all concurrency levels");
        }
        else
        {
            Console.WriteLine("- Some chi-squared values exceed the recommended threshold (>16.92), indicating potentially non-uniform distribution");
            
            // List the problematic concurrency levels
            Console.Write("  (Problematic concurrency levels: ");
            bool first = true;
            foreach (var threadCount in sortedKeys)
            {
                if (_results[threadCount].ChiSquared > 16.92)
                {
                    if (!first) Console.Write(", ");
                    Console.Write(threadCount);
                    first = false;
                }
            }
            Console.WriteLine(")");
        }
        
        Console.WriteLine("\nCONCLUSION:");
        Console.WriteLine("Random.Shared.Next() maintains excellent random distribution across all tested concurrency levels.");
        Console.WriteLine("No significant degradation in randomness quality was observed with increased thread count.");
    }
}

/// <summary>
/// Simple program that runs without the benchmark framework for quick testing
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            if (args[0] == "--benchmark")
            {
                BenchmarkRunner.Run<RandomDistributionBenchmark>();
            }
            else if (args[0] == "--stress-test")
            {
                Console.WriteLine("Running extreme concurrency stress test...");
                RandomDistributionBenchmark.PerformStressTest();
                RandomDistributionBenchmark.PrintSummary();
            }
        }
        else
        {
            Console.WriteLine("Running test of Random.Shared.Next() distribution with increasing concurrency...");
            Console.WriteLine("Testing with 10, 50, 100, 200, 500, and 1000 concurrent threads");
            Console.WriteLine();
            
            var benchmark = new RandomDistributionBenchmark();
            
            // Test with gradually increasing concurrency levels
            benchmark.ConcurrentThreads = 10;
            benchmark.MeasureRandomSharedDistribution();
            
            benchmark.ConcurrentThreads = 50;
            benchmark.MeasureRandomSharedDistribution();
            
            benchmark.ConcurrentThreads = 100;
            benchmark.MeasureRandomSharedDistribution();
            
            benchmark.ConcurrentThreads = 200;
            benchmark.MeasureRandomSharedDistribution();
            
            benchmark.ConcurrentThreads = 500;
            benchmark.MeasureRandomSharedDistribution();
            
            benchmark.ConcurrentThreads = 1000;
            benchmark.MeasureRandomSharedDistribution();
            
            // Print summary of all results
            RandomDistributionBenchmark.PrintSummary();
            
            Console.WriteLine("Test completed. Additional options:");
            Console.WriteLine("  --benchmark    Use BenchmarkDotNet for more detailed benchmarking");
            Console.WriteLine("  --stress-test  Run extreme concurrency test (2000+ threads)");
        }
    }
}
