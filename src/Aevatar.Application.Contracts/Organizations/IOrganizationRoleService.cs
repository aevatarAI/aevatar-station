using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Identity;

namespace Aevatar.Organizations;

public interface IOrganizationRoleService
{
    Task<ListResultDto<IdentityRoleDto>> GetListAsync(Guid organizationId);
    Task<IdentityRoleDto> CreateAsync(Guid organizationId, IdentityRoleCreateDto input);
    Task<IdentityRoleDto> UpdateAsync(Guid organizationId, Guid id, IdentityRoleUpdateDto input);
    Task DeleteAsync(Guid organizationId, Guid id);
}