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
using Aevatar.Core.Interception;

[module: Interceptor]

namespace E2E.Grains;

/// <summary>
/// Event for latency testing with agent-to-agent communication
/// </summary>
[GenerateSerializer]
public class LatencyTestEvent : EventBase
{
    [Id(0)] public new string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    [Id(1)] public long SentTimestamp { get; set; }
    [Id(2)] public string PublisherThreadId { get; set; } = Thread.CurrentThread.ManagedThreadId.ToString();
    [Id(3)] public int EventNumber { get; set; }
    [Id(4)] public Guid PublisherAgentId { get; set; }

    public LatencyTestEvent()
    {
        SentTimestamp = DateTimeOffset.UtcNow.Ticks;
    }

    public LatencyTestEvent(int number, Guid publisherAgentId, string? correlationId = null)
    {
        EventNumber = number;
        PublisherAgentId = publisherAgentId;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        SentTimestamp = DateTimeOffset.UtcNow.Ticks;
        PublisherThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
    }
}



/// <summary>
/// Publisher agent interface
/// </summary>
public interface ILatencyPublisherAgent : IGAgent
{
    Task PublishEventAsync(LatencyTestEvent @event, Guid targetHandlerStreamId);
    Task<long> GetEventsSentAsync();
    Task ResetMetricsAsync();
}

/// <summary>
/// Handler agent interface
/// </summary>
public interface ILatencyHandlerAgent : IGAgent
{
    Task StartListeningAsync(Guid streamId);
    Task StopListeningAsync();
    Task<LatencyMetrics> GetLatencyMetricsAsync();
    Task ResetMetricsAsync();
}



// Simple state classes for GAgentBase
[GenerateSerializer]
public class LatencyPublisherState : StateBase
{
    [Id(0)] public long EventsSent { get; set; } = 0;
}

[GenerateSerializer]
public class LatencyHandlerState : StateBase
{
    [Id(0)] public long TotalEventsProcessed { get; set; } = 0;
}



// Simple event classes for GAgentBase
[GenerateSerializer]
public class LatencyPublisherStateLogEvent : StateLogEventBase<LatencyPublisherStateLogEvent>
{
    [Id(0)] public long EventsSent { get; set; }
}

[GenerateSerializer]
public class LatencyHandlerStateLogEvent : StateLogEventBase<LatencyHandlerStateLogEvent>
{
    [Id(0)] public long TotalEventsProcessed { get; set; }
}



/// <summary>
/// Agent that publishes events to streams for agent-to-agent communication testing
/// </summary>
[KeepAlive]
[SiloNamePatternPlacement("Scheduler")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class LatencyPublisherAgent : GAgentBase<LatencyPublisherState, LatencyPublisherStateLogEvent>, ILatencyPublisherAgent
{
    private static readonly ActivitySource ActivitySource = new("LatencyPublisherAgent", "1.0.0");

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Latency test publisher agent");
    }

    protected override void GAgentTransitionState(LatencyPublisherState state, StateLogEventBase<LatencyPublisherStateLogEvent> @event)
    {
        switch (@event)
        {
            case LatencyPublisherStateLogEvent logEvent:
                state.EventsSent = logEvent.EventsSent;
                break;
        }
    }

    public async Task PublishEventAsync(LatencyTestEvent @event, Guid targetHandlerStreamId)
    {
        using var activity = ActivitySource.StartActivity("PublishEvent", ActivityKind.Producer);

        if (activity != null)
        {
            activity.SetTag("publisher.agent.id", this.GetPrimaryKey());
            activity.SetTag("target.stream.id", targetHandlerStreamId.ToString());
            activity.SetTag("event.number", @event.EventNumber);
            activity.SetTag("event.correlation_id", @event.CorrelationId);
            activity.SetTag("event.sent_timestamp", @event.SentTimestamp);
        }

        // Send to handler's auto-registered GAgent stream
        // Get the actual handler agent and use its GrainId for proper stream routing
        var handlerAgent = GrainFactory.GetGrain<ILatencyHandlerAgent>(targetHandlerStreamId);
        var handlerGrainId = handlerAgent.GetGrainId();
        var stream = GetEventBaseStream(handlerGrainId);
        var eventWrapper = new EventWrapper<LatencyTestEvent>(@event, Guid.NewGuid(), this.GetGrainId());
        await stream.OnNextAsync(eventWrapper);

        // Update metrics
        RaiseEvent(new LatencyPublisherStateLogEvent { EventsSent = State.EventsSent + 1 });
        await ConfirmEvents();

        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebug("Publisher {PublisherId} sent event {CorrelationId} to stream {StreamId}",
                this.GetPrimaryKey(), @event.CorrelationId, targetHandlerStreamId);
        }
    }

    public Task<long> GetEventsSentAsync()
    {
        return Task.FromResult(State.EventsSent);
    }

    public async Task ResetMetricsAsync()
    {
        RaiseEvent(new LatencyPublisherStateLogEvent { EventsSent = 0 });
        await ConfirmEvents();
    }
}

