using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;

namespace Aevatar.Worker.Extensions;

public static class OrleansClientExtension
{
    public static IHostBuilder UseOrleansClientConfiguration(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseOrleansClient((context, clientBuilder) =>
        {
            var config = context.Configuration;

            clientBuilder
                .AddActivityPropagation()
                .UseMongoDBClient(config["Orleans:MongoDBClient"])
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = config["Orleans:DataBase"];
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                    options.CollectionPrefix = "OrleansAevatar";
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = config["Orleans:ClusterId"];
                    options.ServiceId = config["Orleans:ServiceId"];
                });
        });
    }
}