using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.TestingHost;

namespace OrleansServiceDiscoveryBenchmark.Configurators;

public class ZooKeeperClientConfigurator : IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
    {
        // Use shared cluster identifier
        var clusterId = ZooKeeperClusterIdProvider.GetClusterId();
        
        clientBuilder
            .UseZooKeeperClustering(options =>
            {
                options.ConnectionString = "localhost:2181";
            })
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = clusterId;
                options.ServiceId = "ZooKeeperDemoService";
            });
    }
} 