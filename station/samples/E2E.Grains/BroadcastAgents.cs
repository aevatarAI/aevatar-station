using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Placement;
using Orleans.Streams;
using Orleans.Concurrency;
using System.Diagnostics;
using System.Collections.Concurrent;
using Orleans;
using E2E.Grains;

namespace E2E.Grains;

/// <summary>
/// Event for broadcast latency testing
/// </summary>
[GenerateSerializer]
public class BroadcastTestEvent : EventBase
{
    [Id(0)] public new string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    [Id(1)] public long SentTimestamp { get; set; }
    [Id(2)] public int EventNumber { get; set; }
    [Id(3)] public Guid PublisherAgentId { get; set; }
    [Id(4)] public string PublisherThreadId { get; set; } = Thread.CurrentThread.ManagedThreadId.ToString();
    [Id(5)] public int Number { get; set; }

    public BroadcastTestEvent()
    {
        SentTimestamp = DateTimeOffset.UtcNow.Ticks;
    }

    public BroadcastTestEvent(int eventNumber, Guid publisherAgentId, int number, string? correlationId = null)
    {
        EventNumber = eventNumber;
        PublisherAgentId = publisherAgentId;
        Number = number;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        SentTimestamp = DateTimeOffset.UtcNow.Ticks;
        PublisherThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
    }
}

/// <summary>
/// Publisher agent interface for broadcast scenarios
/// </summary>
public interface IBroadcastScheduleAgent : IGAgent
{
    Task BroadcastEventAsync(BroadcastTestEvent @event);
    Task<long> GetEventsSentAsync();
    Task ResetMetricsAsync();
}

/// <summary>
/// Subscriber agent interface for broadcast scenarios
/// </summary>
public interface IBroadcastUserAgent : IGAgent
{
    new Task ActivateAsync();
    Task<int> GetCount();
    Task<BroadcastLatencyMetrics> GetLatencyMetricsAsync();
    Task ResetMetricsAsync();
}

// State classes for agents - Schedule agent needs to use BroadCastGState for broadcast functionality
[GenerateSerializer]
public class BroadcastScheduleState : BroadcastGState
{
    [Id(0)] public long EventsSent { get; set; } = 0;
}

[GenerateSerializer]
public class BroadcastUserState : BroadcastGState
{
    [Id(0)] public int Count { get; set; } = 0;
    [Id(1)] public long TotalEventsProcessed { get; set; } = 0;
}

// Event classes for state transitions
[GenerateSerializer]
public class BroadcastScheduleStateLogEvent : StateLogEventBase<BroadcastScheduleStateLogEvent>
{
    [Id(0)] public long EventsSent { get; set; }
}

[GenerateSerializer]
public class BroadcastUserStateLogEvent : StateLogEventBase<BroadcastUserStateLogEvent>
{
    [Id(0)] public int Count { get; set; }
    [Id(1)] public long TotalEventsProcessed { get; set; }
}

/// <summary>
/// Agent that publishes broadcast events (similar to TestDbScheduleGAgent)
/// </summary>
[KeepAlive]
[SiloNamePatternPlacement("Scheduler")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class BroadcastScheduleAgent : BroadcastGAgentBase<BroadcastScheduleState, BroadcastScheduleStateLogEvent>, IBroadcastScheduleAgent
{
    private static readonly ActivitySource ActivitySource = new("BroadcastScheduleAgent", "1.0.0");

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Broadcast latency test publisher agent");
    }

    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnGAgentActivateAsync(cancellationToken);
    }

    protected override void GAgentTransitionState(BroadcastScheduleState state, StateLogEventBase<BroadcastScheduleStateLogEvent> @event)
    {
        switch (@event)
        {
            case BroadcastScheduleStateLogEvent logEvent:
                state.EventsSent = logEvent.EventsSent;
                break;
        }
    }

    public async Task BroadcastEventAsync(BroadcastTestEvent @event)
    {
        using var activity = ActivitySource.StartActivity("BroadcastEvent", ActivityKind.Producer);
        
        if (activity != null)
        {
            activity.SetTag("publisher.agent.id", this.GetPrimaryKey());
            activity.SetTag("event.number", @event.EventNumber);
            activity.SetTag("event.correlation_id", @event.CorrelationId);
            activity.SetTag("event.sent_timestamp", @event.SentTimestamp);
            activity.SetTag("broadcast.number", @event.Number);
        }

        await this.BroadcastEventAsync("BroadcastScheduleAgent", @event);
        
        // Update metrics
        RaiseEvent(new BroadcastScheduleStateLogEvent { EventsSent = State.EventsSent + 1 });
        await ConfirmEvents();

        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebug("BroadcastScheduleAgent {PublisherId} broadcast event {CorrelationId} with number {Number}",
                this.GetPrimaryKey(), @event.CorrelationId, @event.Number);
        }
    }

    public Task<long> GetEventsSentAsync()
    {
        return Task.FromResult(State.EventsSent);
    }

    public async Task ResetMetricsAsync()
    {
        RaiseEvent(new BroadcastScheduleStateLogEvent { EventsSent = 0 });
        await ConfirmEvents();
    }
}

