using System;
using System.Threading.Tasks;
using Volo.Abp.PermissionManagement;

namespace Aevatar.Organizations;

public interface IOrganizationPermissionService
{
    PermissionScope PermissionScope { get; }
    Task<GetPermissionListResultDto> GetAsync(Guid organizationId, string providerName, string providerKey);
    Task UpdateAsync(Guid organizationId, string providerName, string providerKey, UpdatePermissionsDto input);
}