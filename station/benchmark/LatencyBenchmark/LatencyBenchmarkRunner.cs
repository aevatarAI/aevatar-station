using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Streams;
using Orleans.Streams.Kafka.Config;
using E2E.Grains;
using Aevatar.Core.Streaming.Extensions;

namespace LatencyBenchmark;

/// <summary>
/// Runs agent-to-agent latency benchmark tests
/// </summary>
public class LatencyBenchmarkRunner : IDisposable
{
    private readonly BenchmarkConfig _config;
    private readonly ILogger<LatencyBenchmarkRunner> _logger;
    private IHost? _host;
    private IClusterClient? _clusterClient;
    private ConcurrentPublisherManager? _publisherManager;
    private readonly List<BenchmarkResult> _results = new();
    private readonly List<Guid> _handlerAgentIds = new();
    private readonly List<Guid> _publisherAgentIds = new();
    
    // Add ActivitySource for root tracing
    private static readonly ActivitySource ActivitySource = new("LatencyBenchmark.Root", "1.0.0");

    public LatencyBenchmarkRunner(BenchmarkConfig config, ILogger<LatencyBenchmarkRunner> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<List<BenchmarkResult>> RunBenchmarkAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Latency Benchmark for Agent-to-Agent Communication with config: {Config}", 
                JsonConvert.SerializeObject(_config, Formatting.Indented));

            // Setup Orleans
            await SetupOrleansAsync();

            // Initialize components
            await InitializeComponentsAsync();

            // Check for debug mode
            if (_config.Debug)
            {
                _logger.LogInformation("🔍 DEBUG MODE: Running single event trace...");
                await RunDebugModeAsync();
                return _results; // Return early for debug mode
            }

            // Calculate and log required concurrency for target daily events
            var requiredConcurrency = _config.CalculateRequiredConcurrency();
            var minEventsPerSecond = _config.CalculateMinEventsPerSecond();
            
            _logger.LogInformation("Target: {TargetDailyEvents:N0} events/day = {MinEventsPerSecond:F1} events/sec", 
                _config.TargetDailyEvents, minEventsPerSecond);
            _logger.LogInformation("Required concurrency: {RequiredConcurrency} publisher agents at {EventsPerSecond} events/sec each", 
                requiredConcurrency, _config.EventsPerSecond);

            // Run benchmark for each concurrency level
            var concurrencyLevels = _config.GetConcurrencyLevels().ToList();
            
            if (concurrencyLevels.Count == 0)
            {
                _logger.LogWarning("⚠️ No concurrency levels to test!");
                _logger.LogWarning("   Current range: {BaseConcurrency} - {MaxConcurrency}", _config.BaseConcurrency, _config.MaxConcurrency);
                if (_config.StartFromLevel.HasValue || _config.StopAtLevel.HasValue)
                {
                    _logger.LogWarning("   Range filters: start-from-level={StartFrom}, stop-at-level={StopAt}", 
                        _config.StartFromLevel, _config.StopAtLevel);
                    _logger.LogWarning("   💡 Try: --max-concurrency {SuggestedMax} to include your desired levels", 
                        Math.Max(_config.StartFromLevel ?? _config.MaxConcurrency, _config.StopAtLevel ?? _config.MaxConcurrency));
                }
                _logger.LogWarning("   Skipping benchmark execution...");
                return _results;
            }
            
            _logger.LogInformation("Testing concurrency levels: {ConcurrencyLevels}", 
                string.Join(", ", concurrencyLevels));

            foreach (var concurrencyLevel in concurrencyLevels)
            {
                // Check for cancellation before starting each test
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("🛑 Benchmark cancelled before concurrency level {ConcurrencyLevel}", concurrencyLevel);
                    break;
                }
                
                _logger.LogInformation("=== Running Agent-to-Agent benchmark with {ConcurrencyLevel} concurrent publisher agents ===", 
                    concurrencyLevel);

                var result = await RunSingleBenchmarkAsync(concurrencyLevel, cancellationToken);
                _results.Add(result);

                // Log intermediate results
                LogBenchmarkResult(result);

                // Brief pause between runs (cancellable)
                try
                {
                    await Task.Delay(2000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("🛑 Benchmark cancelled during pause after concurrency level {ConcurrencyLevel}", concurrencyLevel);
                    break;
                }
            }

            // Generate and save final report
            await GenerateReportAsync();

