using Volo.Abp.Authorization.Permissions;

namespace Aevatar;

public class AevatarPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var tenantAdminGroup = context.AddGroup("TenantAdmin");
        tenantAdminGroup.AddPermission("User.Create");
        tenantAdminGroup.AddPermission("User.Delete");
        tenantAdminGroup.AddPermission("User.Update");

        var tenantUserGroup = context.AddGroup("TenantUser");
        tenantUserGroup.AddPermission("Workflow.Create");
        tenantUserGroup.AddPermission("Workflow.Delete");
        tenantUserGroup.AddPermission("Workflow.Update");
    }
}