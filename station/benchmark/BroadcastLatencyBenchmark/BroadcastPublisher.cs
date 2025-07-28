using Microsoft.Extensions.Logging;
using Orleans;
using System.Collections.Concurrent;
using System.Diagnostics;
using E2E.Grains;

namespace BroadcastLatencyBenchmark;

public class BroadcastPublisher : IDisposable
{
    private readonly ILogger<BroadcastPublisher> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly ConcurrentDictionary<string, DateTime> _sentEvents = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly SemaphoreSlim _rateLimiter;
    private readonly int _eventsPerSecond;
    private readonly int _publisherId;
    private readonly string _publisherName;
    private readonly int _eventNumber;
    private long _totalEventsSent = 0;
    private volatile bool _isRunning = false;
    private Task? _publishingTask;

    // Broadcast-specific fields
    private IBroadcastScheduleAgent? _publisherAgent;
    private readonly Guid _publisherAgentId;

    public BroadcastPublisher(
        int publisherId,
        int eventsPerSecond,
        int eventNumber,
        IClusterClient clusterClient,
        ILogger<BroadcastPublisher> logger)
    {
        _publisherId = publisherId;
        _eventsPerSecond = eventsPerSecond;
        _eventNumber = eventNumber;
        _clusterClient = clusterClient;
        _logger = logger;
        _publisherName = $"BroadcastScheduler-{publisherId}";
        _publisherAgentId = Guid.NewGuid();
        
        // Create rate limiter for events per second
        _rateLimiter = new SemaphoreSlim(eventsPerSecond, eventsPerSecond);
        
        // Start rate limiter replenishment task
        _ = Task.Run(ReplenishRateLimiterAsync);
    }

    public async Task StartPublishingAsync(
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

        // Initialize publisher agent for broadcast communication
        _publisherAgent = _clusterClient.GetGrain<IBroadcastScheduleAgent>(_publisherAgentId);

        _logger.LogInformation("Starting broadcast publisher {PublisherName} (Agent: {AgentId}) with {EventsPerSecond} events/sec for {Duration}s",
            _publisherName, _publisherAgentId, _eventsPerSecond, durationSeconds);

        _publishingTask = Task.Run(async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            var endTime = TimeSpan.FromSeconds(durationSeconds);
            var eventSequence = 0;

            try
            {
                while (stopwatch.Elapsed < endTime && !combinedCancellationToken.IsCancellationRequested)
                {
                    await _rateLimiter.WaitAsync(combinedCancellationToken);

                    if (combinedCancellationToken.IsCancellationRequested)
                        break;

                    var correlationId = Guid.NewGuid().ToString();
                    
                    // Create broadcast event
                    var broadcastEvent = new BroadcastTestEvent(eventSequence++, _publisherAgentId, _eventNumber, correlationId);
                    
                    _sentEvents.TryAdd(correlationId, DateTime.UtcNow);

                    try
                    {
                        // Use broadcast communication
                        await _publisherAgent!.BroadcastEventAsync(broadcastEvent);
                        
                        _totalEventsSent++;

                        _logger.LogInformation("Broadcast publisher {PublisherId} sent event {EventNumber} (ID: {CorrelationId}) with number {Number}", 
                            _publisherId, eventSequence - 1, correlationId, _eventNumber);

                        if (_logger.IsEnabled(LogLevel.Debug))
                        {
                            _logger.LogDebug("Broadcast publisher agent {PublisherId} sent event {CorrelationId} with number {Number}", 
                                _publisherAgentId, correlationId, _eventNumber);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send broadcast event {CorrelationId} from publisher agent {PublisherId}", 
                            correlationId, _publisherAgentId);
                        
                        // Remove from sent events if publishing failed
                        _sentEvents.TryRemove(correlationId, out _);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Broadcast publisher {PublisherName} was cancelled", _publisherName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in broadcast publisher {PublisherName}", _publisherName);
            }
            finally
            {
                _isRunning = false;
                _logger.LogInformation("Broadcast publisher {PublisherName} (Agent: {AgentId}) stopped. Sent {TotalEvents} events in {ElapsedTime}s",
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

        _logger.LogInformation("Stopping broadcast publisher {PublisherName}", _publisherName);
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

    public BroadcastPublisherMetrics GetMetrics()
    {
        return new BroadcastPublisherMetrics
        {
            PublisherId = _publisherId,
            PublisherName = _publisherName,
            TotalEventsSent = _totalEventsSent,
            EventsPerSecond = _eventsPerSecond,
            IsRunning = _isRunning,
            PendingEvents = _sentEvents.Count,
            PublisherAgentId = _publisherAgentId.ToString(),
            EventNumber = _eventNumber
        };
    }

    public void ResetMetrics()
    {
        _sentEvents.Clear();
        _totalEventsSent = 0;
    }

    private async Task ReplenishRateLimiterAsync()
    {
        try
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(1000, _cancellationTokenSource.Token);
                
                // Release all available permits up to the maximum
                var currentCount = _rateLimiter.CurrentCount;
                var maxPermits = _eventsPerSecond;
                var permitsToRelease = maxPermits - currentCount;
                
                if (permitsToRelease > 0)
                {
                    _rateLimiter.Release(permitsToRelease);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _rateLimiter.Dispose();
    }
}

public class BroadcastPublisherMetrics
{
    public int PublisherId { get; set; }
    public string PublisherName { get; set; } = "";
    public long TotalEventsSent { get; set; }
    public int EventsPerSecond { get; set; }
    public bool IsRunning { get; set; }
    public int PendingEvents { get; set; }
    public DateTime MeasurementTime { get; set; } = DateTime.UtcNow;
    public string PublisherAgentId { get; set; } = "";
    public int EventNumber { get; set; }
}

public class BroadcastPublisherManager : IDisposable
{
    private readonly ILogger<BroadcastPublisherManager> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly List<BroadcastPublisher> _publishers = new();
    private readonly object _lock = new();

    public BroadcastPublisherManager(
        IClusterClient clusterClient,
        ILoggerFactory loggerFactory)
    {
        _clusterClient = clusterClient;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<BroadcastPublisherManager>();
    }

    public async Task RunConcurrentPublishersAsync(
        int publisherCount,
        int eventsPerSecond,
        int eventNumber,
        int durationSeconds,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting {PublisherCount} concurrent broadcast publishers with {EventsPerSecond} events/sec each for {Duration}s",
            publisherCount, eventsPerSecond, durationSeconds);

        lock (_lock)
        {
            // Clear existing publishers
            _publishers.Clear();

            // Create new publishers
            for (int i = 0; i < publisherCount; i++)
            {
                var publisher = new BroadcastPublisher(
                    i, 
                    eventsPerSecond,
                    eventNumber,
                    _clusterClient, 
                    _loggerFactory.CreateLogger<BroadcastPublisher>());
                _publishers.Add(publisher);
            }
        }

        // Start all publishers concurrently
        var publisherTasks = _publishers.Select(p => 
            p.StartPublishingAsync(durationSeconds, cancellationToken)).ToArray();

        try
        {
            await Task.WhenAll(publisherTasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Broadcast publisher tasks were cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in broadcast publisher tasks");
        }

        _logger.LogInformation("All {PublisherCount} broadcast publishers completed", publisherCount);
    }

    public async Task StopAllPublishersAsync()
    {
        var stopTasks = _publishers.Select(p => p.StopPublishingAsync()).ToArray();
        await Task.WhenAll(stopTasks);
    }

    public List<BroadcastPublisherMetrics> GetAllMetrics()
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
        foreach (var publisher in _publishers)
        {
            publisher.Dispose();
        }
        _publishers.Clear();
    }
} 