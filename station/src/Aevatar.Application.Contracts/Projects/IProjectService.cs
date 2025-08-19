using System;
using System.Threading.Tasks;
using Aevatar.Organizations;
using Volo.Abp.Application.Dtos;

namespace Aevatar.Projects;

public interface IProjectService : IOrganizationService
{
    Task<ProjectDto> CreateAsync(CreateProjectDto input);
    /// <summary>
    /// 创建项目 - V2版本
    /// 自动基于项目名称生成域名
    /// </summary>
    Task<ProjectDto> CreateProjectAsync(CreateProjectV2Dto input);
    Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectDto input);
    Task<ListResultDto<ProjectDto>> GetListAsync(GetProjectListDto input);
    Task<ProjectDto> GetProjectAsync(Guid id);
}