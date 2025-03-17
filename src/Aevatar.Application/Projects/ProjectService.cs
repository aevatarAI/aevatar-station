using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Organizations;
using Aevatar.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace Aevatar.Projects;

[RemoteService(IsEnabled = false)]
public class ProjectService : OrganizationService, IProjectService
{
    public ProjectService(OrganizationUnitManager organizationUnitManager, IdentityUserManager identityUserManager,
        IRepository<OrganizationUnit, Guid> organizationUnitRepository, IdentityRoleManager roleManager,
        IPermissionManager permissionManager, IOrganizationPermissionChecker permissionChecker,
        IPermissionDefinitionManager permissionDefinitionManager, IRepository<IdentityUser, Guid> userRepository) :
        base(organizationUnitManager, identityUserManager, organizationUnitRepository, roleManager, permissionManager,
            permissionChecker, permissionDefinitionManager, userRepository)
    {
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto input)
    {
        var displayName = input.DisplayName.Trim();
        var organizationUnit = new OrganizationUnit(
            GuidGenerator.Create(),
            displayName,
            parentId:input.OrganizationId
        );
        
        var ownerRoleId = await AddOwnerRoleAsync(organizationUnit.Id);
        var readerRoleId = await AddReaderRoleAsync(organizationUnit.Id);

        organizationUnit.ExtraProperties[AevatarConsts.OrganizationTypeKey] = OrganizationType.Project;
        organizationUnit.ExtraProperties[AevatarConsts.OrganizationRoleKey] = new List<Guid> { ownerRoleId, readerRoleId };
        organizationUnit.ExtraProperties[AevatarConsts.ProjectDomainNameKey] = input.DomainName;

        await OrganizationUnitManager.CreateAsync(organizationUnit);
        
        return  ObjectMapper.Map<OrganizationUnit, ProjectDto>(organizationUnit);
    }
    
    protected override async Task<Guid> AddReaderRoleAsync(Guid organizationId)
    {
        var role = new IdentityRole(
            GuidGenerator.Create(),
            organizationId.ToString() + "_Reader"
        );
        await RoleManager.CreateAsync(role);
        await PermissionManager.SetForRoleAsync(role.Name, AevatarPermissions.Organizations.Default, true);
        await PermissionManager.SetForRoleAsync(role.Name, AevatarPermissions.OrganizationMembers.Default, true);
        await PermissionManager.SetForRoleAsync(role.Name, AevatarPermissions.ApiKeys.Default, true);

        return role.Id;
    }

    public async Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectDto input)
    {
        var organization = await OrganizationUnitRepository.GetAsync(id);
        organization.DisplayName = input.DisplayName.Trim();
        organization.ExtraProperties[AevatarConsts.ProjectDomainNameKey] = input.DomainName.Trim();
        await OrganizationUnitManager.UpdateAsync(organization);
        return ObjectMapper.Map<OrganizationUnit, ProjectDto>(organization);
    }

    public async Task<ListResultDto<ProjectDto>> GetListAsync(GetProjectListDto input)
    {
        List<OrganizationUnit> organizations;
        if (CurrentUser.IsInRole(AevatarConsts.AdminRoleName))
        {
            organizations = await OrganizationUnitManager.FindChildrenAsync(input.OrganizationId, true);
            organizations = organizations.Where(o =>
                o.TryGetExtraPropertyValue<OrganizationType>(AevatarConsts.OrganizationTypeKey,out var type) &&
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

        var result = new List<ProjectDto>();
        foreach (var organization in organizations)
        {
            var projectDto = ObjectMapper.Map<OrganizationUnit, ProjectDto>(organization);
            projectDto.MemberCount = await UserRepository.CountAsync(u =>
                u.OrganizationUnits.Any(ou => ou.OrganizationUnitId == organization.Id));
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
        var organizationDto = ObjectMapper.Map<OrganizationUnit, ProjectDto>(organization);
        var members = await IdentityUserManager.GetUsersInOrganizationUnitAsync(organization, true);
        organizationDto.MemberCount = members.Count;
        return organizationDto;
    }
}