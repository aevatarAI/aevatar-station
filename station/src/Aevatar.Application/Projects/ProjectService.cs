using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Common;
using Aevatar.Notification;
using Aevatar.Organizations;
using Aevatar.Permissions;
using Aevatar.Service;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using Volo.Abp.Caching;
using DistributedCacheEntryOptions = Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace Aevatar.Projects;

[RemoteService(IsEnabled = false)]
public class ProjectService : OrganizationService, IProjectService
{
    private readonly IProjectDomainRepository _domainRepository;
    private readonly IDeveloperService _developerService;
    private readonly IOrganizationRoleService _organizationRoleService;
    private readonly IDistributedCache<string, string> _recentUsedProjectCache;
    private const string UserRecentUsedProjectKey = "UserRecentUsedProjectKey";

    public ProjectService(OrganizationUnitManager organizationUnitManager, IdentityUserManager identityUserManager,
        IRepository<OrganizationUnit, Guid> organizationUnitRepository, IdentityRoleManager roleManager,
        IPermissionManager permissionManager, IOrganizationPermissionChecker permissionChecker,
        IPermissionDefinitionManager permissionDefinitionManager, IRepository<IdentityUser, Guid> userRepository,
        INotificationService notificationService, IProjectDomainRepository domainRepository,
        IDeveloperService developerService, IOrganizationRoleService organizationRoleService,
        IDistributedCache<string, string> recentUsedProjectCache) :
        base(organizationUnitManager, identityUserManager, organizationUnitRepository, roleManager, permissionManager,
            permissionChecker, permissionDefinitionManager, userRepository, notificationService)
    {
        _domainRepository = domainRepository;
        _developerService = developerService;
        _organizationRoleService = organizationRoleService;
        _recentUsedProjectCache = recentUsedProjectCache;
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto input)
    {
        var domain = await _domainRepository.FirstOrDefaultAsync(o =>
            o.NormalizedDomainName == input.DomainName.ToUpperInvariant() && o.IsDeleted == false);
        if (domain != null)
        {
            throw new UserFriendlyException($"DomainName: {input.DomainName} already exists");
        }

        var organization = await OrganizationUnitRepository.GetAsync(input.OrganizationId);

        var displayName = input.DisplayName.Trim();
        var project = new OrganizationUnit(
            GuidGenerator.Create(),
            displayName,
            parentId: organization.Id
        );

        await _domainRepository.InsertAsync(new ProjectDomain
        {
            OrganizationId = organization.Id,
            ProjectId = project.Id,
            DomainName = input.DomainName,
            NormalizedDomainName = input.DomainName.ToUpperInvariant()
        });

        var ownerRoleId = await AddOwnerRoleAsync(project.Id);
        var readerRoleId = await AddReaderRoleAsync(project.Id);

        project.ExtraProperties[AevatarConsts.OrganizationTypeKey] = OrganizationType.Project;
        project.ExtraProperties[AevatarConsts.OrganizationRoleKey] = new List<Guid> { ownerRoleId, readerRoleId };

        try
        {
            await OrganizationUnitManager.CreateAsync(project);
        }
        catch (BusinessException ex)
            when (ex.Code == IdentityErrorCodes.DuplicateOrganizationUnitDisplayName)
        {
            throw new UserFriendlyException("The same project name already exists");
        }

        await _developerService.CreateServiceAsync(input.DomainName, project.Id);

        var dto = ObjectMapper.Map<OrganizationUnit, ProjectDto>(project);
        dto.DomainName = input.DomainName;

        return dto;
    }

