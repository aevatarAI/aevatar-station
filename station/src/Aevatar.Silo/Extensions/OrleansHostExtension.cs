using System.Net;
using System.Linq;
using Aevatar.Application.Grains;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Placement;
using Aevatar.CQRS;
using Aevatar.Dapr;
using Aevatar.EventSourcing.MongoDB.Hosting;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.Extensions;
using Aevatar.Extensions;
using Aevatar.PermissionManagement.Extensions;
using Aevatar.SignalR;
using Aevatar.Silo.Startup;
using E2E.Grains;
using Aevatar.Silo.AgentWarmup.Extensions;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Runtime.Placement;
using Orleans.Serialization;
using Orleans.Streams.Kafka.Config;
using Orleans.Streaming;
using Orleans.Streams;
using Orleans.Hosting;
using Aevatar.Core.Streaming.Extensions;

namespace Aevatar.Silo.Extensions;

public static class OrleansHostExtension
{
    // Delegate for environment variable access, allows for mocking in tests
    public static Func<string, string> GetEnvironmentVariable { get; set; } = Environment.GetEnvironmentVariable;
    
    public static IHostBuilder UseOrleansConfiguration(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseOrleans((context, siloBuilder) =>
            {
                var configuration = context.Configuration;
                var hostId = configuration.GetValue<string>("Host:HostId");
                var configSection = context.Configuration.GetSection("Orleans");
                var UseEnvironmentVariables =
                    bool.TryParse(GetEnvironmentVariable("UseEnvironmentVariables"), out var flag) && flag;
                var isRunningInKubernetes = configSection.GetValue<bool>("IsRunningInKubernetes");
                var advertisedIP = UseEnvironmentVariables
                    ? GetEnvironmentVariable("AevatarOrleans__AdvertisedIP")
                    : isRunningInKubernetes
                        ? Environment.GetEnvironmentVariable("POD_IP")
                        : configSection.GetValue<string>("AdvertisedIP");
                var clusterId = isRunningInKubernetes
                    ? Environment.GetEnvironmentVariable("ORLEANS_CLUSTER_ID")
                    : configSection.GetValue<string>("ClusterId");
                var serviceId = isRunningInKubernetes
                    ? Environment.GetEnvironmentVariable("ORLEANS_SERVICE_ID")
                    : configSection.GetValue<string>("ServiceId");
                var siloPort = UseEnvironmentVariables
                    ? int.Parse(GetEnvironmentVariable("AevatarOrleans__SiloPort"))
                    : configSection.GetValue<int>("SiloPort");
                var gatewayPort = UseEnvironmentVariables
                    ? int.Parse(GetEnvironmentVariable("AevatarOrleans__GatewayPort"))
                    : configSection.GetValue<int>("GatewayPort");

                // Read the silo name pattern from environment variable or configuration
                var siloNamePattern = isRunningInKubernetes
                    ? GetEnvironmentVariable("SILO_NAME_PATTERN")
                    : GetEnvironmentVariable("AevatarOrleans__SILO_NAME_PATTERN");
               
                // Register StateProjectionInitializer when SiloNamePattern is "Projector"
                if ( string.IsNullOrEmpty(siloNamePattern) || string.Compare(siloNamePattern, "Projector", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // Register our StateProjectionInitializer as a startup task
                    // This will run during silo startup at ServiceLifecycleStage.ApplicationServices (default)
                    siloBuilder.AddStartupTask<StateProjectionInitializer>();
                }

                if (string.IsNullOrEmpty(siloNamePattern) || string.Compare(siloNamePattern, "Scheduler", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    siloBuilder
                    .AddAgentWarmup<Guid>(options =>
                    {
                        // Bind configuration from appsettings.json with fallback to defaults
                        var agentWarmupSection = configuration.GetSection("AgentWarmup");
                        
                        // Main configuration properties with defaults
                        options.Enabled = agentWarmupSection.GetValue<bool>("Enabled", true);
                        options.MaxConcurrency = agentWarmupSection.GetValue<int>("MaxConcurrency", 10);
                        options.InitialBatchSize = agentWarmupSection.GetValue<int>("InitialBatchSize", 5);
                        options.MaxBatchSize = agentWarmupSection.GetValue<int>("MaxBatchSize", 200);
                        options.BatchSizeIncreaseFactor = agentWarmupSection.GetValue<double>("BatchSizeIncreaseFactor", 1.5);
                        options.DelayBetweenBatchesMs = agentWarmupSection.GetValue<int>("DelayBetweenBatchesMs", 0);
                        options.AgentActivationTimeoutMs = agentWarmupSection.GetValue<int>("AgentActivationTimeoutMs", 5000);
                        options.MaxRetryAttempts = agentWarmupSection.GetValue<int>("MaxRetryAttempts", 3);
                        options.RetryDelayMs = agentWarmupSection.GetValue<int>("RetryDelayMs", 1000);
                        
                        // MongoDB rate limit configuration with defaults
                        var rateLimitSection = agentWarmupSection.GetSection("MongoDbRateLimit");
                        options.MongoDbRateLimit.MaxOperationsPerSecond = rateLimitSection.GetValue<int>("MaxOperationsPerSecond", 50);
                        options.MongoDbRateLimit.BurstAllowance = rateLimitSection.GetValue<int>("BurstAllowance", 10);
                        options.MongoDbRateLimit.TimeWindowMs = rateLimitSection.GetValue<int>("TimeWindowMs", 1000);
                        
                        // Auto discovery configuration with defaults
                        var autoDiscoverySection = agentWarmupSection.GetSection("AutoDiscovery");
                        options.AutoDiscovery.Enabled = autoDiscoverySection.GetValue<bool>("Enabled", true);
                        options.AutoDiscovery.BaseTypes = new List<Type>(); // BaseTypes are complex, leave empty for now
                        options.AutoDiscovery.RequiredAttributes = autoDiscoverySection.GetSection("RequiredAttributes").Get<string[]>()?.ToList() ?? new List<string> { "StorageProvider" };
                        options.AutoDiscovery.StorageProviderName = autoDiscoverySection.GetValue<string>("StorageProviderName", "PubSubStore");
                        options.AutoDiscovery.ExcludedAgentTypes = autoDiscoverySection.GetSection("ExcludedAgentTypes").Get<string[]>()?.ToList() ?? new List<string> { "Orleans.", "Microsoft.Orleans.", "System.", "Microsoft." };
                        options.AutoDiscovery.IncludedAssemblies = autoDiscoverySection.GetSection("IncludedAssemblies").Get<string[]>()?.ToList() ?? new List<string>();
                        
                        // Configure MongoDB integration for agent warmup
                        // Collection prefix matches PubSubStore pattern for consistency
                        options.MongoDbIntegration.CollectionPrefix = hostId.IsNullOrEmpty() ? "StreamStorage" : $"Stream{hostId}";
                        options.MongoDbIntegration.CollectionNamingStrategy = "FullTypeName";
                        options.MongoDbIntegration.BatchSize = 100;
                        options.MongoDbIntegration.QueryTimeoutMs = 30000;
                        
                        // Configure default strategy for high-volume agent warmup
                        var defaultStrategySection = agentWarmupSection.GetSection("DefaultStrategy");
                        options.DefaultStrategy.MaxIdentifiersPerType = defaultStrategySection.GetValue<int>("MaxIdentifiersPerType", 1000000);
                    });
                }
                    
                // Check if ZooKeeper configuration is available
                var zookeeperSection = configSection.GetSection("ZooKeeper");
                var zookeeperConnectionString = zookeeperSection.GetValue<string>("ConnectionString");
                
                siloBuilder
                    .ConfigureEndpoints(advertisedIP: IPAddress.Parse(advertisedIP),
                        siloPort: siloPort,
                        gatewayPort: gatewayPort,
                        listenOnAnyHostAddress: true);
                
                // Configure clustering based on available provider
                if (!string.IsNullOrEmpty(zookeeperConnectionString))
                {
                    // Use ZooKeeper clustering
                    siloBuilder.UseZooKeeperClustering(options =>
                    {
                        options.ConnectionString = zookeeperConnectionString;
                    })
                    .Configure<ClusterMembershipOptions>(options =>
                    {
                        // Read ZooKeeper cluster membership configuration with defaults from benchmark
                        var membershipSection = zookeeperSection.GetSection("ClusterMembership");
                        options.DefunctSiloCleanupPeriod = membershipSection.GetValue<TimeSpan>("DefunctSiloCleanupPeriod", TimeSpan.FromMinutes(1));
                        options.DefunctSiloExpiration = membershipSection.GetValue<TimeSpan>("DefunctSiloExpiration", TimeSpan.FromMinutes(2));
                        options.IAmAliveTablePublishTimeout = membershipSection.GetValue<TimeSpan>("IAmAliveTablePublishTimeout", TimeSpan.FromSeconds(30));
                        options.MaxJoinAttemptTime = membershipSection.GetValue<TimeSpan>("MaxJoinAttemptTime", TimeSpan.FromSeconds(30));
                        options.ProbeTimeout = membershipSection.GetValue<TimeSpan>("ProbeTimeout", TimeSpan.FromSeconds(10));
                        options.TableRefreshTimeout = membershipSection.GetValue<TimeSpan>("TableRefreshTimeout", TimeSpan.FromSeconds(30));
                        options.DeathVoteExpirationTimeout = membershipSection.GetValue<TimeSpan>("DeathVoteExpirationTimeout", TimeSpan.FromMinutes(2));
                        options.NumMissedProbesLimit = membershipSection.GetValue<int>("NumMissedProbesLimit", 2);
                        options.NumProbedSilos = membershipSection.GetValue<int>("NumProbedSilos", 2);
                        options.NumVotesForDeathDeclaration = membershipSection.GetValue<int>("NumVotesForDeathDeclaration", 1);
                        options.UseLivenessGossip = membershipSection.GetValue<bool>("UseLivenessGossip", false);
                    })
                    // Still need MongoDB client for storage and reminders
                    .UseMongoDBClient(proivder => {
                        var setting = MongoClientSettings.FromConnectionString(configSection.GetValue<string>("MongoDBClient"));
                        
                        // Read MongoDB client settings from configuration with MongoDB driver default values
                        var clientSection = configSection.GetSection("MongoDBClientSettings");
                        setting.MaxConnectionPoolSize = clientSection.GetValue<int>("MaxConnectionPoolSize", 512);
                        setting.MinConnectionPoolSize = clientSection.GetValue<int>("MinConnectionPoolSize", 16);
                        setting.WaitQueueSize = clientSection.GetValue<int>("WaitQueueSize", MongoDefaults.ComputedWaitQueueSize);
                        setting.WaitQueueTimeout = clientSection.GetValue<TimeSpan>("WaitQueueTimeout", MongoDefaults.WaitQueueTimeout);
                        setting.MaxConnecting = clientSection.GetValue<int>("MaxConnecting", 4);
                        return setting;
                    });
                }
                else
                {
                    // Use MongoDB clustering (existing behavior)
                    siloBuilder.UseMongoDBClient(proivder => {
                        var setting = MongoClientSettings.FromConnectionString(configSection.GetValue<string>("MongoDBClient"));
                        
                        // Read MongoDB client settings from configuration with MongoDB driver default values
                        var clientSection = configSection.GetSection("MongoDBClientSettings");
                        setting.MaxConnectionPoolSize = clientSection.GetValue<int>("MaxConnectionPoolSize", 512);
                        setting.MinConnectionPoolSize = clientSection.GetValue<int>("MinConnectionPoolSize", 16);
                        setting.WaitQueueSize = clientSection.GetValue<int>("WaitQueueSize", MongoDefaults.ComputedWaitQueueSize);
                        setting.WaitQueueTimeout = clientSection.GetValue<TimeSpan>("WaitQueueTimeout", MongoDefaults.WaitQueueTimeout);
                        setting.MaxConnecting = clientSection.GetValue<int>("MaxConnecting", 4);
                        return setting;
                    })
                    .UseMongoDBClustering(options =>
                    {
                        options.DatabaseName = configSection.GetValue<string>("DataBase");
                        options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                        options.CollectionPrefix = hostId.IsNullOrEmpty() ? "OrleansAevatar" : $"Orleans{hostId}";
                    });
                }
                
                siloBuilder
                    .Configure<JsonGrainStateSerializerOptions>(options => options.ConfigureJsonSerializerSettings =
                        settings =>
                        {
                            settings.NullValueHandling = NullValueHandling.Include;
                            settings.DefaultValueHandling = DefaultValueHandling.Populate;
                            settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                            settings.TypeNameHandling = TypeNameHandling.Auto;
                            settings.SerializationBinder = new GodGPTSerializationBinder();
                        })
                    .Configure<GrainCollectionOptions>(options =>
                    {
                        // Set default collection age for all grains (in days)
                        options.CollectionAge = TimeSpan.FromDays(180);
                        
                        // Optionally, set specific collection ages for particular grain types
                        // options.ClassSpecificCollectionAge[typeof(UserGridGAgent).FullName] = TimeSpan.FromMinutes(10);
                    })
                    .AddMongoDBGrainStorage("Default", (MongoDBGrainStorageOptions op) =>
                    {
                        op.CollectionPrefix = hostId.IsNullOrEmpty() ? "OrleansAevatar" : $"Orleans{hostId}";
                        op.DatabaseName = configSection.GetValue<string>("DataBase");
                    })
                    .UseMongoDBReminders(options =>
                    {
                        options.DatabaseName = configSection.GetValue<string>("DataBase");
                        options.CreateShardKeyForCosmos = false;
                        options.CollectionPrefix = hostId.IsNullOrEmpty() ? "Orleans" : $"Orleans{hostId}";
                        ;
                    })
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = clusterId;
                        options.ServiceId = serviceId;
                    })
                    .Configure<ExceptionSerializationOptions>(options =>
                    {
                        options.SupportedNamespacePrefixes.Add("Volo.Abp");
                        options.SupportedNamespacePrefixes.Add("Newtonsoft.Json");
                        options.SupportedNamespacePrefixes.Add("Autofac.Core");
                    })
                    .AddActivityPropagation()
                    // .UsePluginGAgents()
                    .UseDashboard(options =>
                    {
                        options.Username = configSection.GetValue<string>("DashboardUserName");
                        options.Password = configSection.GetValue<string>("DashboardPassword");
                        options.Host = UseEnvironmentVariables
                            ? GetEnvironmentVariable("AevatarOrleans__DashboardIp")
                            : "*";
                        options.Port = UseEnvironmentVariables
                            ? int.Parse(GetEnvironmentVariable("AevatarOrleans__DashboardPort"))
                            : configSection.GetValue<int>("DashboardPort");
                        options.HostSelf = true;
                        options.CounterUpdateIntervalMs =
                            configSection.GetValue<int>("DashboardCounterUpdateIntervalMs");
                    })
                    .Configure<SiloMessagingOptions>(options =>
                    {
                        options.ResponseTimeout = TimeSpan.FromMinutes(60);
                        options.SystemResponseTimeout = TimeSpan.FromMinutes(60);
                    })
                    .AddMongoDBGrainStorage("PubSubStore", options =>
                    {
                        // Config PubSubStore Storage for Persistent Stream 
                        options.CollectionPrefix = hostId.IsNullOrEmpty() ? "StreamStorage" : $"Stream{hostId}";
                        options.DatabaseName = configSection.GetValue<string>("DataBase");
                    })
                    .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); })
                    .Configure<SiloOptions>(options =>
                    {
                        siloNamePattern = string.IsNullOrEmpty(siloNamePattern) ? "Projector" : siloNamePattern;
                        options.SiloName = $"{siloNamePattern}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";                        
                    })
                    .Configure<OrleansJsonSerializerOptions>(options =>
                    {
                        options.JsonSerializerSettings.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
                    });

