using CommandLine;

namespace LatencyBenchmark;

[Verb("benchmark", HelpText = "Run latency benchmark tests")]
public class BenchmarkConfig
{
    [Option('b', "base-concurrency", Default = 1, HelpText = "Starting concurrency level (number of concurrent publishers)")]
    public int BaseConcurrency { get; set; }

    [Option('m', "max-concurrency", Default = 16, HelpText = "Maximum concurrency level (16 publishers × 10 events/sec = 160 events/sec total, ~13.8M events/day)")]
    public int MaxConcurrency { get; set; }

    [Option('d', "duration", Default = 60, HelpText = "Duration in seconds for each concurrency level")]
    public int Duration { get; set; }

    [Option('o', "output-file", Default = "latency-results.json", HelpText = "Output file for results")]
    public string OutputFile { get; set; } = "latency-results.json";

    [Option('r', "events-per-second", Default = 10, HelpText = "Target events per second per publisher")]
    public int EventsPerSecond { get; set; }

    [Option('s', "scale-factor", Default = 2, HelpText = "Scaling factor for concurrency levels (1, 2, 4, 8, 16, etc.)")]
    public int ScaleFactor { get; set; }

    [Option('v', "verbose", Default = false, HelpText = "Enable verbose logging")]
    public bool Verbose { get; set; }

    [Option('w', "warmup-duration", Default = 10, HelpText = "Warmup duration in seconds before starting measurements")]
    public int WarmupDuration { get; set; }

    [Option('t', "target-daily-events", Default = 10_000_000, HelpText = "Target number of events per day (10M = ~116 events/sec)")]
    public int TargetDailyEvents { get; set; }

    [Option("start-from-level", Default = null, HelpText = "Start testing from a specific concurrency level (useful for resuming tests or avoiding low levels)")]
    public int? StartFromLevel { get; set; }

    [Option("stop-at-level", Default = null, HelpText = "Stop testing at a specific concurrency level (useful when system shows overload symptoms)")]
    public int? StopAtLevel { get; set; }

    [Option("debug", Default = false, HelpText = "Debug mode: Send only 1 event to trace communication flow")]
    public bool Debug { get; set; }

    [Option("max-handlers", Default = 0, HelpText = "Maximum number of handler agents (0 = unlimited, matches publisher count)")]
    public int MaxHandlers { get; set; }

    [Option("handler-ratio", Default = 1.0, HelpText = "Publisher to handler ratio (e.g., 2 = 2 publishers per handler, 0.5 = 2 handlers per publisher)")]
    public double? HandlerRatio { get; set; }

    [Option("completion-timeout", Default = 60, HelpText = "Maximum time to wait for event processing completion after publishers finish (seconds)")]
    public int CompletionTimeout { get; set; }

    [Option("completion-check-interval", Default = 1, HelpText = "Interval between completion checks in seconds")]
    public int CompletionCheckInterval { get; set; }

    /// <summary>
    /// Calculate the minimum events per second needed to reach the target daily events
    /// </summary>
    public double CalculateMinEventsPerSecond()
    {
        // Events per day = 24 * 60 * 60 * events_per_second
        return TargetDailyEvents / (24.0 * 60.0 * 60.0);
    }

    /// <summary>
    /// Calculate the required concurrency level to reach the target daily events
    /// </summary>
    public int CalculateRequiredConcurrency()
    {
        var minEventsPerSecond = CalculateMinEventsPerSecond();
        return (int)Math.Ceiling(minEventsPerSecond / EventsPerSecond);
    }

    /// <summary>
    /// Get the sequence of concurrency levels to test
    /// </summary>
    public IEnumerable<int> GetConcurrencyLevels()
    {
        var levels = new List<int>();
        var current = BaseConcurrency;
        
        while (current <= MaxConcurrency)
        {
            levels.Add(current);
            if (ScaleFactor <= 1)
            {
                current += 1;
            }
            else
            {
                current *= ScaleFactor;
            }
        }
        
        // Apply range filtering if specified
        if (StartFromLevel.HasValue)
        {
            levels = levels.Where(l => l >= StartFromLevel.Value).ToList();
        }
        
        if (StopAtLevel.HasValue)
        {
            levels = levels.Where(l => l <= StopAtLevel.Value).ToList();
        }
        
        return levels;
    }

    /// <summary>
    /// Calculate the optimal number of handler agents based on publisher count and configuration
    /// </summary>
    /// <param name="publisherCount">Number of publisher agents</param>
    /// <returns>Optimal number of handler agents</returns>
    public int CalculateHandlerCount(int publisherCount)
    {
        if (HandlerRatio.HasValue)
        {
            // Use explicit ratio: publisherCount / handlerRatio = handlerCount
            // E.g., 16 publishers with ratio 2.0 → 16/2 = 8 handlers
            // E.g., 8 publishers with ratio 0.5 → 8/0.5 = 16 handlers
            var calculatedHandlers = (int)Math.Ceiling(publisherCount / HandlerRatio.Value);
            return Math.Max(1, calculatedHandlers);
        }
        
        if (MaxHandlers == 0)
        {
            // Unlimited handlers: 1:1 ratio
            return publisherCount;
        }
        
        // Use original logic with configurable max
        return Math.Max(1, Math.Min(publisherCount, MaxHandlers));
    }

    /// <summary>
    /// Validate the configuration parameters
    /// </summary>
    public void Validate()
    {
        if (BaseConcurrency < 1)
            throw new ArgumentException("Base concurrency must be at least 1");
            
        if (MaxConcurrency < BaseConcurrency)
            throw new ArgumentException("Max concurrency must be greater than or equal to base concurrency");
            
        if (Duration <= 0)
            throw new ArgumentException("Duration must be greater than 0");
            
        if (EventsPerSecond <= 0)
            throw new ArgumentException("Events per second must be greater than 0");
            
        if (ScaleFactor < 1)
            throw new ArgumentException("Scale factor must be at least 1");
            
        if (StartFromLevel.HasValue && StartFromLevel.Value < 1)
            throw new ArgumentException("Start from level must be at least 1");
            
        if (StopAtLevel.HasValue && StopAtLevel.Value < 1)
            throw new ArgumentException("Stop at level must be at least 1");
            
        if (StartFromLevel.HasValue && StopAtLevel.HasValue && StartFromLevel.Value > StopAtLevel.Value)
            throw new ArgumentException("Start from level must be less than or equal to stop at level");
            
        if (MaxHandlers < 0)
            throw new ArgumentException("Max handlers cannot be negative (use 0 for unlimited)");
            
        if (HandlerRatio.HasValue && HandlerRatio.Value <= 0)
            throw new ArgumentException("Handler ratio must be greater than 0");
            
        if (CompletionTimeout < 1)
            throw new ArgumentException("Completion timeout must be at least 1 second");
            
        if (CompletionCheckInterval < 1)
            throw new ArgumentException("Completion check interval must be at least 1 second");
    }
} 