    public async Task<ProjectDto> CreateDefaultAsync(CreateDefaultProjectDto input)
    {
        var projectList = await GetListAsync(new GetProjectListDto()
        {
            OrganizationId = input.OrganizationId
        });
        if (projectList.Items.Count > 0)
        {
            throw new UserFriendlyException("Already have project.");
        }

        var randomHash = MD5Util.CalculateMD5(Guid.NewGuid().ToString());
        var projectDto = await CreateAsync(new CreateProjectDto()
        {
            OrganizationId = input.OrganizationId,
            DisplayName = "default project",
            DomainName = $"defaultProject{randomHash.Substring(randomHash.Length - 6)}"
        });
        
        var roleList = await _organizationRoleService.GetListAsync(projectDto.Id);
        var ownerRole = roleList.Items.FirstOrDefault(t => t.Name.Contains(AevatarConsts.OrganizationOwnerRoleName));
        if (ownerRole != null)
        {
            await SetMemberAsync(projectDto.Id, new SetOrganizationMemberDto()
            {
                Email = CurrentUser.Email!,
                Join = true,
                RoleId = ownerRole.Id
            });
        }
        return projectDto;
    }

    protected override List<string> GetOwnerPermissions()
    {
        return
        [
            AevatarPermissions.Members.Default,
            AevatarPermissions.Members.Manage,
            AevatarPermissions.ApiKeys.Default,
            AevatarPermissions.ApiKeys.Create,
            AevatarPermissions.ApiKeys.Edit,
            AevatarPermissions.ApiKeys.Delete,
            AevatarPermissions.Projects.Default,
            AevatarPermissions.Projects.Edit,
            AevatarPermissions.Roles.Default,
            AevatarPermissions.Roles.Create,
            AevatarPermissions.Roles.Edit,
            AevatarPermissions.Roles.Delete,
            AevatarPermissions.Dashboard,
            AevatarPermissions.LLMSModels.Default,
            AevatarPermissions.ApiRequests.Default,
            AevatarPermissions.ProjectCorsOrigins.Default,
            AevatarPermissions.ProjectCorsOrigins.Create,
            AevatarPermissions.ProjectCorsOrigins.Delete,
            AevatarPermissions.Plugins.Default,
            AevatarPermissions.Plugins.Create,
            AevatarPermissions.Plugins.Edit,
            AevatarPermissions.Plugins.Delete
        ];
    }
    
    protected override List<string> GetReaderPermissions()
    {
        return
        [
            AevatarPermissions.Projects.Default,
            AevatarPermissions.Members.Default,
            AevatarPermissions.ApiKeys.Default,
            AevatarPermissions.Dashboard,
            AevatarPermissions.LLMSModels.Default,
            AevatarPermissions.ApiRequests.Default,
            AevatarPermissions.ProjectCorsOrigins.Default,
            AevatarPermissions.Plugins.Default
        ];
    }

