using System.Net;
using Aevatar.Dapr;
using Aevatar.EventSourcing.MongoDB.Hosting;
using Aevatar.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Serialization;
using Orleans.Streams.Kafka.Config;

namespace Aevatar.Silo.Extensions;

public static class OrleansHostExtension
{
    public static IHostBuilder UseOrleansConfiguration(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseOrleans((context, siloBuilder) =>
            {
                var configuration = context.Configuration;
                var configSection = context.Configuration.GetSection("Orleans");
                var isRunningInKubernetes = configSection.GetValue<bool>("IsRunningInKubernetes");
                var advertisedIP = isRunningInKubernetes
                    ? Environment.GetEnvironmentVariable("POD_IP")
                    : configSection.GetValue<string>("AdvertisedIP");
                var clusterId = isRunningInKubernetes
                    ? Environment.GetEnvironmentVariable("ORLEANS_CLUSTER_ID")
                    : configSection.GetValue<string>("ClusterId");
                var serviceId = isRunningInKubernetes
                    ? Environment.GetEnvironmentVariable("ORLEANS_SERVICE_ID")
                    : configSection.GetValue<string>("ServiceId");
                siloBuilder
                    .ConfigureEndpoints(advertisedIP: IPAddress.Parse(advertisedIP),
                        siloPort: configSection.GetValue<int>("SiloPort"),
                        gatewayPort: configSection.GetValue<int>("GatewayPort"), listenOnAnyHostAddress: true)
                    .UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
                    .UseMongoDBClustering(options =>
                    {
                        options.DatabaseName = configSection.GetValue<string>("DataBase");
                        options.Strategy = MongoDBMembershipStrategy.SingleDocument;
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
                        op.CollectionPrefix = "GrainStorage";
                        op.DatabaseName = configSection.GetValue<string>("DataBase");
                    })
                    .UseMongoDBReminders(options =>
                    {
                        options.DatabaseName = configSection.GetValue<string>("DataBase");
                        options.CreateShardKeyForCosmos = false;
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
                        options.Host = "*";
                        options.Port = configSection.GetValue<int>("DashboardPort");
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
                        options.CollectionPrefix = "StreamStorage";
                        options.DatabaseName = configSection.GetValue<string>("DataBase");
                    })
                    .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); });

                var eventSourcingProvider = configuration.GetSection("OrleansEventSourcing:Provider").Get<string>();
                if (string.Equals("mongodb", eventSourcingProvider, StringComparison.CurrentCultureIgnoreCase))
                {
                    siloBuilder.AddMongoDbStorageBasedLogConsistencyProvider("LogStorage", options =>
                    {
                        options.ClientSettings =
                            MongoClientSettings.FromConnectionString(configSection.GetValue<string>("MongoDBClient"));
                        options.Database = configSection.GetValue<string>("DataBase");
                    });
                }
                else
                {
                    siloBuilder.AddLogStorageBasedLogConsistencyProvider("LogStorage");
                }

                var streamProvider = configuration.GetSection("OrleansStream:Provider").Get<string>();
                if (streamProvider == "Kafka")
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
                            options.AddTopic(CommonConstants.StreamNamespace, new TopicCreationConfig
                            {
                                AutoCreate = true,
                                Partitions = partitions,
                                ReplicationFactor = replicationFactor
                            });
                        })
                        .AddJson()
                        .AddLoggingTracker()
                        .Build();
                }
                else
                {
                    siloBuilder.AddMemoryStreams("Aevatar");
                }
                siloBuilder.UseSignalR(); 
                siloBuilder.RegisterHub<AevatarSignalRHub>();
            })
            .UseConsoleLifetime();
    }
}