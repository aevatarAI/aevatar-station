using Aevatar.Organizations;
using Microsoft.Extensions.Options;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SimpleStateChecking;

namespace Aevatar.Projects;

public class ProjectPermissionService : OrganizationPermissionService, IProjectPermissionService
{
    public override PermissionScope PermissionScope { get; } = PermissionScope.Project;

    public ProjectPermissionService(IPermissionManager permissionManager,
        IPermissionDefinitionManager permissionDefinitionManager, IOptionsSnapshot<PermissionManagementOptions> options,
        ISimpleStateCheckerManager<PermissionDefinition> simpleStateCheckerManager) : base(permissionManager,
        permissionDefinitionManager, options, simpleStateCheckerManager)
    {
    }
}