                var eventSourcingProvider = configuration.GetSection("OrleansEventSourcing:Provider").Get<string>();
                if (string.Equals("mongodb", eventSourcingProvider, StringComparison.CurrentCultureIgnoreCase))
                {
                    siloBuilder.AddMongoDbStorageBasedLogConsistencyProvider("LogStorage", options =>
                    {
                        options.ClientSettings =
                            MongoClientSettings.FromConnectionString(configSection.GetValue<string>("MongoDBClient"));

                        // Read MongoDB ES client settings from configuration
                        var esClientSection = configSection.GetSection("MongoDBESClientSettings");
                        options.ClientSettings.WaitQueueSize = esClientSection.GetValue<int>("WaitQueueSize", 81920);
                        options.ClientSettings.MaxConnectionPoolSize = esClientSection.GetValue<int>("MinConnectionPoolSize", 512);
                        options.ClientSettings.MinConnectionPoolSize = esClientSection.GetValue<int>("MinConnectionPoolSize", 16);
                        options.ClientSettings.WaitQueueTimeout = esClientSection.GetValue<TimeSpan>("WaitQueueTimeout", MongoDefaults.WaitQueueTimeout);
                        options.ClientSettings.MaxConnecting = esClientSection.GetValue<int>("MaxConnecting", 8);
                        options.Database = configSection.GetValue<string>("DataBase");
                    });
                }
                else
                {
                    siloBuilder.AddLogStorageBasedLogConsistencyProvider("LogStorage");
                }

