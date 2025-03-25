using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.APIKeys;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;

namespace Aevatar.ApiKey;

public interface IApiKeysRepository : IRepository<ApiKeyInfo, Guid>
{
    Task<PagedResultDto<ApiKeyInfo>> GetProjectApiKeys(APIKeyPagedRequestDto requestDto);
    Task<bool> CheckProjectApiKeyNameExist(Guid projectId, string keyName);
    Task<ApiKeyInfo?> GetAsync(Guid guid);
}