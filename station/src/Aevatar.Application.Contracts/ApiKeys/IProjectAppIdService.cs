using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aevatar.ApiKeys;

public interface IProjectAppIdService
{
    Task CreateAsync(Guid projectId, string keyName, Guid? currentUserId);
    Task DeleteAsync(Guid appId);
    Task ModifyApiKeyAsync(Guid apiKeyId, string keyName);
    Task<List<ProjectAppIdListResponseDto>> GetApiKeysAsync(Guid projectId);
}