/// <summary>
/// Agent that handles events from streams for agent-to-agent communication testing
/// </summary>
[KeepAlive]
[SiloNamePatternPlacement("User")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class LatencyHandlerAgent : GAgentBase<LatencyHandlerState, LatencyHandlerStateLogEvent>, ILatencyHandlerAgent
{
    private static readonly ActivitySource ActivitySource = new("LatencyBenchmark.Handler");

    private readonly ConcurrentDictionary<string, LatencyMeasurement> _latencyMeasurements = new();

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Latency test handler agent");
    }

    protected override void GAgentTransitionState(LatencyHandlerState state, StateLogEventBase<LatencyHandlerStateLogEvent> @event)
    {
        switch (@event)
        {
            case LatencyHandlerStateLogEvent logEvent:
                state.TotalEventsProcessed = logEvent.TotalEventsProcessed;
                break;
        }
    }

    public async Task StartListeningAsync(Guid streamId)
    {
        try
        {
            Logger.LogInformation("🎯 LatencyHandlerAgent {AgentId} starting to listen for LatencyTestEvent on stream {StreamId}",
                this.GetPrimaryKey(), streamId);

            // With EventHandler attribute, Orleans automatically routes events to the handler method
            // No manual subscription needed - Orleans handles this automatically

            Logger.LogInformation("✅ LatencyHandlerAgent {AgentId} ready to receive LatencyTestEvent on stream {StreamId}",
                this.GetPrimaryKey(), streamId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ LatencyHandlerAgent {AgentId} failed to set up listening for stream {StreamId}",
                this.GetPrimaryKey(), streamId);
            throw;
        }
    }

    public async Task StopListeningAsync()
    {
        Logger.LogInformation("🛑 LatencyHandlerAgent {AgentId} stopping listeners", this.GetPrimaryKey());
        await Task.CompletedTask;
    }

    [EventHandler]
    [Interceptor]
    public async Task OnLatencyTestEvent(LatencyTestEvent @event)
    {
        using var activity = ActivitySource.StartActivity("OnLatencyTestEvent", ActivityKind.Consumer);
        var processingTimestamp = DateTimeOffset.UtcNow.Ticks;
        var handlerId = this.GetPrimaryKey().ToString();

        Logger.LogInformation("🎯 Handler {HandlerId} processing LatencyTestEvent {CorrelationId} from publisher {PublisherId} (event #{EventNumber})",
            handlerId, @event.CorrelationId, @event.PublisherAgentId, @event.EventNumber);

        if (activity != null)
        {
            activity.SetTag("handler.agent.id", handlerId);
            activity.SetTag("publisher.agent.id", @event.PublisherAgentId.ToString());
            activity.SetTag("event.number", @event.EventNumber);
            activity.SetTag("event.correlation_id", @event.CorrelationId);
            activity.SetTag("event.sent_timestamp", @event.SentTimestamp);
            activity.SetTag("event.processed_timestamp", processingTimestamp);
            activity.SetTag("trace.id", activity.TraceId.ToString());
        }

        // Calculate latency
        var latencyTicks = processingTimestamp - @event.SentTimestamp;
        var latencyMs = TimeSpan.FromTicks(latencyTicks).TotalMilliseconds;

        // Check for duplicate processing
        if (_latencyMeasurements.ContainsKey(@event.CorrelationId))
        {
            Logger.LogWarning("⚠️ Handler {HandlerId} received DUPLICATE event {CorrelationId} - this indicates stream replay or multiple subscriptions!",
                handlerId, @event.CorrelationId);
            return; // Skip duplicate processing
        }

        // Record latency measurement
        _latencyMeasurements.TryAdd(@event.CorrelationId, new LatencyMeasurement
        {
            CorrelationId = @event.CorrelationId,
            SentTimestamp = @event.SentTimestamp,
            ProcessedTimestamp = processingTimestamp,
            LatencyMs = latencyMs,
            EventNumber = @event.EventNumber,
            PublisherThreadId = @event.PublisherThreadId,
            ProcessorThreadId = Thread.CurrentThread.ManagedThreadId.ToString(),
            PublisherAgentId = @event.PublisherAgentId.ToString(),
            ProcessorAgentId = handlerId
        });

        // Update metrics using event sourcing
        RaiseEvent(new LatencyHandlerStateLogEvent { TotalEventsProcessed = State.TotalEventsProcessed + 1 });
        await ConfirmEvents();

        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebug("✅ Handler {HandlerId} processed event {CorrelationId} from {PublisherId} with latency {LatencyMs:F2}ms",
                handlerId, @event.CorrelationId, @event.PublisherAgentId, latencyMs);
        }
    }

    public Task<LatencyMetrics> GetLatencyMetricsAsync()
    {
        var measurements = _latencyMeasurements.Values.ToList();
        var metrics = LatencyMetrics.FromMeasurements(measurements);
        metrics.TotalEventsProcessed = State.TotalEventsProcessed;
        return Task.FromResult(metrics);
    }

    public async Task ResetMetricsAsync()
    {
        _latencyMeasurements.Clear();
        RaiseEvent(new LatencyHandlerStateLogEvent { TotalEventsProcessed = 0 });
        await ConfirmEvents();
    }
}





