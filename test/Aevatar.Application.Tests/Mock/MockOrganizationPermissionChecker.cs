using System;
using System.Threading.Tasks;
using Aevatar.Organizations;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Mock
{
    public class MockOrganizationPermissionChecker : IOrganizationPermissionChecker, ITransientDependency
    {
        public Task<bool> IsGrantedAsync(Guid organizationId, string permissionName)
        {
            // Always return true for testing
            return Task.FromResult(true);
        }

        public Task<bool> AuthenticateAsync(Guid organizationId, string apiKey)
        {
            // Always return true for testing
            return Task.FromResult(true);
        }
    }
} 