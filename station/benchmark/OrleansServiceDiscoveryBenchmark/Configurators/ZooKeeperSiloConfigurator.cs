using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace OrleansServiceDiscoveryBenchmark.Configurators;

public class ZooKeeperSiloConfigurator : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        // Use shared cluster identifier
        var clusterId = ZooKeeperClusterIdProvider.GetClusterId();
        
        siloBuilder
            .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning))
            .UseZooKeeperClustering(options =>
            {
                options.ConnectionString = "localhost:2181";
            })
            .AddMemoryGrainStorage("Default")
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = clusterId;
                options.ServiceId = "ZooKeeperDemoService";
            })
            .Configure<SiloOptions>(options =>
            {
                options.SiloName = $"ZooKeeperSilo_{Environment.MachineName}_{Guid.NewGuid():N}";
            })
            // Enhanced ZooKeeper cluster stability configuration
            .Configure<ClusterMembershipOptions>(options =>
            {
                options.DefunctSiloCleanupPeriod = TimeSpan.FromMinutes(1);
                options.DefunctSiloExpiration = TimeSpan.FromMinutes(2);
                options.IAmAliveTablePublishTimeout = TimeSpan.FromSeconds(30);
                options.MaxJoinAttemptTime = TimeSpan.FromSeconds(30);
                options.ProbeTimeout = TimeSpan.FromSeconds(10);
                options.TableRefreshTimeout = TimeSpan.FromSeconds(30);
                options.DeathVoteExpirationTimeout = TimeSpan.FromMinutes(2);
                options.NumMissedProbesLimit = 2;
                options.NumProbedSilos = 2;
                options.NumVotesForDeathDeclaration = 1;
                options.UseLivenessGossip = false;
            });
    }
} 