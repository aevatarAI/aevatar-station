using Aevatar.EventSourcing.MongoDB.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aevatar.EventSourcing.MongoDB;

public static class MongoDbLogConsistentStorageFactory
{
    public static MongoDbLogConsistentStorage Create(IServiceProvider serviceProvider, object name)
    {
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<MongoDbStorageOptions>>();
        return ActivatorUtilities.CreateInstance<MongoDbLogConsistentStorage>(serviceProvider, name,
            options.Get(name as string));
    }
}