[GenerateSerializer]
public class LatencyMeasurement
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
}

[GenerateSerializer]
public class LatencyMetrics
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
    [Id(9)] public List<LatencyMeasurement> RawMeasurements { get; set; } = new();

    public static LatencyMetrics FromMeasurements(List<LatencyMeasurement> measurements)
    {
        if (measurements.Count == 0)
        {
            return new LatencyMetrics();
        }

        var latencies = measurements.Select(m => m.LatencyMs).OrderBy(l => l).ToList();
        var mean = latencies.Average();
        var variance = latencies.Select(l => Math.Pow(l - mean, 2)).Average();
        var standardDeviation = Math.Sqrt(variance);

        return new LatencyMetrics
        {
            TotalEventsProcessed = measurements.Count,
            MinLatencyMs = latencies.First(),
            MaxLatencyMs = latencies.Last(),
            AverageLatencyMs = mean,
            MedianLatencyMs = GetPercentile(latencies, 50),
            P95LatencyMs = GetPercentile(latencies, 95),
            P99LatencyMs = GetPercentile(latencies, 99),
            StandardDeviationMs = standardDeviation,
            RawMeasurements = measurements
        };
    }

    private static double GetPercentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) return 0;

        var index = (percentile / 100.0) * (sortedValues.Count - 1);
        var lowerIndex = (int)Math.Floor(index);
        var upperIndex = (int)Math.Ceiling(index);

        if (lowerIndex == upperIndex)
        {
            return sortedValues[lowerIndex];
        }

        var lowerValue = sortedValues[lowerIndex];
        var upperValue = sortedValues[upperIndex];
        var weight = index - lowerIndex;

        return lowerValue + weight * (upperValue - lowerValue);
    }
} 