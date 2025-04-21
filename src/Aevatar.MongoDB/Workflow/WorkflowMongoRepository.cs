using System;
using System.Threading.Tasks;
using Aevatar.MongoDB;
using MongoDB.Driver.Linq;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.MongoDB;
using Volo.Abp.MongoDB;

namespace Aevatar.Workflow;

public class WorkflowMongoRepository : MongoDbRepository<AevatarMongoDbContext, WorkflowInfo, Guid>,
    IWorkflowRepository,
    ITransientDependency
{
    public WorkflowMongoRepository(IMongoDbContextProvider<AevatarMongoDbContext> dbContextProvider) : base(
        dbContextProvider)
    {
    }

    public async Task<WorkflowInfo?> GetByWorkflowGrainId(string grainId)
    {
        var queryable = await GetMongoQueryableAsync();
        return await queryable.FirstOrDefaultAsync(w => w.WorkflowGrainId == grainId);
    }
}