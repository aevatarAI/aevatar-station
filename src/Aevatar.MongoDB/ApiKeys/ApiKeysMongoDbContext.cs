using Aevatar.ApiKey;
using MongoDB.Driver;
using Volo.Abp.MongoDB;

namespace Aevatar.ApiKeys;

public class ApiKeysMongoDbContext:AbpMongoDbContext
{
    public IMongoCollection<ApiKeyInfo> ApiKeyInfoCollection => Collection<ApiKeyInfo>();
}