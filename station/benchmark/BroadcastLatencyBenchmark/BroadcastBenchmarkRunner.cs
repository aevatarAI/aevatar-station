using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Streams.Kafka.Config;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.Json;
using Aevatar.Core.Streaming.Extensions;
using E2E.Grains;

namespace BroadcastLatencyBenchmark;

public class BroadcastBenchmarkRunner : IDisposable
{
    private readonly BroadcastBenchmarkConfig _config;
    private readonly ILogger<BroadcastBenchmarkRunner> _logger;
    private IHost? _host;
    private IClusterClient? _clusterClient;
    private BroadcastPublisherManager? _publisherManager;
    private readonly List<BroadcastBenchmarkResult> _results = new();
    private readonly List<Guid> _userAgentIds = new();
    private readonly string _agentIdsFilePath;

    public BroadcastBenchmarkRunner(BroadcastBenchmarkConfig config, ILogger<BroadcastBenchmarkRunner> logger)
    {
        _config = config;
        _logger = logger;
        _agentIdsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "broadcast_agent_ids.json");
    }

    public async Task<List<BroadcastBenchmarkResult>> RunBenchmarkAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Broadcast Latency Benchmark with config: {Config}", 
                JsonConvert.SerializeObject(_config, Formatting.Indented));

            // Setup Orleans
            await SetupOrleansAsync();

            // Initialize components
            await InitializeComponentsAsync();

            // Check for debug mode
            if (_config.Debug)
            {
                _logger.LogInformation("🔍 DEBUG MODE: Running single broadcast trace...");
                await RunDebugModeAsync();
                return _results;
            }

            // Run benchmark
            _logger.LogInformation("Starting broadcast benchmark with {PublisherCount} publishers and {SubscriberCount} subscribers",
                _config.PublisherCount, _config.SubscriberCount);

            var result = await RunSingleBenchmarkAsync(cancellationToken);
            _results.Add(result);

            // Log results
            LogBenchmarkResult(result);

            // Generate and save final report
            await GenerateReportAsync();

            return _results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Broadcast benchmark failed");
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
                        // Read Kafka broker from environment variable, fallback to localhost for local development
                        var kafkaBrokers = Environment.GetEnvironmentVariable("KAFKA_BROKERS") ?? "localhost:9092";
                        options.BrokerList = kafkaBrokers.Split(',').Select(b => b.Trim()).ToList();
                        _logger.LogInformation("  Kafka Brokers: {KafkaBrokers}", string.Join(", ", options.BrokerList));
                        options.ConsumerGroupId = "Aevatar";
                        options.ConsumeMode = ConsumeMode.LastCommittedMessage;

                        var partitions = 8;
                        var replicationFactor = (short)1;
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
        _logger.LogInformation("Initializing broadcast communication components...");

        if (_clusterClient == null)
        {
            throw new InvalidOperationException("Orleans Client not initialized");
        }

        // Initialize publisher manager
        _publisherManager = new BroadcastPublisherManager(
            _clusterClient, 
            _host!.Services.GetRequiredService<ILoggerFactory>());

        // Initialize subscriber agents
        await InitializeSubscriberAgentsAsync();

        _logger.LogInformation("Broadcast communication components initialized");
    }

    private async Task InitializeSubscriberAgentsAsync()
    {
        var sw = Stopwatch.StartNew();
        var agentIds = new BroadcastAgentIds();
        var initialCounts = new Dictionary<int, int>();

        // Load existing IDs or create new ones
        if (_config.UseStoredIds && File.Exists(_agentIdsFilePath))
        {
            agentIds = await LoadAgentIdsAsync();
            _logger.LogInformation($"Found {agentIds.UserAgentIds.Count} stored user agent IDs");
            
            // Create agents with stored IDs
            for (var i = 0; i < Math.Min(_config.SubscriberCount, agentIds.UserAgentIds.Count); ++i)
            {
                var userAgentId = Guid.Parse(agentIds.UserAgentIds[i]);
                _userAgentIds.Add(userAgentId);
                
                _logger.LogInformation($"Using stored userAgent-{i}: {userAgentId:N}");
                var userAgent = _clusterClient!.GetGrain<IBroadcastUserAgent>(userAgentId);
                
                var activationSw = Stopwatch.StartNew();
                await userAgent.ActivateAsync();
                initialCounts[i] = await userAgent.GetCount();
                activationSw.Stop();
                
                _logger.LogInformation($"Time taken to activate agent-{i}: {activationSw.ElapsedMilliseconds} ms, initial count: {initialCounts[i]}");
            }
            
            // Create additional agents if needed
            if (_config.SubscriberCount > agentIds.UserAgentIds.Count)
            {
                _logger.LogInformation($"Need to create {_config.SubscriberCount - agentIds.UserAgentIds.Count} additional user agents");
                for (var i = agentIds.UserAgentIds.Count; i < _config.SubscriberCount; ++i)
                {
                    var userAgentId = Guid.NewGuid();
                    _userAgentIds.Add(userAgentId);
                    agentIds.UserAgentIds.Add(userAgentId.ToString());
                    
                    _logger.LogInformation($"Created new userAgent-{i}: {userAgentId:N}");
                    var userAgent = _clusterClient!.GetGrain<IBroadcastUserAgent>(userAgentId);
                    
                    var activationSw = Stopwatch.StartNew();
                    await userAgent.ActivateAsync();
                    initialCounts[i] = await userAgent.GetCount();
                    activationSw.Stop();
                    
                    _logger.LogInformation($"Time taken to activate agent-{i}: {activationSw.ElapsedMilliseconds} ms, initial count: {initialCounts[i]}");
                }
            }
        }
        else
        {
            // Create new agents with new IDs
            _logger.LogInformation($"Creating {_config.SubscriberCount} new user agents");
            for (var i = 0; i < _config.SubscriberCount; ++i)
            {
                var userAgentId = Guid.NewGuid();
                _userAgentIds.Add(userAgentId);
                agentIds.UserAgentIds.Add(userAgentId.ToString());
                
                _logger.LogInformation($"Created new userAgent-{i}: {userAgentId:N}");
                var userAgent = _clusterClient!.GetGrain<IBroadcastUserAgent>(userAgentId);
                
                var activationSw = Stopwatch.StartNew();
                await userAgent.ActivateAsync();
                initialCounts[i] = await userAgent.GetCount();
                activationSw.Stop();
                
                _logger.LogInformation($"Time taken to activate agent-{i}: {activationSw.ElapsedMilliseconds} ms, initial count: {initialCounts[i]}");
            }
        }

        sw.Stop();
        _logger.LogInformation("Time taken to create {SubscriberCount} user agents: {ElapsedMs} ms", 
            _config.SubscriberCount, sw.ElapsedMilliseconds);

        // Save all IDs to the JSON file
        await SaveAgentIdsAsync(agentIds);
    }

    private async Task<BroadcastBenchmarkResult> RunSingleBenchmarkAsync(CancellationToken cancellationToken = default)
    {
        var result = new BroadcastBenchmarkResult
        {
            PublisherCount = _config.PublisherCount,
            SubscriberCount = _config.SubscriberCount,
            EventsPerSecond = _config.EventsPerSecond,
            DurationSeconds = _config.Duration,
            StartTime = DateTime.UtcNow,
            EventNumber = _config.EventNumber
        };

        var stopwatch = new Stopwatch();

        try
        {
            // Reset all metrics
            await ResetAllAgentMetricsAsync();

            // Warmup phase
            if (_config.WarmupDuration > 0)
            {
                _logger.LogInformation("🔥 Warming up for {WarmupDuration}s...", _config.WarmupDuration);
                await Task.Delay(_config.WarmupDuration * 1000, cancellationToken);
            }

            // Start measuring actual workload performance (exclude warmup)
            stopwatch.Start();
            
            // Start publishers
            _logger.LogInformation("🚀 Starting {PublisherCount} publishers for {Duration}s...", 
                _config.PublisherCount, _config.Duration);

            await _publisherManager!.RunConcurrentPublishersAsync(
                _config.PublisherCount,
                _config.EventsPerSecond,
                _config.EventNumber,
                _config.Duration,
                cancellationToken);

            // Wait for completion
            await WaitForCompletionAsync(cancellationToken);

            // Collect metrics
            result.PublisherMetrics = _publisherManager.GetAllMetrics();
            result.SubscriberMetrics = await CollectSubscriberMetricsAsync();

            // Calculate totals
            result.TotalEventsSent = result.PublisherMetrics.Sum(p => p.TotalEventsSent);
            result.TotalEventsProcessed = result.SubscriberMetrics.Sum(s => s.TotalEventsProcessed);
            result.Success = true;

            stopwatch.Stop();
            result.EndTime = DateTime.UtcNow;
            result.ActualDurationSeconds = stopwatch.Elapsed.TotalSeconds;

            // Calculate latency metrics
            CalculateLatencyMetrics(result);

            _logger.LogInformation("✅ Broadcast benchmark completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Broadcast benchmark failed");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;
            result.ActualDurationSeconds = stopwatch.Elapsed.TotalSeconds;
        }

        return result;
    }

    private void CalculateLatencyMetrics(BroadcastBenchmarkResult result)
    {
        var allLatencies = new List<double>();
        
        foreach (var subscriberMetrics in result.SubscriberMetrics)
        {
            allLatencies.AddRange(subscriberMetrics.RawMeasurements.Select(m => m.LatencyMs));
        }

        if (allLatencies.Count > 0)
        {
            allLatencies.Sort();
            var average = allLatencies.Average();
            var variance = allLatencies.Select(l => Math.Pow(l - average, 2)).Average();
            var standardDeviation = Math.Sqrt(variance);

            result.MinLatencyMs = allLatencies.First();
            result.MaxLatencyMs = allLatencies.Last();
            result.AverageLatencyMs = average;
            result.MedianLatencyMs = GetPercentile(allLatencies, 0.5);
            result.P95LatencyMs = GetPercentile(allLatencies, 0.95);
            result.P99LatencyMs = GetPercentile(allLatencies, 0.99);
            result.StandardDeviationMs = standardDeviation;
        }
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

    private async Task<List<BroadcastLatencyMetrics>> CollectSubscriberMetricsAsync()
    {
        var metrics = new List<BroadcastLatencyMetrics>();
        
        for (int i = 0; i < _userAgentIds.Count; i++)
        {
            var userAgent = _clusterClient!.GetGrain<IBroadcastUserAgent>(_userAgentIds[i]);
            var subscriberMetrics = await userAgent.GetLatencyMetricsAsync();
            metrics.Add(subscriberMetrics);
        }

        return metrics;
    }

    private async Task ResetAllAgentMetricsAsync()
    {
        // Reset publisher metrics
        _publisherManager!.ResetAllMetrics();

        // Reset subscriber metrics
        var resetTasks = _userAgentIds.Select(async agentId =>
        {
            var userAgent = _clusterClient!.GetGrain<IBroadcastUserAgent>(agentId);
            await userAgent.ResetMetricsAsync();
        });

        await Task.WhenAll(resetTasks);
    }

    private async Task WaitForCompletionAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("⏳ Waiting for broadcast event processing to complete...");

        var timeout = TimeSpan.FromSeconds(_config.CompletionTimeout);
        var checkInterval = TimeSpan.FromSeconds(_config.CompletionCheckInterval);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(checkInterval, cancellationToken);

            // Check if all events have been processed
            var subscriberMetrics = await CollectSubscriberMetricsAsync();
            var totalProcessed = subscriberMetrics.Sum(s => s.TotalEventsProcessed);
            var expectedTotal = _config.CalculateTotalExpectedEvents();

            _logger.LogInformation("Progress: {ProcessedEvents}/{ExpectedEvents} events processed",
                totalProcessed, expectedTotal);

            if (totalProcessed >= expectedTotal)
            {
                _logger.LogInformation("✅ All events processed successfully");
                break;
            }
        }

        if (stopwatch.Elapsed >= timeout)
        {
            _logger.LogWarning("⚠️ Completion timeout reached");
        }
    }

    private async Task RunDebugModeAsync()
    {
        _logger.LogInformation("🔍 DEBUG: Sending single broadcast event...");

        var startTime = DateTime.UtcNow;
        
        // Get a schedule agent
        var scheduleAgentId = Guid.NewGuid();
        var scheduleAgent = _clusterClient!.GetGrain<IBroadcastScheduleAgent>(scheduleAgentId);

        // Create debug event
        var debugEvent = new BroadcastTestEvent(0, scheduleAgentId, _config.EventNumber);

        // Send the event
        await scheduleAgent.BroadcastEventAsync(debugEvent);

        // Wait for processing
        await Task.Delay(5000);
        
        var endTime = DateTime.UtcNow;

        // Check results
        var subscriberMetrics = await CollectSubscriberMetricsAsync();
        var totalProcessed = subscriberMetrics.Sum(s => s.TotalEventsProcessed);

        _logger.LogInformation("🔍 DEBUG: {ProcessedEvents}/{SubscriberCount} subscribers processed the event",
            totalProcessed, _config.SubscriberCount);

        foreach (var metrics in subscriberMetrics)
        {
            if (metrics.TotalEventsProcessed > 0)
            {
                _logger.LogInformation("🔍 DEBUG: Subscriber processed {EventCount} events with avg latency {AvgLatency:F2}ms",
                    metrics.TotalEventsProcessed, metrics.AverageLatencyMs);
            }
        }

        // Create result object for formatted output
        var result = new BroadcastBenchmarkResult
        {
            PublisherCount = _config.PublisherCount,
            SubscriberCount = _config.SubscriberCount,
            EventsPerSecond = _config.EventsPerSecond,
            DurationSeconds = _config.Duration,
            StartTime = startTime,
            EndTime = endTime,
            ActualDurationSeconds = (endTime - startTime).TotalSeconds,
            Success = totalProcessed > 0,
            ErrorMessage = totalProcessed == 0 ? "No events processed" : "",
            EventNumber = _config.EventNumber,
            TotalEventsSent = 1,
            TotalEventsProcessed = totalProcessed,
            SubscriberMetrics = subscriberMetrics
        };

        // Calculate latency metrics
        CalculateLatencyMetrics(result);

        // Add result to collection
        _results.Add(result);

        // Display formatted results
        DisplayBroadcastResults(result);
    }

    private async Task<BroadcastAgentIds> LoadAgentIdsAsync()
    {
        try
        {
            if (!File.Exists(_agentIdsFilePath))
            {
                _logger.LogInformation($"No agent IDs file found at {_agentIdsFilePath}");
                return new BroadcastAgentIds();
            }

            string jsonString = await File.ReadAllTextAsync(_agentIdsFilePath);
            var agentIds = System.Text.Json.JsonSerializer.Deserialize<BroadcastAgentIds>(jsonString);
            if (agentIds == null)
            {
                _logger.LogInformation("Failed to deserialize agent IDs, creating new ones");
                return new BroadcastAgentIds();
            }
            _logger.LogInformation($"Loaded {agentIds.UserAgentIds.Count} user agent IDs from {_agentIdsFilePath}");
            return agentIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading agent IDs");
            return new BroadcastAgentIds();
        }
    }

    private async Task SaveAgentIdsAsync(BroadcastAgentIds agentIds)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = System.Text.Json.JsonSerializer.Serialize(agentIds, options);
            await File.WriteAllTextAsync(_agentIdsFilePath, jsonString);
            _logger.LogInformation($"Agent IDs saved to {_agentIdsFilePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving agent IDs");
        }
    }

    private void LogBenchmarkResult(BroadcastBenchmarkResult result)
    {
        DisplayBroadcastResults(result);
    }

    private void DisplayBroadcastResults(BroadcastBenchmarkResult result)
    {
        Console.WriteLine();
        Console.WriteLine("🎯 Broadcast Latency Results");
        Console.WriteLine("============================");
        
        if (_config.Debug)
        {
            DisplayDebugResults(result);
            return;
        }

        // Overall summary
        Console.WriteLine();
        Console.WriteLine("📊 Overall Summary");
        Console.WriteLine("=================");
        Console.WriteLine($"Workload duration: {result.ActualDurationSeconds:F2}s (excluding warmup)");
        Console.WriteLine($"Publishers: {result.PublisherCount}, Subscribers: {result.SubscriberCount}");
        Console.WriteLine($"Events sent: {result.TotalEventsSent:N0}, Events processed: {result.TotalEventsProcessed:N0}");
        Console.WriteLine($"Broadcast fan-out: 1-to-{result.SubscriberCount}");
        Console.WriteLine($"Event number used: {result.EventNumber}");

        // Performance metrics table
        Console.WriteLine();
        Console.WriteLine("📈 Broadcast Performance Metrics");
        Console.WriteLine("================================");
        Console.WriteLine();
        
        // Calculate metrics
        var expectedEvents = result.TotalEventsSent * result.SubscriberCount;
        var successRate = expectedEvents > 0 ? (double)result.TotalEventsProcessed / expectedEvents * 100 : 0;
        var throughput = result.ActualDurationSeconds > 0 ? result.TotalEventsProcessed / result.ActualDurationSeconds : 0;
        var targetThroughput = 115.7; // 10M events per day = 115.7 events/sec
        var achievement = throughput >= targetThroughput * 0.95 ? "✅ ACHIEVED" : "❌ MISSED";
        var status = result.Success ? "✅ OK" : "❌ FAIL";
        
        // Fixed-width table headers
        Console.WriteLine($"{"Publishers",-10} | {"Subscribers",-11} | {"Events/Sec",-10} | {"Target/Sec",-10} | {"Achievement",-12} | {"Avg Latency",-11} | {"P95 Latency",-11} | {"P99 Latency",-11} | {"Status",-8}");
        Console.WriteLine($"{new string('-', 10)} | {new string('-', 11)} | {new string('-', 10)} | {new string('-', 10)} | {new string('-', 12)} | {new string('-', 11)} | {new string('-', 11)} | {new string('-', 11)} | {new string('-', 8)}");
        
        Console.WriteLine($"{result.PublisherCount,-10} | {result.SubscriberCount,-11} | {throughput,-10:F1} | {targetThroughput,-10:F1} | {achievement,-12} | {result.AverageLatencyMs,-11:F2}ms | {result.P95LatencyMs,-11:F2}ms | {result.P99LatencyMs,-11:F2}ms | {status,-8}");
        
        // Event processing breakdown
        Console.WriteLine();
        Console.WriteLine("📊 Event Processing Breakdown");
        Console.WriteLine("=============================");
        Console.WriteLine();
        
        Console.WriteLine($"{"Publishers",-10} | {"Events Sent",-11} | {"Subscribers",-11} | {"Events Recv",-11} | {"Success Rate",-12} | {"Throughput",-11} | {"End-to-End",-11}");
        Console.WriteLine($"{new string('-', 10)} | {new string('-', 11)} | {new string('-', 11)} | {new string('-', 11)} | {new string('-', 12)} | {new string('-', 11)} | {new string('-', 11)}");
        
        Console.WriteLine($"{result.PublisherCount,-10} | {result.TotalEventsSent,-11} | {result.SubscriberCount,-11} | {result.TotalEventsProcessed,-11} | {successRate,-12:F1}% | {throughput,-11:F1}/s | {result.AverageLatencyMs,-11:F2}ms");
        
        // Latency distribution
        Console.WriteLine();
        Console.WriteLine("⏱️ Latency Distribution");
        Console.WriteLine("=======================");
        Console.WriteLine($"Min latency: {result.MinLatencyMs:F2}ms");
        Console.WriteLine($"Max latency: {result.MaxLatencyMs:F2}ms");
        Console.WriteLine($"Median latency: {result.MedianLatencyMs:F2}ms");
        Console.WriteLine($"Standard deviation: {result.StandardDeviationMs:F2}ms");
        
        // Performance assessment
        Console.WriteLine();
        Console.WriteLine("🎯 Performance Assessment");
        Console.WriteLine("=========================");
        
        var latencyAssessment = result.AverageLatencyMs switch
        {
            < 10 => "🟢 EXCELLENT",
            < 50 => "🟡 GOOD",
            < 100 => "🟠 ACCEPTABLE",
            _ => "🔴 POOR"
        };
        
        var throughputAssessment = achievement == "✅ ACHIEVED" ? "🟢 TARGET MET" : "🔴 BELOW TARGET";
        
        Console.WriteLine($"Latency performance: {latencyAssessment} ({result.AverageLatencyMs:F2}ms avg)");
        Console.WriteLine($"Throughput performance: {throughputAssessment} ({throughput:F1}/{targetThroughput:F1} events/sec)");
        Console.WriteLine($"Success rate: {successRate:F1}% ({result.TotalEventsProcessed}/{expectedEvents} events)");
        
        if (result.Success)
        {
            Console.WriteLine($"✅ Broadcast communication: Working correctly");
        }
        else
        {
            Console.WriteLine($"❌ Broadcast communication: {result.ErrorMessage}");
        }
    }

    private void DisplayDebugResults(BroadcastBenchmarkResult result)
    {
        Console.WriteLine();
        Console.WriteLine("🔍 DEBUG MODE - Broadcast Latency Results");
        Console.WriteLine("=========================================");

        // Overall summary (same format as normal mode)
        Console.WriteLine();
        Console.WriteLine("📊 Overall Summary");
        Console.WriteLine("=================");
        Console.WriteLine($"Workload duration: {result.ActualDurationSeconds:F2}s (excluding warmup)");
        Console.WriteLine($"Publishers: {result.PublisherCount}, Subscribers: {result.SubscriberCount}");
        Console.WriteLine($"Events sent: {result.TotalEventsSent:N0}, Events processed: {result.TotalEventsProcessed:N0}");
        Console.WriteLine($"Broadcast fan-out: 1-to-{result.SubscriberCount}");
        Console.WriteLine($"Event number used: {result.EventNumber}");
        Console.WriteLine($"🔍 Debug mode: Single event trace for communication analysis");

        // Performance metrics table (same format as normal mode)
        Console.WriteLine();
        Console.WriteLine("📈 Broadcast Performance Metrics");
        Console.WriteLine("================================");
        Console.WriteLine();
        
        // Calculate metrics (same as normal mode)
        var expectedEvents = result.TotalEventsSent * result.SubscriberCount;
        var successRate = expectedEvents > 0 ? (double)result.TotalEventsProcessed / expectedEvents * 100 : 0;
        var throughput = result.ActualDurationSeconds > 0 ? result.TotalEventsProcessed / result.ActualDurationSeconds : 0;
        var targetThroughput = 115.7; // 10M events per day = 115.7 events/sec
        var achievement = throughput >= targetThroughput * 0.95 ? "✅ ACHIEVED" : "❌ MISSED";
        var status = result.Success ? "✅ OK" : "❌ FAIL";
        
        // Fixed-width table headers (same as normal mode)
        Console.WriteLine($"{"Publishers",-10} | {"Subscribers",-11} | {"Events/Sec",-10} | {"Target/Sec",-10} | {"Achievement",-12} | {"Avg Latency",-11} | {"P95 Latency",-11} | {"P99 Latency",-11} | {"Status",-8}");
        Console.WriteLine($"{new string('-', 10)} | {new string('-', 11)} | {new string('-', 10)} | {new string('-', 10)} | {new string('-', 12)} | {new string('-', 11)} | {new string('-', 11)} | {new string('-', 11)} | {new string('-', 8)}");
        
        Console.WriteLine($"{result.PublisherCount,-10} | {result.SubscriberCount,-11} | {throughput,-10:F1} | {targetThroughput,-10:F1} | {achievement,-12} | {result.AverageLatencyMs,-11:F2}ms | {result.P95LatencyMs,-11:F2}ms | {result.P99LatencyMs,-11:F2}ms | {status,-8}");
        
        // Event processing breakdown (same format as normal mode)
        Console.WriteLine();
        Console.WriteLine("📊 Event Processing Breakdown");
        Console.WriteLine("=============================");
        Console.WriteLine();
        
        Console.WriteLine($"{"Publishers",-10} | {"Events Sent",-11} | {"Subscribers",-11} | {"Events Recv",-11} | {"Success Rate",-12} | {"Throughput",-11} | {"End-to-End",-11}");
        Console.WriteLine($"{new string('-', 10)} | {new string('-', 11)} | {new string('-', 11)} | {new string('-', 11)} | {new string('-', 12)} | {new string('-', 11)} | {new string('-', 11)}");
        
        Console.WriteLine($"{result.PublisherCount,-10} | {result.TotalEventsSent,-11} | {result.SubscriberCount,-11} | {result.TotalEventsProcessed,-11} | {successRate,-12:F1}% | {throughput,-11:F1}/s | {result.AverageLatencyMs,-11:F2}ms");
        
        // Latency distribution (same as normal mode)
        if (result.TotalEventsProcessed > 0)
        {
            Console.WriteLine();
            Console.WriteLine("⏱️ Latency Distribution");
            Console.WriteLine("=======================");
            Console.WriteLine($"Min latency: {result.MinLatencyMs:F2}ms");
            Console.WriteLine($"Max latency: {result.MaxLatencyMs:F2}ms");
            Console.WriteLine($"Median latency: {result.MedianLatencyMs:F2}ms");
            Console.WriteLine($"Standard deviation: {result.StandardDeviationMs:F2}ms");
        }
        
        // Performance assessment (same as normal mode)
        Console.WriteLine();
        Console.WriteLine("🎯 Performance Assessment");
        Console.WriteLine("=========================");
        
        if (result.TotalEventsProcessed > 0)
        {
            var latencyAssessment = result.AverageLatencyMs switch
            {
                < 10 => "🟢 EXCELLENT",
                < 50 => "🟡 GOOD",
                < 100 => "🟠 ACCEPTABLE",
                _ => "🔴 POOR"
            };
            
            var throughputAssessment = achievement == "✅ ACHIEVED" ? "🟢 TARGET MET" : "🔴 BELOW TARGET";
            
            Console.WriteLine($"Latency performance: {latencyAssessment} ({result.AverageLatencyMs:F2}ms avg)");
            Console.WriteLine($"Throughput performance: {throughputAssessment} ({throughput:F1}/{targetThroughput:F1} events/sec)");
            Console.WriteLine($"Success rate: {successRate:F1}% ({result.TotalEventsProcessed}/{expectedEvents} events)");
            
            if (result.Success)
            {
                Console.WriteLine($"✅ Broadcast communication: Working correctly");
            }
            else
            {
                Console.WriteLine($"❌ Broadcast communication: {result.ErrorMessage}");
            }
        }
        else
        {
            Console.WriteLine($"❌ No events processed");
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"❌ Error: {result.ErrorMessage}");
            }
        }
        
        // Debug-specific communication flow analysis
        Console.WriteLine();
        Console.WriteLine("🔄 Debug Communication Flow Analysis");
        Console.WriteLine("====================================");
        Console.WriteLine($"📡 Publishers: Sent {result.TotalEventsSent} broadcast events");
        Console.WriteLine($"📥 Subscribers: Received {result.TotalEventsProcessed} events total");
        Console.WriteLine($"🔄 Broadcast ratio: {(result.TotalEventsProcessed > 0 && result.TotalEventsSent > 0 ? (double)result.TotalEventsProcessed / result.TotalEventsSent : 0):F1}:1");
        
        if (result.SubscriberMetrics.Count > 0)
        {
            var activeSubscribers = result.SubscriberMetrics.Count(s => s.TotalEventsProcessed > 0);
            Console.WriteLine($"👥 Active subscribers: {activeSubscribers}/{result.SubscriberCount}");
            
            if (activeSubscribers > 0)
            {
                var avgEventsPerSubscriber = result.SubscriberMetrics.Where(s => s.TotalEventsProcessed > 0).Average(s => s.TotalEventsProcessed);
                Console.WriteLine($"📊 Avg events per subscriber: {avgEventsPerSubscriber:F1}");
            }
            
            // Show individual subscriber breakdown in debug mode
            Console.WriteLine();
            Console.WriteLine("👥 Individual Subscriber Breakdown:");
            var subscriberIndex = 0;
            foreach (var subscriber in result.SubscriberMetrics.Take(5)) // Show first 5 for brevity
            {
                Console.WriteLine($"    Subscriber {subscriberIndex + 1}: {subscriber.TotalEventsProcessed} events processed");
                subscriberIndex++;
            }
            if (result.SubscriberMetrics.Count > 5)
            {
                Console.WriteLine($"    ... and {result.SubscriberMetrics.Count - 5} more subscribers");
            }
        }
    }

    private async Task GenerateReportAsync()
    {
        var report = new BroadcastBenchmarkReport
        {
            Configuration = _config,
            Results = _results,
            GeneratedAt = DateTime.UtcNow
        };

        var json = JsonConvert.SerializeObject(report, Formatting.Indented);
        await File.WriteAllTextAsync(_config.OutputFile, json);
        _logger.LogInformation("📊 Broadcast benchmark report saved to {OutputFile}", _config.OutputFile);
    }

    public void Dispose()
    {
        _publisherManager?.Dispose();
        _host?.Dispose();
    }
}

// Result classes
public class BroadcastBenchmarkResult
{
    public int PublisherCount { get; set; }
    public int SubscriberCount { get; set; }
    public int EventsPerSecond { get; set; }
    public int DurationSeconds { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double ActualDurationSeconds { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = "";
    public int EventNumber { get; set; }

    public long TotalEventsSent { get; set; }
    public long TotalEventsProcessed { get; set; }

    public double MinLatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
    public double AverageLatencyMs { get; set; }
    public double MedianLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public double P99LatencyMs { get; set; }
    public double StandardDeviationMs { get; set; }

    public List<BroadcastPublisherMetrics> PublisherMetrics { get; set; } = new();
    public List<BroadcastLatencyMetrics> SubscriberMetrics { get; set; } = new();
}

public class BroadcastBenchmarkReport
{
    public BroadcastBenchmarkConfig Configuration { get; set; } = new();
    public List<BroadcastBenchmarkResult> Results { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
} 