using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.TestingHost;
using MongoDB.Driver;

namespace OrleansServiceDiscoveryBenchmark.Configurators;

public class MongoDBClientConfigurator : IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
    {
        clientBuilder
            .UseMongoDBClient(provider =>
            {
                var connectionString = "mongodb://localhost:27017";
                var settings = MongoClientSettings.FromConnectionString(connectionString);
                settings.MaxConnectionPoolSize = 100;
                settings.MinConnectionPoolSize = 10;
                // settings.WaitQueueSize = 500; // Obsolete property
                settings.WaitQueueTimeout = TimeSpan.FromMinutes(1);
                settings.MaxConnecting = 4;
                return settings;
            })
            .UseMongoDBClustering(options =>
            {
                options.DatabaseName = "OrleansServiceDiscoveryBenchmark";
                options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                options.CollectionPrefix = "OrleansCluster";
            })
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "OrleansMongoCluster";
                options.ServiceId = "OrleansServiceDiscoveryBenchmark";
            })
;
    }
} 