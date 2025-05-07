using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Notification;
using Aevatar.Notification.Parameters;
using Aevatar.Permissions;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace Aevatar.Organizations;

[RemoteService(IsEnabled = false)]
public class OrganizationService : AevatarAppService, IOrganizationService
{
    protected readonly OrganizationUnitManager OrganizationUnitManager;
    protected readonly IdentityUserManager IdentityUserManager;
    protected readonly IRepository<OrganizationUnit, Guid> OrganizationUnitRepository;
    protected readonly IdentityRoleManager RoleManager;
    protected readonly IPermissionManager PermissionManager;
    protected readonly IOrganizationPermissionChecker PermissionChecker;
    protected readonly IPermissionDefinitionManager PermissionDefinitionManager;
    protected readonly IRepository<IdentityUser, Guid> UserRepository;
    private readonly IDistributedEventBus _distributedEvent;

    protected const string OwnerRoleName = "Owner";
    protected const string ReaderRoleName = "Reader";

    public OrganizationService(OrganizationUnitManager organizationUnitManager, IdentityUserManager identityUserManager,
        IRepository<OrganizationUnit, Guid> organizationUnitRepository, IdentityRoleManager roleManager,
        IPermissionManager permissionManager, IOrganizationPermissionChecker permissionChecker,
        IPermissionDefinitionManager permissionDefinitionManager, IRepository<IdentityUser, Guid> userRepository,
        IDistributedEventBus distributedEvent)
    {
        OrganizationUnitManager = organizationUnitManager;
        IdentityUserManager = identityUserManager;
        OrganizationUnitRepository = organizationUnitRepository;
        RoleManager = roleManager;
        PermissionManager = permissionManager;
        PermissionChecker = permissionChecker;
        PermissionDefinitionManager = permissionDefinitionManager;
        UserRepository = userRepository;
        _distributedEvent = distributedEvent;
    }

    public virtual async Task<ListResultDto<OrganizationDto>> GetListAsync(GetOrganizationListDto input)
    {
        List<OrganizationUnit> organizations;
        if (CurrentUser.IsInRole(AevatarConsts.AdminRoleName))
        {
            organizations = await OrganizationUnitRepository.GetListAsync();
        }
        else
        {
            var user = await IdentityUserManager.GetByIdAsync(CurrentUser.Id.Value);
            organizations = await IdentityUserManager.GetOrganizationUnitsAsync(user);
        }

        organizations = organizations.Where(o =>
            o.TryGetExtraPropertyValue<OrganizationType>(AevatarConsts.OrganizationTypeKey, out var type) &&
            type == OrganizationType.Organization).ToList();

        return new ListResultDto<OrganizationDto>
        {
            Items = ObjectMapper.Map<List<OrganizationUnit>, List<OrganizationDto>>(organizations)
        };
    }

    public virtual async Task<OrganizationDto> GetAsync(Guid id)
    {
        var organization = await OrganizationUnitRepository.GetAsync(id);
        var organizationDto = ObjectMapper.Map<OrganizationUnit, OrganizationDto>(organization);

        var organizationUnits = await OrganizationUnitManager
            .FindChildrenAsync(id, recursive: true);
        organizationUnits.Add(organization);
        var organizationUnitIds = organizationUnits
            .Select(ou => ou.Id)
            .ToList();
        var userCount = await UserRepository
            .CountAsync(u => u.OrganizationUnits.Any(ou => organizationUnitIds.Contains(ou.OrganizationUnitId)));
        organizationDto.MemberCount = userCount;

        return organizationDto;
    }

    public virtual async Task<OrganizationDto> CreateAsync(CreateOrganizationDto input)
    {
        var displayName = input.DisplayName.Trim();
        var organizationUnit = new OrganizationUnit(
            GuidGenerator.Create(),
            displayName
        );

        var ownerRoleId = await AddOwnerRoleAsync(organizationUnit.Id);
        var readerRoleId = await AddReaderRoleAsync(organizationUnit.Id);

        organizationUnit.ExtraProperties[AevatarConsts.OrganizationTypeKey] = OrganizationType.Organization;
        organizationUnit.ExtraProperties[AevatarConsts.OrganizationRoleKey] =
            new List<Guid> { ownerRoleId, readerRoleId };
        await OrganizationUnitManager.CreateAsync(organizationUnit);

        if (!CurrentUser.IsInRole(AevatarConsts.AdminRoleName))
        {
            await IdentityUserManager.AddToOrganizationUnitAsync(CurrentUser.Id.Value, organizationUnit.Id);
            var user = await IdentityUserManager.GetByIdAsync(CurrentUser.Id.Value);
            user.AddRole(ownerRoleId);
            await IdentityUserManager.UpdateAsync(user);
        }

        return ObjectMapper.Map<OrganizationUnit, OrganizationDto>(organizationUnit);
    }

