using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Security.Claims;
using Volo.Abp.SimpleStateChecking;

namespace Aevatar.Organizations;

public class OrganizationPermissionChecker : IOrganizationPermissionChecker, ITransientDependency
{
    private readonly IdentityRoleManager _roleManager;
    private readonly IPermissionStore _permissionStore;
    private readonly IRepository<OrganizationUnit, Guid> _organizationUnitRepository;
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;
    private readonly ICurrentPrincipalAccessor _principalAccessor;
    private readonly ISimpleStateCheckerManager<PermissionDefinition> _stateCheckerManager;
    
    public const string ProviderName = "R";

    public OrganizationPermissionChecker(IdentityRoleManager roleManager, IPermissionStore permissionStore,
        IRepository<OrganizationUnit, Guid> organizationUnitRepository,
        IPermissionDefinitionManager permissionDefinitionManager, ICurrentPrincipalAccessor principalAccessor,
        ISimpleStateCheckerManager<PermissionDefinition> stateCheckerManager)
    {
        _roleManager = roleManager;
        _permissionStore = permissionStore;
        _organizationUnitRepository = organizationUnitRepository;
        _permissionDefinitionManager = permissionDefinitionManager;
        _principalAccessor = principalAccessor;
        _stateCheckerManager = stateCheckerManager;
    }

    public async Task AuthenticateAsync(Guid organizationId, string permissionName)
    {
        if (!await IsGrantedAsync(organizationId, permissionName))
        {
            throw new AbpAuthorizationException();
        }
    }

    public async Task<bool> IsGrantedAsync(Guid organizationId, string permissionName)
    {
        var currentUserRoles = _principalAccessor.Principal?.FindAll(AbpClaimTypes.Role).Select(c => c.Value).ToList();
        if (currentUserRoles == null || !currentUserRoles.Any())
        {
            return false;
        }
        
        if (currentUserRoles.Contains(AevatarConsts.AdminRoleName))
        {
            return true;
        }

        var role = await FindOrganizationRoleAsync(organizationId, currentUserRoles);
        
        if (role.IsNullOrEmpty() || !await IsGrantedAsync(permissionName, role))
        {
            return false;
        }

        return true;
    }

    private async Task<bool> IsGrantedAsync(string name, string role)
    {
        var permission = await _permissionDefinitionManager.GetOrNullAsync(name);
        if (permission == null)
        {
            return false;
        }
    
        if (!permission.IsEnabled)
        {
            return false;
        }

        if (!await _stateCheckerManager.IsEnabledAsync(permission))
        {
            return false;
        }
        
        if (!await _permissionStore.IsGrantedAsync(name, ProviderName, role))
        {
            return false;
        }

        return true;
    }

    private async Task<string> FindOrganizationRoleAsync(Guid organizationId, List<string> roles)
    {
        while (true)
        {
            var organization = await _organizationUnitRepository.GetAsync(organizationId);
            if (organization.TryGetOrganizationRoles(AevatarConsts.OrganizationRoleKey,
                    out var organizationRoleIds))
            {
                foreach (var organizationRoleId in organizationRoleIds)
                {
                    var role = await _roleManager.GetByIdAsync(organizationRoleId);
                    if (roles.Contains(role.Name))
                    {
                        return role.Name;
                    }
                }
            }

            if (organization.ParentId.HasValue)
            {
                organizationId = organization.ParentId.Value;
                continue;
            }

            return null;
        }
    }
}