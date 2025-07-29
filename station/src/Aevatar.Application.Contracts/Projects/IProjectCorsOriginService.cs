using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace Aevatar.Projects;

public interface IProjectCorsOriginService
{
    Task<ListResultDto<ProjectCorsOriginDto>> GetListAsync(Guid projectId);

    Task<ProjectCorsOriginDto> CreateAsync(Guid projectId, CreateProjectCorsOriginDto input);

    Task DeleteAsync(Guid projectId, Guid id);
}
