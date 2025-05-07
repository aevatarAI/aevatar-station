using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace Aevatar.Organizations;

public interface IOrganizationService
{
    Task<ListResultDto<OrganizationDto>> GetListAsync(GetOrganizationListDto input);
    Task<OrganizationDto> GetAsync(Guid id);
    Task<OrganizationDto> CreateAsync(CreateOrganizationDto input);
    Task<OrganizationDto> UpdateAsync(Guid id, UpdateOrganizationDto input);
    Task DeleteAsync(Guid id);

    Task<ListResultDto<OrganizationMemberDto>> GetMemberListAsync(Guid organizationId, GetOrganizationMemberListDto input);
    Task SetMemberAsync(Guid organizationId, SetOrganizationMemberDto input);
    Task SetMemberRoleAsync(Guid organizationId, SetOrganizationMemberRoleDto input);

    Task<ListResultDto<IdentityRoleDto>> GetRoleListAsync(Guid organizationId);
    Task<ListResultDto<PermissionGrantInfoDto>> GetPermissionListAsync(Guid organizationId);
}