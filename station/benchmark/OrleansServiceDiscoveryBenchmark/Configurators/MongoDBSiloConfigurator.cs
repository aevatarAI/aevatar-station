using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.TestingHost;
using MongoDB.Driver;

namespace OrleansServiceDiscoveryBenchmark.Configurators;

public class MongoDBSiloConfigurator : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder
            .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning))
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
            .AddMongoDBGrainStorage("Default", options =>
            {
                options.CollectionPrefix = "OrleansGrains";
                options.DatabaseName = "OrleansServiceDiscoveryBenchmark";
            })
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "OrleansMongoCluster";
                options.ServiceId = "OrleansServiceDiscoveryBenchmark";
            })
            .Configure<SiloOptions>(options =>
            {
                options.SiloName = $"MongoSilo_{Environment.MachineName}_{Guid.NewGuid():N}";
            })
;
    }
} 