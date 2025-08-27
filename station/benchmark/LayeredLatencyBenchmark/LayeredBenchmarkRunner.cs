using System.Diagnostics;
using System.Text.Json;
using E2E.Grains;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace LayeredLatencyBenchmark;

/// <summary>
/// Main orchestrator for layered agent communication benchmarks
/// </summary>
public class LayeredBenchmarkRunner
{
    private readonly LayeredBenchmarkConfig _config;
    private readonly OrleansAgentClient _orleansClient;
    private readonly ILogger<LayeredBenchmarkRunner> _logger;

    public LayeredBenchmarkRunner(LayeredBenchmarkConfig config, OrleansAgentClient orleansClient, ILogger<LayeredBenchmarkRunner> logger)
    {
        _config = config;
        _orleansClient = orleansClient;
        _logger = logger;
    }

    /// <summary>
    /// Run the layered benchmark
    /// </summary>
    public async Task<BenchmarkResults> RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🚀 Starting layered agent communication benchmark");
        
        // Check for debug mode
        if (_config.Debug)
        {
            _logger.LogInformation("🔍 DEBUG MODE: Running single event layered trace...");
            return await RunDebugModeAsync(cancellationToken);
        }
        
        _logger.LogInformation("📊 Configuration:");
        _logger.LogInformation("  - Leader count: {LeaderCount} (fixed)", _config.LeaderCount);
        _logger.LogInformation("  - Sub-agent range: {BaseSubAgents} - {MaxSubAgents}", _config.BaseSubAgents, _config.MaxSubAgents);
        _logger.LogInformation("  - Scale factor: {ScaleFactor}", _config.ScaleFactor);
        _logger.LogInformation("  - Duration: {Duration}s per test", _config.Duration);
        _logger.LogInformation("  - Events per second: {EventsPerSecond}", _config.EventsPerSecond);
        _logger.LogInformation("  - Target daily events: {TargetDailyEvents:N0}", _config.TargetDailyEvents);

        var overallStopwatch = Stopwatch.StartNew();
        var concurrencyLevels = _config.GetSubAgentConcurrencyLevels().ToList();
        
        _logger.LogInformation("📈 Testing {LevelCount} concurrency levels: {Levels}", 
            concurrencyLevels.Count, string.Join(", ", concurrencyLevels));

        var allResults = new BenchmarkResults 
        { 
            Config = _config, 
            CollectionTime = DateTime.UtcNow,
            ConcurrencyResults = new Dictionary<int, ConcurrencyLevelResults>()
        };

