using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Identity;

namespace Aevatar.Projects;

[RemoteService(IsEnabled = false)]
public class ProjectCorsOriginService : AevatarAppService, IProjectCorsOriginService
{
    private readonly IProjectCorsOriginRepository _projectCorsOriginRepository;
    private readonly IdentityUserManager _identityUserManager;

    public ProjectCorsOriginService(IProjectCorsOriginRepository projectCorsOriginRepository,
        IdentityUserManager identityUserManager)
    {
        _projectCorsOriginRepository = projectCorsOriginRepository;
        _identityUserManager = identityUserManager;
    }

    public async Task<ListResultDto<ProjectCorsOriginDto>> GetListAsync(Guid projectId)
    {
        var query = await _projectCorsOriginRepository.GetQueryableAsync();
        query = query.Where(o =>
                o.ProjectId == projectId && o.IsDeleted == false)
            .OrderBy(o => o.CreationTime);
        var projectCorsOriginDto = query.ToList();
        var projectCorsOriginDtos = ObjectMapper.Map<List<ProjectCorsOrigin>, List<ProjectCorsOriginDto>>(projectCorsOriginDto);

        foreach (var origin in projectCorsOriginDtos)
        {
            var creator = await _identityUserManager.GetByIdAsync(origin.CreatorId);
            origin.CreatorName = creator.UserName;
        }

        return new ListResultDto<ProjectCorsOriginDto>
        {
            Items = projectCorsOriginDtos
        };
    }

    public async Task<ProjectCorsOriginDto> CreateAsync(Guid projectId, CreateProjectCorsOriginDto input)
    {
        var corsOrigin = new ProjectCorsOrigin
        {
            ProjectId = projectId,
            Domain = input.Domain
        };

        corsOrigin = await _projectCorsOriginRepository.InsertAsync(corsOrigin);

        return ObjectMapper.Map<ProjectCorsOrigin, ProjectCorsOriginDto>(corsOrigin);
    }

    public async Task DeleteAsync(Guid projectId, Guid id)
    {
        await _projectCorsOriginRepository.DeleteAsync(o => o.Id == id && o.ProjectId == projectId);
    }
}
