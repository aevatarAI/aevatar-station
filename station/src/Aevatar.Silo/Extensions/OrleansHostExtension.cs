using System.Net;
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

namespace Aevatar.Silo.Extensions;

public static class OrleansHostExtension
{
    public static IHostBuilder UseOrleansConfiguration(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseOrleans((context, siloBuilder) =>
            {
                var configuration = context.Configuration;
                var hostId = configuration.GetValue<string>("Host:HostId");
                var configSection = context.Configuration.GetSection("Orleans");
                var clusterId = isRunningInKubernetes
                    ? Environment.GetEnvironmentVariable("ORLEANS_CLUSTER_ID")
                    : configSection.GetValue<string>("ClusterId");
                var serviceId = isRunningInKubernetes
                    ? Environment.GetEnvironmentVariable("ORLEANS_SERVICE_ID")
                    : configSection.GetValue<string>("ServiceId");
                    .UseMongoDBClustering(options =>
                    {
                        options.DatabaseName = configSection.GetValue<string>("DataBase");
                        options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                        options.CollectionPrefix = hostId.IsNullOrEmpty() ? "OrleansAevatar" : $"Orleans{hostId}";
                    .Configure<JsonGrainStateSerializerOptions>(options => options.ConfigureJsonSerializerSettings =
                        settings =>
                        {
                            settings.NullValueHandling = NullValueHandling.Include;
                            settings.DefaultValueHandling = DefaultValueHandling.Populate;
                            settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
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
                    siloBuilder.AddMemoryStreams("Aevatar");
                }

                siloBuilder.UseAevatar()
                    .UseAevatarPermissionManagement()
                    .UseSignalR()
                    .RegisterHub<AevatarSignalRHub>();
            }).ConfigureServices((context, services) =>
            {
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