    protected virtual async Task<Guid> AddOwnerRoleAsync(Guid organizationId)
    {
        var role = new IdentityRole(
            GuidGenerator.Create(),
            GetOrganizationRoleName(organizationId, OwnerRoleName)
        );
        await RoleManager.CreateAsync(role);
        await PermissionManager.SetForRoleAsync(role.Name, AevatarPermissions.Organizations.Default, true);
        await PermissionManager.SetForRoleAsync(role.Name, AevatarPermissions.Organizations.Create, true);
        await PermissionManager.SetForRoleAsync(role.Name, AevatarPermissions.Organizations.Edit, true);
        await PermissionManager.SetForRoleAsync(role.Name, AevatarPermissions.Organizations.Delete, true);
        await PermissionManager.SetForRoleAsync(role.Name, AevatarPermissions.OrganizationMembers.Default, true);
        await PermissionManager.SetForRoleAsync(role.Name, AevatarPermissions.OrganizationMembers.Manage, true);
        await PermissionManager.SetForRoleAsync(role.Name, AevatarPermissions.ApiKeys.Default, true);
        await PermissionManager.SetForRoleAsync(role.Name, AevatarPermissions.ApiKeys.Create, true);
        await PermissionManager.SetForRoleAsync(role.Name, AevatarPermissions.ApiKeys.Edit, true);
        await PermissionManager.SetForRoleAsync(role.Name, AevatarPermissions.ApiKeys.Delete, true);

        return role.Id;
    }

    protected virtual async Task<Guid> AddReaderRoleAsync(Guid organizationId)
    {
        var role = new IdentityRole(
            GuidGenerator.Create(),
            GetOrganizationRoleName(organizationId, ReaderRoleName)
        );
        await RoleManager.CreateAsync(role);
        await PermissionManager.SetForRoleAsync(role.Name, AevatarPermissions.Organizations.Default, true);
        await PermissionManager.SetForRoleAsync(role.Name, AevatarPermissions.OrganizationMembers.Default, true);

        return role.Id;
    }

    public virtual async Task<OrganizationDto> UpdateAsync(Guid id, UpdateOrganizationDto input)
    {
        var organization = await OrganizationUnitRepository.GetAsync(id);
        organization.DisplayName = input.DisplayName.Trim();
        await OrganizationUnitManager.UpdateAsync(organization);
        return ObjectMapper.Map<OrganizationUnit, OrganizationDto>(organization);
    }

    public virtual async Task DeleteAsync(Guid id)
    {
        var children = await OrganizationUnitManager.FindChildrenAsync(id, true);
        foreach (var child in children)
        {
            await DeleteOrganizationRoleAsync(child);
        }

        var organizationUnit = await OrganizationUnitRepository.GetAsync(id);
        await DeleteOrganizationRoleAsync(organizationUnit);

        await OrganizationUnitManager.DeleteAsync(id);
    }

    public virtual async Task<ListResultDto<OrganizationMemberDto>> GetMemberListAsync(Guid organizationId,
        GetOrganizationMemberListDto input)
    {
        var organization = await OrganizationUnitRepository.GetAsync(organizationId);
        var members = await IdentityUserManager.GetUsersInOrganizationUnitAsync(organization, true);
        var result = new List<OrganizationMemberDto>();
        foreach (var member in members)
        {
            var memberDto = ObjectMapper.Map<IdentityUser, OrganizationMemberDto>(member);
            memberDto.RoleId = FindOrganizationRole(organization, member);
            result.Add(memberDto);
        }

        return new ListResultDto<OrganizationMemberDto>
        {
            Items = result
        };
    }

    public virtual async Task SetMemberAsync(Guid organizationId, SetOrganizationMemberDto input)
    {
        var user = await IdentityUserManager.FindByEmailAsync(input.Email);
        if (user == null)
        {
            throw new UserFriendlyException("User not exists.");
        }

        if (input.Join)
        {
            await AddMemberAsync(organizationId, user, input.RoleId);
        }
        else
        {
            await RemoveMemberAsync(organizationId, user);
        }
    }

    protected virtual async Task AddMemberAsync(Guid organizationId, IdentityUser user, Guid? roleId)
    {
        await IdentityUserManager.AddToOrganizationUnitAsync(user.Id, organizationId);
        await CurrentUnitOfWork.SaveChangesAsync();
        
        await _distributedEvent.PublishAsync(new NotificationCreatForEventBusDto()
        {
            Type = NotificationTypeEnum.OrganizationInvitation,
            Creator = CurrentUser.Id.Value,
            Target = user.Id,
            Content = JsonConvert.SerializeObject(new OrganizationVisitInfo
            {
                Creator = CurrentUser.Id.Value,
                OrganizationId = organizationId,
                RoleId = roleId.Value,
                Vistor = user.Id
            })
        });
    }

