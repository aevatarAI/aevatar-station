using System;
using System.Collections.Generic;
using Aevatar.Dapr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Serialization;
using Orleans.Streams.Kafka.Config;

namespace Aevatar.Developer.Host.Extensions;

public static class OrleansHostExtensions
{
    public static IHostBuilder UseOrleansClientConfigration(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseOrleansClient((context, clientBuilder) =>
        {
            var config = context.Configuration;
            var configSection = context.Configuration.GetSection("Orleans");
            if (configSection == null)
                throw new ArgumentNullException(nameof(configSection), "The Orleans config node is missing");
            clientBuilder.UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configSection.GetValue<string>("DataBase");
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configSection.GetValue<string>("ClusterId");
                    options.ServiceId = configSection.GetValue<string>("ServiceId");
                })
                .Configure<ExceptionSerializationOptions>(options =>
                {
                    options.SupportedNamespacePrefixes.Add("Volo.Abp");
                    options.SupportedNamespacePrefixes.Add("Newtonsoft.Json");
                    options.SupportedNamespacePrefixes.Add("MongoDB.Driver");
                })
                .AddActivityPropagation();
                
            var streamProvider = config.GetSection("OrleansStream:Provider").Get<string>();
            if (streamProvider == "Kafka")
            {
                clientBuilder
                    .AddKafka("Aevatar")
                    .WithOptions(options =>
                    {
                        options.BrokerList = config.GetSection("OrleansStream:Brokers").Get<List<string>>();
                        options.ConsumerGroupId = "Aevatar";
                        options.ConsumeMode = ConsumeMode.LastCommittedMessage;

                        var partitions = config.GetSection("OrleansStream:Partitions").Get<int>();
                        var replicationFactor =
                            config.GetSection("OrleansStream:ReplicationFactor").Get<short>();
                        var topic = config.GetSection("OrleansStream:Topic").Get<string>();
                        topic = topic.IsNullOrEmpty() ? CommonConstants.StreamNamespace : topic;
                        options.AddTopic(topic, new TopicCreationConfig
                        {
                            AutoCreate = true,
                            Partitions = partitions,
                            ReplicationFactor = replicationFactor
                        });
                    })
                    .AddJson()
                    .Build();
            }
            else
            {
                clientBuilder.AddMemoryStreams("Aevatar");
            }
        });
    }
}