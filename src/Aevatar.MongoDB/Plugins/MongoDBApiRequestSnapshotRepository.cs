using System;
using Aevatar.ApiRequests;
using Aevatar.MongoDB;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.MongoDB;
using Volo.Abp.MongoDB;

namespace Aevatar.Plugins;

public class MongoDBPluginRepository : MongoDbRepository<AevatarMongoDbContext, Plugin, Guid>,
    IPluginRepository,
    ITransientDependency
{
    public MongoDBPluginRepository(IMongoDbContextProvider<AevatarMongoDbContext> dbContextProvider) : base(
        dbContextProvider)
    {
    }
}