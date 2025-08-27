using System;
using Microsoft.Extensions.Logging.Abstractions;
using Mongo2Go;

namespace Aevatar.MongoDB;

public class AevatarMongoDbFixture : IDisposable
{
    private static readonly IMongoRunner MongoDbRunner;

    static AevatarMongoDbFixture()
    {
        MongoDbRunner = MongoDbRunner.Start(singleNodeReplSet: true, singleNodeReplSetWaitTimeout: 20, logger: NullLogger.Instance);
        ConnectionString = MongoDbRunner.ConnectionString;
    }

    public static string GetRandomConnectionString()
    {
        return GetConnectionString("Db_" + Guid.NewGuid().ToString("N"));
    }

    public static string GetConnectionString(string databaseName)
    {
        var stringArray = MongoDbRunner.ConnectionString.Split('?');
        var connectionString = stringArray[0].EnsureEndsWith('/') + databaseName + "/?" + stringArray[1];
        return connectionString;
    }

    public void Dispose()
    {
        MongoDbRunner?.Dispose();
    }
}
