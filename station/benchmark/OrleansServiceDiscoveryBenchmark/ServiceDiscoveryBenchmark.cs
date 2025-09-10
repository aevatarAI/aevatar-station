using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Runtime;
using Orleans.TestingHost;
using MongoDB.Driver;
using EphemeralMongo;
using E2E.Grains;
using System.Diagnostics;
using Serilog;
using OrleansServiceDiscoveryBenchmark.Configurators;
using MsftHost = Microsoft.Extensions.Hosting.IHost;
using org.apache.zookeeper;
using ILogger = Serilog.ILogger;

namespace OrleansServiceDiscoveryBenchmark;

[Config(typeof(BenchmarkConfig))]
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MarkdownExporter]
[HtmlExporter]
[JsonExporter]
public class ServiceDiscoveryBenchmark
{
    private MsftHost? _mongoHost;
    private MsftHost? _zooKeeperHost;
    private IClusterClient? _mongoClient;
    private IClusterClient? _zooKeeperClient;
    private IMongoRunner? _mongoRunner;
    private int _activeAgentCount;
    private readonly object _lockObject = new object();
    private const string _zooKeeperConnectionString = "localhost:2181";
    private const int _zooKeeperTimeout = 30000; // 30 seconds
    private const int _maxRetries = 3;
    private const int _retryDelay = 2000; // 2 seconds
    
    private const int TestIterations = 10;
    private const int StabilizationDelay = 3000; // 3 seconds - increased for ZooKeeper stability

    [IterationSetup]
    public void IterationSetup()
    {
        _activeAgentCount = 0;
        Log.Information("Starting new iteration. Active agents reset to 0.");
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        // Run cleanup synchronously
        Task.Run(async () =>
        {
            var count = Interlocked.Exchange(ref _activeAgentCount, 0);
            if (count > 0)
            {
                Log.Information($"Cleaning up {count} agents");
                // Allow some time for cleanup
                await Task.Delay(200);
            }
            // Add small delay between iterations to avoid agent activation clustering
            await Task.Delay(100);
        }).Wait(); // Wait for the async operations to complete
    }

    private async Task CleanupAgents()
    {
        var count = Interlocked.Exchange(ref _activeAgentCount, 0);
        if (count > 0)
        {
            Log.Information($"Cleaning up {count} agents");
            // Allow some time for cleanup
            await Task.Delay(200);
        }
    }

    [GlobalSetup]
    public async Task Setup()
    {
        Log.Information("🚀 Starting benchmark test environment setup");
        
        // Reset ZooKeeper cluster ID to ensure each test uses a new cluster ID
        ZooKeeperClusterIdProvider.ResetClusterId();
        
        // Setup logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        // Setup MongoDB
        var mongoRunnerOptions = new MongoRunnerOptions
        {
            UseSingleNodeReplicaSet = false
        };
        _mongoRunner = MongoRunner.Run(mongoRunnerOptions);

        // Verify ZooKeeper connection (Orleans will auto-create paths)
        try
        {
            await VerifyZooKeeperConnection();
            Log.Information("ZooKeeper connection verified successfully");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to verify ZooKeeper connection, ZooKeeper tests may fail");
        }

        await SetupMongoClusterAsync();
        
        // Only setup ZooKeeper cluster if path initialization succeeded
        try
        {
            await SetupZooKeeperClusterAsync();
            Log.Information("ZooKeeper cluster setup completed");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to setup ZooKeeper cluster, ZooKeeper benchmarks will be skipped");
        }
    }

    private class ZooKeeperWatcher : Watcher
    {
        public override Task process(WatchedEvent @event)
        {
            Log.Debug($"ZooKeeper event: {@event.getState()} {@event.getPath()}");
            return Task.CompletedTask;
        }
    }