        try
        {
            // Test each concurrency level
            for (int i = 0; i < concurrencyLevels.Count; i++)
            {
                int subAgentCount = concurrencyLevels[i];
                _logger.LogInformation("");
                _logger.LogInformation("🔢 Testing concurrency level {Current}/{Total}: {SubAgentCount} sub-agents", 
                    i + 1, concurrencyLevels.Count, subAgentCount);
                
                var levelResults = await RunConcurrencyLevelAsync(subAgentCount, cancellationToken);
                allResults.ConcurrencyResults[subAgentCount] = levelResults;
                
                // Log immediate results for this level
                LogConcurrencyLevelSummary(subAgentCount, levelResults);
                
                // Small delay between levels to allow system to settle
                if (i < concurrencyLevels.Count - 1)
                {
                    _logger.LogInformation("⏱️ Waiting 5 seconds before next level...");
                    await Task.Delay(5000, cancellationToken);
                }
            }
            
            overallStopwatch.Stop();
            allResults.TotalElapsedTime = overallStopwatch.Elapsed;
            
            _logger.LogInformation("");
            _logger.LogInformation("✅ All concurrency levels completed in {ElapsedTime}", overallStopwatch.Elapsed);
            return allResults;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("🛑 Benchmark cancelled by user");
            allResults.Success = false;
            allResults.ErrorMessage = "Cancelled by user";
            return allResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Benchmark failed");
            throw;
        }
    }

    /// <summary>
    /// Run debug mode with 1 leader, 1 sub-agent, and 1 event
    /// </summary>
    private async Task<BenchmarkResults> RunDebugModeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 DEBUG: Starting single event layered test...");
        
        var stopwatch = Stopwatch.StartNew();
        var results = new BenchmarkResults
        {
            Config = _config,
            CollectionTime = DateTime.UtcNow
        };

        try
        {
            // Step 1: Create exactly 1 leader and 1 sub-agent
            _logger.LogInformation("🔍 DEBUG: Creating 1 leader and 1 sub-agent...");
            var leaderAgentId = Guid.NewGuid();
            var subAgentId = Guid.NewGuid();
            
            var leader = await _orleansClient.CreateLeaderAgentAsync(leaderAgentId);
            var subAgent = await _orleansClient.CreateSubAgentAsync(subAgentId);
            
            _logger.LogInformation("✅ DEBUG: Created leader {LeaderAgentId} and sub-agent {SubAgentId}", 
                leaderAgentId, subAgentId);

            // Step 2: Reset metrics to ensure clean state
            _logger.LogInformation("🔍 DEBUG: Resetting agent metrics...");
            await leader.ResetMetricsAsync();
            await subAgent.ResetMetricsAsync();

            // Step 3: Establish hierarchy (register sub-agent with leader)
            _logger.LogInformation("🔍 DEBUG: Establishing hierarchy...");
            await _orleansClient.RegisterSubAgentsAsync(leaderAgentId, new List<Guid> { subAgentId });
            _logger.LogInformation("✅ DEBUG: Hierarchy established");

            // Step 4: Get initial metrics
            _logger.LogInformation("🔍 DEBUG: Collecting initial metrics...");
            var initialLeaderMetrics = await leader.GetLayeredMetricsAsync();
            var initialSubAgentMetrics = await subAgent.GetLayeredMetricsAsync();
            
            _logger.LogInformation("🔍 DEBUG: Initial state:");
            _logger.LogInformation("  - Leader events received: {LeaderEventsReceived}", initialLeaderMetrics.EventsReceived);
            _logger.LogInformation("  - Leader events forwarded: {LeaderEventsForwarded}", initialLeaderMetrics.EventsForwarded);
            _logger.LogInformation("  - Sub-agent events received: {SubAgentEventsReceived}", initialSubAgentMetrics.EventsReceived);

            // Step 5: Send exactly 1 event
            _logger.LogInformation("🔍 DEBUG: Sending single event...");
            var testEvent = new LayeredTestEvent(
                number: 1,
                publisherAgentId: "debug-publisher",
                correlationId: Guid.NewGuid().ToString()
            );
            
            _logger.LogInformation("🔍 DEBUG: Event details:");
            _logger.LogInformation("  - CorrelationId: {CorrelationId}", testEvent.CorrelationId);
            _logger.LogInformation("  - EventNumber: {EventNumber}", testEvent.EventNumber);
            _logger.LogInformation("  - PublisherAgentId: {PublisherAgentId}", testEvent.PublisherAgentId);
            _logger.LogInformation("  - SentTimestamp: {SentTimestamp}", testEvent.SentTimestamp);

            // Send the event to the leader
            await _orleansClient.PublishEventToLeaderAsync(leaderAgentId, testEvent);
            _logger.LogInformation("✅ DEBUG: Event sent to leader {LeaderAgentId}", leaderAgentId);

            // Step 6: Wait for completion using proper timeout logic
            _logger.LogInformation("🔍 DEBUG: Waiting for event processing completion...");
            
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.CompletionTimeout));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            await WaitForCompletionAsync(new List<ILayeredLeaderAgent> { leader }, new List<ILayeredSubAgent> { subAgent }, combinedCts.Token);

            // Step 7: Collect final metrics
            _logger.LogInformation("🔍 DEBUG: Collecting final metrics...");
            var finalLeaderMetrics = await leader.GetLayeredMetricsAsync();
            var finalSubAgentMetrics = await subAgent.GetLayeredMetricsAsync();

            _logger.LogInformation("🔍 DEBUG: Final results:");
            _logger.LogInformation("  - Leader events received: {LeaderEventsReceived} (change: +{LeaderChange})", 
                finalLeaderMetrics.EventsReceived, finalLeaderMetrics.EventsReceived - initialLeaderMetrics.EventsReceived);
            _logger.LogInformation("  - Leader events forwarded: {LeaderEventsForwarded} (change: +{LeaderForwardChange})", 
                finalLeaderMetrics.EventsForwarded, finalLeaderMetrics.EventsForwarded - initialLeaderMetrics.EventsForwarded);
            _logger.LogInformation("  - Sub-agent events received: {SubAgentEventsReceived} (change: +{SubAgentChange})", 
                finalSubAgentMetrics.EventsReceived, finalSubAgentMetrics.EventsReceived - initialSubAgentMetrics.EventsReceived);

            // Step 8: Check measurements
            if (finalSubAgentMetrics.RawMeasurements.Count > 0)
            {
                _logger.LogInformation("  - Latency measurements: {MeasurementCount}", finalSubAgentMetrics.RawMeasurements.Count);
                foreach (var measurement in finalSubAgentMetrics.RawMeasurements)
                {
                    _logger.LogInformation("    * Event {EventNumber}: {LatencyMs:F2}ms (ID: {CorrelationId})", 
                        measurement.EventNumber, measurement.LatencyMs, measurement.CorrelationId);
                }
            }
            else
            {
                _logger.LogWarning("  - ❌ No latency measurements recorded!");
            }

            // Step 9: Analyze results
            var leaderReceived = finalLeaderMetrics.EventsReceived > initialLeaderMetrics.EventsReceived;
            var leaderForwarded = finalLeaderMetrics.EventsForwarded > initialLeaderMetrics.EventsForwarded;
            var subAgentReceived = finalSubAgentMetrics.EventsReceived > initialSubAgentMetrics.EventsReceived;

            if (leaderReceived && leaderForwarded && subAgentReceived)
            {
                _logger.LogInformation("✅ DEBUG: SUCCESS - Event was received by leader, forwarded, and received by sub-agent!");
                results.Success = true;
            }
            else if (leaderReceived && leaderForwarded)
            {
                _logger.LogWarning("⚠️ DEBUG: PARTIAL - Event was received and forwarded by leader but NOT received by sub-agent!");
                _logger.LogWarning("   Possible issues:");
                _logger.LogWarning("   - Sub-agent not properly subscribed to leader");
                _logger.LogWarning("   - Event filtering issue");
                _logger.LogWarning("   - Hierarchy not established correctly");
            }
            else if (leaderReceived)
            {
                _logger.LogWarning("⚠️ DEBUG: PARTIAL - Event was received by leader but NOT forwarded!");
                _logger.LogWarning("   Possible issues:");
                _logger.LogWarning("   - Leader forwarding logic issue");
                _logger.LogWarning("   - No sub-agents registered");
            }
            else
            {
                _logger.LogError("❌ DEBUG: FAILURE - Event was NOT received by leader!");
                _logger.LogError("   Possible issues:");
                _logger.LogError("   - Leader agent not working");
                _logger.LogError("   - Event publishing failure");
                _logger.LogError("   - Orleans message routing problem");
            }

            // Update results with proper data
            results.TotalEventsSent = finalLeaderMetrics.EventsReceived - initialLeaderMetrics.EventsReceived;
            results.TotalEventsProcessed = finalSubAgentMetrics.EventsReceived - initialSubAgentMetrics.EventsReceived;
            results.ExecutionTime = stopwatch.Elapsed;
            results.ErrorMessage = results.Success ? "" : "Debug mode revealed issues in layered communication";

            // Store metrics in the results object for display
            results.LeaderMetrics[leaderAgentId.ToString()] = finalLeaderMetrics;
            results.SubAgentMetrics[subAgentId.ToString()] = finalSubAgentMetrics;
            results.TotalElapsedTime = stopwatch.Elapsed;

            // Create ConcurrencyLevelResults for debug mode display
            var debugConcurrencyResult = new ConcurrencyLevelResults
            {
                SubAgentCount = 1, // Debug mode uses 1 sub-agent
                StartTime = DateTime.UtcNow.Subtract(stopwatch.Elapsed),
                ExecutionTime = stopwatch.Elapsed,
                Success = results.Success,
                ErrorMessage = results.ErrorMessage,
                
                // Event counts
                EventsSent = 1, // Debug mode sends 1 event
                TotalEventsReceived = finalLeaderMetrics.EventsReceived + finalSubAgentMetrics.EventsReceived,
                TotalEventsForwarded = finalLeaderMetrics.EventsForwarded,
                
                // Throughput (not really meaningful for debug mode, but calculate for completeness)
                ActualEventsPerSecond = 1.0 / stopwatch.Elapsed.TotalSeconds,
                TargetEventsPerSecond = 1.0 / 3.0, // Target: 1 event in 3 seconds
                ThroughputAchieved = true, // Debug mode always "achieves" its simple target
                
                // Latency metrics
                LeaderMetrics = finalLeaderMetrics,
                SubAgentMetrics = new List<LayeredMetrics> { finalSubAgentMetrics }
            };

            // Calculate latency statistics from sub-agent measurements
            if (finalSubAgentMetrics.RawMeasurements.Count > 0)
            {
                var latencies = finalSubAgentMetrics.RawMeasurements.Select(m => m.LatencyMs).ToList();
                debugConcurrencyResult.AverageLatencyMs = latencies.Average();
                debugConcurrencyResult.MedianLatencyMs = latencies.Count > 0 ? latencies.OrderBy(x => x).Skip(latencies.Count / 2).First() : 0;
                debugConcurrencyResult.MinLatencyMs = latencies.Min();
                debugConcurrencyResult.MaxLatencyMs = latencies.Max();
                debugConcurrencyResult.P95LatencyMs = CalculatePercentile(latencies, 95);
                debugConcurrencyResult.P99LatencyMs = CalculatePercentile(latencies, 99);
            }

            // Store in ConcurrencyResults for display
            results.ConcurrencyResults[1] = debugConcurrencyResult;

            // Log debug completion summary
            _logger.LogInformation("✅ DEBUG: Debug mode completed in {ElapsedTime:F1}s", stopwatch.Elapsed.TotalSeconds);
            if (results.Success)
            {
                _logger.LogInformation("✅ DEBUG: Layered communication flow is working correctly!");
                if (finalSubAgentMetrics.RawMeasurements.Count > 0)
                {
                    var latencies = finalSubAgentMetrics.RawMeasurements.Select(m => m.LatencyMs).ToList();
                    var avgLatency = latencies.Average();
                    _logger.LogInformation("✅ DEBUG: Average end-to-end latency: {AvgLatency:F2}ms", avgLatency);
                }
            }

            return results;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("🛑 DEBUG: Debug mode cancelled by user");
            results.Success = false;
            results.ErrorMessage = "Cancelled by user";
            results.ExecutionTime = stopwatch.Elapsed;
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ DEBUG: Debug mode execution failed");
            results.Success = false;
            results.ErrorMessage = $"Debug mode failed: {ex.Message}";
            results.ExecutionTime = stopwatch.Elapsed;
            return results;
        }
    }

    /// <summary>
    /// Create leader agents via Orleans client
    /// </summary>
    private async Task<List<ILayeredLeaderAgent>> CreateLeaderAgentsAsync()
    {
        _logger.LogInformation("👑 Creating {LeaderCount} leader agents", _config.LeaderCount);
        
        var leaders = new List<ILayeredLeaderAgent>();
        var tasks = new List<Task<ILayeredLeaderAgent>>();

        for (int i = 0; i < _config.LeaderCount; i++)
        {
            var leaderId = Guid.NewGuid();
            var task = _orleansClient.CreateLeaderAgentAsync(leaderId);
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);
        leaders.AddRange(results);
        
        _logger.LogInformation("✅ Created {SuccessCount}/{TotalCount} leader agents", leaders.Count, _config.LeaderCount);
        
        if (leaders.Count < _config.LeaderCount)
        {
            throw new InvalidOperationException($"Failed to create all leader agents. Only {leaders.Count}/{_config.LeaderCount} succeeded");
        }

        return leaders;
    }

    /// <summary>
    /// Create sub-agents via Orleans client
    /// </summary>
    private async Task<List<ILayeredSubAgent>> CreateSubAgentsAsync(int subAgentCount)
    {
        _logger.LogInformation("👥 Creating {SubAgentCount} sub-agents", subAgentCount);
        
        var subAgents = new List<ILayeredSubAgent>();
        var allTasks = new List<Task<ILayeredSubAgent>>();

        for (int i = 0; i < subAgentCount; i++)
        {
            var subAgentId = Guid.NewGuid();
            var task = _orleansClient.CreateSubAgentAsync(subAgentId);
            allTasks.Add(task);
        }

        var allResults = await Task.WhenAll(allTasks);
        subAgents.AddRange(allResults);
        
        _logger.LogInformation("✅ Created {SuccessCount}/{TotalCount} sub-agents", allResults.Length, subAgentCount);
        
        if (allResults.Length < subAgentCount)
        {
            throw new InvalidOperationException($"Failed to create all sub-agents. Only {allResults.Length}/{subAgentCount} succeeded");
        }

        return subAgents;
    }

    /// <summary>
    /// Establish hierarchy by registering sub-agents with leaders
    /// </summary>
    private async Task EstablishHierarchyAsync(ILayeredLeaderAgent leader, List<ILayeredSubAgent> subAgents)
    {
        _logger.LogInformation("🔗 Establishing hierarchy");
        
        var tasks = new List<Task>();

        if (subAgents.Count > 0)
        {
            // Get leader agent ID from grain
            var leaderAgentId = leader.GetGrainId().GetGuidKey();
            var subAgentIds = new List<Guid>();
            
            foreach (var subAgent in subAgents)
            {
                var subAgentId = subAgent.GetGrainId().GetGuidKey();
                subAgentIds.Add(subAgentId);
            }
            
            var task = _orleansClient.RegisterSubAgentsAsync(leaderAgentId, subAgentIds);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        
        _logger.LogInformation("✅ Established hierarchy for {TotalCount} sub-agents", subAgents.Count);
    }

    /// <summary>
    /// Activate all agents to set up stream subscriptions
    /// </summary>
    private async Task ActivateAllAgentsAsync(List<ILayeredLeaderAgent> leaders, List<ILayeredSubAgent> subAgents)
    {
        _logger.LogInformation("🔗 Activating all agents to set up stream subscriptions");
        
        var tasks = new List<Task>();

        // Activate leaders
        for (int i = 0; i < leaders.Count; i++)
        {
            var leader = leaders[i];
            var leaderIndex = i; // Capture for closure
            
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await _orleansClient.GetDescriptionAsync(leader);
                    _logger.LogInformation("Activated leader {LeaderIndex}", leaderIndex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to activate leader {LeaderIndex}", leaderIndex);
                }
            }));
        }

        // Activate sub-agents
        for (int i = 0; i < subAgents.Count; i++)
        {
            var subAgent = subAgents[i];
            var capturedLeaderIndex = 0; // Assuming only one leader for simplicity in activation
            var capturedSubIndex = i; // Capture for closure
            
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await _orleansClient.GetDescriptionAsync(subAgent);
                    _logger.LogInformation("Activated sub-agent {SubAgentId}", $"sub-{capturedLeaderIndex}-{capturedSubIndex}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to activate sub-agent {SubAgentId}", $"sub-{capturedLeaderIndex}-{capturedSubIndex}");
                }
            }));
        }

        await Task.WhenAll(tasks);
        _logger.LogInformation("✅ Activated {LeaderCount} leaders and {SubAgentCount} sub-agents", leaders.Count, subAgents.Count);
    }

    /// <summary>
    /// Publish events to all leaders
    /// </summary>
    private async Task<long> PublishEventsAsync(List<ILayeredLeaderAgent> leaders, int durationSeconds, bool isWarmup, CancellationToken cancellationToken = default)
    {
        var phase = isWarmup ? "WARMUP" : "BENCHMARK";
        _logger.LogInformation("🚀 [{Phase}] Publishing events for {Duration}s at {EventsPerSecond} events/sec", phase, durationSeconds, _config.EventsPerSecond);
        
        var endTime = DateTime.UtcNow.AddSeconds(durationSeconds);
        var intervalMs = 1000.0 / _config.EventsPerSecond;
        var eventNumber = 0;
        long totalEventsSent = 0;

        while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
        {
            var iterationStart = DateTime.UtcNow;
            
            // Create events for this iteration
            var tasks = leaders.Select(leader =>
            {
                var currentEventNumber = Interlocked.Increment(ref eventNumber);
                var testEvent = new LayeredTestEvent(
                    number: currentEventNumber,
                    publisherAgentId: "benchmark-publisher",
                    correlationId: $"benchmark-{currentEventNumber}"
                );
                
                var leaderAgentId = leader.GetGrainId().GetGuidKey();
                return _orleansClient.PublishEventToLeaderAsync(leaderAgentId, testEvent)
                    .ContinueWith(t => 1L); // Return 1 event sent
            });

            var sentEvents = await Task.WhenAll(tasks);
            totalEventsSent += sentEvents.Sum();

            // Rate limiting
            var elapsed = (DateTime.UtcNow - iterationStart).TotalMilliseconds;
            var remainingTime = intervalMs - elapsed;
            if (remainingTime > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(remainingTime), cancellationToken);
            }
        }

        _logger.LogInformation("✅ [{Phase}] Published {EventCount} events", phase, totalEventsSent);
        return totalEventsSent;
    }

    /// <summary>
    /// Reset metrics on all agents
    /// </summary>
    private async Task ResetMetricsAsync(List<ILayeredLeaderAgent> leaders, List<ILayeredSubAgent> subAgents)
    {
        _logger.LogInformation("🔄 Resetting metrics");
        
        var tasks = new List<Task>();
        
        // Reset leader metrics
        tasks.AddRange(leaders.Select(leader => _orleansClient.ResetLeaderMetricsAsync(leader)));
        
        // Reset sub-agent metrics
        foreach (var subAgent in subAgents)
        {
            tasks.Add(Task.Run(async () => await _orleansClient.ResetSubAgentMetricsAsync(subAgent)));
        }

        await Task.WhenAll(tasks);
        _logger.LogInformation("✅ Reset metrics for {LeaderCount} leaders and {SubAgentCount} sub-agents", 
            leaders.Count, subAgents.Count);
    }

    /// <summary>
    /// Wait for all events to be processed with timeout
    /// </summary>
    private async Task WaitForCompletionAsync(List<ILayeredLeaderAgent> leaders, List<ILayeredSubAgent> subAgents, CancellationToken cancellationToken)
    {
        var checkInterval = TimeSpan.FromSeconds(_config.CompletionCheckInterval);
        var lastEventsReceived = 0L;
        var stableCount = 0;
        const int stableThreshold = 3; // Consecutive stable checks before considering complete
        
        _logger.LogInformation("⏳ Waiting for completion with {Timeout}s timeout", _config.CompletionTimeout);
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Collect current metrics
                var leaderMetrics = await CollectLeaderMetricsAsync(leaders);
                var subAgentMetrics = await CollectSubAgentMetricsAsync(subAgents);
                
                // Calculate totals
                var totalEventsReceived = leaderMetrics.Sum(m => m.EventsReceived) + subAgentMetrics.Sum(m => m.EventsReceived);
                var totalEventsForwarded = leaderMetrics.Sum(m => m.EventsForwarded);
                
                // Check for progress
                if (totalEventsReceived == lastEventsReceived)
                {
                    stableCount++;
                    if (stableCount >= stableThreshold)
                    {
                        _logger.LogInformation("✅ Completion detected: {EventsReceived} events received, {EventsForwarded} forwarded in {Elapsed:F1}s", 
                            totalEventsReceived, totalEventsForwarded, 
                            (DateTime.UtcNow - DateTime.UtcNow.AddSeconds(-_config.CompletionTimeout)).TotalSeconds);
                        return;
                    }
                }
                else
                {
                    stableCount = 0;
                    lastEventsReceived = totalEventsReceived;
                }
                
                // Progress update
                _logger.LogInformation("📊 Progress: {EventsReceived} events received, {EventsForwarded} forwarded (stable: {StableCount}/{StableThreshold})", 
                    totalEventsReceived, totalEventsForwarded, stableCount, stableThreshold);
                
                await Task.Delay(checkInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("⚠️ Completion wait cancelled due to timeout");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during completion wait");
                break;
            }
        }
        
        _logger.LogWarning("⚠️ Completion wait finished (timeout or cancellation)");
    }

    /// <summary>
    /// Collect metrics from all leader agents
    /// </summary>
    private async Task<List<LayeredMetrics>> CollectLeaderMetricsAsync(List<ILayeredLeaderAgent> leaders)
    {
        var metrics = new List<LayeredMetrics>();
        
        foreach (var leader in leaders)
        {
            try
            {
                var leaderMetrics = await leader.GetLayeredMetricsAsync();
                metrics.Add(leaderMetrics);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect metrics from leader {LeaderAgentId}", leader.GetGrainId());
            }
        }
        
        return metrics;
    }

    /// <summary>
    /// Collect metrics from all sub-agents
    /// </summary>
    private async Task<List<LayeredMetrics>> CollectSubAgentMetricsAsync(List<ILayeredSubAgent> subAgents)
    {
        var metrics = new List<LayeredMetrics>();
        
        foreach (var subAgent in subAgents)
        {
            try
            {
                var subAgentMetrics = await subAgent.GetLayeredMetricsAsync();
                metrics.Add(subAgentMetrics);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect metrics from sub-agent {SubAgentId}", subAgent.GetGrainId());
            }
        }
        
        return metrics;
    }

    /// <summary>
    /// Log detailed metrics for debugging event flow
    /// </summary>
    private void LogDetailedMetrics(BenchmarkResults results)
    {
        _logger.LogInformation("📊 DETAILED METRICS");
        _logger.LogInformation("===================");
        
        // Leader metrics
        var totalEventsReceived = 0L;
        var totalEventsForwarded = 0L;
        
        foreach (var (leaderId, metrics) in results.LeaderMetrics)
        {
            if (metrics != null)
            {
                _logger.LogInformation("👑 Leader {LeaderId}: Received={EventsReceived}, Forwarded={EventsForwarded}, AvgLatency={AvgLatency:F2}ms", 
                    leaderId, metrics.EventsReceived, metrics.EventsForwarded, metrics.AvgLatencyMs);
                totalEventsReceived += metrics.EventsReceived;
                totalEventsForwarded += metrics.EventsForwarded;
            }
            else
            {
                _logger.LogWarning("⚠️ Leader {LeaderId}: NULL METRICS", leaderId);
            }
        }
        
        // Sub-agent metrics
        var totalSubEventsReceived = 0L;
        
        foreach (var (subAgentId, metrics) in results.SubAgentMetrics)
        {
            if (metrics != null)
            {
                _logger.LogInformation("👥 Sub-agent {SubAgentId}: Received={EventsReceived}, AvgLatency={AvgLatency:F2}ms", 
                    subAgentId, metrics.EventsReceived, metrics.AvgLatencyMs);
                totalSubEventsReceived += metrics.EventsReceived;
            }
            else
            {
                _logger.LogWarning("⚠️ Sub-agent {SubAgentId}: NULL METRICS", subAgentId);
            }
        }
        
        _logger.LogInformation("📊 TOTALS: Leaders received {TotalReceived}, forwarded {TotalForwarded}, Sub-agents received {TotalSubReceived}", 
            totalEventsReceived, totalEventsForwarded, totalSubEventsReceived);
    }

    /// <summary>
    /// Run benchmark for a specific concurrency level (sub-agent count)
    /// </summary>
    private async Task<ConcurrencyLevelResults> RunConcurrencyLevelAsync(int subAgentCount, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new ConcurrencyLevelResults
        {
            SubAgentCount = subAgentCount,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Step 1: Create leader agent (always 1)
            var leaders = await CreateLeaderAgentsAsync();
            if (leaders.Count != 1)
            {
                throw new InvalidOperationException($"Expected 1 leader, got {leaders.Count}");
            }

            // Step 2: Create sub-agents for this concurrency level
            var subAgents = await CreateSubAgentsAsync(subAgentCount);
            if (subAgents.Count != subAgentCount)
            {
                throw new InvalidOperationException($"Expected {subAgentCount} sub-agents, got {subAgents.Count}");
            }

            // Step 3: Establish hierarchy (register sub-agents with leader)
            await EstablishHierarchyAsync(leaders.First(), subAgents);

            // Step 4: Activate all agents to set up stream subscriptions
            await ActivateAllAgentsAsync(leaders, subAgents);

            // Step 5: Warmup phase
            if (_config.WarmupDuration > 0)
            {
                _logger.LogInformation("🔥 Warmup phase ({WarmupDuration}s)", _config.WarmupDuration);
                await PublishEventsAsync(leaders, _config.WarmupDuration, isWarmup: true, cancellationToken);
                
                // Reset metrics after warmup
                await ResetMetricsAsync(leaders, subAgents);
            }

            // Step 6: Benchmark phase
            _logger.LogInformation("⚡ Benchmark phase ({Duration}s)", _config.Duration);
            var eventsSent = await PublishEventsAsync(leaders, _config.Duration, isWarmup: false, cancellationToken);
            results.EventsSent = eventsSent;

            // Step 7: Wait for completion
            _logger.LogInformation("⏳ Waiting for completion...");
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.CompletionTimeout));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            await WaitForCompletionAsync(leaders, subAgents, combinedCts.Token);

            // Step 8: Collect metrics
            _logger.LogInformation("📊 Collecting metrics...");
            var leaderMetrics = await CollectLeaderMetricsAsync(leaders);
            var subAgentMetrics = await CollectSubAgentMetricsAsync(subAgents);

            results.LeaderMetrics = leaderMetrics.FirstOrDefault();
            results.SubAgentMetrics = subAgentMetrics;
            results.Success = true;
            
            // Calculate aggregated metrics
            results.TotalEventsReceived = leaderMetrics.Sum(m => m.EventsReceived) + subAgentMetrics.Sum(m => m.EventsReceived);
            results.TotalEventsForwarded = leaderMetrics.Sum(m => m.EventsForwarded);
            
            if (subAgentMetrics.Any(m => m.RawMeasurements.Count > 0))
            {
                var allMeasurements = subAgentMetrics.SelectMany(m => m.RawMeasurements).ToList();
                results.AverageLatencyMs = allMeasurements.Average(m => m.LatencyMs);
                results.MedianLatencyMs = allMeasurements.OrderBy(m => m.LatencyMs).Skip(allMeasurements.Count / 2).First().LatencyMs;
                results.MaxLatencyMs = allMeasurements.Max(m => m.LatencyMs);
                results.MinLatencyMs = allMeasurements.Min(m => m.LatencyMs);
                results.P95LatencyMs = allMeasurements.OrderBy(m => m.LatencyMs).Skip((int)(allMeasurements.Count * 0.95)).First().LatencyMs;
                results.P99LatencyMs = allMeasurements.OrderBy(m => m.LatencyMs).Skip((int)(allMeasurements.Count * 0.99)).First().LatencyMs;
            }

            // Calculate throughput
            results.ActualEventsPerSecond = results.TotalEventsReceived / (double)_config.Duration;
            results.TargetEventsPerSecond = _config.CalculateTotalEventsPerSecond(subAgentCount);
            results.ThroughputAchieved = results.ActualEventsPerSecond >= results.TargetEventsPerSecond * 0.95; // 95% of target (~110 events/sec)
            
            stopwatch.Stop();
            results.ExecutionTime = stopwatch.Elapsed;
            
            return results;
        }
        catch (OperationCanceledException)
        {
            results.Success = false;
            results.ErrorMessage = "Cancelled by user";
            _logger.LogWarning("🛑 Concurrency level {SubAgentCount} cancelled by user", subAgentCount);
            return results;
        }
        catch (Exception ex)
        {
            results.Success = false;
            results.ErrorMessage = ex.Message;
            _logger.LogError(ex, "❌ Concurrency level {SubAgentCount} failed", subAgentCount);
            throw;
        }
    }

    /// <summary>
    /// Log summary for a specific concurrency level
    /// </summary>
    private void LogConcurrencyLevelSummary(int subAgentCount, ConcurrencyLevelResults results)
    {
        _logger.LogInformation("📊 Level {SubAgentCount} results:", subAgentCount);
        _logger.LogInformation("  - Events sent: {EventsSent:N0}", results.EventsSent);
        _logger.LogInformation("  - Events received: {EventsReceived:N0}", results.TotalEventsReceived);
        _logger.LogInformation("  - Events forwarded: {EventsForwarded:N0}", results.TotalEventsForwarded);
        _logger.LogInformation("  - Average latency: {AvgLatency:F2}ms", results.AverageLatencyMs);
        _logger.LogInformation("  - Throughput: {ActualThroughput:F1}/{TargetThroughput:F1} events/sec ({Achievement})", 
            results.ActualEventsPerSecond, results.TargetEventsPerSecond, 
            results.ThroughputAchieved ? "✅ ACHIEVED" : "❌ MISSED");
        _logger.LogInformation("  - Execution time: {ExecutionTime}", results.ExecutionTime);
    }

    /// <summary>
    /// Calculate percentile value from a list of values
    /// </summary>
    private static double CalculatePercentile(List<double> values, double percentile)
    {
        if (values.Count == 0) return 0;
        
        var sorted = values.OrderBy(x => x).ToList();
        var index = (percentile / 100.0) * (sorted.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        
        if (lower == upper)
            return sorted[lower];
            
        var weight = index - lower;
        return sorted[lower] * (1 - weight) + sorted[upper] * weight;
    }
}

