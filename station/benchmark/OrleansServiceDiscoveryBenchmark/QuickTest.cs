using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace OrleansServiceDiscoveryBenchmark;

public class QuickTest
{
    public static async Task RunAsync()
    {
        Console.WriteLine("🧪 Quick ZooKeeper test");
        
        try
        {
            var host = new HostBuilder()
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
                .Build();

            Console.WriteLine("Starting Orleans Silo...");
            await host.StartAsync();
            Console.WriteLine("✅ Silo started successfully!");
            
            await Task.Delay(3000);
            
            Console.WriteLine("Stopping Silo...");
            await host.StopAsync();
            Console.WriteLine("✅ Test completed!");
            
            host.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
} 