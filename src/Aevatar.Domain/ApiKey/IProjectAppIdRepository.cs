using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.APIKeys;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;

namespace Aevatar.ApiKey;

public interface IProjectAppIdRepository : IRepository<ProjectAppIdInfo, Guid>
{
    Task<PagedResultDto<ProjectAppIdInfo>> GetProjectAppIds(APIKeyPagedRequestDto requestDto);
    Task<bool> CheckProjectAppNameExist(Guid projectId, string keyName);
    Task<ProjectAppIdInfo?> GetAsync(Guid guid);
}