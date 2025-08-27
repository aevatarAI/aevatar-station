using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Placement;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace E2E.Grains;

/// <summary>
/// Simplified AgentData for benchmark purposes - compatible with CreatorGAgent
/// </summary>
[GenerateSerializer]
public class AgentData
{
    [Id(0)] public string AgentType { get; set; } = "";
    [Id(1)] public string Name { get; set; } = "";
    [Id(2)] public string Properties { get; set; } = "";
    [Id(3)] public Guid UserId { get; set; } = Guid.Empty;
    [Id(4)] public GrainId BusinessAgentGrainId { get; set; }
}

/// <summary>
/// Simplified UpdateAgentInput for benchmark purposes - compatible with CreatorGAgent
/// </summary>
[GenerateSerializer]
public class UpdateAgentInput
{
    [Id(0)] public string Name { get; set; } = "";
    [Id(1)] public string Properties { get; set; } = "";
}

/// <summary>
/// Event for layered communication testing
/// </summary>
[GenerateSerializer]
public class LayeredTestEvent : EventBase
{
    [Id(0)] public new string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    [Id(1)] public long SentTimestamp { get; set; }
    [Id(2)] public string PublisherThreadId { get; set; } = Thread.CurrentThread.ManagedThreadId.ToString();
    [Id(3)] public int EventNumber { get; set; }
    [Id(4)] public string PublisherAgentId { get; set; } = "";
    [Id(5)] public long LeaderReceivedTimestamp { get; set; }
    [Id(6)] public string LeaderAgentId { get; set; } = "";

    public LayeredTestEvent()
    {
        SentTimestamp = DateTimeOffset.UtcNow.Ticks;
    }

    public LayeredTestEvent(int number, string publisherAgentId, string? correlationId = null)
    {
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        SentTimestamp = DateTimeOffset.UtcNow.Ticks;
        EventNumber = number;
        PublisherAgentId = publisherAgentId;
        PublisherThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
    }
}

/// <summary>
/// Interface for layered leader agent - simplified main agent (replaces CreatorGAgent)
/// </summary>
public interface ILayeredLeaderAgent : IStateGAgent<LayeredLeaderState>, IExtGAgent
{
    // ===== Creator Agent Methods (LayeredLeaderAgent IS the creator agent) =====
    Task<LayeredLeaderState> GetAgentAsync();
    Task CreateAgentAsync(AgentData agentData);
    Task UpdateAgentAsync(UpdateAgentInput dto);
    Task DeleteAgentAsync();
    Task PublishEventAsync<T>(T @event) where T : EventBase;
    Task UpdateAvailableEventsAsync(List<Type>? eventTypeList);
    
    // ===== Metrics Methods (Benchmark-specific) =====
    Task<LayeredMetrics> GetLayeredMetricsAsync();
    Task ResetMetricsAsync();
}

/// <summary>
/// Interface for layered sub-agent
/// </summary>
public interface ILayeredSubAgent : IStateGAgent<LayeredSubState>
{
    Task<LayeredMetrics> GetLayeredMetricsAsync();
    Task ResetMetricsAsync();
}

/// <summary>
/// State classes for layered agents
/// </summary>
[GenerateSerializer]
public class LayeredLeaderState : StateBase
{
    // ===== Metrics fields (existing) =====
    [Id(0)] public long EventsReceived { get; set; }
    [Id(1)] public long EventsForwarded { get; set; }
    
    // ===== Agent data fields (Creator Agent compatibility) =====
    [Id(2)] public Guid Id { get; set; }
    [Id(3)] public Guid UserId { get; set; }
    [Id(4)] public string AgentType { get; set; } = "";
    [Id(5)] public string Name { get; set; } = "";
    [Id(6)] public string Properties { get; set; } = "";
    [Id(7)] public GrainId BusinessAgentGrainId { get; set; }
    [Id(8)] public List<string> EventInfoList { get; set; } = new();
    [Id(9)] public DateTime CreateTime { get; set; }
    [Id(10)] public string FormattedBusinessAgentGrainId { get; set; } = "";
}

[GenerateSerializer]
public class LayeredSubState : StateBase
{
    [Id(0)] public long EventsReceived { get; set; }
}

