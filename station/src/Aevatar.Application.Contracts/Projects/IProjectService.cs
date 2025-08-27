using System;
using System.Threading.Tasks;
using Aevatar.Organizations;
using Volo.Abp.Application.Dtos;

namespace Aevatar.Projects;

public interface IProjectService : IOrganizationService
{
    Task<ProjectDto> CreateAsync(CreateProjectDto input);
    Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectDto input);
    Task<ListResultDto<ProjectDto>> GetListAsync(GetProjectListDto input);
    Task<ProjectDto> GetProjectAsync(Guid id);
}