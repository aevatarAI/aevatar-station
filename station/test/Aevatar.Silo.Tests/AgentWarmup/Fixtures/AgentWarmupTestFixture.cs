using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Silo.Tests.AgentWarmup.TestAgents;
using Aevatar.Silo.AgentWarmup;
using Aevatar.Silo.AgentWarmup.Extensions;
using Aevatar.Silo.AgentWarmup.Strategies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.TestingHost;
using Xunit;

namespace Aevatar.Silo.Tests.AgentWarmup.Fixtures;

/// <summary>
/// Test fixture for agent warmup testing integrated with Aevatar.Silo.Tests infrastructure
/// </summary>
public class AgentWarmupTestFixture : IAsyncLifetime
{
    private TestCluster? _cluster;

    public TestCluster Cluster => _cluster ?? throw new InvalidOperationException("Cluster not initialized");

    public async Task InitializeAsync()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurator>();
        builder.AddClientBuilderConfigurator<TestClientConfigurator>();
        
        _cluster = builder.Build();
        await _cluster.DeployAsync();
    }

    public async Task DisposeAsync()
    {
        if (_cluster != null)
        {
            await _cluster.StopAllSilosAsync();
            _cluster.Dispose();
        }
    }

    private class TestSiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Orleans:ClusterId", "test-cluster" },
                    { "Orleans:ServiceId", "test-service" },
                    { "Orleans:DataBase", "agent-warmup-test" },
                    { "Orleans:MongoDBClient", "mongodb://localhost:27017" } // Placeholder for config
                })
                .Build();

            siloBuilder
                .ConfigureServices(services =>
                {
                    // Add logging
                    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
                    
                    // Add configuration
                    services.AddSingleton<IConfiguration>(configuration);
                    
                    // Register mock MongoDB services for testing (in-memory)
                    RegisterInMemoryMongoDbServices(services);
                    
                    // Register agent warmup services manually for testing
                    RegisterAgentWarmupServicesForTesting(services);
                })
                // Use memory storage for testing (following existing test infrastructure pattern)
                .UseInMemoryReminderService()
                .AddMemoryGrainStorageAsDefault()
                .AddMemoryGrainStorage("PubSubStore")  // Required for TestStreams
                .AddMemoryGrainStorage("TestStreams")  // Required for TestStreams
                .AddMemoryStreams("TestStreams")
                // Configure logging
                .ConfigureLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Information));
        }
        
        private static void RegisterInMemoryMongoDbServices(IServiceCollection services)
        {
            // Create mock MongoDB client for in-memory testing
            var mockMongoClient = new Mock<IMongoClient>();
            var mockDatabase = new Mock<IMongoDatabase>();
            var mockCollection = new Mock<IMongoCollection<object>>();
            
            // Setup basic MongoDB mocks to avoid null reference exceptions
            mockMongoClient.Setup(x => x.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
                          .Returns(mockDatabase.Object);
            
            mockDatabase.Setup(x => x.GetCollection<object>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
                       .Returns(mockCollection.Object);
            
            // Register as singletons for testing
            services.AddSingleton(mockMongoClient.Object);
            services.AddSingleton(mockDatabase.Object);
        }
        
        private static void RegisterAgentWarmupServicesForTesting(IServiceCollection services)
        {
            // Use the same registration pattern as production code
            // This calls the AddAgentWarmup<Guid> extension method which handles all service registration
            services.AddAgentWarmup<Guid>(options =>
            {
                options.Enabled = true;
                options.MaxConcurrency = 3;
                options.InitialBatchSize = 5;
                options.MaxBatchSize = 10;
                options.DelayBetweenBatchesMs = 100;
                options.AgentActivationTimeoutMs = 5000;
                
                // Configure auto-discovery for test environment
                options.AutoDiscovery.Enabled = true;
                options.AutoDiscovery.RequiredAttributes.Clear(); // Don't require StorageProvider for tests
                options.AutoDiscovery.StorageProviderName = string.Empty; // Don't require specific storage provider
                options.AutoDiscovery.IncludedAssemblies.Add("Aevatar.Silo.Tests"); // Include test assembly
                options.AutoDiscovery.CacheDiscoveredTypes = false; // Don't cache for tests
                
                // Configure default strategy for testing
                options.DefaultStrategy.Enabled = true;
                options.DefaultStrategy.IdentifierSource = "Range"; // Use range instead of MongoDB for tests
                options.DefaultStrategy.MaxIdentifiersPerType = 10; // Limit for testing
                
                // Configure MongoDB integration (even though we won't use it for identifier generation)
                options.MongoDbIntegration.ConnectionString = "mongodb://localhost:27017";
                options.MongoDbIntegration.DatabaseName = "test";
            });
        }
    }

    private class TestClientConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
            clientBuilder
                .AddMemoryStreams("TestStreams")
                .ConfigureServices(services =>
                {
                    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
                });
        }
    }
}

/// <summary>
/// Collection definition for agent warmup tests
/// </summary>
[CollectionDefinition("AgentWarmup")]
public class AgentWarmupTestCollection : ICollectionFixture<AgentWarmupTestFixture>
{
} 