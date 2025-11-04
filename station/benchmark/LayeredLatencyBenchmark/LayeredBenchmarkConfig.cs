using CommandLine;

namespace LayeredLatencyBenchmark;

[Verb("layered", HelpText = "Run layered agent-to-agent latency benchmark tests")]
public class LayeredBenchmarkConfig
{
    [Option('l', "leader-count", Default = 1, HelpText = "Number of leader agents (fixed at 1 for layered architecture)")]
    public int LeaderCount { get; set; }

    [Option("base-sub-agents", Default = 1, HelpText = "Base number of sub-agents to start testing with")]
    public int BaseSubAgents { get; set; }

    [Option("max-sub-agents", Default = 8192, HelpText = "Maximum number of sub-agents to test with")]
    public int MaxSubAgents { get; set; }

    [Option("scale-factor", Default = 2, HelpText = "Scale factor for sub-agent count progression")]
    public int ScaleFactor { get; set; }

    [Option('d', "duration", Default = 60, HelpText = "Duration in seconds for each test")]
    public int Duration { get; set; }

    [Option('o', "output-file", Default = "layered-results.json", HelpText = "Output file for results")]
    public string OutputFile { get; set; } = "layered-results.json";

    [Option('r', "events-per-second", Default = 10, HelpText = "Target events per second per leader")]
    public int EventsPerSecond { get; set; }

    [Option('v', "verbose", Default = false, HelpText = "Enable verbose logging")]
    public bool Verbose { get; set; }

    [Option('w', "warmup-duration", Default = 10, HelpText = "Warmup duration in seconds before starting measurements")]
    public int WarmupDuration { get; set; }

    [Option("debug", Default = false, HelpText = "Debug mode: Create 1 leader + 1 sub-agent and send only 1 event")]
    public bool Debug { get; set; }

    [Option("completion-timeout", Default = 60, HelpText = "Maximum time to wait for event processing completion after leaders finish (seconds)")]
    public int CompletionTimeout { get; set; }

    [Option("completion-check-interval", Default = 1, HelpText = "Interval between completion checks in seconds")]
    public int CompletionCheckInterval { get; set; }

    [Option("target-daily-events", Default = 10_000_000, HelpText = "Target daily events to process")]
    public long TargetDailyEvents { get; set; }

    [Option("start-from-level", HelpText = "Start testing from this sub-agent count level (skip lower levels)")]
    public int? StartFromLevel { get; set; }

    [Option("stop-at-level", Default = 32, HelpText = "Stop testing at this sub-agent count level (avoid higher levels)")]
    public int? StopAtLevel { get; set; }

    /// <summary>
    /// Get the sub-agent counts to test (concurrency levels)
    /// </summary>
    public IEnumerable<int> GetSubAgentConcurrencyLevels()
    {
        var levels = new List<int>();
        
        for (int subAgents = BaseSubAgents; subAgents <= MaxSubAgents; subAgents *= ScaleFactor)
        {
            // Apply range filtering
            if (StartFromLevel.HasValue && subAgents < StartFromLevel.Value)
                continue;
                
            if (StopAtLevel.HasValue && subAgents > StopAtLevel.Value)
                break;
                
            levels.Add(subAgents);
        }
        
        return levels;
    }

    /// <summary>
    /// Calculate minimum events per second needed to achieve target daily events
    /// </summary>
    public double CalculateMinEventsPerSecond()
    {
        return TargetDailyEvents / (24.0 * 60.0 * 60.0);
    }

    /// <summary>
    /// Calculate required sub-agent count to achieve target daily events
    /// </summary>
    public int CalculateRequiredSubAgents()
    {
        var eventsPerSecondNeeded = CalculateMinEventsPerSecond();
        var eventsPerSecondPerSubAgent = EventsPerSecond; // Events flow from leader to each sub-agent
        return (int)Math.Ceiling(eventsPerSecondNeeded / eventsPerSecondPerSubAgent);
    }

    /// <summary>
    /// Calculate expected total events per second for a given sub-agent count
    /// </summary>
    public double CalculateTotalEventsPerSecond(int subAgentCount)
    {
        // Target is the daily consumption requirement, not scaled by sub-agent count
        // To achieve 10M events/day, we need ~115.7 events/sec total throughput
        return CalculateMinEventsPerSecond();
    }

    /// <summary>
    /// Validate the configuration parameters
    /// </summary>
    public void Validate()
    {
        if (LeaderCount != 1)
            throw new ArgumentException("Leader count must be 1 for layered architecture benchmarks");

        if (BaseSubAgents < 1)
            throw new ArgumentException("Base sub-agents must be at least 1");

        if (MaxSubAgents < BaseSubAgents)
            throw new ArgumentException("Max sub-agents must be >= base sub-agents");

        if (ScaleFactor < 2)
            throw new ArgumentException("Scale factor must be at least 2");

        if (Duration <= 0)
            throw new ArgumentException("Duration must be greater than 0");

        if (EventsPerSecond <= 0)
            throw new ArgumentException("Events per second must be greater than 0");

        if (WarmupDuration < 0)
            throw new ArgumentException("Warmup duration cannot be negative");

        if (CompletionTimeout < 1)
            throw new ArgumentException("Completion timeout must be at least 1 second");

        if (CompletionCheckInterval < 1)
            throw new ArgumentException("Completion check interval must be at least 1 second");

        if (TargetDailyEvents <= 0)
            throw new ArgumentException("Target daily events must be greater than 0");

        if (StartFromLevel.HasValue && StartFromLevel.Value < BaseSubAgents)
            throw new ArgumentException("Start-from-level must be >= base sub-agents");

        if (StopAtLevel.HasValue && StopAtLevel.Value > MaxSubAgents)
            throw new ArgumentException("Stop-at-level must be <= max sub-agents");
    }

    /// <summary>
    /// Display configuration summary
    /// </summary>
    public void DisplaySummary()
    {
        Console.WriteLine($"Leader count: {LeaderCount} (fixed for layered architecture)");
        Console.WriteLine($"Sub-agent range: {BaseSubAgents} - {MaxSubAgents}");
        Console.WriteLine($"Scale factor: {ScaleFactor}");
        Console.WriteLine($"Events per second per leader: {EventsPerSecond}");
        Console.WriteLine($"Duration: {Duration}s per test");
        Console.WriteLine($"Warmup duration: {WarmupDuration}s");
        Console.WriteLine($"Target daily events: {TargetDailyEvents:N0}");
        Console.WriteLine($"Debug mode: {Debug}");
        
        if (StartFromLevel.HasValue || StopAtLevel.HasValue)
        {
            Console.WriteLine("Range filtering:");
            if (StartFromLevel.HasValue)
                Console.WriteLine($"  Start from level: {StartFromLevel.Value}");
            if (StopAtLevel.HasValue)
                Console.WriteLine($"  Stop at level: {StopAtLevel.Value}");
        }
    }
} 