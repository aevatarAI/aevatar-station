using Microsoft.Extensions.Logging;
using Orleans;
using E2E.Grains;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace LatencyBenchmark;

public class ConcurrentPublisher : IDisposable
{
    private readonly ILogger<ConcurrentPublisher> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly ConcurrentDictionary<string, DateTime> _sentEvents = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly SemaphoreSlim _rateLimiter;
    private readonly int _eventsPerSecond;
    private readonly int _publisherId;
    private readonly string _publisherName;
    private long _totalEventsSent = 0;
    private long _totalEventsAcknowledged = 0;
    private volatile bool _isRunning = false;
    private Task? _publishingTask;

    // Agent-to-agent communication fields
    private ILatencyPublisherAgent? _publisherAgent;
    private readonly Guid _publisherAgentId;
    private readonly List<Guid> _handlerAgentIds;

    public ConcurrentPublisher(
        int publisherId,
        int eventsPerSecond,
        IClusterClient clusterClient,
        ILogger<ConcurrentPublisher> logger,
        List<Guid> handlerAgentIds,
        Guid publisherAgentId)
    {
        _publisherId = publisherId;
        _eventsPerSecond = eventsPerSecond;
        _clusterClient = clusterClient;
        _logger = logger;
        _publisherName = $"Scheduler-{publisherId}";  // Use "Scheduler" silo for publisher agents
        _publisherAgentId = publisherAgentId;  // Use provided agent ID for consistency
        _handlerAgentIds = handlerAgentIds ?? throw new ArgumentNullException(nameof(handlerAgentIds));
        
        // Create rate limiter for events per second
        _rateLimiter = new SemaphoreSlim(eventsPerSecond, eventsPerSecond);
        
        // Start rate limiter replenishment task
        _ = Task.Run(ReplenishRateLimiterAsync);
    }

    public async Task StartPublishingAsync(
        int targetGrainCount,
        int durationSeconds,
        CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            throw new InvalidOperationException("Publisher is already running");
        }

        _isRunning = true;
        var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _cancellationTokenSource.Token).Token;

        // Initialize publisher agent for agent-to-agent communication
        _publisherAgent = _clusterClient.GetGrain<ILatencyPublisherAgent>(_publisherAgentId);

        _logger.LogInformation("Starting publisher {PublisherName} (Agent: {AgentId}) with {EventsPerSecond} events/sec for {Duration}s targeting {GrainCount} handler agents",
            _publisherName, _publisherAgentId, _eventsPerSecond, durationSeconds, targetGrainCount);

        _publishingTask = Task.Run(async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            var endTime = TimeSpan.FromSeconds(durationSeconds);
            var eventNumber = 0;

            try
            {
                while (stopwatch.Elapsed < endTime && !combinedCancellationToken.IsCancellationRequested)
                {
                    await _rateLimiter.WaitAsync(combinedCancellationToken);

                    if (combinedCancellationToken.IsCancellationRequested)
                        break;

                    var correlationId = Guid.NewGuid().ToString();
                    var targetHandlerAgentId = GetTargetHandlerAgentId(targetGrainCount);
                    
                    // Create event for agent-to-agent communication
                    var latencyEvent = new LatencyTestEvent(eventNumber++, _publisherAgentId, correlationId);
                    
                    _sentEvents.TryAdd(correlationId, DateTime.UtcNow);

                    try
                    {
                        // Use agent-to-agent communication via stream
                        await _publisherAgent!.PublishEventAsync(latencyEvent, targetHandlerAgentId);
                        
                        _totalEventsSent++;

                        _logger.LogInformation("Publisher {PublisherId} sent event {EventNumber} (ID: {CorrelationId}) to handler {HandlerId}", 
                            _publisherId, eventNumber - 1, correlationId, targetHandlerAgentId);

                        if (_logger.IsEnabled(LogLevel.Debug))
                        {
                            _logger.LogDebug("Publisher agent {PublisherId} sent event {CorrelationId} to handler agent {HandlerId}", 
                                _publisherAgentId, correlationId, targetHandlerAgentId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send event {CorrelationId} from publisher agent {PublisherId} to handler agent {HandlerId}", 
                            correlationId, _publisherAgentId, targetHandlerAgentId);
                        
                        // Remove from sent events if publishing failed
                        _sentEvents.TryRemove(correlationId, out _);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Publisher {PublisherName} was cancelled", _publisherName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in publisher {PublisherName}", _publisherName);
            }
            finally
            {
                _isRunning = false;
                _logger.LogInformation("Publisher {PublisherName} (Agent: {AgentId}) stopped. Sent {TotalEvents} events in {ElapsedTime}s",
                    _publisherName, _publisherAgentId, _totalEventsSent, stopwatch.Elapsed.TotalSeconds);
            }
        }, combinedCancellationToken);

        await _publishingTask;
    }

    public async Task StopPublishingAsync()
    {
        if (!_isRunning)
        {
            return;
        }

        _logger.LogInformation("Stopping publisher {PublisherName}", _publisherName);
        _cancellationTokenSource.Cancel();

        if (_publishingTask != null)
        {
            try
            {
                await _publishingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }

        _isRunning = false;
    }

    public PublisherMetrics GetMetrics()
    {
        return new PublisherMetrics
        {
            PublisherId = _publisherId,
            PublisherName = _publisherName,
            TotalEventsSent = _totalEventsSent,
            TotalEventsAcknowledged = _totalEventsAcknowledged,
            EventsPerSecond = _eventsPerSecond,
            IsRunning = _isRunning,
            PendingEvents = _sentEvents.Count,
            PublisherAgentId = _publisherAgentId.ToString()
        };
    }

    public void ResetMetrics()
    {
        _sentEvents.Clear();
        _totalEventsSent = 0;
        _totalEventsAcknowledged = 0;
    }

    private Guid GetTargetHandlerAgentId(int targetGrainCount)
    {
        // Use deterministic/sticky assignment: each publisher always uses the same handler
        // This prevents random clustering on busy handlers and ensures consistent latency
        var availableHandlers = Math.Min(targetGrainCount, _handlerAgentIds.Count);
        var handlerIndex = _publisherId % availableHandlers;
        var targetHandlerId = _handlerAgentIds[handlerIndex];
        
        // Add debugging to verify deterministic assignment
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Publisher {PublisherId} consistently targeting handler {HandlerId} (index {HandlerIndex} of {TotalHandlers} handlers)", 
                _publisherId, targetHandlerId, handlerIndex, availableHandlers);
        }
        
        return targetHandlerId;
    }

    private async Task ReplenishRateLimiterAsync()
    {
        var interval = TimeSpan.FromMilliseconds(1000.0 / _eventsPerSecond);
        
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, _cancellationTokenSource.Token);
                
                // Release one permit per interval to maintain the desired rate
                if (_rateLimiter.CurrentCount < _eventsPerSecond)
                {
                    _rateLimiter.Release();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _rateLimiter.Dispose();
    }
}

