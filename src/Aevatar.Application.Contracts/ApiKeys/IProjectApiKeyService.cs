using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aevatar.ApiKeys;

public interface IProjectApiKeyService
{
    Task CreateAsync(Guid projectId, string keyName);
    Task DeleteAsync(Guid apiKeyId);
    Task ModifyApiKeyAsync(Guid apiKeyId, string keyName);
    Task<List<ApiKeyListResponseDto>> GetApiKeysAsync(Guid projectId);
}
