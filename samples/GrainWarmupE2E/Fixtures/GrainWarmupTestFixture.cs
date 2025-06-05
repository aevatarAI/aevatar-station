using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Streams.Kafka.Config;
using E2E.Grains;

namespace GrainWarmupE2E.Fixtures;

/// <summary>
/// Test fixture for Orleans client connecting to a real silo
/// This connects to an existing silo instead of creating an in-memory cluster
/// </summary>
public class GrainWarmupTestFixture : IAsyncDisposable
{
    private IHost? _host;
    private IClusterClient? _client;
    private readonly ILogger<GrainWarmupTestFixture> _logger;

    public GrainWarmupTestFixture()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<GrainWarmupTestFixture>();
    }

    public IClusterClient Client => _client ?? throw new InvalidOperationException("Client not initialized");
    public IGrainFactory GrainFactory => Client;

    public async Task InitializeAsync()
    {
        if (_host != null)
            return; // Already initialized

        _logger.LogInformation("Initializing Orleans client to connect to real silo...");

        var hostBuilder = Host.CreateDefaultBuilder()
            .UseOrleansClient(client =>
            {
                var hostId = "Aevatar";
                client.UseMongoDBClient("mongodb://localhost:27017/?maxPoolSize=15000")
                    .UseMongoDBClustering(options =>
                    {
                        options.DatabaseName = "AevatarDb";
                        options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                        options.CollectionPrefix = hostId.IsNullOrEmpty() ? "OrleansAevatar" : $"Orleans{hostId}";
                    })
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "AevatarSiloCluster";
                        options.ServiceId = "AevatarBasicService";
                    })
                    .AddActivityPropagation()
                    .AddKafka("Aevatar")
                    .WithOptions(options =>
                    {
                        options.BrokerList = new List<string> { "localhost:9092" };
                        options.ConsumerGroupId = "Aevatar";
                        options.ConsumeMode = ConsumeMode.LastCommittedMessage;

                        var partitions = 1;
                        var replicationFactor = (short)1;
                        var topics = "Aevatar,AevatarStateProjection,AevatarBroadCast";
                        foreach (var topic in topics.Split(','))
                        {
                            options.AddTopic(topic.Trim(), new TopicCreationConfig
                            {
                                AutoCreate = true,
                                Partitions = partitions,
                                ReplicationFactor = replicationFactor
                            });
                        }
                    })
                    .AddJson()
                    .Build();
            })
            .ConfigureLogging(logging => 
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

        _host = hostBuilder.Build();
        
        await _host.StartAsync();
        _client = _host.Services.GetRequiredService<IClusterClient>();
        
        _logger.LogInformation("Orleans client connected successfully to real silo");
    }

    public async ValueTask DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    /// <summary>
    /// Gets a test warmup grain instance
    /// </summary>
    public ITestWarmupGrain GetTestGrain(Guid grainId)
    {
        return GrainFactory.GetGrain<ITestWarmupGrain>(grainId);
    }

    /// <summary>
    /// Generates test grain IDs with optional prefix
    /// </summary>
    public static List<Guid> GenerateTestGrainIds(int count, string? prefix = null)
    {
        var grainIds = new List<Guid>();
        
        for (var i = 0; i < count; i++)
        {
            var seedValue = prefix != null ? $"{prefix}_{i}" : i.ToString();
            var grainId = GenerateTestGuidFromSeed(seedValue);
            grainIds.Add(grainId);
        }
        
        return grainIds;
    }

    /// <summary>
    /// Generates a deterministic GUID from a seed value
    /// </summary>
    private static Guid GenerateTestGuidFromSeed(string seed)
    {
        using var sha1 = System.Security.Cryptography.SHA1.Create();
        var hash = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(seed));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    /// <summary>
    /// Waits for grain activations to complete within specified timeout
    /// </summary>
    public async Task WaitForGrainActivationsAsync(List<Guid> grainIds, TimeSpan timeout)
    {
        var tasks = grainIds.Select(async grainId =>
        {
            var grain = GetTestGrain(grainId);
            // Activate grain by calling a method
            await grain.PingAsync();
        });

        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await Task.WhenAll(tasks).WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Timeout waiting for {grainIds.Count} grain activations after {timeout}");
        }
    }
} 