public class PublisherMetrics
{
    public int PublisherId { get; set; }
    public string PublisherName { get; set; } = "";
    public long TotalEventsSent { get; set; }
    public long TotalEventsAcknowledged { get; set; }
    public int EventsPerSecond { get; set; }
    public bool IsRunning { get; set; }
    public int PendingEvents { get; set; }
    public DateTime MeasurementTime { get; set; } = DateTime.UtcNow;
    public string PublisherAgentId { get; set; } = "";
}

public class ConcurrentPublisherManager : IDisposable
{
    private readonly ILogger<ConcurrentPublisherManager> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly List<ConcurrentPublisher> _publishers = new();
    private readonly object _lock = new();

    public ConcurrentPublisherManager(
        IClusterClient clusterClient,
        ILoggerFactory loggerFactory)
    {
        _clusterClient = clusterClient;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ConcurrentPublisherManager>();
    }

    public async Task RunConcurrentPublishersAsync(
        int publisherCount,
        int eventsPerSecond,
        int targetGrainCount,
        int durationSeconds,
        List<Guid> handlerAgentIds,
        List<Guid> publisherAgentIds,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting {PublisherCount} concurrent publishers with {EventsPerSecond} events/sec each for {Duration}s targeting {HandlerCount} handler agents",
            publisherCount, eventsPerSecond, durationSeconds, targetGrainCount);

        if (publisherAgentIds.Count < publisherCount)
        {
            throw new ArgumentException($"Not enough publisher agent IDs provided. Need {publisherCount}, got {publisherAgentIds.Count}");
        }

        // Create publishers using the provided agent IDs
        lock (_lock)
        {
            _publishers.Clear();
            for (int i = 0; i < publisherCount; i++)
            {
                var publisher = new ConcurrentPublisher(
                i,
                eventsPerSecond,
                _clusterClient,
                _loggerFactory.CreateLogger<ConcurrentPublisher>(),
                handlerAgentIds,
                publisherAgentIds[i]);
                _publishers.Add(publisher);
            }
        }

        // Start all publishers concurrently
        var publishingTasks = _publishers.Select(p => 
            p.StartPublishingAsync(targetGrainCount, durationSeconds, cancellationToken)).ToArray();

        try
        {
            await Task.WhenAll(publishingTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during concurrent publishing");
            throw;
        }
    }

    public async Task StopAllPublishersAsync()
    {
        var stopTasks = _publishers.Select(p => p.StopPublishingAsync()).ToArray();
        await Task.WhenAll(stopTasks);
    }

    public List<PublisherMetrics> GetAllMetrics()
    {
        lock (_lock)
        {
            return _publishers.Select(p => p.GetMetrics()).ToList();
        }
    }

    public void ResetAllMetrics()
    {
        lock (_lock)
        {
            foreach (var publisher in _publishers)
            {
                publisher.ResetMetrics();
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var publisher in _publishers)
            {
                publisher.Dispose();
            }
            _publishers.Clear();
        }
    }
} 