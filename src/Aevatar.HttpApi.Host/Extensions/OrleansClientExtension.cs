using System;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;

namespace Aevatar.Extensions;

public static class OrleansClientExtension
{
    public static IHostBuilder UseOrleansClientConfiguration(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseOrleansClient((context, clientBuilder) =>
        {
            var config = context.Configuration;
            var hostId = context.Configuration.GetValue<string>("Host:HostId");
            clientBuilder
                .AddActivityPropagation()
                .UseMongoDBClient(config["Orleans:MongoDBClient"])
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = config["Orleans:DataBase"];
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                    options.CollectionPrefix = hostId.IsNullOrEmpty() ? "OrleansAevatar" :$"Orleans{hostId}";
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = config["Orleans:ClusterId"];
                    options.ServiceId = config["Orleans:ServiceId"];
                })
                .Configure<ClientMessagingOptions>(options =>
                {
                    options.ResponseTimeout = TimeSpan.FromMinutes(60);
                })
                .AddMemoryStreams("Aevatar");
        });
    }
}