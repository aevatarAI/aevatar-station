using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Notification;
using Aevatar.Notification.Parameters;
using Aevatar.Permissions;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
using IdentityRole = Volo.Abp.Identity.IdentityRole;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

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
    protected readonly INotificationService NotificationService;

    public OrganizationService(OrganizationUnitManager organizationUnitManager, IdentityUserManager identityUserManager,
        IRepository<OrganizationUnit, Guid> organizationUnitRepository, IdentityRoleManager roleManager,
        IPermissionManager permissionManager, IOrganizationPermissionChecker permissionChecker,
        IPermissionDefinitionManager permissionDefinitionManager, IRepository<IdentityUser, Guid> userRepository,
        INotificationService notificationService)
    {
        OrganizationUnitManager = organizationUnitManager;
        IdentityUserManager = identityUserManager;
        OrganizationUnitRepository = organizationUnitRepository;
        RoleManager = roleManager;
        PermissionManager = permissionManager;
        PermissionChecker = permissionChecker;
        PermissionDefinitionManager = permissionDefinitionManager;
        UserRepository = userRepository;
        NotificationService = notificationService;
    }

    public virtual async Task<ListResultDto<OrganizationDto>> GetListAsync(GetOrganizationListDto input)
    {
        var result = new List<OrganizationDto>();
        List<OrganizationUnit> organizations;
        if (CurrentUser.IsInRole(AevatarConsts.AdminRoleName))
        {
            organizations = await OrganizationUnitRepository.GetListAsync();
            foreach (var organization in organizations.OrderBy(o=>o.CreationTime))
            {
                if (!organization.TryGetExtraPropertyValue<OrganizationType>(AevatarConsts.OrganizationTypeKey,
                        out var type) || type != OrganizationType.Organization)
                {
                    continue;
                }
                
                result.Add(ObjectMapper.Map<OrganizationUnit, OrganizationDto>(organization));
            }
        }
        else
        {
            var user = await IdentityUserManager.GetByIdAsync(CurrentUser.Id.Value);
            organizations = await IdentityUserManager.GetOrganizationUnitsAsync(user);
            foreach (var organization in organizations.OrderBy(o=>o.CreationTime))
            {
                if (!organization.TryGetExtraPropertyValue<OrganizationType>(AevatarConsts.OrganizationTypeKey,
                        out var type) || type != OrganizationType.Organization)
                {
                    continue;
                }

                if (FindOrganizationRole(organization, user) == null)
                {
                    continue;
                }

                result.Add(ObjectMapper.Map<OrganizationUnit, OrganizationDto>(organization));
            }
        }

        return new ListResultDto<OrganizationDto>
        {
            Items = result
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
        try
        {
            await OrganizationUnitManager.CreateAsync(organizationUnit);
        }
        catch (BusinessException ex)
            when (ex.Code == IdentityErrorCodes.DuplicateOrganizationUnitDisplayName)
        {
            throw new UserFriendlyException("The same organization name already exists");
        }

        if (!CurrentUser.IsInRole(AevatarConsts.AdminRoleName))
        {
            await IdentityUserManager.AddToOrganizationUnitAsync(CurrentUser.Id.Value, organizationUnit.Id);
            var user = await IdentityUserManager.GetByIdAsync(CurrentUser.Id.Value);
            user.AddRole(ownerRoleId);
            (await IdentityUserManager.UpdateAsync(user)).CheckErrors();
            (await IdentityUserManager.UpdateSecurityStampAsync(user)).CheckErrors();
        }

        return ObjectMapper.Map<OrganizationUnit, OrganizationDto>(organizationUnit);
    }

    protected virtual async Task<Guid> AddOwnerRoleAsync(Guid organizationId)
    {
        var role = new IdentityRole(
            GuidGenerator.Create(),
            OrganizationRoleHelper.GetRoleName(organizationId, AevatarConsts.OrganizationOwnerRoleName)
        );
        (await RoleManager.CreateAsync(role)).CheckErrors();

        foreach (var permission in GetOwnerPermissions())
        {
            await PermissionManager.SetForRoleAsync(role.Name, permission, true);
        }

        return role.Id;
    }

    protected virtual List<string> GetOwnerPermissions()
    {
        return
        [
            AevatarPermissions.Organizations.Default,
            AevatarPermissions.Organizations.Edit,
            AevatarPermissions.Organizations.Delete,
            AevatarPermissions.Members.Default,
            AevatarPermissions.Members.Manage,
            AevatarPermissions.ApiKeys.Default,
            AevatarPermissions.ApiKeys.Create,
            AevatarPermissions.ApiKeys.Edit,
            AevatarPermissions.ApiKeys.Delete,
            AevatarPermissions.Projects.Default,
            AevatarPermissions.Projects.Create,
            AevatarPermissions.Projects.Edit,
            AevatarPermissions.Projects.Delete,
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

    protected virtual async Task<Guid> AddReaderRoleAsync(Guid organizationId)
    {
        var role = new IdentityRole(
            GuidGenerator.Create(),
            OrganizationRoleHelper.GetRoleName(organizationId, AevatarConsts.OrganizationReaderRoleName)
        );
        (await RoleManager.CreateAsync(role)).CheckErrors();
        
        foreach (var permission in GetReaderPermissions())
        {
            await PermissionManager.SetForRoleAsync(role.Name, permission, true);
        }

        return role.Id;
    }
    
    protected virtual List<string> GetReaderPermissions()
    {
        return
        [
            AevatarPermissions.Organizations.Default,
            AevatarPermissions.Members.Default
        ];
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
            memberDto.Status = GetMemberStatus(organizationId, member);

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
            if (user.Id == CurrentUser.Id.Value)
            {
                throw new UserFriendlyException("Can't remove yourself.");
            }

            await RemoveMemberAsync(organizationId, user);
        }
    }

    protected virtual async Task AddMemberAsync(Guid organizationId, IdentityUser user, Guid? roleId)
    {
        var notificationId = await NotificationService.CreateAsync(NotificationTypeEnum.OrganizationInvitation,
            CurrentUser.Id.Value, user.Id, JsonConvert.SerializeObject(new OrganizationVisitInfo
            {
                Creator = CurrentUser.Id.Value,
                OrganizationId = organizationId,
                RoleId = roleId.Value,
                Vistor = user.Id
            }));
        
        SetMemberStatus(organizationId, user, MemberStatus.Inviting);
        SetMemberInvitationInfo(organizationId,user, new MemberInvitationInfo
        {
            Inviter = CurrentUser.Id.Value,
            InvitationId = notificationId
        });

        (await IdentityUserManager.UpdateAsync(user)).CheckErrors();
        await IdentityUserManager.AddToOrganizationUnitAsync(user.Id, organizationId);
    }

    protected virtual async Task RemoveMemberAsync(Guid organizationId, IdentityUser user)
    {
        var userStatus = GetMemberStatus(organizationId, user);
        if (userStatus == MemberStatus.Inviting)
        {
            var invitationInfo = GetMemberInvitationInfo(organizationId, user);
            await NotificationService.WithdrawAsync(invitationInfo.Inviter, invitationInfo.InvitationId);
        }

        var children = await OrganizationUnitManager.FindChildrenAsync(organizationId, true);
        foreach (var child in children)
        {
            await RemoveMemberAsync(child, user.Id);
        }

        var organization = await OrganizationUnitRepository.GetAsync(organizationId);
        await RemoveMemberAsync(organization, user.Id);
    }

    protected virtual async Task RemoveMemberAsync(OrganizationUnit organization, Guid userId)
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
            (await IdentityUserManager.UpdateAsync(user)).CheckErrors();
            (await IdentityUserManager.UpdateSecurityStampAsync(user)).CheckErrors();
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
        SetMemberStatus(organizationId, user, MemberStatus.Joined);
        
        (await IdentityUserManager.UpdateAsync(user)).CheckErrors();;
        (await IdentityUserManager.UpdateSecurityStampAsync(user)).CheckErrors();;
    }

    public virtual async Task RefuseInvitationAsync(Guid organizationId, Guid userId)
    {
        var user = await IdentityUserManager.GetByIdAsync(userId);

        if (!user.IsInOrganizationUnit(organizationId))
        {
            throw new UserFriendlyException("User is not in current organization.");
        }
        
        var userStatus = GetMemberStatus(organizationId, user);
        if (userStatus == MemberStatus.Inviting)
        {
            SetMemberStatus(organizationId,user,MemberStatus.Refused);
            (await IdentityUserManager.UpdateAsync(user)).CheckErrors();;
        }
    }

    public virtual async Task<ListResultDto<IdentityRoleDto>> GetRoleListAsync(Guid organizationId)
    {
        var organization = await OrganizationUnitRepository.GetAsync(organizationId);

        var result = new List<IdentityRoleDto>();
        if (organization.TryGetOrganizationRoles(out var roleIds))
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
        if (organizationUnit.TryGetOrganizationRoles(out var roles))
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
        if (!organizationUnit.TryGetOrganizationRoles(out var roles))
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

    protected virtual async Task<bool> IsOrganizationOwnerAsync(Guid organizationId,Guid userId)
    {
        var organization = await OrganizationUnitRepository.GetAsync(organizationId);
        var user = await IdentityUserManager.GetByIdAsync(userId);
        var roleId = FindOrganizationRole(organization, user);
        if (!roleId.HasValue)
        {
            return false;
        }

        var role = await RoleManager.FindByIdAsync(roleId.Value.ToString());
        return role.Name == OrganizationRoleHelper.GetRoleName(organizationId, AevatarConsts.OrganizationOwnerRoleName);
    }

    private MemberStatus GetMemberStatus(Guid organizationId, IdentityUser user)
    {
        if (user.ExtraProperties.TryGetValue(AevatarConsts.MemberStatusKey, out var status))
        {
            var userStatus = status as Dictionary<string, object>;
            if (userStatus.TryGetValue(organizationId.ToString(), out var value))
            {
                return (MemberStatus)value;
            }
        }

        return MemberStatus.Joined;
    }

    private void SetMemberStatus(Guid organizationId, IdentityUser user, MemberStatus memberStatus)
    {
        if (user.ExtraProperties.TryGetValue(AevatarConsts.MemberStatusKey, out var status))
        {
            var userStatus = status as Dictionary<string, object>;
            userStatus[organizationId.ToString()] = memberStatus;
            user.ExtraProperties[AevatarConsts.MemberStatusKey] = userStatus;
        }
        else
        {
            user.ExtraProperties[AevatarConsts.MemberStatusKey] = new Dictionary<string, MemberStatus>
                { { organizationId.ToString(), memberStatus } };
        }
    }
    
    private MemberInvitationInfo GetMemberInvitationInfo(Guid organizationId, IdentityUser user)
    {
        if (user.ExtraProperties.TryGetValue(AevatarConsts.MemberInvitationInfoKey, out var info))
        {
            var invitationInfo = info as Dictionary<string, object>;
            if (invitationInfo.TryGetValue(organizationId.ToString(), out var value))
            {
                return JsonConvert.DeserializeObject<MemberInvitationInfo>(value as string);
            }
        }

        return null;
    }

    private void SetMemberInvitationInfo(Guid organizationId, IdentityUser user, MemberInvitationInfo memberInvitation)
    {
        if (user.ExtraProperties.TryGetValue(AevatarConsts.MemberInvitationInfoKey, out var info))
        {
            var invitationInfo = info as Dictionary<string, object>;
            invitationInfo[organizationId.ToString()] = JsonConvert.SerializeObject(memberInvitation);
            user.ExtraProperties[AevatarConsts.MemberInvitationInfoKey] = invitationInfo;
        }
        else
        {
            user.ExtraProperties[AevatarConsts.MemberInvitationInfoKey] = new Dictionary<string, string>
                { { organizationId.ToString(), JsonConvert.SerializeObject(memberInvitation) } };
        }
    }
}