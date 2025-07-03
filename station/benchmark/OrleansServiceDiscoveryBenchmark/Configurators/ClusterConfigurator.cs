using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using MongoDB.Driver;
using Serilog;
using System.Net;

namespace OrleansServiceDiscoveryBenchmark.Configurators;

public static class ClusterConfigurator
{
    private static int _instanceCounter = 0;

    public static IHostBuilder CreateHostBuilder(string[] args, string clusterType)
    {
        var instanceId = Interlocked.Increment(ref _instanceCounter);
        var siloName = $"{clusterType}Silo_{Environment.MachineName}_{DateTime.UtcNow:yyyyMMddHHmmss}_{instanceId}";
        
        // Create unique cluster identifier for each test instance, including timestamp and random number
        var uniqueClusterId = $"{clusterType.ToLower()}-cluster-{DateTime.UtcNow:yyyyMMddHHmmss}-{instanceId}-{Guid.NewGuid().ToString("N")[..8]}";

        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // ClusterOptions will be configured in Orleans section to avoid conflicts
            })
            .UseOrleans((context, siloBuilder) =>
            {
                siloBuilder.ConfigureLogging(logging => logging.AddFilter("Orleans", LogLevel.Warning));

                if (clusterType == "MongoDB")
                {
                    var dbName = $"OrleansServiceDiscoveryBenchmark_{instanceId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                    siloBuilder
                        .UseMongoDBClient(provider =>
                        {
                            var connectionString = "mongodb://localhost:27017";
                            var settings = MongoClientSettings.FromConnectionString(connectionString);
                            settings.MaxConnectionPoolSize = 100;
                            settings.MinConnectionPoolSize = 10;
                            settings.WaitQueueTimeout = TimeSpan.FromMinutes(1);
                            settings.MaxConnecting = 4;
                            return settings;
                        })
                        .UseMongoDBClustering(options =>
                        {
                            options.DatabaseName = dbName;
                            options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                            options.CollectionPrefix = $"OrleansCluster_{instanceId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                            options.CreateShardKeyForCosmos = false;
                        })
                        .AddMongoDBGrainStorage("Default", options =>
                        {
                            options.CollectionPrefix = $"OrleansGrains_{instanceId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                            options.DatabaseName = dbName;
                            options.CreateShardKeyForCosmos = false;
                        })
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = uniqueClusterId;
                            options.ServiceId = "MongoDBDemoService";
                        });
                        
                    // Configure endpoints with unique ports for MongoDB
                    siloBuilder.ConfigureEndpoints(
                        advertisedIP: IPAddress.Loopback, // Force use of 127.0.0.1
                        siloPort: 11111 + (instanceId * 2),
                        gatewayPort: 30000 + (instanceId * 2),
                        listenOnAnyHostAddress: false); // Use localhost only
                }
                else
                {
                    siloBuilder
                        .UseZooKeeperClustering(options =>
                        {
                            options.ConnectionString = "localhost:2181";
                            // ZooKeeper will use unique ClusterId to isolate different test instances
                            // Each cluster will have independent path in ZooKeeper: /orleans/{ClusterId}
                        })
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = uniqueClusterId;
                            options.ServiceId = "ZooKeeperDemoService";
                        })
                        // Enhanced ZooKeeper cluster stability configuration
                        .Configure<ClusterMembershipOptions>(options =>
                        {
                            options.DefunctSiloCleanupPeriod = TimeSpan.FromMinutes(1); // Period to clean up defunct silos
                            options.DefunctSiloExpiration = TimeSpan.FromMinutes(2); // Expiration time for defunct silos
                            options.IAmAliveTablePublishTimeout = TimeSpan.FromSeconds(30); // Heartbeat publish timeout
                            options.MaxJoinAttemptTime = TimeSpan.FromSeconds(30); // Maximum join attempt time
                            options.ProbeTimeout = TimeSpan.FromSeconds(10); // Probe timeout
                            options.TableRefreshTimeout = TimeSpan.FromSeconds(30); // Table refresh timeout
                            options.DeathVoteExpirationTimeout = TimeSpan.FromMinutes(2); // Death vote expiration timeout
                            options.NumMissedProbesLimit = 2; // Maximum number of missed probes
                            options.NumProbedSilos = 2; // Number of silos to probe
                            options.NumVotesForDeathDeclaration = 1; // Number of votes required to declare death
                            options.UseLivenessGossip = false; // Disable gossip to reduce network noise
                        })
                        .AddMemoryGrainStorage("Default");
                        
                    // Configure endpoints with unique ports for ZooKeeper
                    siloBuilder.ConfigureEndpoints(
                        advertisedIP: IPAddress.Loopback, // Force use of 127.0.0.1, avoid using external IP
                        siloPort: 12111 + (instanceId * 2), // Use different range from MongoDB
                        gatewayPort: 31000 + (instanceId * 2), // Use different range from MongoDB
                        listenOnAnyHostAddress: false); // Use localhost only
                }

                siloBuilder.Configure<SiloOptions>(options => { options.SiloName = siloName; });
            })
            .UseSerilog((context, loggerConfig) =>
            {
                loggerConfig
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .Enrich.WithProperty("SiloName", siloName)
                    .Enrich.WithProperty("InstanceId", instanceId);
            });
    }
}