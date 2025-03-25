using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.ApiKey;
using Aevatar.APIKeys;
using Aevatar.MongoDB;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.MongoDB;
using Volo.Abp.MongoDB;

namespace Aevatar.ApiKeys;

public class ApiKeyMongoRepository : MongoDbRepository<AevatarMongoDbContext, ApiKeyInfo, Guid>, IApiKeysRepository,
    ITransientDependency
{
    public ApiKeyMongoRepository(IMongoDbContextProvider<AevatarMongoDbContext> dbContextProvider) : base(
        dbContextProvider)
    {
    }

    public async Task<PagedResultDto<ApiKeyInfo>> GetProjectApiKeys(APIKeyPagedRequestDto requestDto)
    {
        var queryable = await GetMongoQueryableAsync();
        if (requestDto.ProjectId != Guid.Empty)
        {
            queryable = queryable.Where(w => w.ProjectId == requestDto.ProjectId);
        }

        var result = new List<ApiKeyInfo>();
        var queryResponse = await queryable
            .OrderByDescending(o => o.CreationTime)
            .Take(requestDto.MaxResultCount)
            .Skip(requestDto.SkipCount).ToListAsync();

        
        if (queryResponse != null)
        {
            result = queryResponse;
        }
        
        return new PagedResultDto<ApiKeyInfo>(result.Count, result.AsReadOnly());
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