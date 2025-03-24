using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.ApiKey;

namespace Aevatar.ApiKeys;

public interface IProjectApiKeyService
{
    Task CreateAsync(Guid projectId, string keyName, Guid? currentUserId);
    Task DeleteAsync(Guid apiKeyId);
    Task ModifyApiKeyAsync(Guid apiKeyId, string keyName);
    Task<List<ApiKeyInfo>> GetApiKeysAsync(Guid projectId);
}
