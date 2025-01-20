using System.Net;
using Aevatar.ArtifactGAgent.Extensions;
using Aevatar.EventSourcing.MongoDB.Hosting;
using Aevatar.Plugins;
using Aevatar.Plugins.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Serialization;

namespace PluginGAgent.Silo.Extensions;

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
                    .Configure<PluginGAgentLoadOptions>(options =>
                    {
                        options.TenantId = "test".ToGuid();
                    })
                    .AddMongoDBGrainStorage("PubSubStore", options =>
                    {
                        options.CollectionPrefix = "StreamStorage";
                        options.DatabaseName = configSection.GetValue<string>("DataBase");
                    })
                    .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); })
                    .AddMongoDbStorageBasedLogConsistencyProvider("LogStorage", options =>
                    {
                        options.ClientSettings =
                            MongoClientSettings.FromConnectionString(configSection.GetValue<string>("MongoDBClient"));
                        options.Database = configSection.GetValue<string>("DataBase");
                    })
                    .AddMemoryStreams("Aevatar")
                    .AddMemoryStreams("AISmart")
                    .UsePluginGAgents()
                    .UseArtifactGAgent();
            })
            .UseConsoleLifetime();
        
    }
}