            return _results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Benchmark failed");
            throw;
        }
    }

    private async Task SetupOrleansAsync()
    {
        _logger.LogInformation("Setting up Orleans Client...");

        // Read Orleans configuration from environment variables
        var mongoClient = Environment.GetEnvironmentVariable("Orleans__MongoDBClient") ?? "mongodb://localhost:27017";
        var clusterId = Environment.GetEnvironmentVariable("Orleans__ClusterId") ?? "AevatarSiloCluster";
        var serviceId = Environment.GetEnvironmentVariable("Orleans__ServiceId") ?? "AevatarBasicService";
        var database = Environment.GetEnvironmentVariable("Orleans__DataBase") ?? "AevatarDb";
        var hostId = Environment.GetEnvironmentVariable("Orleans__HostId") ?? "Aevatar";
        
        _logger.LogInformation("Orleans Configuration:");
        _logger.LogInformation("  MongoDB Client: {MongoClient}", mongoClient);
        _logger.LogInformation("  Cluster ID: {ClusterId}", clusterId);
        _logger.LogInformation("  Service ID: {ServiceId}", serviceId);
        _logger.LogInformation("  Database: {Database}", database);
        _logger.LogInformation("  Host ID: {HostId}", hostId);
        
        // Log Kafka topics configuration
        var kafkaTopics = !string.IsNullOrEmpty(hostId) && !hostId.Equals("Aevatar", StringComparison.OrdinalIgnoreCase)
            ? $"{hostId}Silo,{hostId}SiloProjector,{hostId}SiloBroadcast"
            : "Aevatar,AevatarStateProjection,AevatarBroadCast";
        _logger.LogInformation("  Kafka Topics: {KafkaTopics}", kafkaTopics);

        var hostBuilder = Host.CreateDefaultBuilder()
            .UseOrleansClient(client =>
            {
                client.UseMongoDBClient(mongoClient)
                    .UseMongoDBClustering(options =>
                    {
                        options.DatabaseName = database;
                        options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                        options.CollectionPrefix = hostId.IsNullOrEmpty() ? "OrleansAevatar" : $"Orleans{hostId}";
                    })
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = clusterId;
                        options.ServiceId = serviceId;
                    })
                    .AddActivityPropagation()
                    .AddAevatarKafkaStreaming("Aevatar", options =>
                    {
                        options.BrokerList = new List<string> { "localhost:9092" };
                        options.ConsumerGroupId = "Aevatar";
                        options.ConsumeMode = ConsumeMode.LastCommittedMessage;

                        var partitions = 8; // Multiple partitions for load distribution
                        var replicationFactor = (short)1;  // ReplicationFactor should be short
                        var topics = !string.IsNullOrEmpty(hostId) && !hostId.Equals("Aevatar", StringComparison.OrdinalIgnoreCase)
                            ? $"{hostId}Silo,{hostId}SiloProjector,{hostId}SiloBroadcast"
                            : "Aevatar,AevatarStateProjection,AevatarBroadCast";
                        foreach (var topic in topics.Split(','))
                        {
                            options.AddTopic(topic.Trim(), new TopicCreationConfig
                            {
                                AutoCreate = true,
                                Partitions = partitions,
                                ReplicationFactor = replicationFactor
                            });
                        }
                    });
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(_config.Verbose ? LogLevel.Debug : LogLevel.Information);
            })
            .UseConsoleLifetime();

        _host = hostBuilder.Build();
        await _host.StartAsync();

        _clusterClient = _host.Services.GetRequiredService<IClusterClient>();
        _logger.LogInformation("Orleans Client setup complete");
    }

    private async Task InitializeComponentsAsync()
    {
        _logger.LogInformation("Initializing Agent-to-Agent communication components...");

        if (_clusterClient == null)
        {
            throw new InvalidOperationException("Orleans Client not initialized");
        }

        // Initialize publisher agent IDs for the maximum concurrency level
        await InitializePublisherAgentsAsync(_config.MaxConcurrency);

        // Initialize publisher manager
        _publisherManager = new ConcurrentPublisherManager(
            _clusterClient, 
            _host!.Services.GetRequiredService<ILoggerFactory>());

        _logger.LogInformation("Agent-to-Agent communication components initialized");
    }

    private async Task<BenchmarkResult> RunSingleBenchmarkAsync(int concurrencyLevel, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new BenchmarkResult
        {
            ConcurrencyLevel = concurrencyLevel,
            EventsPerSecond = _config.EventsPerSecond,
            DurationSeconds = _config.Duration,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Calculate target handler agent count based on configuration
            var targetHandlerCount = _config.CalculateHandlerCount(concurrencyLevel);
            
            _logger.LogInformation("Benchmark configuration: {PublisherCount} publishers → {HandlerCount} handlers (ratio: {Ratio:F1}:1)", 
                concurrencyLevel, targetHandlerCount, (double)concurrencyLevel / targetHandlerCount);

            // Initialize handler agents and set up stream listening
            await InitializeHandlerAgentsAsync(targetHandlerCount);

            // Warmup phase
            if (_config.WarmupDuration > 0)
            {
                var warmupPublisherCount = Math.Min(concurrencyLevel, 10); // Use fewer publishers for warmup
                _logger.LogInformation("Warming up Agent-to-Agent communication for {WarmupDuration} seconds...", _config.WarmupDuration);
                await _publisherManager!.RunConcurrentPublishersAsync(
                    warmupPublisherCount,
                    _config.EventsPerSecond,
                    targetHandlerCount,
                    _config.WarmupDuration,
                    _handlerAgentIds,
                    _publisherAgentIds.Take(warmupPublisherCount).ToList());
                
                // Reset metrics after warmup
                _publisherManager.ResetAllMetrics();
                await ResetAllAgentMetricsAsync(concurrencyLevel, targetHandlerCount);
            }

            // Actual benchmark run
            _logger.LogInformation("Starting Agent-to-Agent benchmark run with {PublisherCount} publisher agents and {HandlerCount} handler agents...", 
                concurrencyLevel, targetHandlerCount);
            
            await _publisherManager!.RunConcurrentPublishersAsync(
                concurrencyLevel,
                _config.EventsPerSecond,
                targetHandlerCount,
                _config.Duration,
                _handlerAgentIds,
                _publisherAgentIds.Take(concurrencyLevel).ToList());

            // Wait for event processing completion with cancellation support
            _logger.LogInformation("Publishers finished. Waiting for handlers to process all events...");
            using var completionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            completionCts.CancelAfter(TimeSpan.FromSeconds(_config.CompletionTimeout));
            await WaitForCompletionAsync(targetHandlerCount, completionCts.Token);

            // Collect final metrics
            var publisherMetrics = _publisherManager.GetAllMetrics();
            var handlerMetrics = await CollectHandlerMetricsAsync(targetHandlerCount);

            // Calculate totals
            result.TotalEventsSent = publisherMetrics.Sum(m => m.TotalEventsSent);
            result.TotalEventsProcessed = handlerMetrics.Sum(m => m.TotalEventsProcessed);
            result.ActualThroughput = result.TotalEventsSent / (double)_config.Duration;

            // Calculate latency statistics
            var allMeasurements = handlerMetrics.SelectMany(m => m.RawMeasurements).ToList();
            if (allMeasurements.Count > 0)
            {
                var combinedMetrics = LatencyMetrics.FromMeasurements(allMeasurements);
                result.MinLatencyMs = combinedMetrics.MinLatencyMs;
                result.MaxLatencyMs = combinedMetrics.MaxLatencyMs;
                result.AverageLatencyMs = combinedMetrics.AverageLatencyMs;
                result.MedianLatencyMs = combinedMetrics.MedianLatencyMs;
                result.P95LatencyMs = combinedMetrics.P95LatencyMs;
                result.P99LatencyMs = combinedMetrics.P99LatencyMs;
                result.StandardDeviationMs = combinedMetrics.StandardDeviationMs;
            }

            result.PublisherMetrics = publisherMetrics;
            result.LatencyMetrics = handlerMetrics;
            result.Success = true;

            // Log agent communication summary
            _logger.LogInformation("Agent-to-Agent Communication Summary:");
            _logger.LogInformation("  Publisher Agents: {PublisherCount}", concurrencyLevel);
            _logger.LogInformation("  Handler Agents: {HandlerCount}", targetHandlerCount);
            _logger.LogInformation("  Events sent by publishers: {EventsSent}", result.TotalEventsSent);
            _logger.LogInformation("  Events processed by handlers: {EventsProcessed}", result.TotalEventsProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent-to-Agent benchmark run failed for concurrency level {ConcurrencyLevel}", concurrencyLevel);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            result.EndTime = DateTime.UtcNow;
            result.ActualDurationSeconds = stopwatch.Elapsed.TotalSeconds;
        }

        return result;
    }

    private async Task InitializeHandlerAgentsAsync(int handlerCount)
    {
        _logger.LogInformation("Initializing {HandlerCount} handler agents for stream listening...", handlerCount);

        // Generate consistent handler agent GUIDs if not already done
        if (_handlerAgentIds.Count != handlerCount)
        {
            _handlerAgentIds.Clear();
            for (int i = 0; i < handlerCount; i++)
            {
                _handlerAgentIds.Add(Guid.NewGuid());
            }
        }

        var initializationTasks = new List<Task>();
        
        for (int i = 0; i < handlerCount; i++)
        {
            var handlerAgentId = _handlerAgentIds[i];
            initializationTasks.Add(Task.Run(async () =>
            {
                try
                {
                    var handlerAgent = _clusterClient!.GetGrain<ILatencyHandlerAgent>(handlerAgentId);
                    await handlerAgent.StartListeningAsync(handlerAgentId);
                    
                    _logger.LogDebug("Handler agent {HandlerAgentId} initialized and listening", handlerAgentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize handler agent {HandlerAgentId}", handlerAgentId);
                }
            }));
        }
        
        await Task.WhenAll(initializationTasks);
        _logger.LogInformation("All {HandlerCount} handler agents initialized", handlerCount);
    }

    private async Task InitializePublisherAgentsAsync(int maxPublisherCount)
    {
        _logger.LogInformation("Initializing {MaxPublisherCount} publisher agent IDs for benchmark run...", maxPublisherCount);

        // Generate consistent publisher agent GUIDs for the entire benchmark run
        if (_publisherAgentIds.Count != maxPublisherCount)
        {
            _publisherAgentIds.Clear();
            for (int i = 0; i < maxPublisherCount; i++)
            {
                _publisherAgentIds.Add(Guid.NewGuid());
            }
        }

        _logger.LogInformation("All {PublisherCount} publisher agent IDs initialized", maxPublisherCount);
        await Task.CompletedTask; // Make it async for consistency with other initialization methods
    }

    private async Task<List<LatencyMetrics>> CollectHandlerMetricsAsync(int handlerCount)
    {
        var metrics = new List<LatencyMetrics>();
        
        for (int i = 0; i < handlerCount; i++)
        {
            var handlerAgentId = _handlerAgentIds[i];
            try
            {
                var handlerAgent = _clusterClient!.GetGrain<ILatencyHandlerAgent>(handlerAgentId);
                var handlerMetrics = await handlerAgent.GetLatencyMetricsAsync();
                metrics.Add(handlerMetrics);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect metrics from handler agent {HandlerAgentId}", handlerAgentId);
            }
        }
        
        return metrics;
    }

    private async Task ResetAllAgentMetricsAsync(int publisherCount, int handlerCount)
    {
        var resetTasks = new List<Task>();
        
        // Reset publisher agent metrics using consistent agent IDs
        for (int i = 0; i < publisherCount && i < _publisherAgentIds.Count; i++)
        {
            var publisherAgentId = _publisherAgentIds[i];
            resetTasks.Add(Task.Run(async () =>
            {
                try
                {
                    var publisherAgent = _clusterClient!.GetGrain<ILatencyPublisherAgent>(publisherAgentId);
                    await publisherAgent.ResetMetricsAsync();
                }
                catch
                {
                    // Ignore errors for non-existent agents
                }
            }));
        }
        
        // Reset handler agent metrics
        for (int i = 0; i < handlerCount; i++)
        {
            var handlerAgentId = _handlerAgentIds[i];
            resetTasks.Add(Task.Run(async () =>
            {
                try
                {
                    var handlerAgent = _clusterClient!.GetGrain<ILatencyHandlerAgent>(handlerAgentId);
                    await handlerAgent.ResetMetricsAsync();
                }
                catch
                {
                    // Ignore errors for non-existent agents
                }
            }));
        }
        
        await Task.WhenAll(resetTasks);
    }

    private async Task WaitForCompletionAsync(int handlerCount, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var lastLogTime = DateTime.UtcNow;
        var logInterval = TimeSpan.FromSeconds(5); // Log progress every 5 seconds
        
        try
        {
            // Get initial metrics from publishers to know total events sent
            var publisherMetrics = _publisherManager!.GetAllMetrics();
            var totalEventsSent = publisherMetrics.Sum(m => m.TotalEventsSent);
            
            _logger.LogInformation("Waiting for completion: {TotalEventsSent} events sent by publishers", totalEventsSent);
            
            if (totalEventsSent == 0)
            {
                _logger.LogInformation("No events were sent, completion immediate");
                return;
            }

            long lastProcessedCount = 0;
            var stableCount = 0;
            const int maxStableChecks = 3; // Require 3 consecutive stable readings
            
            while (!cancellationToken.IsCancellationRequested)
            {
                // Check current handler metrics
                var handlerMetrics = await CollectHandlerMetricsAsync(handlerCount);
                var totalEventsProcessed = handlerMetrics.Sum(m => m.TotalEventsProcessed);
                
                // Check if processing is complete
                if (totalEventsProcessed >= totalEventsSent)
                {
                    _logger.LogInformation("✅ Processing complete! {ProcessedEvents}/{SentEvents} events processed in {ElapsedSeconds:F1}s", 
                        totalEventsProcessed, totalEventsSent, stopwatch.Elapsed.TotalSeconds);
                    return;
                }
                
                // Check if processing has stalled
                if (totalEventsProcessed == lastProcessedCount)
                {
                    stableCount++;
                    if (stableCount >= maxStableChecks)
                    {
                        _logger.LogWarning("⚠️ Processing appears stalled at {ProcessedEvents}/{SentEvents} events after {ElapsedSeconds:F1}s", 
                            totalEventsProcessed, totalEventsSent, stopwatch.Elapsed.TotalSeconds);
                        return;
                    }
                }
                else
                {
                    stableCount = 0; // Reset stable count if progress made
                }
                lastProcessedCount = totalEventsProcessed;
                
                // Log progress periodically
                var now = DateTime.UtcNow;
                if (now - lastLogTime >= logInterval)
                {
                    var progressPercent = (double)totalEventsProcessed / totalEventsSent * 100;
                    var eventsPerSecond = totalEventsProcessed / stopwatch.Elapsed.TotalSeconds;
                    
                    _logger.LogInformation("🔄 Processing progress: {ProcessedEvents}/{SentEvents} ({ProgressPercent:F1}%) at {EventsPerSecond:F1} events/sec - {ElapsedSeconds:F1}s elapsed", 
                        totalEventsProcessed, totalEventsSent, progressPercent, eventsPerSecond, stopwatch.Elapsed.TotalSeconds);
                    lastLogTime = now;
                }
                
                // Wait before next check
                await Task.Delay(TimeSpan.FromSeconds(_config.CompletionCheckInterval), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (stopwatch.Elapsed.TotalSeconds >= _config.CompletionTimeout)
            {
                _logger.LogWarning("⏰ Completion timeout reached after {TimeoutSeconds}s - continuing with current metrics", _config.CompletionTimeout);
            }
            else
            {
                _logger.LogInformation("🛑 Completion wait cancelled by user");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during completion wait - continuing with current metrics");
        }
    }

    private async Task RunDebugModeAsync()
    {
        _logger.LogInformation("🔍 DEBUG: Starting single event test...");

        // Use pre-initialized agent IDs for consistency
        var publisherAgentId = _publisherAgentIds.Count > 0 ? _publisherAgentIds[0] : Guid.NewGuid();
        var handlerAgentId = _handlerAgentIds.Count > 0 ? _handlerAgentIds[0] : Guid.NewGuid();

        // Create root activity for distributed tracing
        using var rootActivity = ActivitySource.StartActivity("LatencyBenchmark.DebugEvent");
        rootActivity?.SetTag("operation", "debug-single-event");
        rootActivity?.SetTag("publisher.agent.id", publisherAgentId.ToString());
        rootActivity?.SetTag("handler.agent.id", handlerAgentId.ToString());
        rootActivity?.SetTag("correlation.id", "DEBUG-TRACE-001");
        rootActivity?.SetTag("benchmark.mode", "debug");

        try
        {
            // Step 1: Get agent references
            _logger.LogInformation("🔍 DEBUG: Getting agent references...");
            var publisherAgent = _clusterClient!.GetGrain<ILatencyPublisherAgent>(publisherAgentId);
            var handlerAgent = _clusterClient.GetGrain<ILatencyHandlerAgent>(handlerAgentId);

            // Step 2: Reset any existing metrics
            _logger.LogInformation("🔍 DEBUG: Resetting agent metrics...");
            await publisherAgent.ResetMetricsAsync();
            await handlerAgent.ResetMetricsAsync();

            // Step 3: Start handler listening
            _logger.LogInformation("🔍 DEBUG: Starting handler agent {HandlerAgentId} listening...", handlerAgentId);
            await handlerAgent.StartListeningAsync(handlerAgentId);

            // Step 4: Get initial metrics
            var initialHandlerMetrics = await handlerAgent.GetLatencyMetricsAsync();
            var initialPublisherEvents = await publisherAgent.GetEventsSentAsync();
            
            _logger.LogInformation("🔍 DEBUG: Initial state:");
            _logger.LogInformation("  - Publisher events sent: {InitialPublisherEvents}", initialPublisherEvents);
            _logger.LogInformation("  - Handler events processed: {InitialHandlerEvents}", initialHandlerMetrics.TotalEventsProcessed);

            // Step 5: Create and send exactly 1 event with tracing context
            _logger.LogInformation("🔍 DEBUG: Creating and sending single event...");
            var testEvent = new LatencyTestEvent(1, publisherAgentId, "DEBUG-TRACE-001");
            
            // Add trace context to event
            rootActivity?.SetTag("event.number", testEvent.EventNumber);
            rootActivity?.SetTag("event.sent.timestamp", testEvent.SentTimestamp);
            
            _logger.LogInformation("🔍 DEBUG: Event details:");
            _logger.LogInformation("  - CorrelationId: {CorrelationId}", testEvent.CorrelationId);
            _logger.LogInformation("  - EventNumber: {EventNumber}", testEvent.EventNumber);
            _logger.LogInformation("  - PublisherAgentId: {PublisherAgentId}", testEvent.PublisherAgentId);
            _logger.LogInformation("  - SentTimestamp: {SentTimestamp}", testEvent.SentTimestamp);
            _logger.LogInformation("  - TraceId: {TraceId}", rootActivity?.TraceId);
            _logger.LogInformation("  - SpanId: {SpanId}", rootActivity?.SpanId);

            // Send the event (this will propagate the trace context)
            await publisherAgent.PublishEventAsync(testEvent, handlerAgentId);
            _logger.LogInformation("✅ DEBUG: Event sent from {PublisherAgentId} to {HandlerAgentId}", publisherAgentId, handlerAgentId);

            // Step 6: Wait for processing
            _logger.LogInformation("🔍 DEBUG: Waiting 10 seconds for event processing...");
            await Task.Delay(10000);

            // Step 7: Check final metrics
            _logger.LogInformation("🔍 DEBUG: Collecting final metrics...");
            var finalHandlerMetrics = await handlerAgent.GetLatencyMetricsAsync();
            var finalPublisherEvents = await publisherAgent.GetEventsSentAsync();

            _logger.LogInformation("🔍 DEBUG: Final results:");
            _logger.LogInformation("  - Publisher events sent: {FinalPublisherEvents} (change: +{PublisherChange})", 
                finalPublisherEvents, finalPublisherEvents - initialPublisherEvents);
            _logger.LogInformation("  - Handler events processed: {FinalHandlerEvents} (change: +{HandlerChange})", 
                finalHandlerMetrics.TotalEventsProcessed, finalHandlerMetrics.TotalEventsProcessed - initialHandlerMetrics.TotalEventsProcessed);
            
            rootActivity?.SetTag("events.sent", finalPublisherEvents - initialPublisherEvents);
            rootActivity?.SetTag("events.processed", finalHandlerMetrics.TotalEventsProcessed - initialHandlerMetrics.TotalEventsProcessed);
            
            if (finalHandlerMetrics.RawMeasurements.Count > 0)
            {
                _logger.LogInformation("  - Latency measurements: {MeasurementCount}", finalHandlerMetrics.RawMeasurements.Count);
                foreach (var measurement in finalHandlerMetrics.RawMeasurements)
                {
                    _logger.LogInformation("    * Event {EventNumber}: {LatencyMs:F2}ms (ID: {CorrelationId})", 
                        measurement.EventNumber, measurement.LatencyMs, measurement.CorrelationId);
                    
                    // Add latency to root activity
                    rootActivity?.SetTag("measured.latency.ms", measurement.LatencyMs);
                }
            }
            else
            {
                _logger.LogWarning("  - ❌ No latency measurements recorded!");
            }

            // Step 8: Analyze results
            var success = finalPublisherEvents > initialPublisherEvents && finalHandlerMetrics.TotalEventsProcessed > initialHandlerMetrics.TotalEventsProcessed;
            rootActivity?.SetTag("benchmark.success", success);
            
            if (success)
            {
                _logger.LogInformation("✅ DEBUG: SUCCESS - Event was sent and received!");
                _logger.LogInformation("🔍 DEBUG: Use this TraceId in Jaeger: {TraceId}", rootActivity?.TraceId);
            }
            else if (finalPublisherEvents > initialPublisherEvents)
            {
                _logger.LogWarning("⚠️ DEBUG: PARTIAL - Event was sent but NOT received by handler!");
                _logger.LogWarning("   Possible issues:");
                _logger.LogWarning("   - Stream routing problem");
                _logger.LogWarning("   - Handler not properly subscribed");
                _logger.LogWarning("   - Event filtering issue");
            }
            else
            {
                _logger.LogError("❌ DEBUG: FAILURE - Event was NOT sent!");
                _logger.LogError("   Possible issues:");
                _logger.LogError("   - Publisher agent not working");
                _logger.LogError("   - Stream publishing failure");
            }

            // Create a debug result
            var debugResult = new BenchmarkResult
            {
                ConcurrencyLevel = 1,
                EventsPerSecond = 0, // Debug mode doesn't use rate limiting
                DurationSeconds = 10,
                StartTime = DateTime.UtcNow.AddSeconds(-10),
                EndTime = DateTime.UtcNow,
                ActualDurationSeconds = 10,
                Success = success,
                TotalEventsSent = finalPublisherEvents - initialPublisherEvents,
                TotalEventsProcessed = finalHandlerMetrics.TotalEventsProcessed - initialHandlerMetrics.TotalEventsProcessed,
                ErrorMessage = finalPublisherEvents <= initialPublisherEvents ? "Event was not sent" : 
                              finalHandlerMetrics.TotalEventsProcessed <= initialHandlerMetrics.TotalEventsProcessed ? "Event was not received" : ""
            };

            if (finalHandlerMetrics.RawMeasurements.Count > 0)
            {
                var combinedMetrics = LatencyMetrics.FromMeasurements(finalHandlerMetrics.RawMeasurements);
                debugResult.MinLatencyMs = combinedMetrics.MinLatencyMs;
                debugResult.MaxLatencyMs = combinedMetrics.MaxLatencyMs;
                debugResult.AverageLatencyMs = combinedMetrics.AverageLatencyMs;
                debugResult.MedianLatencyMs = combinedMetrics.MedianLatencyMs;
                debugResult.P95LatencyMs = combinedMetrics.P95LatencyMs;
                debugResult.P99LatencyMs = combinedMetrics.P99LatencyMs;
                debugResult.StandardDeviationMs = combinedMetrics.StandardDeviationMs;
            }

            _results.Add(debugResult);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ DEBUG: Test execution failed");
            
            rootActivity?.SetTag("benchmark.success", false);
            rootActivity?.SetTag("error.message", ex.Message);
            rootActivity?.SetTag("error.type", ex.GetType().Name);
            
            var errorResult = new BenchmarkResult
            {
                ConcurrencyLevel = 1,
                EventsPerSecond = 0,
                DurationSeconds = 0,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                ActualDurationSeconds = 0,
                Success = false,
                ErrorMessage = ex.Message
            };
            _results.Add(errorResult);
        }
    }

    private void LogBenchmarkResult(BenchmarkResult result)
    {
        _logger.LogInformation("Agent-to-Agent Benchmark Result for {ConcurrencyLevel} concurrent publisher agents:", result.ConcurrencyLevel);
        _logger.LogInformation("  Events Sent: {EventsSent:N0}", result.TotalEventsSent);
        _logger.LogInformation("  Events Processed: {EventsProcessed:N0}", result.TotalEventsProcessed);
        _logger.LogInformation("  Throughput: {Throughput:F1} events/sec", result.ActualThroughput);
        _logger.LogInformation("  Latency - Min: {Min:F2}ms, Avg: {Avg:F2}ms, Max: {Max:F2}ms", 
            result.MinLatencyMs, result.AverageLatencyMs, result.MaxLatencyMs);
        _logger.LogInformation("  Latency - P95: {P95:F2}ms, P99: {P99:F2}ms", 
            result.P95LatencyMs, result.P99LatencyMs);
    }

    private async Task GenerateReportAsync()
    {
        _logger.LogInformation("Generating final Agent-to-Agent communication report...");

        var successfulResults = _results.Where(r => r.Success).ToList();
        
        var report = new BenchmarkReport
        {
            Configuration = _config,
            Results = _results,
            Summary = new BenchmarkSummary
            {
                TotalResults = _results.Count,
                SuccessfulResults = _results.Count(r => r.Success),
                FailedResults = _results.Count(r => !r.Success),
                MaxThroughput = successfulResults.Count > 0 ? successfulResults.Max(r => r.ActualThroughput) : 0,
                MinLatency = successfulResults.Count > 0 ? successfulResults.Min(r => r.MinLatencyMs) : 0,
                MaxLatency = successfulResults.Count > 0 ? successfulResults.Max(r => r.MaxLatencyMs) : 0,
                BestP95Latency = successfulResults.Count > 0 ? successfulResults.Min(r => r.P95LatencyMs) : 0,
                BestP99Latency = successfulResults.Count > 0 ? successfulResults.Min(r => r.P99LatencyMs) : 0
            }
        };

        var json = JsonConvert.SerializeObject(report, Formatting.Indented);
        await File.WriteAllTextAsync(_config.OutputFile, json);

        _logger.LogInformation("Agent-to-Agent Communication Report saved to {OutputFile}", _config.OutputFile);
        _logger.LogInformation("=== Agent-to-Agent Benchmark Summary ===");
        _logger.LogInformation("Total Results: {TotalResults}", report.Summary.TotalResults);
        _logger.LogInformation("Successful: {SuccessfulResults}", report.Summary.SuccessfulResults);
        _logger.LogInformation("Failed: {FailedResults}", report.Summary.FailedResults);
        
        if (successfulResults.Count > 0)
        {
            _logger.LogInformation("Max Throughput: {MaxThroughput:F1} events/sec", report.Summary.MaxThroughput);
            _logger.LogInformation("Best Latency - Min: {MinLatency:F2}ms", report.Summary.MinLatency);
            _logger.LogInformation("Best Latency - P95: {BestP95:F2}ms, P99: {BestP99:F2}ms", 
                report.Summary.BestP95Latency, report.Summary.BestP99Latency);
        }
        else
        {
            _logger.LogWarning("⚠️ No successful test results to summarize");
            if (_results.Count == 0)
            {
                _logger.LogWarning("   No tests were executed - check your concurrency level range");
                _logger.LogWarning("   Current range: {BaseConcurrency} - {MaxConcurrency}", _config.BaseConcurrency, _config.MaxConcurrency);
                if (_config.StartFromLevel.HasValue || _config.StopAtLevel.HasValue)
                {
                    _logger.LogWarning("   Range filters: start-from-level={StartFrom}, stop-at-level={StopAt}", 
                        _config.StartFromLevel, _config.StopAtLevel);
                    _logger.LogWarning("   💡 Try: --max-concurrency {SuggestedMax} to include level {RequestedLevel}", 
                        Math.Max(_config.StartFromLevel ?? _config.MaxConcurrency, _config.StopAtLevel ?? _config.MaxConcurrency),
                        _config.StartFromLevel ?? _config.StopAtLevel);
                }
            }
        }
    }

    public void Dispose()
    {
        _publisherManager?.Dispose();
        _host?.Dispose();
    }
}

public class BenchmarkResult
{
    public int ConcurrencyLevel { get; set; }
    public int EventsPerSecond { get; set; }
    public int DurationSeconds { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double ActualDurationSeconds { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = "";
    
    public long TotalEventsSent { get; set; }
    public long TotalEventsProcessed { get; set; }
    public double ActualThroughput { get; set; }
    
    public double MinLatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
    public double AverageLatencyMs { get; set; }
    public double MedianLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public double P99LatencyMs { get; set; }
    public double StandardDeviationMs { get; set; }
    
    public List<PublisherMetrics> PublisherMetrics { get; set; } = new();
    public List<LatencyMetrics> LatencyMetrics { get; set; } = new();
}

public class BenchmarkReport
{
    public BenchmarkConfig Configuration { get; set; } = new();
    public List<BenchmarkResult> Results { get; set; } = new();
    public BenchmarkSummary Summary { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class BenchmarkSummary
{
    public int TotalResults { get; set; }
    public int SuccessfulResults { get; set; }
    public int FailedResults { get; set; }
    public double MaxThroughput { get; set; }
    public double MinLatency { get; set; }
    public double MaxLatency { get; set; }
    public double BestP95Latency { get; set; }
    public double BestP99Latency { get; set; }
} 