/// <summary>
/// State log events for layered agents
/// </summary>
[GenerateSerializer]
public class LayeredLeaderStateLogEvent : StateLogEventBase<LayeredLeaderStateLogEvent>
{
    [Id(0)] public long EventsReceived { get; set; }
    [Id(1)] public long EventsForwarded { get; set; }
}

[GenerateSerializer]
public class LayeredSubStateLogEvent : StateLogEventBase<LayeredSubStateLogEvent>
{
    [Id(0)] public long EventsReceived { get; set; }
}

/// <summary>
/// LayeredLeaderAgent - Main agent that replaces CreatorGAgent role
/// </summary>
[KeepAlive]
[GAgent]
[SiloNamePatternPlacement("User")]
public class LayeredLeaderAgent : GAgentBase<LayeredLeaderState, LayeredLeaderStateLogEvent>, ILayeredLeaderAgent
{
    private static readonly ActivitySource ActivitySource = new("LayeredBenchmark.LeaderAgent");
    private readonly List<LayeredMeasurement> _measurements = new();

    public LayeredLeaderAgent()
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Layered leader agent for hierarchical benchmarking");
    }

    protected override void GAgentTransitionState(LayeredLeaderState state, StateLogEventBase<LayeredLeaderStateLogEvent> @event)
    {
        if (@event is LayeredLeaderStateLogEvent logEvent)
        {
            state.EventsReceived = logEvent.EventsReceived;
            state.EventsForwarded = logEvent.EventsForwarded;
        }
    }

    // ===== Main Agent Methods Implementation =====
    
    public async Task PublishEventAsync<T>(T @event) where T : EventBase
    {
        using var activity = ActivitySource.StartActivity("PublishEventAsync", ActivityKind.Consumer);
        var leaderAgentId = this.GetPrimaryKey().ToString();
        var receivedTimestamp = DateTimeOffset.UtcNow.Ticks;
        
        Logger.LogDebug("🔥 LEADER {LeaderAgentId} PUBLISH-EVENT-ASYNC CALLED - Event type: {EventType}", 
            leaderAgentId, typeof(T).Name);
        
        Logger.LogInformation("📤 LayeredLeaderAgent {AgentId} received PublishEventAsync for {EventType}", 
            this.GetPrimaryKey(), typeof(T).Name);
        
        // Measure Client→Leader latency if this is a LayeredTestEvent
        if (@event is LayeredTestEvent testEvent)
        {
            Logger.LogDebug("🔥 LEADER {LeaderAgentId} PROCESSING LayeredTestEvent - CorrelationId: {CorrelationId}", 
                leaderAgentId, testEvent.CorrelationId);
            
            // Calculate latency: client publish time → leader receive time
            var latencyTicks = receivedTimestamp - testEvent.SentTimestamp;
            var latencyMs = TimeSpan.FromTicks(latencyTicks).TotalMilliseconds;
            
            Logger.LogDebug("🔥 LEADER {LeaderAgentId} CLIENT-LEADER LATENCY: {LatencyMs:F2}ms", 
                leaderAgentId, latencyMs);
            
            // Store Client→Leader latency measurement
            _measurements.Add(new LayeredMeasurement
            {
                CorrelationId = testEvent.CorrelationId,
                EventNumber = testEvent.EventNumber,
                LatencyMs = latencyMs,
                SubAgentId = leaderAgentId, // Use leader ID for client→leader measurements
                SentTimestamp = testEvent.SentTimestamp,
                ReceivedTimestamp = receivedTimestamp
            });
            
            Logger.LogDebug("🔥 LEADER {LeaderAgentId} STORED MEASUREMENT - Total measurements: {MeasurementCount}", 
                leaderAgentId, _measurements.Count);
            
            Logger.LogWarning("🔥 LEADER {LeaderAgentId} RECEIVED EVENT {CorrelationId} - Event #{EventNumber} from CLIENT - Latency: {LatencyMs:F2}ms", 
                leaderAgentId, testEvent.CorrelationId, testEvent.EventNumber, latencyMs);
            
            // Update the event with leader received timestamp and leader ID for sub-agent latency calculations
            testEvent.LeaderReceivedTimestamp = receivedTimestamp;
            testEvent.LeaderAgentId = leaderAgentId;
            
            if (activity != null)
            {
                activity.SetTag("leader.agent.id", leaderAgentId);
                activity.SetTag("event.number", testEvent.EventNumber);
                activity.SetTag("correlation.id", testEvent.CorrelationId.ToString());
                activity.SetTag("client.leader.latency.ms", latencyMs);
            }
        }
        else
        {
            Logger.LogDebug("🔥 LEADER {LeaderAgentId} NOT LayeredTestEvent - Event type: {EventType}", 
                leaderAgentId, typeof(T).Name);
        }
        
        // Update metrics
        var children = await GetChildrenAsync();
        RaiseEvent(new LayeredLeaderStateLogEvent 
        { 
            EventsReceived = State.EventsReceived + 1,
            EventsForwarded = State.EventsForwarded + children.Count
        });
        
        // Forward to children using GAgent framework (automatic routing)
        await PublishAsync(@event);
        await ConfirmEvents();
    }

    // ===== Creator Agent Methods Implementation =====
    
    public Task<LayeredLeaderState> GetAgentAsync()
    {
        Logger.LogInformation("GetAgentAsync {state}", JsonConvert.SerializeObject(State));
        return Task.FromResult(State);
    }
    
    public async Task CreateAgentAsync(AgentData agentData)
    {
        Logger.LogInformation("CreateAgentAsync");
        RaiseEvent(new LayeredLeaderStateLogEvent
        {
            EventsReceived = State.EventsReceived,
            EventsForwarded = State.EventsForwarded
        });
        await ConfirmEvents();
    }
    
    public async Task UpdateAgentAsync(UpdateAgentInput dto)
    {
        Logger.LogInformation("UpdateAgentAsync");
        await Task.CompletedTask;
    }
    
    public async Task DeleteAgentAsync()
    {
        Logger.LogInformation("DeleteAgentAsync");
        await Task.CompletedTask;
    }

    public async Task UpdateAvailableEventsAsync(List<Type>? eventTypeList)
    {
        Logger.LogInformation("UpdateAvailableEventsAsync");
        await Task.CompletedTask;
    }

    // ===== Additional Metrics Methods =====

    public Task<LayeredMetrics> GetLayeredMetricsAsync()
    {
        Logger.LogDebug("🔥 LEADER GetLayeredMetricsAsync CALLED - Measurements count: {MeasurementCount}", 
            _measurements.Count);
        
        var metrics = LayeredMetrics.FromMeasurements(_measurements, State.EventsReceived, State.EventsForwarded);
        
        Logger.LogDebug("🔥 LEADER METRICS CREATED - EventsReceived: {EventsReceived}, EventsForwarded: {EventsForwarded}, AvgLatency: {AvgLatencyMs:F2}ms", 
            metrics.EventsReceived, metrics.EventsForwarded, metrics.AvgLatencyMs);
        
        return Task.FromResult(metrics);
    }

    public async Task ResetMetricsAsync()
    {
        _measurements.Clear(); // Clear measurements
        RaiseEvent(new LayeredLeaderStateLogEvent { EventsReceived = 0, EventsForwarded = 0 });
        await ConfirmEvents();
    }
}