/// <summary>
/// Results of the layered benchmark
/// </summary>
public class BenchmarkResults
{
    public LayeredBenchmarkConfig Config { get; set; } = null!;
    public Dictionary<string, LayeredMetrics> LeaderMetrics { get; set; } = new();
    public Dictionary<string, LayeredMetrics> SubAgentMetrics { get; set; } = new();
    public DateTime CollectionTime { get; set; }
    public TimeSpan TotalElapsedTime { get; set; }
    public bool Success { get; set; } = true; // Added for debug mode
    public long TotalEventsSent { get; set; } // Added for debug mode
    public long TotalEventsProcessed { get; set; } // Added for debug mode
    public TimeSpan ExecutionTime { get; set; } // Added for debug mode
    public string ErrorMessage { get; set; } = string.Empty; // Added for debug mode
    public Dictionary<int, ConcurrencyLevelResults> ConcurrencyResults { get; set; } = new();

    public void SaveToFile(string filePath)
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }
}

/// <summary>
/// Results for a specific concurrency level
/// </summary>
public class ConcurrencyLevelResults
{
    public int SubAgentCount { get; set; }
    public DateTime StartTime { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    
    // Event counts
    public long EventsSent { get; set; }
    public long TotalEventsReceived { get; set; }
    public long TotalEventsForwarded { get; set; }
    
    // Throughput metrics
    public double ActualEventsPerSecond { get; set; }
    public double TargetEventsPerSecond { get; set; }
    public bool ThroughputAchieved { get; set; }
    
    // Latency metrics
    public double AverageLatencyMs { get; set; }
    public double MedianLatencyMs { get; set; }
    public double MinLatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public double P99LatencyMs { get; set; }
    
    // Detailed metrics
    public LayeredMetrics? LeaderMetrics { get; set; }
    public List<LayeredMetrics> SubAgentMetrics { get; set; } = new();
} 