    protected virtual async Task RemoveMemberAsync(Guid organizationId, IdentityUser user)
    {
        var children = await OrganizationUnitManager.FindChildrenAsync(organizationId, true);
        foreach (var child in children)
        {
            await RemoveMemberAsync(child, user.Id);
        }

        var organization = await OrganizationUnitRepository.GetAsync(organizationId);
        await RemoveMemberAsync(organization, user.Id);
    }

    private async Task RemoveMemberAsync(OrganizationUnit organization, Guid userId)
    {
        var user = await IdentityUserManager.GetByIdAsync(userId);
        if (!user.IsInOrganizationUnit(organization.Id))
        {
            return;
        }

        var role = FindOrganizationRole(organization, user);
        if (role.HasValue)
        {
            user.RemoveRole(role.Value);
            await IdentityUserManager.UpdateAsync(user);
        }

        await IdentityUserManager.RemoveFromOrganizationUnitAsync(user.Id, organization.Id);
    }

    public virtual async Task SetMemberRoleAsync(Guid organizationId, SetOrganizationMemberRoleDto input)
    {
        var user = await IdentityUserManager.GetByIdAsync(input.UserId);

        if (!user.IsInOrganizationUnit(organizationId))
        {
            throw new UserFriendlyException("User is not in current organization.");
        }

        var organization = await OrganizationUnitRepository.GetAsync(organizationId);
        var existRole = FindOrganizationRole(organization, user);
        if (existRole.HasValue)
        {
            user.RemoveRole(existRole.Value);
        }

        user.AddRole(input.RoleId);
        await IdentityUserManager.UpdateAsync(user);
    }

    public virtual async Task<ListResultDto<IdentityRoleDto>> GetRoleListAsync(Guid organizationId)
    {
        var organization = await OrganizationUnitRepository.GetAsync(organizationId);

        var result = new List<IdentityRoleDto>();
        if (organization.TryGetOrganizationRoles(AevatarConsts.OrganizationRoleKey, out var roleIds))
        {
            foreach (var roleId in roleIds)
            {
                var role = await RoleManager.GetByIdAsync(roleId);
                result.Add(ObjectMapper.Map<IdentityRole, IdentityRoleDto>(role));
            }
        }

        return new ListResultDto<IdentityRoleDto>
        {
            Items = result
        };
    }

    public virtual async Task<ListResultDto<PermissionGrantInfoDto>> GetPermissionListAsync(Guid organizationId)
    {
        var group = await PermissionDefinitionManager.GetGroupsAsync();
        var developerPlatformPermission = group.First(o => o.Name == AevatarPermissions.DeveloperPlatform);

        var permissions = new List<PermissionGrantInfoDto>();
        foreach (var permission in developerPlatformPermission.GetPermissionsWithChildren())
        {
            if (await PermissionChecker.IsGrantedAsync(organizationId, permission.Name))
            {
                permissions.Add(new PermissionGrantInfoDto
                {
                    Name = permission.Name,
                    DisplayName = permission.DisplayName?.Localize(StringLocalizerFactory),
                    ParentName = permission.Parent?.Name,
                    AllowedProviders = permission.Providers,
                    GrantedProviders = new List<ProviderInfoDto>(),
                    IsGranted = true
                });
            }
        }

        return new ListResultDto<PermissionGrantInfoDto>
        {
            Items = permissions
        };
    }

    protected virtual async Task DeleteOrganizationRoleAsync(OrganizationUnit organizationUnit)
    {
        if (organizationUnit.TryGetOrganizationRoles(AevatarConsts.OrganizationRoleKey, out var roles))
        {
            foreach (var roleId in roles)
            {
                var role = await RoleManager.FindByIdAsync(roleId.ToString());
                if (role != null)
                {
                    await RoleManager.DeleteAsync(role);
                }
            }
        }
    }

    protected virtual Guid? FindOrganizationRole(OrganizationUnit organizationUnit, IdentityUser user)
    {
        if (!organizationUnit.TryGetOrganizationRoles(AevatarConsts.OrganizationRoleKey, out var roles))
        {
            return null;
        }

        foreach (var role in roles)
        {
            if (user.IsInRole(role))
            {
                return role;
            }
        }

        return null;
    }

    protected virtual async Task<bool> IsOrganizationOwonerAsync(Guid organizationId, Guid userId)
    {
        var organization = await OrganizationUnitRepository.GetAsync(organizationId);
        var user = await IdentityUserManager.GetByIdAsync(userId);
        var roleId = FindOrganizationRole(organization, user);
        if (!roleId.HasValue)
        {
            return false;
        }

        var role = await RoleManager.FindByIdAsync(roleId.Value.ToString());
        return role.Name == GetOrganizationRoleName(organizationId, OwnerRoleName);
    }

    protected virtual string GetOrganizationRoleName(Guid organizationId, string roleName)
    {
        return $"{organizationId.ToString()}_{roleName}";
    }
}