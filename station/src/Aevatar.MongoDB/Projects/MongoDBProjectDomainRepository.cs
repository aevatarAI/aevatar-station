using System;
using Aevatar.MongoDB;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.MongoDB;
using Volo.Abp.MongoDB;

namespace Aevatar.Projects;

public class MongoDBProjectDomainRepository : MongoDbRepository<AevatarMongoDbContext, ProjectDomain, Guid>,
    IProjectDomainRepository,
    ITransientDependency
{
    public MongoDBProjectDomainRepository(IMongoDbContextProvider<AevatarMongoDbContext> dbContextProvider) : base(
        dbContextProvider)
    {
    }
}