    public async Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectDto input)
    {
        var organization = await OrganizationUnitRepository.GetAsync(id);
        organization.DisplayName = input.DisplayName.Trim();
        await OrganizationUnitManager.UpdateAsync(organization);
        return ObjectMapper.Map<OrganizationUnit, ProjectDto>(organization);
    }

    public async Task<ListResultDto<ProjectDto>> GetListAsync(GetProjectListDto input)
    {
        List<OrganizationUnit> organizations;
        if (CurrentUser.IsInRole(AevatarConsts.AdminRoleName) ||
            await IsOrganizationOwnerAsync(input.OrganizationId, CurrentUser.Id.Value))
        {
            organizations = await OrganizationUnitManager.FindChildrenAsync(input.OrganizationId, true);
            organizations = organizations.Where(o =>
                o.TryGetExtraPropertyValue<OrganizationType>(AevatarConsts.OrganizationTypeKey, out var type) &&
                type == OrganizationType.Project).ToList();
        }
        else
        {
            var user = await IdentityUserManager.GetByIdAsync(CurrentUser.Id.Value);
            organizations = await IdentityUserManager.GetOrganizationUnitsAsync(user);
            organizations = organizations.Where(o =>
                o.ParentId == input.OrganizationId &&
                o.TryGetExtraPropertyValue<OrganizationType>(AevatarConsts.OrganizationTypeKey, out var type) &&
                type == OrganizationType.Project).ToList();
        }

        var domains =
            await _domainRepository.GetListAsync(o => o.OrganizationId == input.OrganizationId && o.IsDeleted == false);
        var domainDic = domains.ToDictionary(o => o.ProjectId, o => o.DomainName);

        var result = new List<ProjectDto>();
        foreach (var organization in organizations.OrderBy(o=>o.CreationTime))
        {
            var projectDto = ObjectMapper.Map<OrganizationUnit, ProjectDto>(organization);
            projectDto.MemberCount = await UserRepository.CountAsync(u =>
                u.OrganizationUnits.Any(ou => ou.OrganizationUnitId == organization.Id));
            projectDto.DomainName = domainDic[projectDto.Id];
            result.Add(projectDto);
        }

        return new ListResultDto<ProjectDto>
        {
            Items = result
        };
    }

    public async Task<ProjectDto> GetProjectAsync(Guid id)
    {
        var organization = await OrganizationUnitRepository.GetAsync(id);
        var projectDto = ObjectMapper.Map<OrganizationUnit, ProjectDto>(organization);
        var members = await IdentityUserManager.GetUsersInOrganizationUnitAsync(organization, true);
        projectDto.MemberCount = members.Count;

        var domain = await _domainRepository.GetAsync(o => o.ProjectId == id && o.IsDeleted == false);
        projectDto.DomainName = domain.DomainName; 
        
        return projectDto;
    }

    protected override async Task AddMemberAsync(Guid organizationId, IdentityUser user, Guid? roleId)
    {
        if (!roleId.HasValue)
        {
            throw new UserFriendlyException("Must set a user role.");
        }

        user.AddRole(roleId.Value);
        (await IdentityUserManager.UpdateAsync(user)).CheckErrors();
        (await IdentityUserManager.UpdateSecurityStampAsync(user)).CheckErrors();
        await IdentityUserManager.AddToOrganizationUnitAsync(user.Id, organizationId);
    }
    
    protected override async Task RemoveMemberAsync(Guid organizationId, IdentityUser user)
    {
        var children = await OrganizationUnitManager.FindChildrenAsync(organizationId, true);
        foreach (var child in children)
        {
            await RemoveMemberAsync(child, user.Id);
        }

        var organization = await OrganizationUnitRepository.GetAsync(organizationId);
        await RemoveMemberAsync(organization, user.Id);
    }

    public override async Task DeleteAsync(Guid id)
    {
        var domain = await _domainRepository.FirstOrDefaultAsync(o => o.ProjectId == id);
        await base.DeleteAsync(id);

        if (domain != null)
        {
            await _developerService.DeleteServiceAsync(domain.DomainName);
        }
    }

    public async Task SaveRecentUsedProjectAsync(RecentUsedProjectDto input)
    {
        try
        {
            await OrganizationUnitRepository.GetAsync(input.OrganizationId);
            await OrganizationUnitRepository.GetAsync(input.ProjectId);
        }
        catch (Exception e)
        {
            throw new UserFriendlyException("Organization or project not existed");
        }
        

        var userId = CurrentUser.Id;
        var cacheKey = $"{UserRecentUsedProjectKey}:{userId.ToString()}";
        await _recentUsedProjectCache.SetAsync(cacheKey, JsonConvert.SerializeObject(input), new DistributedCacheEntryOptions());
    }

    public async Task<RecentUsedProjectDto> GetRecentUsedProjectAsync()
    {
        var userId = CurrentUser.Id;
        var cacheKey = $"{UserRecentUsedProjectKey}:{userId.ToString()}";
        var value = await _recentUsedProjectCache.GetAsync(cacheKey);
        if (value.IsNullOrEmpty())
        {
            throw new UserFriendlyException("No recent used projectId");
        }
        return JsonConvert.DeserializeObject<RecentUsedProjectDto>(value)!;
    }
}