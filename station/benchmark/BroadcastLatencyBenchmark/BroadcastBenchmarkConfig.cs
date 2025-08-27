using CommandLine;

namespace BroadcastLatencyBenchmark;

[Verb("broadcast-benchmark", HelpText = "Run broadcast latency benchmark tests")]
public class BroadcastBenchmarkConfig
{
    [Option('s', "subscriber-count", Default = 10, HelpText = "Number of subscriber agents to create")]
    public int SubscriberCount { get; set; } = 10;

    [Option('p', "publisher-count", Default = 1, HelpText = "Number of publisher agents to create")]
    public int PublisherCount { get; set; } = 1;

    [Option('d', "duration", Default = 5, HelpText = "Duration in seconds for each test")]
    public int Duration { get; set; } = 5;

    [Option('o', "output-file", Default = "broadcast-latency-results.json", HelpText = "Output file for results")]
    public string OutputFile { get; set; } = "broadcast-latency-results.json";

    [Option('r', "events-per-second", Default = 1, HelpText = "Target events per second per publisher")]
    public int EventsPerSecond { get; set; } = 1;

    [Option('v', "verbose", Default = false, HelpText = "Enable verbose logging")]
    public bool Verbose { get; set; } = false;

    [Option('w', "warmup-duration", Default = 10, HelpText = "Warmup duration in seconds before starting measurements")]
    public int WarmupDuration { get; set; } = 10;

    [Option("use-stored-ids", Default = true, HelpText = "Use stored agent IDs from broadcast_agent_ids.json")]
    public bool UseStoredIds { get; set; } = true;

    [Option("debug", Default = false, HelpText = "Debug mode: Send only 1 event to trace communication flow")]
    public bool Debug { get; set; } = false;

    [Option("completion-timeout", Default = 60, HelpText = "Maximum time to wait for event processing completion after publishers finish (seconds)")]
    public int CompletionTimeout { get; set; } = 60;

    [Option("completion-check-interval", Default = 1, HelpText = "Interval between completion checks in seconds")]
    public int CompletionCheckInterval { get; set; } = 1;

    [Option("event-number", Default = 100, HelpText = "Number to use in broadcast events (similar to VerifyDbIssue545)")]
    public int EventNumber { get; set; } = 100;

    /// <summary>
    /// Calculate the total events expected to be processed across all subscribers
    /// </summary>
    public long CalculateTotalExpectedEvents()
    {
        var eventsPerPublisher = EventsPerSecond * Duration;
        var totalEventsPublished = eventsPerPublisher * PublisherCount;
        return totalEventsPublished * SubscriberCount; // Each event is received by all subscribers
    }

    /// <summary>
    /// Calculate the total events that will be published
    /// </summary>
    public long CalculateTotalEventsToPublish()
    {
        var eventsPerPublisher = EventsPerSecond * Duration;
        return eventsPerPublisher * PublisherCount;
    }

    /// <summary>
    /// Validate the configuration parameters
    /// </summary>
    public void Validate()
    {
        if (SubscriberCount < 1)
            throw new ArgumentException("Subscriber count must be at least 1");
            
        if (PublisherCount < 1)
            throw new ArgumentException("Publisher count must be at least 1");
            
        if (Duration <= 0)
            throw new ArgumentException("Duration must be greater than 0");
            
        if (EventsPerSecond <= 0)
            throw new ArgumentException("Events per second must be greater than 0");
            
        if (CompletionTimeout < 1)
            throw new ArgumentException("Completion timeout must be at least 1 second");
            
        if (CompletionCheckInterval < 1)
            throw new ArgumentException("Completion check interval must be at least 1 second");
            
        if (EventNumber < 1)
            throw new ArgumentException("Event number must be at least 1");
    }
} 