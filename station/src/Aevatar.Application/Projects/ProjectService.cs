using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Notification;
using Aevatar.Organizations;
using Aevatar.Permissions;
using Aevatar.Service;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
using Microsoft.AspNetCore.Identity;
using IdentityRole = Volo.Abp.Identity.IdentityRole;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace Aevatar.Projects;

[RemoteService(IsEnabled = false)]
public class ProjectService : OrganizationService, IProjectService
{
    private readonly IProjectDomainRepository _domainRepository;
    private readonly IDeveloperService _developerService;

    public ProjectService(OrganizationUnitManager organizationUnitManager, IdentityUserManager identityUserManager,
        IRepository<OrganizationUnit, Guid> organizationUnitRepository, IdentityRoleManager roleManager,
        IPermissionManager permissionManager, IOrganizationPermissionChecker permissionChecker,
        IPermissionDefinitionManager permissionDefinitionManager, IRepository<IdentityUser, Guid> userRepository,
        INotificationService notificationService, IProjectDomainRepository domainRepository,
        IDeveloperService developerService) :
        base(organizationUnitManager, identityUserManager, organizationUnitRepository, roleManager, permissionManager,
            permissionChecker, permissionDefinitionManager, userRepository, notificationService)
    {
        _domainRepository = domainRepository;
        _developerService = developerService;
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto input)
    {
        return await CreateProjectInternalAsync(input.OrganizationId, input.DisplayName, input.DomainName);
    }

    /// <summary>
    /// 创建项目 - 自动域名版本
    /// 自动基于项目名称生成域名
    /// </summary>
    public async Task<ProjectDto> CreateProjectAsync(CreateProjectAutoDto input)
    {
        // 基于项目名称生成域名（框架已验证DisplayName不为空）
        var domainName = new string(input.DisplayName
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());

        if (string.IsNullOrEmpty(domainName))
        {
            throw new ArgumentException("Project name must contain at least one letter or digit", nameof(input.DisplayName));
        }

        return await CreateProjectInternalAsync(input.OrganizationId, input.DisplayName, domainName);
    }

    /// <summary>
    /// 项目创建的核心逻辑
    /// </summary>
    private async Task<ProjectDto> CreateProjectInternalAsync(Guid organizationId, string displayName, string domainName)
    {
        Logger.LogInformation("Starting project creation process. OrganizationId: {OrganizationId}, DisplayName: {DisplayName}, DomainName: {DomainName}", 
            organizationId, displayName, domainName);

        try
        {
            // 检查域名唯一性
            var domain = await _domainRepository.FirstOrDefaultAsync(o =>
                o.NormalizedDomainName == domainName.ToUpperInvariant() && o.IsDeleted == false);
            if (domain != null)
            {
                Logger.LogWarning("Domain name already exists: {DomainName}, ExistingProjectId: {ExistingProjectId}", 
                    domainName, domain.ProjectId);
                throw new UserFriendlyException($"DomainName: {domainName} already exists");
            }

            // 获取组织信息
            var organization = await OrganizationUnitRepository.GetAsync(organizationId);

            // 创建项目实体
            var trimmedDisplayName = displayName.Trim();
            var projectId = GuidGenerator.Create();
            var project = new OrganizationUnit(
                projectId,
                trimmedDisplayName,
                parentId: organization.Id
            );
            Logger.LogInformation("Project entity created with ID: {ProjectId}, Name: {ProjectName}", 
                projectId, trimmedDisplayName);

            // 插入域名记录
            await _domainRepository.InsertAsync(new ProjectDomain
            {
                OrganizationId = organization.Id,
                ProjectId = project.Id,
                DomainName = domainName,
                NormalizedDomainName = domainName.ToUpperInvariant()
            });
            Logger.LogInformation("Project domain record created successfully for: {DomainName}", domainName);

            // 创建角色
            var ownerRoleId = await AddOwnerRoleAsync(project.Id);
            var readerRoleId = await AddReaderRoleAsync(project.Id);
            Logger.LogInformation("Project roles created successfully. OwnerRoleId: {OwnerRoleId}, ReaderRoleId: {ReaderRoleId}", 
                ownerRoleId, readerRoleId);

            // 设置项目属性
            project.ExtraProperties[AevatarConsts.OrganizationTypeKey] = OrganizationType.Project;
            project.ExtraProperties[AevatarConsts.OrganizationRoleKey] = new List<Guid> { ownerRoleId, readerRoleId };

            // 在组织管理器中创建项目
            try
            {
                await OrganizationUnitManager.CreateAsync(project);
                Logger.LogInformation("Project created successfully in OrganizationUnitManager: {ProjectId}", project.Id);
            }
            catch (BusinessException ex)
                when (ex.Code == IdentityErrorCodes.DuplicateOrganizationUnitDisplayName)
            {
                Logger.LogWarning("Duplicate project name detected: {ProjectName}", trimmedDisplayName);
                throw new UserFriendlyException("The same project name already exists");
            }

            // 创建开发者服务
            await _developerService.CreateServiceAsync(domainName, project.Id);
            Logger.LogInformation("Developer service created successfully for domain: {DomainName}", domainName);

            // 构建返回DTO
            var dto = ObjectMapper.Map<OrganizationUnit, ProjectDto>(project);
            dto.DomainName = domainName;

            Logger.LogInformation("Project creation completed successfully. ProjectId: {ProjectId}, DomainName: {DomainName}", 
                project.Id, domainName);
            return dto;
        }
        catch (Exception ex) when (!(ex is UserFriendlyException))
        {
            Logger.LogError(ex, "Failed to create project. OrganizationId: {OrganizationId}, DisplayName: {DisplayName}, DomainName: {DomainName}", 
                organizationId, displayName, domainName);
            throw;
        }
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


}