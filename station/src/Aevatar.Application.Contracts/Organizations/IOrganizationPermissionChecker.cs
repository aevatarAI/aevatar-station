using System;
using System.Threading.Tasks;

namespace Aevatar.Organizations;

public interface IOrganizationPermissionChecker
{
    Task AuthenticateAsync(Guid organizationId, string permissionName);
    Task<bool> IsGrantedAsync(Guid organizationId, string permissionName);
}