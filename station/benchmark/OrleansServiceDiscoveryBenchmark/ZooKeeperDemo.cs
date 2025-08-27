using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using E2E.Grains;

namespace OrleansServiceDiscoveryBenchmark;

/// <summary>
/// Orleans ZooKeeper standalone demo program
/// Used for testing and validating Orleans integration with ZooKeeper
/// </summary>
public class ZooKeeperDemo
{
    private readonly ILogger<ZooKeeperDemo> _logger;
    
    public ZooKeeperDemo()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = loggerFactory.CreateLogger<ZooKeeperDemo>();
    }

    /// <summary>
    /// Run Orleans ZooKeeper demonstration
    /// </summary>
    public async Task RunAsync()
    {
        _logger.LogInformation("🚀 Starting Orleans ZooKeeper demonstration...");

        try
        {
            // Test 1: Start single Silo
            await TestSingleSilo();
            
            // Test 2: Start multiple Silo cluster
            await TestSiloCluster();
            
            // Test 3: Client connection test
            await TestClientConnection();
            
            _logger.LogInformation("✅ Orleans ZooKeeper demonstration completed!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Orleans ZooKeeper demonstration failed");
            throw;
        }
    }

    /// <summary>
    /// Test single Silo startup
    /// </summary>
    private async Task TestSingleSilo()
    {
        _logger.LogInformation("📍 Test 1: Single Silo startup");

        var siloHost = new HostBuilder()
            .UseOrleans(siloBuilder =>
            {
                siloBuilder
                    .UseLocalhostClustering(siloPort: 11111, gatewayPort: 30000)
                    .UseZooKeeperClustering(options =>
                    {
                        options.ConnectionString = "localhost:2181";
                    })
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "orleans-zk-demo";
                        options.ServiceId = "ZooKeeperDemoService";
                    })
                    .ConfigureLogging(logging => 
                        logging.AddConsole().SetMinimumLevel(LogLevel.Information));
            })
            .ConfigureServices(services =>
            {
                services.AddLogging(builder => 
                    builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            })
            .Build();

        _logger.LogInformation("Starting Silo...");
        await siloHost.StartAsync();
        
        _logger.LogInformation("✅ Silo started successfully! Waiting for 5 seconds...");
        await Task.Delay(5000);
        
        _logger.LogInformation("Stopping Silo...");
        await siloHost.StopAsync();
        siloHost.Dispose();
        
        _logger.LogInformation("✅ Single Silo test completed");
    }

    /// <summary>
    /// Test multiple Silo cluster
    /// </summary>
    private async Task TestSiloCluster()
    {
        _logger.LogInformation("📍 Test 2: Multiple Silo cluster");

        var silo1 = CreateSilo(11111, 30000, "Silo-1");
        var silo2 = CreateSilo(11112, 30001, "Silo-2");

        try
        {
            _logger.LogInformation("Starting Silo-1...");
            await silo1.StartAsync();
            await Task.Delay(3000);

            _logger.LogInformation("Starting Silo-2...");
            await silo2.StartAsync();
            await Task.Delay(3000);

            _logger.LogInformation("✅ Cluster started successfully! Waiting for 10 seconds...");
            await Task.Delay(10000);
        }
        finally
        {
            _logger.LogInformation("Stopping cluster...");
            await Task.WhenAll(silo1.StopAsync(), silo2.StopAsync());
            silo1.Dispose();
            silo2.Dispose();
        }
        
        _logger.LogInformation("✅ Multiple Silo cluster test completed");
    }

    /// <summary>
    /// Test client connection
    /// </summary>
    private async Task TestClientConnection()
    {
        _logger.LogInformation("📍 Test 3: Client connection");

        // Start a Silo for client testing
        var silo = CreateSilo(11113, 30002, "Client-Test-Silo");
        
        try
        {
            _logger.LogInformation("Starting test Silo...");
            await silo.StartAsync();
            await Task.Delay(3000);

            // Create client
            var clientHost = new HostBuilder()
                .UseOrleansClient(clientBuilder =>
                {
                    clientBuilder
                        .UseZooKeeperClustering(options =>
                        {
                            options.ConnectionString = "localhost:2181";
                        })
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "orleans-zk-demo";
                            options.ServiceId = "ZooKeeperDemoService";
                        });
                })
                .ConfigureLogging(logging => 
                    logging.AddConsole().SetMinimumLevel(LogLevel.Information))
                .Build();
            
            await clientHost.StartAsync();
            var client = clientHost.Services.GetRequiredService<IClusterClient>();

            _logger.LogInformation("Testing Grain call...");
            var testGrain = client.GetGrain<ITestGrain>(0);
            var result = await testGrain.GetAsync();
            _logger.LogInformation($"Grain call result: {result}");

            _logger.LogInformation("Closing client...");
            await clientHost.StopAsync();
            clientHost.Dispose();
        }
        finally
        {
            _logger.LogInformation("Stopping test Silo...");
            await silo.StopAsync();
            silo.Dispose();
        }
        
        _logger.LogInformation("✅ Client connection test completed");
    }

    /// <summary>
    /// Create Silo Host
    /// </summary>
    private IHost CreateSilo(int siloPort, int gatewayPort, string siloName)
    {
        return new HostBuilder()
            .UseOrleans(siloBuilder =>
            {
                siloBuilder
                    .UseLocalhostClustering(siloPort: siloPort, gatewayPort: gatewayPort)
                    .UseZooKeeperClustering(options =>
                    {
                        options.ConnectionString = "localhost:2181";
                    })
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "orleans-zk-demo";
                        options.ServiceId = "ZooKeeperDemoService";
                    })
                    .Configure<SiloOptions>(options =>
                    {
                        options.SiloName = siloName;
                    })
                    .ConfigureLogging(logging => 
                        logging.AddConsole().SetMinimumLevel(LogLevel.Information));
            })
            .ConfigureServices(services =>
            {
                services.AddLogging(builder => 
                    builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            })
            .Build();
    }
}

/// <summary>
/// Simple test Grain interface
/// </summary>
public interface ITestGrain : IGrainWithIntegerKey
{
    Task<string> GetAsync();
    Task SetAsync(string value);
}

/// <summary>
/// Simple test Grain implementation
/// </summary>
public class TestGrain : Grain, ITestGrain
{
    private string _value = "Hello from Orleans ZooKeeper!";

    public Task<string> GetAsync()
    {
        return Task.FromResult($"{_value} - Silo: {RuntimeIdentity}");
    }

    public Task SetAsync(string value)
    {
        _value = value;
        return Task.CompletedTask;
    }
} 