using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.ApiKey;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.MongoDB;
using Volo.Abp.MongoDB;

namespace Aevatar.ApiKeys;

public class ApiKeyMongoRepository : MongoDbRepository<ApiKeysMongoDbContext, ApiKeyInfo, Guid>, IApiKeysRepository,
    ITransientDependency
{
    public ApiKeyMongoRepository(IMongoDbContextProvider<ApiKeysMongoDbContext> dbContextProvider) : base(
        dbContextProvider)
    {
    }

    public async Task<List<ApiKeyInfo>> GetProjectApiKeys(Guid projectId, int limit, int skip)
    {
        var queryable = await GetMongoQueryableAsync();
        var result = await queryable.Where(game => game.ProjectId == projectId)
            .OrderByDescending(o => o.CreationTime)
            .Take(limit)
            .Skip(skip).ToListAsync();

        return result;
    }

    public async Task<bool> CheckProjectApiKeyNameExist(Guid projectId, string keyName)
    {
        var queryable = await GetMongoQueryableAsync();
        var result = await queryable.FirstOrDefaultAsync(f => f.ProjectId == projectId && f.ApiKeyName == keyName);
        return result != null;
    }

    public async Task<ApiKeyInfo?> GetAsync(Guid guid)
    {
        var queryable = await GetMongoQueryableAsync();
        return await queryable.FirstOrDefaultAsync(f => f.Id == guid);
    }
}