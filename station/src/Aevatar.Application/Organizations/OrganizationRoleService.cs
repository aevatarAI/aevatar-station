using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using IdentityRole = Volo.Abp.Identity.IdentityRole;

namespace Aevatar.Organizations;

[RemoteService(IsEnabled = false)]
public class OrganizationRoleService : AevatarAppService, IOrganizationRoleService
{
    protected readonly IRepository<OrganizationUnit, Guid> OrganizationUnitRepository;
    protected readonly IdentityRoleManager RoleManager;
    protected readonly OrganizationUnitManager OrganizationUnitManager;

    public OrganizationRoleService(IRepository<OrganizationUnit, Guid> organizationUnitRepository,
        IdentityRoleManager roleManager, OrganizationUnitManager organizationUnitManager)
    {
        OrganizationUnitRepository = organizationUnitRepository;
        RoleManager = roleManager;
        OrganizationUnitManager = organizationUnitManager;
    }

    public virtual async Task<ListResultDto<IdentityRoleDto>> GetListAsync(Guid organizationId)
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

    public async Task<IdentityRoleDto> CreateAsync(Guid organizationId, IdentityRoleCreateDto input)
    {
        var role = new IdentityRole(
            GuidGenerator.Create(),
            OrganizationRoleHelper.GetRoleName(organizationId, input.Name)
        );
        (await RoleManager.CreateAsync(role)).CheckErrors();
        
        var organization = await OrganizationUnitRepository.GetAsync(organizationId);
        organization.TryGetOrganizationRoles(out var roles);
        roles.Add(role.Id);
        organization.ExtraProperties[AevatarConsts.OrganizationRoleKey] = roles;
        await OrganizationUnitManager.UpdateAsync(organization);

        return ObjectMapper.Map<IdentityRole, IdentityRoleDto>(role);
    }

    public async Task<IdentityRoleDto> UpdateAsync(Guid organizationId, Guid id, IdentityRoleUpdateDto input)
    {
        var role = await RoleManager.GetByIdAsync(id);

        if (OrganizationRoleHelper.IsOwner(role.Name))
        {
            throw new UserFriendlyException("The owner role cannot be modified.");
        }

        OrganizationRoleHelper.CheckRoleInOrganization(organizationId, role.Name);

        var newName = OrganizationRoleHelper.GetRoleName(organizationId, input.Name);

        (await RoleManager.SetRoleNameAsync(role,newName)).CheckErrors();
        
        (await RoleManager.UpdateAsync(role)).CheckErrors();
        
        return ObjectMapper.Map<IdentityRole, IdentityRoleDto>(role);
    }

    public async Task DeleteAsync(Guid organizationId, Guid id)
    {
        var role = await RoleManager.GetByIdAsync(id);
        
        if (OrganizationRoleHelper.IsOwner(role.Name))
        {
            throw new UserFriendlyException("The owner role cannot be deleted.");
        }
        
        OrganizationRoleHelper.CheckRoleInOrganization(organizationId, role.Name);
        
        var organization = await OrganizationUnitRepository.GetAsync(organizationId);
        organization.TryGetOrganizationRoles(out var roles);
        roles.Remove(role.Id);
        organization.ExtraProperties[AevatarConsts.OrganizationRoleKey] = roles;
        await OrganizationUnitManager.UpdateAsync(organization);
        
        await RoleManager.DeleteAsync(role);
    }
}