/// <summary>
/// Agent that receives broadcast events (similar to TestDbGAgent)
/// </summary>
[KeepAlive]
[SiloNamePatternPlacement("User")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class BroadcastUserAgent : BroadcastGAgentBase<BroadcastUserState, BroadcastUserStateLogEvent>, IBroadcastUserAgent
{
    private static readonly ActivitySource ActivitySource = new("BroadcastUserAgent", "1.0.0");
    private readonly ConcurrentDictionary<string, BroadcastLatencyMeasurement> _latencyMeasurements = new();

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Broadcast latency test subscriber agent");
    }

    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnGAgentActivateAsync(cancellationToken);

        await SubscribeBroadcastEventAsync<BroadcastTestEvent>("BroadcastScheduleAgent", OnBroadcastTestEvent);
    }

    protected override void GAgentTransitionState(BroadcastUserState state, StateLogEventBase<BroadcastUserStateLogEvent> @event)
    {
        switch (@event)
        {
            case BroadcastUserStateLogEvent logEvent:
                state.Count = logEvent.Count;
                state.TotalEventsProcessed = logEvent.TotalEventsProcessed;
                break;
        }
    }

    public new async Task ActivateAsync()
    {
        // Similar to VerifyDbIssue545, just activate the agent
        await Task.CompletedTask;
        Logger.LogInformation("BroadcastUserAgent {AgentId} activated", this.GetPrimaryKey());
    }

    public Task<int> GetCount()
    {
        return Task.FromResult(State.Count);
    }

    [EventHandler]
    public async Task OnBroadcastTestEvent(BroadcastTestEvent @event)
    {
        using var activity = ActivitySource.StartActivity("ProcessBroadcastEvent", ActivityKind.Consumer);
        
        var processedTimestamp = DateTimeOffset.UtcNow.Ticks;
        var latencyMs = (processedTimestamp - @event.SentTimestamp) / 10000.0; // Convert ticks to milliseconds
        
        if (activity != null)
        {
            activity.SetTag("subscriber.agent.id", this.GetPrimaryKey());
            activity.SetTag("event.number", @event.EventNumber);
            activity.SetTag("event.correlation_id", @event.CorrelationId);
            activity.SetTag("publisher.agent.id", @event.PublisherAgentId);
            activity.SetTag("latency.ms", latencyMs);
            activity.SetTag("broadcast.number", @event.Number);
        }

        // Record latency measurement
        var measurement = new BroadcastLatencyMeasurement
        {
            CorrelationId = @event.CorrelationId,
            SentTimestamp = @event.SentTimestamp,
            ProcessedTimestamp = processedTimestamp,
            LatencyMs = latencyMs,
            EventNumber = @event.EventNumber,
            PublisherThreadId = @event.PublisherThreadId,
            ProcessorThreadId = Thread.CurrentThread.ManagedThreadId.ToString(),
            PublisherAgentId = @event.PublisherAgentId.ToString(),
            ProcessorAgentId = this.GetPrimaryKey().ToString(),
            Number = @event.Number
        };
        
        _latencyMeasurements.TryAdd(@event.CorrelationId, measurement);

        // Update state similar to VerifyDbIssue545
        var newCount = State.Count + @event.Number;
        var newTotalProcessed = State.TotalEventsProcessed + 1;
        
        RaiseEvent(new BroadcastUserStateLogEvent 
        { 
            Count = newCount, 
            TotalEventsProcessed = newTotalProcessed 
        });
        await ConfirmEvents();

        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebug("BroadcastUserAgent {AgentId} processed event {CorrelationId} with number {Number}, latency: {LatencyMs}ms, new count: {Count}",
                this.GetPrimaryKey(), @event.CorrelationId, @event.Number, latencyMs, newCount);
        }
    }

    public Task<BroadcastLatencyMetrics> GetLatencyMetricsAsync()
    {
        var measurements = _latencyMeasurements.Values.ToList();
        var metrics = BroadcastLatencyMetrics.FromMeasurements(measurements);
        metrics.TotalEventsProcessed = State.TotalEventsProcessed;
        return Task.FromResult(metrics);
    }

    public async Task ResetMetricsAsync()
    {
        _latencyMeasurements.Clear();
        RaiseEvent(new BroadcastUserStateLogEvent { Count = 0, TotalEventsProcessed = 0 });
        await ConfirmEvents();
    }
}