/// <summary>
/// LayeredSubAgent - Receives events from leader agents
/// </summary>
[KeepAlive]
[GAgent]
[SiloNamePatternPlacement("User")]
public class LayeredSubAgent : GAgentBase<LayeredSubState, LayeredSubStateLogEvent>, ILayeredSubAgent
{
    private static readonly ActivitySource ActivitySource = new("LayeredBenchmark.SubAgent");
    private readonly List<LayeredMeasurement> _measurements = new();

    public LayeredSubAgent()
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Layered sub-agent for hierarchical benchmarking");
    }

    protected override void GAgentTransitionState(LayeredSubState state, StateLogEventBase<LayeredSubStateLogEvent> @event)
    {
        if (@event is LayeredSubStateLogEvent logEvent)
        {
            state.EventsReceived = logEvent.EventsReceived;
        }
    }

    [EventHandler]
    public async Task OnLayeredTestEvent(LayeredTestEvent @event)
    {
        using var activity = ActivitySource.StartActivity("OnLayeredTestEvent", ActivityKind.Consumer);
        var subAgentId = this.GetPrimaryKey().ToString();
        var receivedTimestamp = DateTimeOffset.UtcNow.Ticks;
        
        // Calculate latency: leader forward time → sub-agent receive time
        // Use LeaderReceivedTimestamp if available, otherwise fall back to SentTimestamp
        var leaderTimestamp = @event.LeaderReceivedTimestamp > 0 ? @event.LeaderReceivedTimestamp : @event.SentTimestamp;
        var latencyTicks = receivedTimestamp - leaderTimestamp;
        var latencyMs = TimeSpan.FromTicks(latencyTicks).TotalMilliseconds;
        
        // Collect measurement for reporting
        _measurements.Add(new LayeredMeasurement
        {
            CorrelationId = @event.CorrelationId,
            EventNumber = @event.EventNumber,
            LatencyMs = latencyMs,
            SubAgentId = subAgentId,
            SentTimestamp = leaderTimestamp, // Use leader timestamp for Leader→Sub latency
            ReceivedTimestamp = receivedTimestamp
        });
        
        Logger.LogWarning("🔥 SUB-AGENT {SubAgentId} RECEIVED EVENT {CorrelationId} - Event #{EventNumber} from LEADER {LeaderAgentId} - Latency: {LatencyMs:F2}ms", 
            subAgentId, @event.CorrelationId, @event.EventNumber, @event.LeaderAgentId, latencyMs);
        
        if (activity != null)
        {
            activity.SetTag("sub.agent.id", subAgentId);
            activity.SetTag("leader.agent.id", @event.LeaderAgentId);
            activity.SetTag("event.number", @event.EventNumber);
            activity.SetTag("correlation.id", @event.CorrelationId.ToString());
            activity.SetTag("leader.sub.latency.ms", latencyMs);
        }

        // Update metrics: increment counter and store latency measurement
        RaiseEvent(new LayeredSubStateLogEvent { EventsReceived = State.EventsReceived + 1 });
        await ConfirmEvents();
    }

    public Task<LayeredMetrics> GetLayeredMetricsAsync()
    {
        var metrics = LayeredMetrics.FromMeasurements(_measurements, State.EventsReceived, 0);
        return Task.FromResult(metrics);
    }

    public async Task ResetMetricsAsync()
    {
        _measurements.Clear(); // Clear measurements
        RaiseEvent(new LayeredSubStateLogEvent { EventsReceived = 0 });
        await ConfirmEvents();
    }
}