                var streamProvider = configuration.GetSection("OrleansStream:Provider").Get<string>();
                if (string.Compare(streamProvider, "Kafka", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // Use Aevatar monitored Kafka streaming provider with optimized pulling agent options
                    siloBuilder.ConfigureServices(services =>
                    {
                        services.AddAevatarStreamingMonitoring();
                    })
                    .AddPersistentStreams("Aevatar", Aevatar.Core.Streaming.Kafka.AevatarKafkaAdapterFactory.Create, b =>
                    {
                        b.ConfigureStreamPubSub(StreamPubSubType.ExplicitGrainBasedAndImplicit);
                        b.Configure<KafkaStreamOptions>(ob => ob.Configure(options =>
                        {
                            options.BrokerList = configuration.GetSection("OrleansStream:Brokers").Get<List<string>>();
                            options.ConsumerGroupId = "Aevatar";
                            options.ConsumeMode = ConsumeMode.LastCommittedMessage;
                            
                            // Configure PollTimeout for Kafka consumer polling - defaults to 10ms if not specified
                            var streamSection = configuration.GetSection("OrleansStream");
                            options.PollTimeout = TimeSpan.FromMilliseconds(streamSection.GetValue<int>("PollTimeoutMs", 10));

                            var partitions = configuration.GetSection("OrleansStream:Partitions").Get<int>();
                            var replicationFactor =
                                configuration.GetSection("OrleansStream:ReplicationFactor").Get<short>();
                            var topics = configuration.GetSection("OrleansStream:Topics").Get<string>();
                            topics = topics.IsNullOrEmpty() ? CommonConstants.StreamNamespace : topics;
                            foreach (var topic in topics.Split(','))
                            {
                                options.AddTopic(topic.Trim(), new TopicCreationConfig
                                {
                                    AutoCreate = true,
                                    Partitions = partitions,
                                    ReplicationFactor = replicationFactor
                                });
                            }
                        }));
                        // Configure pulling agent options for better performance
                        b.ConfigurePullingAgent(ob => ob.Configure(options =>
                        {
                            // Read GetQueueMsgsTimerPeriod from configuration - defaults to 50ms if not specified
                            var streamSection = configuration.GetSection("OrleansStream");
                            options.GetQueueMsgsTimerPeriod = TimeSpan.FromMilliseconds(streamSection.GetValue<int>("GetQueueMsgsTimerPeriodMs", 50));
                        }));
                    });
                }
                else
                {
                    // Use Orleans built-in memory streaming with optimized pulling agent options
                    siloBuilder.AddMemoryStreams("Aevatar", b =>
                    {
                        // Configure pulling agent options for better performance
                        b.ConfigurePullingAgent(ob => ob.Configure(options =>
                        {
                            // Read GetQueueMsgsTimerPeriod from configuration - defaults to 50ms if not specified
                            var streamSection = configuration.GetSection("OrleansStream");
                            options.GetQueueMsgsTimerPeriod = TimeSpan.FromMilliseconds(streamSection.GetValue<int>("GetQueueMsgsTimerPeriodMs", 50));
                        }));
                    });
                }

                siloBuilder.UseAevatar()
                    .UseAevatarPermissionManagement()
                    .UseSignalR()
                    .RegisterHub<AevatarSignalRHub>();
            }).ConfigureServices((context, services) =>
            {
                // services.AddSingleton<ICancellationTokenProvider, NullCancellationTokenProvider>();
                //services.AddSingleton<IGrainStateSerializer, BinaryGrainStateSerializer>();
                services.AddSingleton<IGrainStateSerializer, HybridGrainStateSerializer>();
                // services.Configure<AzureOpenAIConfig>(context.Configuration.GetSection("AIServices:AzureOpenAI"));
                // services.Configure<AzureDeepSeekConfig>(context.Configuration.GetSection("AIServices:DeepSeek"));
                services.Configure<QdrantConfig>(context.Configuration.GetSection("VectorStores:Qdrant"));
                
                // Register the SiloNamePatternPlacement director
                services.AddPlacementDirector<SiloNamePatternPlacement, SiloNamePatternPlacementDirector>();
                
                services.Configure<SystemLLMConfigOptions>(context.Configuration);
                services.Configure<AzureOpenAIEmbeddingsConfig>(
                    context.Configuration.GetSection("AIServices:AzureOpenAIEmbeddings"));
                services.Configure<RagConfig>(context.Configuration.GetSection("Rag"));
                services.AddSingleton(typeof(HubLifetimeManager<>), typeof(OrleansHubLifetimeManager<>));
                // services.AddSingleton<IStateProjector, AevatarStateProjector>();
                services.AddSingleton<IStateDispatcher, StateDispatcher>();
                services.AddSingleton<IGAgentFactory, GAgentFactory>();
                services.AddSemanticKernel()
                    .AddQdrantVectorStore()
                    .AddAzureOpenAITextEmbedding();
            })
            .UseConsoleLifetime();
    }
}