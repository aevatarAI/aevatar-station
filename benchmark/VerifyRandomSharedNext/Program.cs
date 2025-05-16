using System;
using System.Collections.Concurrent;
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

/// <summary>
/// Non-thread-safe singleton class to generate random numbers using a single Random instance
/// This shows the problems that can occur with a single shared Random instance with internal state
/// </summary>
public sealed class NonThreadSafeRandomNumberGenerator
{
    private static readonly Lazy<NonThreadSafeRandomNumberGenerator> _instance =
        new Lazy<NonThreadSafeRandomNumberGenerator>(() => new NonThreadSafeRandomNumberGenerator());

    public static NonThreadSafeRandomNumberGenerator Instance => _instance.Value;

    // A single shared instance - not thread-safe!
    private readonly Random _random = new Random();
    
    // Internal state that will be corrupted by concurrent access
    private int[] _internalBuffer;
    private int _currentIndex;
    private int _internalState;
    private readonly SpinWait _spinWait = new SpinWait();
    
    // Counts for tracking occurrences of thread-safety issues for demonstration
    private int _indexOutOfRangeCount;
    
    private NonThreadSafeRandomNumberGenerator() 
    {
        // Initialize the internal state
        _internalBuffer = new int[10];
        for (int i = 0; i < 10; i++)
        {
            _internalBuffer[i] = _random.Next(10);
        }
        _currentIndex = 0;
        _internalState = 0;
        _indexOutOfRangeCount = 0;
    }
    
    /// <summary>
    /// Returns the count of index out of range exceptions encountered
    /// </summary>
    public int GetIndexOutOfRangeCount() => _indexOutOfRangeCount;

    /// <summary>
    /// Generates a random number between 0 and 9 (inclusive)
    /// This method deliberately makes thread-safety issues more likely by manipulating an internal state
    /// that can be corrupted by concurrent access
    /// </summary>
    public int GetRandomDigit()
    {
        int result;
        
        try
        {
            // Get the current value
            result = _internalBuffer[_currentIndex];
            
            // Introduce a small delay to increase the chance of race conditions
            if (Environment.ProcessorCount > 1)
            {
                for (int i = 0; i < 5; i++)
                {
                    _spinWait.SpinOnce();
                }
            }
            
            // Increment the internal state - this operation is not atomic and can be corrupted
            _internalState++;
            
            // Update the index based on the internal state - not atomic and can be corrupted
            _currentIndex = (_internalState % 10);
            
            // Introduce another small delay
            if (Environment.ProcessorCount > 1)
            {
                for (int i = 0; i < 5; i++)
                {
                    _spinWait.SpinOnce();
                }
            }
            
            // Update the value at the new index - this can be corrupted by concurrent access
            _internalBuffer[_currentIndex] = _random.Next(10);
        }
        catch (IndexOutOfRangeException)
        {
            // If we get an index out of range exception, it's a clear thread-safety issue
            Interlocked.Increment(ref _indexOutOfRangeCount);
            
            // Reset the state to recover
            _currentIndex = 0;
            _internalState = 0;
            result = 0; // Return 0 as an indicator of the issue
        }
        
        return result;
    }
}

[MemoryDiagnoser]
public class RandomDistributionBenchmark
{
    private const int NumbersPerThread = 1000;
    
    [Params(10, 50, 100)]
    public int ConcurrentThreads { get; set; }
    