/// <summary>
/// Measurement data for layered communication
/// </summary>
[GenerateSerializer]
public class LayeredMeasurement
{
    [Id(0)] public string CorrelationId { get; set; } = "";
    [Id(1)] public int EventNumber { get; set; }
    [Id(2)] public double LatencyMs { get; set; }
    [Id(3)] public string SubAgentId { get; set; } = "";
    [Id(4)] public long SentTimestamp { get; set; }
    [Id(5)] public long ReceivedTimestamp { get; set; }
}

/// <summary>
/// Metrics for layered communication
/// </summary>
[GenerateSerializer]
public class LayeredMetrics
{
    [Id(0)] public long EventsReceived { get; set; }
    [Id(1)] public long EventsForwarded { get; set; }
    [Id(2)] public double MinLatencyMs { get; set; }
    [Id(3)] public double MaxLatencyMs { get; set; }
    [Id(4)] public double AvgLatencyMs { get; set; }
    [Id(5)] public double P95LatencyMs { get; set; }
    [Id(6)] public double P99LatencyMs { get; set; }
    [Id(7)] public DateTime MeasurementTime { get; set; } = DateTime.UtcNow;
    [Id(8)] public List<LayeredMeasurement> RawMeasurements { get; set; } = new();

    public static LayeredMetrics FromMeasurements(List<LayeredMeasurement> measurements, long eventsReceived = 0, long eventsForwarded = 0)
    {
        if (measurements.Count == 0)
        {
            return new LayeredMetrics
            {
                EventsReceived = eventsReceived,
                EventsForwarded = eventsForwarded
            };
        }

        var latencies = measurements.Select(m => m.LatencyMs).OrderBy(l => l).ToList();

        return new LayeredMetrics
        {
            EventsReceived = eventsReceived,
            EventsForwarded = eventsForwarded,
            MinLatencyMs = latencies.First(),
            MaxLatencyMs = latencies.Last(),
            AvgLatencyMs = latencies.Average(),
            P95LatencyMs = GetPercentile(latencies, 95),
            P99LatencyMs = GetPercentile(latencies, 99),
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