/// <summary>
/// Latency measurement for broadcast events
/// </summary>
[GenerateSerializer]
public class BroadcastLatencyMeasurement
{
    [Id(0)] public string CorrelationId { get; set; } = "";
    [Id(1)] public long SentTimestamp { get; set; }
    [Id(2)] public long ProcessedTimestamp { get; set; }
    [Id(3)] public double LatencyMs { get; set; }
    [Id(4)] public int EventNumber { get; set; }
    [Id(5)] public string PublisherThreadId { get; set; } = "";
    [Id(6)] public string ProcessorThreadId { get; set; } = "";
    [Id(7)] public string PublisherAgentId { get; set; } = "";
    [Id(8)] public string ProcessorAgentId { get; set; } = "";
    [Id(9)] public int Number { get; set; }
}

/// <summary>
/// Latency metrics for broadcast events
/// </summary>
[GenerateSerializer]
public class BroadcastLatencyMetrics
{
    [Id(0)] public long TotalEventsProcessed { get; set; }
    [Id(1)] public double MinLatencyMs { get; set; }
    [Id(2)] public double MaxLatencyMs { get; set; }
    [Id(3)] public double AverageLatencyMs { get; set; }
    [Id(4)] public double MedianLatencyMs { get; set; }
    [Id(5)] public double P95LatencyMs { get; set; }
    [Id(6)] public double P99LatencyMs { get; set; }
    [Id(7)] public double StandardDeviationMs { get; set; }
    [Id(8)] public DateTime MeasurementTime { get; set; } = DateTime.UtcNow;
    [Id(9)] public List<BroadcastLatencyMeasurement> RawMeasurements { get; set; } = new();

    public static BroadcastLatencyMetrics FromMeasurements(List<BroadcastLatencyMeasurement> measurements)
    {
        if (measurements.Count == 0)
        {
            return new BroadcastLatencyMetrics();
        }

        var latencies = measurements.Select(m => m.LatencyMs).OrderBy(l => l).ToList();
        var average = latencies.Average();
        var variance = latencies.Select(l => Math.Pow(l - average, 2)).Average();
        var standardDeviation = Math.Sqrt(variance);

        return new BroadcastLatencyMetrics
        {
            TotalEventsProcessed = measurements.Count,
            MinLatencyMs = latencies.First(),
            MaxLatencyMs = latencies.Last(),
            AverageLatencyMs = average,
            MedianLatencyMs = GetPercentile(latencies, 0.5),
            P95LatencyMs = GetPercentile(latencies, 0.95),
            P99LatencyMs = GetPercentile(latencies, 0.99),
            StandardDeviationMs = standardDeviation,
            RawMeasurements = measurements
        };
    }

    private static double GetPercentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) return 0;
        if (sortedValues.Count == 1) return sortedValues[0];

        double rank = percentile * (sortedValues.Count - 1);
        int index = (int)Math.Floor(rank);
        double fraction = rank - index;

        if (index >= sortedValues.Count - 1)
            return sortedValues[sortedValues.Count - 1];

        return sortedValues[index] + fraction * (sortedValues[index + 1] - sortedValues[index]);
    }
} 