using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SimpleStateChecking;
using System.Linq;
using Aevatar.Permissions;

namespace Aevatar.Organizations;

[RemoteService(IsEnabled = false)]
public class OrganizationPermissionService : AevatarAppService, IOrganizationPermissionService
{
    protected readonly PermissionManagementOptions Options;
    protected readonly IPermissionManager PermissionManager;
    protected readonly IPermissionDefinitionManager PermissionDefinitionManager;
    protected readonly ISimpleStateCheckerManager<PermissionDefinition> SimpleStateCheckerManager;
    
    public OrganizationPermissionService(
        IPermissionManager permissionManager,
        IPermissionDefinitionManager permissionDefinitionManager,
        IOptionsSnapshot<PermissionManagementOptions> options,
        ISimpleStateCheckerManager<PermissionDefinition> simpleStateCheckerManager)
    {
        Options = options.Value;
        PermissionManager = permissionManager;
        PermissionDefinitionManager = permissionDefinitionManager;
        SimpleStateCheckerManager = simpleStateCheckerManager;
    }

    public virtual PermissionScope PermissionScope { get; } = PermissionScope.Organization;

    public async Task<GetPermissionListResultDto> GetAsync(Guid organizationId, string providerName, string providerKey)
    {
        OrganizationRoleHelper.CheckRoleInOrganization(organizationId, providerKey);
        await CheckProviderPolicy(providerName);

        var result = new GetPermissionListResultDto
        {
            EntityDisplayName = providerKey,
            Groups = new List<PermissionGroupDto>()
        };

        foreach (var group in (await PermissionDefinitionManager.GetGroupsAsync()).Where(o =>
                     o.Name == AevatarPermissions.DeveloperPlatform))
        {
            var groupDto = CreatePermissionGroupDto(group);

            var neededCheckPermissions = new List<PermissionDefinition>();

            var permissions = group.GetPermissionsWithChildren()
                .Where(x => x.IsEnabled)
                .Where(x => !x.Providers.Any() || x.Providers.Contains(providerName))
                .Where(x => x.Properties.TryGetValue(AevatarPermissions.OrganizationScopeKey, out var value) &&
                            ((PermissionScope)value).HasFlag(PermissionScope));

            foreach (var permission in permissions)
            {
                if (permission.Parent != null && !neededCheckPermissions.Contains(permission.Parent))
                {
                    continue;
                }

                if (await SimpleStateCheckerManager.IsEnabledAsync(permission))
                {
                    neededCheckPermissions.Add(permission);
                }
            }

            if (!neededCheckPermissions.Any())
            {
                continue;
            }

            var grantInfoDtos = neededCheckPermissions
                .Select(CreatePermissionGrantInfoDto)
                .ToList();

            var multipleGrantInfo =
                await PermissionManager.GetAsync(neededCheckPermissions.Select(x => x.Name).ToArray(), providerName,
                    providerKey);

            foreach (var grantInfo in multipleGrantInfo.Result)
            {
                var grantInfoDto = grantInfoDtos.First(x => x.Name == grantInfo.Name);

                grantInfoDto.IsGranted = grantInfo.IsGranted;

                foreach (var provider in grantInfo.Providers)
                {
                    grantInfoDto.GrantedProviders.Add(new ProviderInfoDto
                    {
                        ProviderName = provider.Name,
                        ProviderKey = provider.Key,
                    });
                }

                groupDto.Permissions.Add(grantInfoDto);
            }

            if (groupDto.Permissions.Any())
            {
                result.Groups.Add(groupDto);
            }
        }

        return result;
    }

    public async Task UpdateAsync(Guid organizationId, string providerName, string providerKey, UpdatePermissionsDto input)
    {
        OrganizationRoleHelper.CheckRoleInOrganization(organizationId, providerKey);
        
        if (OrganizationRoleHelper.IsOwner(providerKey))
        {
            throw new UserFriendlyException("The owner role cannot be modified.");
        }
        
        await CheckProviderPolicy(providerName);

        foreach (var permissionDto in input.Permissions)
        {
            await PermissionManager.SetAsync(permissionDto.Name, providerName, providerKey, permissionDto.IsGranted);
        }
    }
    
    private PermissionGrantInfoDto CreatePermissionGrantInfoDto(PermissionDefinition permission)
    {
        return new PermissionGrantInfoDto {
            Name = permission.Name,
            DisplayName = permission.DisplayName?.Localize(StringLocalizerFactory),
            ParentName = permission.Parent?.Name,
            AllowedProviders = permission.Providers,
            GrantedProviders = new List<ProviderInfoDto>()
        };
    }

    private PermissionGroupDto CreatePermissionGroupDto(PermissionGroupDefinition group)
    {
        var localizableDisplayName = group.DisplayName as LocalizableString;
        
        return new PermissionGroupDto
        {
            Name = group.Name,
            DisplayName = group.DisplayName?.Localize(StringLocalizerFactory),
            DisplayNameKey = localizableDisplayName?.Name,
            DisplayNameResource = localizableDisplayName?.ResourceType != null
                ? LocalizationResourceNameAttribute.GetName(localizableDisplayName.ResourceType)
                : null,
            Permissions = new List<PermissionGrantInfoDto>()
        };
    }
    
    protected virtual async Task CheckProviderPolicy(string providerName)
    {
        var policyName = Options.ProviderPolicies.GetOrDefault(providerName);
        if (policyName.IsNullOrEmpty())
        {
            throw new AbpException($"No policy defined to get/set permissions for the provider '{providerName}'. Use {nameof(PermissionManagementOptions)} to map the policy.");
        }
    }
}