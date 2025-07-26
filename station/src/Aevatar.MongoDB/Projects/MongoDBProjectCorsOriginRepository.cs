using System;
using Aevatar.MongoDB;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.MongoDB;
using Volo.Abp.MongoDB;

namespace Aevatar.Projects;

public class MongoDBProjectCorsOriginRepository : MongoDbRepository<AevatarMongoDbContext, ProjectCorsOrigin, Guid>,
    IProjectCorsOriginRepository,
    ITransientDependency
{
    public MongoDBProjectCorsOriginRepository(IMongoDbContextProvider<AevatarMongoDbContext> dbContextProvider) : base(
        dbContextProvider)
    {
    }
}
