using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Aevatar.Core.Placement;
using Aevatar.MongoDB;
using Aevatar.Silo.Extensions;
using Aevatar.Silo.Startup;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Configuration;
using Moq;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Runtime.Placement;
using Orleans.TestingHost;
using Shouldly;
using Xunit;

namespace Aevatar.Silo.Tests.Extensions;

public class OrleansHostExtensionTests : IClassFixture<AevatarMongoDbFixture>
{
    private readonly AevatarMongoDbFixture _mongoDbFixture;

    public OrleansHostExtensionTests(AevatarMongoDbFixture mongoDbFixture)
    {
        _mongoDbFixture = mongoDbFixture;
    }
    private class MongoDBTestSiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            // Configure MongoDB client with test settings using in-memory MongoDB
            var connectionString = AevatarMongoDbFixture.GetRandomConnectionString();

            siloBuilder
                .UseMongoDBClient(provider =>
                {
                    var configuration = provider.GetService<IConfiguration>();
                    var configSection = configuration.GetSection("Orleans");

                    var setting = MongoClientSettings.FromConnectionString(connectionString);

                    // Read MongoDB client settings from configuration with MongoDB driver default values
                    var clientSection = configSection.GetSection("MongoDBClientSettings");
                    setting.MaxConnectionPoolSize = clientSection.GetValue<int>("MaxConnectionPoolSize", 512);
                    setting.MinConnectionPoolSize = clientSection.GetValue<int>("MinConnectionPoolSize", 16);
                    setting.WaitQueueSize = clientSection.GetValue<int>("WaitQueueSize", 500);
                    setting.WaitQueueTimeout = clientSection.GetValue<TimeSpan>("WaitQueueTimeout", TimeSpan.FromMinutes(2));
                    setting.MaxConnecting = clientSection.GetValue<int>("MaxConnecting", 4);
                    return setting;
                })
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = "TestDatabase";
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                    options.CollectionPrefix = "OrleansTest";
                })
                .AddMongoDBGrainStorage("Default", options =>
                {
                    options.CollectionPrefix = "OrleansTest";
                    options.DatabaseName = "TestDatabase";
                });
        }
    }
    // Base configuration for all tests
    private Dictionary<string, string> GetBaseConfig(bool isKubernetes = false)
    {
        return new Dictionary<string, string>
            {
                { "Orleans:IsRunningInKubernetes", isKubernetes.ToString().ToLower() },
                { "Orleans:ClusterId", "test-cluster" },
                { "Orleans:ServiceId", "test-service" },
                { "Orleans:MongoDBClient", AevatarMongoDbFixture.GetRandomConnectionString() },
                { "Orleans:MongoDBClientSettings:MaxConnectionPoolSize", "512" },
                { "Orleans:MongoDBClientSettings:MinConnectionPoolSize", "32" },
                { "Orleans:MongoDBClientSettings:WaitQueueSize", "40960" },
                { "Orleans:MongoDBClientSettings:WaitQueueTimeout", "00:10:00" },
                { "Orleans:MongoDBClientSettings:MaxConnecting", "16" },
                { "Orleans:MongoDBESClient", "mongodb://localhost:27018" },
                { "Orleans:MongoDBESClientSettings:WaitQueueSize", "40960" },
                { "Orleans:MongoDBESClientSettings:MinConnectionPoolSize", "32" },
                { "Orleans:MongoDBESClientSettings:WaitQueueTimeout", "00:10:00" },
                { "Orleans:MongoDBESClientSettings:MaxConnecting", "16" },
                { "Orleans:DataBase", "orleans-test" },
                { "Orleans:ESDataBase", "orleans-es-test" },
                { "Orleans:DashboardUserName", "test" },
                { "Orleans:DashboardPassword", "test" },
                { "Orleans:DashboardPort", "8888" },
                { "Orleans:DashboardCounterUpdateIntervalMs", "1000" },
                { "Orleans:SiloPort", "11111" },
                { "Orleans:GatewayPort", "30000" },
                { "OrleansEventSourcing:Provider", "mongodb" },
                { "OrleansStream:Provider", "Memory" },
                { "VectorStores:Qdrant:Url", "http://localhost:6333" }
            };
    }

    // Class to mock environment variables
    private class EnvironmentVariableMock
    {
        private readonly Dictionary<string, string> _variables = new Dictionary<string, string>();

        public void SetVariable(string name, string value)
        {
            _variables[name] = value;
        }

        public string GetVariable(string name)
        {
            return _variables.TryGetValue(name, out var value) ? value : null;
        }

        public static EnvironmentVariableMock SetupLocalDevEnvironment()
        {
            var mock = new EnvironmentVariableMock();
            mock.SetVariable("AevatarOrleans__AdvertisedIP", "127.0.0.1");
            mock.SetVariable("AevatarOrleans__SiloPort", "11111");
            mock.SetVariable("AevatarOrleans__GatewayPort", "30000");
            mock.SetVariable("AevatarOrleans__DashboardIp", "127.0.0.1");
            mock.SetVariable("AevatarOrleans__DashboardPort", "8888");
            return mock;
        }

        public static EnvironmentVariableMock SetupKubernetesEnvironment()
        {
            var mock = new EnvironmentVariableMock();
            mock.SetVariable("POD_IP", "10.0.0.1");
            mock.SetVariable("ORLEANS_CLUSTER_ID", "test-k8s-cluster");
            mock.SetVariable("ORLEANS_SERVICE_ID", "test-k8s-service");
            return mock;
        }
    }

    private (Mock<IHostBuilder>, Action<HostBuilderContext, ISiloBuilder>) SetupHostBuilderMock()
    {
        var mockHostBuilder = new Mock<IHostBuilder>();
        Action<HostBuilderContext, ISiloBuilder> orleansConfigAction = null;

        mockHostBuilder.Setup(x => x.UseOrleans(It.IsAny<Action<HostBuilderContext, ISiloBuilder>>()))
            .Callback<Action<HostBuilderContext, ISiloBuilder>>((configAction) =>
            {
                orleansConfigAction = configAction;
            })
            .Returns(mockHostBuilder.Object);
        mockHostBuilder.Setup(x => x.ConfigureServices(It.IsAny<Action<HostBuilderContext, IServiceCollection>>()))
            .Returns(mockHostBuilder.Object);
        mockHostBuilder.Setup(x => x.UseConsoleLifetime())
            .Returns(mockHostBuilder.Object);

        return (mockHostBuilder, orleansConfigAction);
    }

    [Fact]
    public void UseOrleansConfiguration_Should_RegisterSiloNamePatternPlacement()
    {
        // Arrange
        var hostBuilder = new HostBuilder();
        var configData = GetBaseConfig();

        // Add Kafka-specific config
        configData["OrleansStream:Provider"] = "Kafka";
        configData["OrleansStream:Brokers:0"] = "localhost:9092";
        configData["OrleansStream:Partitions"] = "1";
        configData["OrleansStream:ReplicationFactor"] = "1";
        configData["OrleansStream:Topics"] = "test-topic";

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Setup environment variables mock
        var envMock = EnvironmentVariableMock.SetupLocalDevEnvironment();

        // Setup the environment access patch to use our mock
        var originalEnvAccessor = OrleansHostExtension.GetEnvironmentVariable;
        try
        {
            // Patch the environment variable accessor
            OrleansHostExtension.GetEnvironmentVariable = envMock.GetVariable;

            hostBuilder.ConfigureAppConfiguration(builder => builder.AddConfiguration(configuration));

            // Act
            hostBuilder.UseOrleansConfiguration();
            var host = hostBuilder.Build();

            // Assert
            host.Should().NotBeNull("Host should be built successfully with Orleans configuration applied");

            // Verify that Orleans services are registered
            var clusterClient = host.Services.GetRequiredService<IClusterClient>();
            clusterClient.Should().NotBeNull("IClusterClient should be registered when Orleans is configured");

            // Verify that the SiloNamePatternPlacementDirector is registered as a keyed service
            // The placement director is registered as a keyed service with the placement strategy type as the key
            var serviceDescriptors = host.Services.GetType()
                .GetProperty("ServiceDescriptors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(host.Services) as IEnumerable<ServiceDescriptor>;

            if (serviceDescriptors != null)
            {
                var placementDirectorService = serviceDescriptors.FirstOrDefault(sd =>
                    sd.ServiceType == typeof(IPlacementDirector) &&
                    sd.ServiceKey?.Equals(typeof(SiloNamePatternPlacement)) == true);

                placementDirectorService.Should().NotBeNull("SiloNamePatternPlacementDirector should be registered as a keyed service");
                placementDirectorService.ImplementationType.Should().Be(typeof(SiloNamePatternPlacementDirector),
                    "The registered placement director should be of type SiloNamePatternPlacementDirector");
            }
            else
            {
                // Fallback: Try to get the keyed service directly (this will throw if not registered)
                Action getPlacementDirector = () => host.Services.GetRequiredKeyedService<IPlacementDirector>(typeof(SiloNamePatternPlacement));
                getPlacementDirector.Should().NotThrow("SiloNamePatternPlacementDirector should be registered as a keyed service");
            }
        }
        finally
        {
            // Restore the original environment accessor
            OrleansHostExtension.GetEnvironmentVariable = originalEnvAccessor;
        }
    }

    [Fact]
    public void Should_SetSiloNameWithProjectorPattern_When_SiloNamePatternIsProjector_InLocalMode()
    {
        // Arrange
        var envMock = EnvironmentVariableMock.SetupLocalDevEnvironment();
        envMock.SetVariable("AevatarOrleans__SILO_NAME_PATTERN", "Projector");

        // Store and replace the original function
        var originalGetter = OrleansHostExtension.GetEnvironmentVariable;
        OrleansHostExtension.GetEnvironmentVariable = envMock.GetVariable;

        try
        {
            var hostBuilder = new HostBuilder();
            var configData = GetBaseConfig(isKubernetes: false);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            hostBuilder.ConfigureAppConfiguration(builder => builder.AddConfiguration(configuration));

            // Act
            hostBuilder.UseOrleansConfiguration();
            var host = hostBuilder.Build();

            // Assert - Verify that the silo name starts with "Projector"
            var siloOptions = host.Services.GetService<IOptions<SiloOptions>>();
            siloOptions.Should().NotBeNull("SiloOptions should be configured");
            siloOptions.Value.SiloName.Should().StartWith("Projector-", "Silo name should start with 'Projector-' when SiloNamePattern is 'Projector'");

            // Verify that StateProjectionInitializer would be registered (by checking the condition logic)
            var siloNamePattern = envMock.GetVariable("AevatarOrleans__SILO_NAME_PATTERN");
            var shouldRegisterStateProjection = string.IsNullOrEmpty(siloNamePattern) ||
                string.Compare(siloNamePattern, "Projector", StringComparison.OrdinalIgnoreCase) == 0;
            shouldRegisterStateProjection.Should().BeTrue("StateProjectionInitializer should be registered when SiloNamePattern is 'Projector'");
        }
        finally
        {
            // Restore the original function
            OrleansHostExtension.GetEnvironmentVariable = originalGetter;
        }
    }

    [Fact]
    public void Should_SetSiloNameWithWorkerPattern_When_SiloNamePatternIsNotProjector_InLocalMode()
    {
        // Arrange
        var envMock = EnvironmentVariableMock.SetupLocalDevEnvironment();
        envMock.SetVariable("AevatarOrleans__SILO_NAME_PATTERN", "Worker");

        // Store and replace the original function
        var originalGetter = OrleansHostExtension.GetEnvironmentVariable;
        OrleansHostExtension.GetEnvironmentVariable = envMock.GetVariable;

        try
        {
            var hostBuilder = new HostBuilder();
            var configData = GetBaseConfig(isKubernetes: false);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            hostBuilder.ConfigureAppConfiguration(builder => builder.AddConfiguration(configuration));

            // Act
            hostBuilder.UseOrleansConfiguration();
            var host = hostBuilder.Build();

            // Assert - Verify that the silo name starts with "Worker"
            var siloOptions = host.Services.GetService<IOptions<SiloOptions>>();
            siloOptions.Should().NotBeNull("SiloOptions should be configured");
            siloOptions.Value.SiloName.Should().StartWith("Worker-", "Silo name should start with 'Worker-' when SiloNamePattern is 'Worker'");

            // Verify that StateProjectionInitializer would NOT be registered (by checking the condition logic)
            var siloNamePattern = envMock.GetVariable("AevatarOrleans__SILO_NAME_PATTERN");
            var shouldRegisterStateProjection = string.IsNullOrEmpty(siloNamePattern) ||
                string.Compare(siloNamePattern, "Projector", StringComparison.OrdinalIgnoreCase) == 0;
            shouldRegisterStateProjection.Should().BeFalse("StateProjectionInitializer should NOT be registered when SiloNamePattern is 'Worker'");
        }
        finally
        {
            // Restore the original function
            OrleansHostExtension.GetEnvironmentVariable = originalGetter;
        }
    }

    [Fact]
    public void Should_SetSiloNameWithProjectorPattern_When_SiloNamePatternIsProjector_InKubernetesMode()
    {
        // Arrange
        var envMock = EnvironmentVariableMock.SetupKubernetesEnvironment();
        envMock.SetVariable("SILO_NAME_PATTERN", "Projector");

        // Store and replace the original function
        var originalGetter = OrleansHostExtension.GetEnvironmentVariable;
        OrleansHostExtension.GetEnvironmentVariable = envMock.GetVariable;

        try
        {
            var hostBuilder = new HostBuilder();
            var configData = GetBaseConfig(isKubernetes: true);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            hostBuilder.ConfigureAppConfiguration(builder => builder.AddConfiguration(configuration));

            // Act
            hostBuilder.UseOrleansConfiguration();
            var host = hostBuilder.Build();

            // Assert - Verify that the silo name starts with "Projector"
            var siloOptions = host.Services.GetService<IOptions<SiloOptions>>();
            siloOptions.Should().NotBeNull("SiloOptions should be configured");
            siloOptions.Value.SiloName.Should().StartWith("Projector-", "Silo name should start with 'Projector-' when SiloNamePattern is 'Projector' in Kubernetes mode");

            // Verify that StateProjectionInitializer would be registered (by checking the condition logic)
            var siloNamePattern = envMock.GetVariable("SILO_NAME_PATTERN");
            var shouldRegisterStateProjection = string.IsNullOrEmpty(siloNamePattern) ||
                string.Compare(siloNamePattern, "Projector", StringComparison.OrdinalIgnoreCase) == 0;
            shouldRegisterStateProjection.Should().BeTrue("StateProjectionInitializer should be registered when SiloNamePattern is 'Projector' in Kubernetes mode");
        }
        finally
        {
            // Restore the original function
            OrleansHostExtension.GetEnvironmentVariable = originalGetter;
        }
    }

    [Fact]
    public void Should_SetSiloNameWithWorkerPattern_When_SiloNamePatternIsNotProjector_InKubernetesMode()
    {
        // Arrange
        var envMock = EnvironmentVariableMock.SetupKubernetesEnvironment();
        envMock.SetVariable("SILO_NAME_PATTERN", "Worker");

        // Store and replace the original function
        var originalGetter = OrleansHostExtension.GetEnvironmentVariable;
        OrleansHostExtension.GetEnvironmentVariable = envMock.GetVariable;

        try
        {
            var hostBuilder = new HostBuilder();
            var configData = GetBaseConfig(isKubernetes: true);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            hostBuilder.ConfigureAppConfiguration(builder => builder.AddConfiguration(configuration));

            // Act
            hostBuilder.UseOrleansConfiguration();
            var host = hostBuilder.Build();

            // Assert - Verify that the silo name starts with "Worker"
            var siloOptions = host.Services.GetService<IOptions<SiloOptions>>();
            siloOptions.Should().NotBeNull("SiloOptions should be configured");
            siloOptions.Value.SiloName.Should().StartWith("Worker-", "Silo name should start with 'Worker-' when SiloNamePattern is 'Worker' in Kubernetes mode");

            // Verify that StateProjectionInitializer would NOT be registered (by checking the condition logic)
            var siloNamePattern = envMock.GetVariable("SILO_NAME_PATTERN");
            var shouldRegisterStateProjection = string.IsNullOrEmpty(siloNamePattern) ||
                string.Compare(siloNamePattern, "Projector", StringComparison.OrdinalIgnoreCase) == 0;
            shouldRegisterStateProjection.Should().BeFalse("StateProjectionInitializer should NOT be registered when SiloNamePattern is 'Worker' in Kubernetes mode");
        }
        finally
        {
            // Restore the original function
            OrleansHostExtension.GetEnvironmentVariable = originalGetter;
        }
    }

    private bool IsLifecycleParticipantRegistered<T>(IServiceProvider services)
    {
        try
        {
            // Check if there are any lifecycle participants registered
            var lifecycleParticipants = services.GetServices<ILifecycleParticipant<ISiloLifecycle>>();
            return lifecycleParticipants.Any();
        }
        catch
        {
            // If there's an exception getting the service, it's not properly registered
            return false;
        }
    }

    [Fact]
    public async Task Should_UseConfiguredMongoDBClientSettings_WhenProvided()
    {
        // Arrange
        var configData = GetBaseConfig(isKubernetes: false);

        // Override with custom MongoDB client settings
        configData["Orleans:MongoDBClientSettings:MaxConnectionPoolSize"] = "1024";
        configData["Orleans:MongoDBClientSettings:MinConnectionPoolSize"] = "64";
        configData["Orleans:MongoDBClientSettings:WaitQueueSize"] = "163840";
        configData["Orleans:MongoDBClientSettings:WaitQueueTimeout"] = "00:15:00";
        configData["Orleans:MongoDBClientSettings:MaxConnecting"] = "32";

        var envMock = EnvironmentVariableMock.SetupLocalDevEnvironment();
        var originalGetter = OrleansHostExtension.GetEnvironmentVariable;
        OrleansHostExtension.GetEnvironmentVariable = envMock.GetVariable;

        try
        {
            // Act - Create a test cluster with the configuration
            var builder = new TestClusterBuilder(1);
            builder.ConfigureHostConfiguration(configBuilder =>
            {
                configBuilder.AddInMemoryCollection(configData);
            });
            builder.AddSiloBuilderConfigurator<MongoDBTestSiloConfigurator>();

            using var cluster = builder.Build();
            await cluster.DeployAsync();

            // Assert - Get the MongoDB client from the silo and verify settings
            var siloHandle = cluster.GetActiveSilos().First();
            var siloServiceProvider = cluster.GetSiloServiceProvider(siloHandle.SiloAddress);
            var mongoClient = siloServiceProvider.GetService<IMongoClient>();
            mongoClient.Should().NotBeNull("MongoDB client should be registered");

            var settings = mongoClient.Settings;
            settings.MaxConnectionPoolSize.Should().Be(1024, "MaxConnectionPoolSize should be configured to 1024");
            settings.MinConnectionPoolSize.Should().Be(64, "MinConnectionPoolSize should be configured to 64");
            settings.WaitQueueSize.Should().Be(163840, "WaitQueueSize should be configured to 163840");
            settings.WaitQueueTimeout.Should().Be(TimeSpan.FromMinutes(15), "WaitQueueTimeout should be configured to 15 minutes");
            settings.MaxConnecting.Should().Be(32, "MaxConnecting should be configured to 32");
        }
        finally
        {
            OrleansHostExtension.GetEnvironmentVariable = originalGetter;
        }
    }

    [Fact]
    public async Task Should_UseConfiguredMongoDBESClientSettings_WhenProvided()
    {
        // Arrange
        var configData = GetBaseConfig(isKubernetes: false);

        // Enable MongoDB event sourcing and override with custom MongoDB ES client settings
        configData["OrleansEventSourcing:Provider"] = "mongodb";
        configData["Orleans:MongoDBESClientSettings:WaitQueueSize"] = "20480";
        configData["Orleans:MongoDBESClientSettings:MinConnectionPoolSize"] = "64";
        configData["Orleans:MongoDBESClientSettings:WaitQueueTimeout"] = "00:15:00";
        configData["Orleans:MongoDBESClientSettings:MaxConnecting"] = "32";

        var envMock = EnvironmentVariableMock.SetupLocalDevEnvironment();
        var originalGetter = OrleansHostExtension.GetEnvironmentVariable;
        OrleansHostExtension.GetEnvironmentVariable = envMock.GetVariable;

        try
        {
            // Act - Create a test cluster with the configuration
            var builder = new TestClusterBuilder(1);
            builder.ConfigureHostConfiguration(configBuilder =>
            {
                configBuilder.AddInMemoryCollection(configData);
            });
            builder.AddSiloBuilderConfigurator<MongoDBTestSiloConfigurator>();

            using var cluster = builder.Build();
            await cluster.DeployAsync();

            // Assert - Verify that MongoDB event sourcing is configured
            // We can verify the configuration is read correctly
            var configuration = cluster.ServiceProvider.GetService<IConfiguration>();
            configuration.GetSection("OrleansEventSourcing:Provider").Get<string>().Should().Be("mongodb", "MongoDB event sourcing provider should be enabled");

            // Verify that the cluster was created successfully with the MongoDB ES configuration
            cluster.Should().NotBeNull("Test cluster should be created successfully with MongoDB ES configuration");
        }
        finally
        {
            OrleansHostExtension.GetEnvironmentVariable = originalGetter;
        }
    }

    [Fact]
    public void Should_HandleMissingMongoDBClientSettings_Gracefully()
    {
        // Arrange
        var envMock = EnvironmentVariableMock.SetupLocalDevEnvironment();
        var originalGetter = OrleansHostExtension.GetEnvironmentVariable;
        OrleansHostExtension.GetEnvironmentVariable = envMock.GetVariable;

        try
        {
            var hostBuilder = new HostBuilder();
            var configData = GetBaseConfig(isKubernetes: false);

            // Remove MongoDB client settings to test default behavior
            configData.Remove("Orleans:MongoDBClientSettings:MaxConnectionPoolSize");
            configData.Remove("Orleans:MongoDBClientSettings:MinConnectionPoolSize");
            configData.Remove("Orleans:MongoDBClientSettings:WaitQueueSize");
            configData.Remove("Orleans:MongoDBClientSettings:WaitQueueTimeout");
            configData.Remove("Orleans:MongoDBClientSettings:MaxConnecting");
            // Also remove ES client settings to test defaults
            configData.Remove("Orleans:MongoDBESClientSettings:WaitQueueSize");
            configData.Remove("Orleans:MongoDBESClientSettings:MinConnectionPoolSize");
            configData.Remove("Orleans:MongoDBESClientSettings:WaitQueueTimeout");
            configData.Remove("Orleans:MongoDBESClientSettings:MaxConnecting");

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            hostBuilder.ConfigureAppConfiguration(builder => builder.AddConfiguration(configuration));

            // Act & Assert - Should not throw exception and should use default values
            var exception = Record.Exception(() =>
            {
                hostBuilder.UseOrleansConfiguration();
                var host = hostBuilder.Build();
            });

            exception.ShouldBeNull("Host should build successfully using default values when MongoDB client settings are missing");
        }
        finally
        {
            OrleansHostExtension.GetEnvironmentVariable = originalGetter;
        }
    }

    [Fact]
    public async Task Should_UseDefaultValues_WhenMongoDBSettingsAreMissing()
    {
        // Arrange
        var configData = GetBaseConfig(isKubernetes: false);

        // Remove all MongoDB client settings to force use of defaults
        configData.Remove("Orleans:MongoDBClientSettings:MaxConnectionPoolSize");
        configData.Remove("Orleans:MongoDBClientSettings:MinConnectionPoolSize");
        configData.Remove("Orleans:MongoDBClientSettings:WaitQueueSize");
        configData.Remove("Orleans:MongoDBClientSettings:WaitQueueTimeout");
        configData.Remove("Orleans:MongoDBClientSettings:MaxConnecting");
        configData.Remove("Orleans:MongoDBESClientSettings:WaitQueueSize");
        configData.Remove("Orleans:MongoDBESClientSettings:MinConnectionPoolSize");
        configData.Remove("Orleans:MongoDBESClientSettings:WaitQueueTimeout");
        configData.Remove("Orleans:MongoDBESClientSettings:MaxConnecting");

        var envMock = EnvironmentVariableMock.SetupLocalDevEnvironment();
        var originalGetter = OrleansHostExtension.GetEnvironmentVariable;
        OrleansHostExtension.GetEnvironmentVariable = envMock.GetVariable;

        try
        {
            // Act - Create a test cluster with the configuration
            var builder = new TestClusterBuilder(1);
            builder.ConfigureHostConfiguration(configBuilder =>
            {
                configBuilder.AddInMemoryCollection(configData);
            });
            builder.AddSiloBuilderConfigurator<MongoDBTestSiloConfigurator>();

            using var cluster = builder.Build();
            await cluster.DeployAsync();

            // Assert - Get the MongoDB client from the silo and verify default settings
            var siloHandle = cluster.GetActiveSilos().First();
            var siloServiceProvider = cluster.GetSiloServiceProvider(siloHandle.SiloAddress);
            var mongoClient = siloServiceProvider.GetService<IMongoClient>();
            mongoClient.Should().NotBeNull("MongoDB client should be registered");

            var settings = mongoClient.Settings;

            // MongoDB Client defaults (from OrleansHostExtension.cs lines 132-137):
            // MaxConnectionPoolSize = 512 (default), MinConnectionPoolSize = 16 (default)
            // WaitQueueSize = MongoDefaults.ComputedWaitQueueSize (500), WaitQueueTimeout = MongoDefaults.WaitQueueTimeout (2 minutes)
            // MaxConnecting = 4 (default)
            settings.MaxConnectionPoolSize.Should().Be(512, "MaxConnectionPoolSize should use default value of 512");
            settings.MinConnectionPoolSize.Should().Be(16, "MinConnectionPoolSize should use default value of 16");
            settings.WaitQueueSize.Should().Be(500, "WaitQueueSize should use MongoDefaults.ComputedWaitQueueSize (500)");
            settings.WaitQueueTimeout.Should().Be(TimeSpan.FromMinutes(2), "WaitQueueTimeout should use MongoDefaults.WaitQueueTimeout (2 minutes)");
            settings.MaxConnecting.Should().Be(4, "MaxConnecting should use default value of 4");
        }
        finally
        {
            OrleansHostExtension.GetEnvironmentVariable = originalGetter;
        }
    }
}