    private async Task VerifyZooKeeperConnection()
    {
        ZooKeeper? zk = null;
        try
        {
            zk = new ZooKeeper(_zooKeeperConnectionString, _zooKeeperTimeout, new ZooKeeperWatcher());
            
            // Wait for connection
            var start = DateTime.UtcNow;
            while (zk.getState() != ZooKeeper.States.CONNECTED)
            {
                if (DateTime.UtcNow - start > TimeSpan.FromMilliseconds(_zooKeeperTimeout))
                {
                    throw new TimeoutException("Failed to connect to ZooKeeper");
                }
                await Task.Delay(100);
            }

            Log.Information($"Successfully connected to ZooKeeper at {_zooKeeperConnectionString}");
            Log.Information($"ZooKeeper state: {zk.getState()}");
        }
        finally
        {
            if (zk != null)
            {
                await zk.closeAsync();
            }
        }
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        try
        {
            // Final cleanup of any remaining agents
            await CleanupAgents();
            Log.Information("Performing final cleanup in GlobalCleanup");

            // Cleanup MongoDB cluster
            if (_mongoClient != null && _mongoClient is IDisposable disposableMongoClient)
            {
                disposableMongoClient.Dispose();
                Log.Information("Disposed MongoDB client");
            }
            
            if (_mongoHost != null)
            {
                await _mongoHost.StopAsync();
                if (_mongoHost is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else if (_mongoHost is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                Log.Information("Stopped and disposed MongoDB host");
            }
            
            // Cleanup ZooKeeper cluster
            if (_zooKeeperClient != null && _zooKeeperClient is IDisposable disposableZooKeeperClient)
            {
                disposableZooKeeperClient.Dispose();
                Log.Information("Disposed ZooKeeper client");
            }
            
            if (_zooKeeperHost != null)
            {
                await _zooKeeperHost.StopAsync();
                if (_zooKeeperHost is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else if (_zooKeeperHost is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                Log.Information("Stopped and disposed ZooKeeper host");
            }
            
            // Stop external services
            if (_mongoRunner != null)
            {
                try
                {
                    _mongoRunner.Dispose();
                    Log.Information("Disposed MongoDB runner");
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error disposing MongoDB runner");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during cleanup");
            throw;
        }
        finally
        {
            // Ensure we log the final agent count before closing
            Log.Information($"Final active agent count before shutdown: {_activeAgentCount}");
            Log.CloseAndFlush();
        }
    }

    [Benchmark(Description = "MongoDB Service Discovery - Cluster Startup")]
    public async Task<TimeSpan> MongoDBClusterStartup()
    {
        var stopwatch = Stopwatch.StartNew();
        MsftHost? host = null;
        
        try
        {
            var builder = ClusterConfigurator.CreateHostBuilder(Array.Empty<string>(), "MongoDB");
            host = builder.Build();
            await host.StartAsync();
            
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during MongoDB cluster startup");
            throw;
        }
        finally
        {
            if (host != null)
            {
                try
                {
                    await host.StopAsync();
                    if (host is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else if (host is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during host cleanup");
                }
            }
        }
    }

    [Benchmark(Description = "ZooKeeper Service Discovery - Cluster Startup")]
    public async Task<TimeSpan> ZooKeeperClusterStartup()
    {
        var stopwatch = Stopwatch.StartNew();
        MsftHost? host = null;
        var iterationId = Guid.NewGuid().ToString("N").Substring(0, 8);
        
        try
        {
            Log.Information($"Starting ZooKeeper cluster startup iteration {iterationId}");
            
            var builder = ClusterConfigurator.CreateHostBuilder(Array.Empty<string>(), "ZooKeeper");
            host = builder.Build();
            await host.StartAsync();
            
            // Wait for stabilization
            await Task.Delay(StabilizationDelay);
            
            stopwatch.Stop();
            Log.Information($"Completed ZooKeeper cluster startup iteration {iterationId} in {stopwatch.ElapsedMilliseconds}ms");
            return stopwatch.Elapsed;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error during ZooKeeper cluster startup iteration {iterationId}");
            stopwatch.Stop();
            return stopwatch.Elapsed; // Return partial time instead of throwing
        }
        finally
        {
            if (host != null)
            {
                try
                {
                    await host.StopAsync();
                    if (host is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else if (host is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    Log.Information($"Cleaned up host for iteration {iterationId}");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error during host cleanup for iteration {iterationId}");
                }
            }
        }
    }

    [Benchmark(Description = "MongoDB Service Discovery - Grain Calls")]
    public async Task<TimeSpan> MongoDBGrainCalls()
    {
        if (_mongoClient == null) throw new InvalidOperationException("MongoDB client not initialized");
        
        var stopwatch = Stopwatch.StartNew();
        var iterationId = Guid.NewGuid().ToString("N").Substring(0, 8);
        Log.Information($"Starting MongoDB grain calls iteration {iterationId}");
        
        try
        {
            var tasks = new List<Task>();
            
            for (int i = 0; i < TestIterations; i++)
            {
                var grain = _mongoClient.GetGrain<ITestWarmupAgent>(Guid.NewGuid());
                tasks.Add(grain.PingAsync());
                Interlocked.Increment(ref _activeAgentCount);
                Log.Debug($"Created agent {i + 1}/{TestIterations} in iteration {iterationId}");
            }
            
            await Task.WhenAll(tasks);
            
            stopwatch.Stop();
            Log.Information($"Completed MongoDB grain calls iteration {iterationId} with {_activeAgentCount} agents in {stopwatch.ElapsedMilliseconds}ms");
            return stopwatch.Elapsed;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error during MongoDB grain calls iteration {iterationId}");
            throw;
        }
    }

    [Benchmark(Description = "ZooKeeper Service Discovery - Grain Calls")]
    public async Task<TimeSpan> ZooKeeperGrainCalls()
    {
        if (_zooKeeperClient == null) 
        {
            Log.Warning("ZooKeeper client not initialized, skipping test");
            return TimeSpan.Zero;
        }
        
        var stopwatch = Stopwatch.StartNew();
        var iterationId = Guid.NewGuid().ToString("N").Substring(0, 8);
        Log.Information($"Starting ZooKeeper grain calls iteration {iterationId}");
        
        try
        {
            var tasks = new List<Task>();
            
            for (int i = 0; i < TestIterations; i++)
            {
                var grain = _zooKeeperClient.GetGrain<ITestWarmupAgent>(Guid.NewGuid());
                tasks.Add(grain.PingAsync());
                Interlocked.Increment(ref _activeAgentCount);
                Log.Debug($"Created agent {i + 1}/{TestIterations} in iteration {iterationId}");
            }
            
            await Task.WhenAll(tasks);
            
            stopwatch.Stop();
            Log.Information($"Completed ZooKeeper grain calls iteration {iterationId} with {_activeAgentCount} agents in {stopwatch.ElapsedMilliseconds}ms");
            return stopwatch.Elapsed;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error during ZooKeeper grain calls iteration {iterationId}");
            return TimeSpan.Zero;
        }
    }

    [Benchmark(Description = "MongoDB Service Discovery - Silo Join/Leave")]
    public async Task<TimeSpan> MongoDBSiloJoinLeave()
    {
        var stopwatch = Stopwatch.StartNew();
        MsftHost? host = null;
        var iterationId = Guid.NewGuid().ToString("N").Substring(0, 8);
        
        try
        {
            Log.Information($"Starting MongoDB silo join/leave iteration {iterationId}");
            var builder = ClusterConfigurator.CreateHostBuilder(Array.Empty<string>(), "MongoDB");
            host = builder.Build();
            await host.StartAsync();
            
            // Wait for stabilization
            await Task.Delay(StabilizationDelay);
            
            stopwatch.Stop();
            Log.Information($"Completed MongoDB silo join/leave iteration {iterationId} in {stopwatch.ElapsedMilliseconds}ms");
            return stopwatch.Elapsed;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error during MongoDB silo join/leave iteration {iterationId}");
            throw;
        }
        finally
        {
            if (host != null)
            {
                try
                {
                    await host.StopAsync();
                    if (host is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else if (host is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    Log.Information($"Cleaned up host for iteration {iterationId}");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error during host cleanup for iteration {iterationId}");
                }
            }
        }
    }

    [Benchmark(Description = "ZooKeeper Service Discovery - Silo Join/Leave")]
    public async Task<TimeSpan> ZooKeeperSiloJoinLeave()
    {
        var stopwatch = Stopwatch.StartNew();
        MsftHost? host = null;
        var iterationId = Guid.NewGuid().ToString("N").Substring(0, 8);
        
        try
        {
            Log.Information($"Starting ZooKeeper silo join/leave iteration {iterationId}");
            var builder = ClusterConfigurator.CreateHostBuilder(Array.Empty<string>(), "ZooKeeper");
            host = builder.Build();
            await host.StartAsync();
            
            // Wait for stabilization
            await Task.Delay(StabilizationDelay);
            
            stopwatch.Stop();
            Log.Information($"Completed ZooKeeper silo join/leave iteration {iterationId} in {stopwatch.ElapsedMilliseconds}ms");
            return stopwatch.Elapsed;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error during ZooKeeper silo join/leave iteration {iterationId}");
            stopwatch.Stop();
            return stopwatch.Elapsed; // Return partial time instead of throwing
        }
        finally
        {
            if (host != null)
            {
                try
                {
                    await host.StopAsync();
                    if (host is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else if (host is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    Log.Information($"Cleaned up host for iteration {iterationId}");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error during host cleanup for iteration {iterationId}");
                }
            }
        }
    }

    private async Task SetupMongoClusterAsync()
    {
        var builder = ClusterConfigurator.CreateHostBuilder(Array.Empty<string>(), "MongoDB");
        _mongoHost = builder.Build();
        await _mongoHost.StartAsync();
        _mongoClient = _mongoHost.Services.GetRequiredService<IClusterClient>();
    }

    private async Task SetupZooKeeperClusterAsync()
    {
        var builder = ClusterConfigurator.CreateHostBuilder(Array.Empty<string>(), "ZooKeeper");
        _zooKeeperHost = builder.Build();
        await _zooKeeperHost.StartAsync();
        _zooKeeperClient = _zooKeeperHost.Services.GetRequiredService<IClusterClient>();
    }
}

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default
            .WithId("Orleans Service Discovery Benchmark")
            .WithLaunchCount(1)
            .WithIterationCount(5)
            .WithWarmupCount(2)
            .WithInvocationCount(1)
            .WithUnrollFactor(1)
            .WithStrategy(RunStrategy.Throughput));

        WithOptions(ConfigOptions.DisableOptimizationsValidator);
    }
} 