    [Benchmark(Baseline = true)]
    public void MeasureRandomSharedDistribution()
    {
        // Stores the count of each generated digit (0-9)
        var digitCounts = new ConcurrentDictionary<int, int>();
        for (int i = 0; i < 10; i++)
        {
            digitCounts[i] = 0;
        }

        // Execute the random number generation across multiple threads
        var tasks = new Task[ConcurrentThreads];
        for (int t = 0; t < ConcurrentThreads; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < NumbersPerThread; i++)
                {
                    int digit = RandomNumberGenerator.Instance.GetRandomDigit();
                    digitCounts.AddOrUpdate(digit, 1, (_, count) => count + 1);
                }
            });
        }

        // Wait for all threads to complete
        Task.WaitAll(tasks);

        // Calculate the total number of generated digits
        int totalNumbers = ConcurrentThreads * NumbersPerThread;
        
        // Display the distribution
        Console.WriteLine($"Results for Random.Shared with {ConcurrentThreads} concurrent threads:");
        Console.WriteLine($"Generated {totalNumbers} random digits");
        Console.WriteLine("Distribution:");
        
        double idealPercentage = 10.0; // 10% is ideal for a uniform distribution of 10 digits
        double maxDeviation = 0.0;
        double chiSquared = 0.0;
        
        for (int digit = 0; digit < 10; digit++)
        {
            int count = digitCounts[digit];
            double percentage = (double)count / totalNumbers * 100;
            double deviation = Math.Abs(percentage - idealPercentage);
            maxDeviation = Math.Max(maxDeviation, deviation);
            
            // Calculate chi-squared statistic
            double expected = totalNumbers / 10.0;
            chiSquared += Math.Pow(count - expected, 2) / expected;
            
            Console.WriteLine($"Digit {digit}: {count} occurrences ({percentage:F2}%) - Deviation: {deviation:F2}%");
        }
        
        Console.WriteLine($"Maximum deviation from ideal 10%: {maxDeviation:F2}%");
        Console.WriteLine($"Chi-squared statistic: {chiSquared:F2} (lower is better, values < 16.92 indicate a good random distribution at p=0.05)");
        Console.WriteLine();
    }
    
    [Benchmark]
    public void MeasureNonThreadSafeRandomDistribution()
    {
        // Reset the global NonThreadSafeRandomNumberGenerator instance
        // This is only for demonstration purposes
        var instance = NonThreadSafeRandomNumberGenerator.Instance;
        
        // Stores the count of each generated digit (0-9)
        var digitCounts = new ConcurrentDictionary<int, int>();
        for (int i = 0; i < 10; i++)
        {
            digitCounts[i] = 0;
        }

        // Execute the random number generation across multiple threads
        var tasks = new Task[ConcurrentThreads];
        for (int t = 0; t < ConcurrentThreads; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < NumbersPerThread; i++)
                {
                    int digit = NonThreadSafeRandomNumberGenerator.Instance.GetRandomDigit();
                    digitCounts.AddOrUpdate(digit, 1, (_, count) => count + 1);
                }
            });
        }

        // Wait for all threads to complete
        Task.WaitAll(tasks);

        // Calculate the total number of generated digits
        int totalNumbers = ConcurrentThreads * NumbersPerThread;
        
        // Display the distribution
        Console.WriteLine($"Results for non-thread-safe Random with {ConcurrentThreads} concurrent threads:");
        Console.WriteLine($"Generated {totalNumbers} random digits");
        Console.WriteLine("Distribution:");
        
        double idealPercentage = 10.0; // 10% is ideal for a uniform distribution of 10 digits
        double maxDeviation = 0.0;
        int digitsWithHighDeviation = 0;
        double chiSquared = 0.0;
        
        for (int digit = 0; digit < 10; digit++)
        {
            int count = digitCounts.TryGetValue(digit, out int value) ? value : 0;
            double percentage = (double)count / totalNumbers * 100;
            double deviation = Math.Abs(percentage - idealPercentage);
            maxDeviation = Math.Max(maxDeviation, deviation);
            
            // Calculate chi-squared statistic
            double expected = totalNumbers / 10.0;
            chiSquared += Math.Pow(count - expected, 2) / expected;
            
            if (deviation > 1.0)
            {
                digitsWithHighDeviation++;
            }
            
            Console.WriteLine($"Digit {digit}: {count} occurrences ({percentage:F2}%) - Deviation: {deviation:F2}%");
        }
        
        Console.WriteLine($"Maximum deviation from ideal 10%: {maxDeviation:F2}%");
        Console.WriteLine($"Chi-squared statistic: {chiSquared:F2} (lower is better, values < 16.92 indicate a good random distribution at p=0.05)");
        
        // Check for symptoms of thread-safety issues
        if (maxDeviation > 3.0)
        {
            Console.WriteLine($"WARNING: High maximum deviation ({maxDeviation:F2}%) - Indication of thread-safety issues!");
        }
        
        if (digitsWithHighDeviation >= 3)
        {
            Console.WriteLine($"WARNING: {digitsWithHighDeviation} digits have deviations greater than 1% - Indication of non-uniform distribution!");
        }
        
        // Check if we encountered any index out of range exceptions
        int indexOutOfRangeCount = NonThreadSafeRandomNumberGenerator.Instance.GetIndexOutOfRangeCount();
        if (indexOutOfRangeCount > 0)
        {
            Console.WriteLine($"CRITICAL ERROR: Encountered {indexOutOfRangeCount} IndexOutOfRangeException(s) due to thread-safety issues!");
        }
        
        if (chiSquared > 16.92)
        {
            Console.WriteLine($"WARNING: Chi-squared statistic ({chiSquared:F2}) is high, indicating a non-uniform distribution!");
        }
        
        Console.WriteLine();
    }
}

/// <summary>
/// Simple program that runs without the benchmark framework for quick testing
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--benchmark")
        {
            BenchmarkRunner.Run<RandomDistributionBenchmark>();
        }
        else
        {
            Console.WriteLine("Running quick test without benchmarking framework...");
            Console.WriteLine("Testing both Random.Shared and non-thread-safe Random with 10, 50, and 100 concurrent threads");
            Console.WriteLine("The non-thread-safe implementation uses internal state that can be corrupted by concurrent access");
            Console.WriteLine("Thread-safety issues may manifest as:");
            Console.WriteLine("1. Non-uniform distribution (high chi-squared values)");
            Console.WriteLine("2. IndexOutOfRangeException errors (clear sign of state corruption)");
            Console.WriteLine("3. High deviation from the expected 10% for each digit");
            Console.WriteLine();
            
            var benchmark = new RandomDistributionBenchmark();
            
            benchmark.ConcurrentThreads = 10;
            benchmark.MeasureRandomSharedDistribution();
            benchmark.MeasureNonThreadSafeRandomDistribution();
            
            benchmark.ConcurrentThreads = 50;
            benchmark.MeasureRandomSharedDistribution();
            benchmark.MeasureNonThreadSafeRandomDistribution();
            
            benchmark.ConcurrentThreads = 100;
            benchmark.MeasureRandomSharedDistribution();
            benchmark.MeasureNonThreadSafeRandomDistribution();
            
            Console.WriteLine("Test completed. Run with --benchmark to use BenchmarkDotNet");
        }
    }
}
