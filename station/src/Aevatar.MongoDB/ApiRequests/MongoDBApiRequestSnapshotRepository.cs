using System;
using Aevatar.MongoDB;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.MongoDB;
using Volo.Abp.MongoDB;

namespace Aevatar.ApiRequests;

public class MongoDBApiRequestSnapshotRepository : MongoDbRepository<AevatarMongoDbContext, ApiRequestSnapshot, Guid>,
    IApiRequestSnapshotRepository,
    ITransientDependency
{
    public MongoDBApiRequestSnapshotRepository(IMongoDbContextProvider<AevatarMongoDbContext> dbContextProvider) : base(
        dbContextProvider)
    {
    }
}