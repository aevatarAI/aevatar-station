using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Aevatar.ApiKey;

public interface IApiKeysRepository : IRepository<ApiKeyInfo, Guid>
{
    Task<List<ApiKeyInfo>> GetProjectApiKeys(Guid projectId, int limit, int skip);
    Task<bool> CheckProjectApiKeyNameExist(Guid projectId, string keyName);
    Task<ApiKeyInfo?> GetAsync(Guid guid);
}