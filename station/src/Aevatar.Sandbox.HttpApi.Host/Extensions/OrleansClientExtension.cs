using System;
using System.Collections.Generic;
using System.Net;
using Aevatar.Dapr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Serialization;
using Orleans.Streams.Kafka.Config;
using Serilog;

namespace Aevatar.Sandbox.HttpApi.Host.Extensions;

public static class OrleansHostExtensions
{
    public static IHostBuilder UseOrleansClientConfigration(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseOrleansClient((context, clientBuilder) =>
        {
            var config = context.Configuration;
            var configSection = context.Configuration.GetSection("Orleans");
            var hostId = context.Configuration.GetValue<string>("Host:HostId");
            if (configSection == null)
                throw new ArgumentNullException(nameof(configSection), "The Orleans config node is missing");
                
            // Configure Gateway connection
            // 使用静态集群连接，完全禁用MongoDB集群发现
            clientBuilder.UseStaticClustering(new IPEndPoint(IPAddress.Loopback, 20001))
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configSection.GetValue<string>("ClusterId");
                    options.ServiceId = configSection.GetValue<string>("ServiceId");
                });
                
            // 保留MongoDB客户端用于其他存储，但不使用集群发现
            clientBuilder.UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
                .Configure<ClientMessagingOptions>(options =>
                {
                    options.ResponseTimeout = TimeSpan.FromSeconds(10); // 增加响应超时时间
                    options.ResponseTimeoutWithDebugger = TimeSpan.FromSeconds(10);
                    options.DropExpiredMessages = false; // 不丢弃过期消息
                })
                .Configure<ConnectionOptions>(options => 
                {
                    options.OpenConnectionTimeout = TimeSpan.FromSeconds(10); // 增加连接打开超时
                });
                
            // 配置其他选项
            clientBuilder
                .Configure<ExceptionSerializationOptions>(options =>
                {
                    options.SupportedNamespacePrefixes.Add("Volo.Abp");
                    options.SupportedNamespacePrefixes.Add("Newtonsoft.Json");
                    options.SupportedNamespacePrefixes.Add("MongoDB.Driver");
                })
                .AddActivityPropagation();
                
            var streamProvider = config.GetSection("OrleansStream:Provider").Get<string>();
            Log.Information("Stream Provider: {streamProvider}", streamProvider);
            if (string.Equals("kafka", streamProvider, StringComparison.CurrentCultureIgnoreCase))
            {
                Log.Information("Using Kafka as stream provider.");
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
                        var topics = config.GetSection("OrleansStream:Topics").Get<string>();
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
                        Log.Information("Kafka Options: {@options}", options);
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