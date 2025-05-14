using System.Net;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS;
using Aevatar.Dapr;
using Aevatar.EventSourcing.MongoDB.Hosting;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.Extensions;
using Aevatar.Extensions;
using Aevatar.PermissionManagement.Extensions;
using Aevatar.SignalR;
using Aevatar.Silo.Startup;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Runtime.Placement;
using Orleans.Serialization;
using Orleans.Streams.Kafka.Config;

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
                var isRunningInKubernetes = configSection.GetValue<bool>("IsRunningInKubernetes");
                var advertisedIP = isRunningInKubernetes
                    ? GetEnvironmentVariable("POD_IP")
                    : GetEnvironmentVariable("AevatarOrleans__AdvertisedIP");
                var clusterId = isRunningInKubernetes
                    ? GetEnvironmentVariable("ORLEANS_CLUSTER_ID")
                    : configSection.GetValue<string>("ClusterId");
                var serviceId = isRunningInKubernetes
                    ? GetEnvironmentVariable("ORLEANS_SERVICE_ID")
                    : configSection.GetValue<string>("ServiceId");
                var siloPort = isRunningInKubernetes
                    ? configSection.GetValue<int>("SiloPort")
                    : int.Parse(GetEnvironmentVariable("AevatarOrleans__SiloPort"));
                var gatewayPort = isRunningInKubernetes
                    ? configSection.GetValue<int>("GatewayPort")
                    : int.Parse(GetEnvironmentVariable("AevatarOrleans__GatewayPort"));

                // Read the silo name pattern from environment variable or configuration
                var siloNamePattern = isRunningInKubernetes
                    ? GetEnvironmentVariable("SILO_NAME_PATTERN")
                    : GetEnvironmentVariable("AevatarOrleans__SILO_NAME_PATTERN");

                // Register StateProjectionInitializer when SiloNamePattern is "Projector"
                if (string.IsNullOrEmpty(siloNamePattern) ||
                    string.Compare(siloNamePattern, "Projector", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // Register our StateProjectionInitializer as a startup task
                    // This will run during silo startup at ServiceLifecycleStage.ApplicationServices (default)
                    siloBuilder.AddStartupTask<StateProjectionInitializer>();
                }

                siloBuilder
                    .ConfigureEndpoints(advertisedIP: IPAddress.Parse(advertisedIP),
                        siloPort: siloPort,
                        gatewayPort: gatewayPort,
                        listenOnAnyHostAddress: true)
                    .UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
                    .UseMongoDBClustering(options =>
                    {
                        options.DatabaseName = configSection.GetValue<string>("DataBase");
                        options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                        options.CollectionPrefix = hostId.IsNullOrEmpty() ? "OrleansAevatar" : $"Orleans{hostId}";
                    })
                    .Configure<JsonGrainStateSerializerOptions>(options => options.ConfigureJsonSerializerSettings =
                        settings =>
                        {
                            settings.NullValueHandling = NullValueHandling.Include;
                            settings.DefaultValueHandling = DefaultValueHandling.Populate;
                            settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
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
                        options.Host = isRunningInKubernetes
                            ? "*"
                            : GetEnvironmentVariable("AevatarOrleans__DashboardIp");
                        options.Port = isRunningInKubernetes
                            ? configSection.GetValue<int>("DashboardPort")
                            : int.Parse(GetEnvironmentVariable("AevatarOrleans__DashboardPort"));
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
                        options.SiloName = $"{siloNamePattern}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";
                    });

                var eventSourcingProvider = configuration.GetSection("OrleansEventSourcing:Provider").Get<string>();
                if (string.Equals("mongodb", eventSourcingProvider, StringComparison.CurrentCultureIgnoreCase))
                {
                    siloBuilder.AddMongoDbStorageBasedLogConsistencyProvider("LogStorage", options =>
                    {
                        options.ClientSettings =
                            MongoClientSettings.FromConnectionString(
                                configSection.GetValue<string>("LogMongoDBClient"));
                        options.Database = configSection.GetValue<string>("LogDataBase");
                    });
                }
                else
                {
                    siloBuilder.AddLogStorageBasedLogConsistencyProvider("LogStorage");
                }

                var streamProvider = configuration.GetSection("OrleansStream:Provider").Get<string>();
                if (string.Compare(streamProvider, "Kafka", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    siloBuilder.AddKafka("Aevatar")
                        .WithOptions(options =>
                        {
                            options.BrokerList = configuration.GetSection("OrleansStream:Brokers").Get<List<string>>();
                            options.ConsumerGroupId = "Aevatar";
                            options.ConsumeMode = ConsumeMode.LastCommittedMessage;

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
                        })
                        .AddJson()
                        .AddLoggingTracker()
                        .Build();
                }
                else
                {
                    siloBuilder.AddMemoryStreams("Aevatar");
                }

                siloBuilder.UseAevatar()
                    .UseAevatarPermissionManagement()
                    .UseSignalR()
                    .RegisterHub<AevatarSignalRHub>();
            }).ConfigureServices((context, services) =>
            {
                // services.Configure<AzureOpenAIConfig>(context.Configuration.GetSection("AIServices:AzureOpenAI"));
                // services.Configure<AzureDeepSeekConfig>(context.Configuration.GetSection("AIServices:DeepSeek"));
                services.Configure<QdrantConfig>(context.Configuration.GetSection("VectorStores:Qdrant"));

                // Register the SiloNamePatternPlacement director
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