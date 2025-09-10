using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AgentWarmupE2E.Utilities;

/// <summary>
/// Performance monitoring utility for agent warmup tests
/// </summary>
public class PerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly ConcurrentDictionary<string, List<PerformanceMetric>> _metrics = new();
    private readonly object _lock = new();

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Measures the execution time of an operation
    /// </summary>
    public async Task<T> MeasureAsync<T>(string operationName, Func<Task<T>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;
        
        try
        {
            var result = await operation();
            stopwatch.Stop();
            
            RecordMetric(operationName, stopwatch.ElapsedMilliseconds, true, startTime);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordMetric(operationName, stopwatch.ElapsedMilliseconds, false, startTime, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Measures the execution time of a synchronous operation
    /// </summary>
    public T Measure<T>(string operationName, Func<T> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;
        
        try
        {
            var result = operation();
            stopwatch.Stop();
            
            RecordMetric(operationName, stopwatch.ElapsedMilliseconds, true, startTime);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordMetric(operationName, stopwatch.ElapsedMilliseconds, false, startTime, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Measures agent activation latency
    /// </summary>
    public async Task<TimeSpan> MeasureAgentActivationLatencyAsync<T>(Func<Task<T>> agentOperation)
    {
        var stopwatch = Stopwatch.StartNew();
        await agentOperation();
        stopwatch.Stop();
        
        return stopwatch.Elapsed;
    }

    /// <summary>
    /// Measures multiple agent operations concurrently
    /// </summary>
    public async Task<ConcurrentOperationResult> MeasureConcurrentOperationsAsync<T>(
        string operationName,
        IEnumerable<Func<Task<T>>> operations,
        int maxConcurrency = 10)
    {
        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var results = new ConcurrentBag<OperationResult>();
        var overallStopwatch = Stopwatch.StartNew();
        
        var tasks = operations.Select(async operation =>
        {
            await semaphore.WaitAsync();
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var startTime = DateTime.UtcNow;
                
                try
                {
                    await operation();
                    stopwatch.Stop();
                    results.Add(new OperationResult
                    {
                        Success = true,
                        Duration = stopwatch.Elapsed,
                        StartTime = startTime
                    });
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    results.Add(new OperationResult
                    {
                        Success = false,
                        Duration = stopwatch.Elapsed,
                        StartTime = startTime,
                        Error = ex.Message
                    });
                }
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        await Task.WhenAll(tasks);
        overallStopwatch.Stop();
        
        var resultList = results.ToList();
        return new ConcurrentOperationResult
        {
            OperationName = operationName,
            TotalDuration = overallStopwatch.Elapsed,
            Results = resultList,
            SuccessCount = resultList.Count(r => r.Success),
            FailureCount = resultList.Count(r => !r.Success),
            AverageLatency = resultList.Where(r => r.Success).Average(r => r.Duration.TotalMilliseconds),
            MedianLatency = CalculateMedian(resultList.Where(r => r.Success).Select(r => r.Duration.TotalMilliseconds)),
            P95Latency = CalculatePercentile(resultList.Where(r => r.Success).Select(r => r.Duration.TotalMilliseconds), 95),
            P99Latency = CalculatePercentile(resultList.Where(r => r.Success).Select(r => r.Duration.TotalMilliseconds), 99)
        };
    }

    /// <summary>
    /// Records a performance metric
    /// </summary>
    public void RecordMetric(string operationName, double durationMs, bool success, DateTime startTime, string? error = null)
    {
        var metric = new PerformanceMetric
        {
            OperationName = operationName,
            DurationMs = durationMs,
            Success = success,
            Timestamp = startTime,
            Error = error
        };

        _metrics.AddOrUpdate(operationName, 
            new List<PerformanceMetric> { metric },
            (key, existing) =>
            {
                lock (_lock)
                {
                    existing.Add(metric);
                    return existing;
                }
            });

        _logger.LogDebug("Recorded metric: {OperationName} - {Duration}ms - Success: {Success}",
            operationName, durationMs, success);
    }

    /// <summary>
    /// Gets performance statistics for an operation
    /// </summary>
    public PerformanceStatistics GetStatistics(string operationName)
    {
        if (!_metrics.TryGetValue(operationName, out var metrics))
        {
            return new PerformanceStatistics { OperationName = operationName };
        }

        lock (_lock)
        {
            var successfulMetrics = metrics.Where(m => m.Success).ToList();
            var durations = successfulMetrics.Select(m => m.DurationMs).ToList();

            if (!durations.Any())
            {
                return new PerformanceStatistics
                {
                    OperationName = operationName,
                    TotalOperations = metrics.Count,
                    SuccessfulOperations = 0,
                    FailedOperations = metrics.Count
                };
            }

            return new PerformanceStatistics
            {
                OperationName = operationName,
                TotalOperations = metrics.Count,
                SuccessfulOperations = successfulMetrics.Count,
                FailedOperations = metrics.Count - successfulMetrics.Count,
                AverageLatencyMs = durations.Average(),
                MinLatencyMs = durations.Min(),
                MaxLatencyMs = durations.Max(),
                MedianLatencyMs = CalculateMedian(durations),
                P95LatencyMs = CalculatePercentile(durations, 95),
                P99LatencyMs = CalculatePercentile(durations, 99),
                StandardDeviation = CalculateStandardDeviation(durations),
                SuccessRate = (double)successfulMetrics.Count / metrics.Count
            };
        }
    }

    /// <summary>
    /// Gets all recorded metrics
    /// </summary>
    public Dictionary<string, List<PerformanceMetric>> GetAllMetrics()
    {
        lock (_lock)
        {
            return _metrics.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToList()
            );
        }
    }

    /// <summary>
    /// Clears all recorded metrics
    /// </summary>
    public void ClearMetrics()
    {
        lock (_lock)
        {
            _metrics.Clear();
        }
        _logger.LogInformation("Performance metrics cleared");
    }

    /// <summary>
    /// Generates a performance report
    /// </summary>
    public string GenerateReport()
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Performance Report ===");
        report.AppendLine($"Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        report.AppendLine();

        foreach (var operationName in _metrics.Keys.OrderBy(k => k))
        {
            var stats = GetStatistics(operationName);
            report.AppendLine($"Operation: {stats.OperationName}");
            report.AppendLine($"  Total Operations: {stats.TotalOperations}");
            report.AppendLine($"  Success Rate: {stats.SuccessRate:P2}");
            
            if (stats.SuccessfulOperations > 0)
            {
                report.AppendLine($"  Average Latency: {stats.AverageLatencyMs:F2}ms");
                report.AppendLine($"  Median Latency: {stats.MedianLatencyMs:F2}ms");
                report.AppendLine($"  P95 Latency: {stats.P95LatencyMs:F2}ms");
                report.AppendLine($"  P99 Latency: {stats.P99LatencyMs:F2}ms");
                report.AppendLine($"  Min/Max: {stats.MinLatencyMs:F2}ms / {stats.MaxLatencyMs:F2}ms");
                report.AppendLine($"  Std Deviation: {stats.StandardDeviation:F2}ms");
            }
            
            report.AppendLine();
        }

        return report.ToString();
    }

    private static double CalculateMedian(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(x => x).ToList();
        if (!sorted.Any()) return 0;
        
        var count = sorted.Count;
        if (count % 2 == 0)
        {
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
        }
        return sorted[count / 2];
    }

    private static double CalculatePercentile(IEnumerable<double> values, int percentile)
    {
        var sorted = values.OrderBy(x => x).ToList();
        if (!sorted.Any()) return 0;
        
        var index = (percentile / 100.0) * (sorted.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        
        if (lower == upper) return sorted[lower];
        
        var weight = index - lower;
        return sorted[lower] * (1 - weight) + sorted[upper] * weight;
    }

    private static double CalculateStandardDeviation(IEnumerable<double> values)
    {
        var valueList = values.ToList();
        if (valueList.Count <= 1) return 0;
        
        var average = valueList.Average();
        var sumOfSquares = valueList.Sum(x => Math.Pow(x - average, 2));
        return Math.Sqrt(sumOfSquares / (valueList.Count - 1));
    }
}

/// <summary>
/// Performance metric data
/// </summary>
public class PerformanceMetric
{
    public string OperationName { get; set; } = string.Empty;
    public double DurationMs { get; set; }
    public bool Success { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Performance statistics for an operation
/// </summary>
public class PerformanceStatistics
{
    public string OperationName { get; set; } = string.Empty;
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public int FailedOperations { get; set; }
    public double AverageLatencyMs { get; set; }
    public double MinLatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
    public double MedianLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public double P99LatencyMs { get; set; }
    public double StandardDeviation { get; set; }
    public double SuccessRate { get; set; }
}

/// <summary>
/// Result of a single operation
/// </summary>
public class OperationResult
{
    public bool Success { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime StartTime { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result of concurrent operations
/// </summary>
public class ConcurrentOperationResult
{
    public string OperationName { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public List<OperationResult> Results { get; set; } = new();
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double AverageLatency { get; set; }
    public double MedianLatency { get; set; }
    public double P95Latency { get; set; }
    public double P99Latency { get; set; }
    public double SuccessRate => (double)SuccessCount / (SuccessCount + FailureCount);
} 