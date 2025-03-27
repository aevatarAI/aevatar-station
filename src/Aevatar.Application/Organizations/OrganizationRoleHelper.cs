using System;
using Volo.Abp;

namespace Aevatar.Organizations;

public class OrganizationRoleHelper
{
    public static string GetRoleName(Guid organizationId, string roleName)
    {
        return $"{organizationId.ToString()}_{roleName}";
    }
    
    public static string GetRoleOrganizationIdName(string roleName)
    {
        return roleName.Split("_")[0];
    }
    
    public static void CheckRoleInOrganization(Guid organizationId, string roleName)
    {
        var roleOrganizationId = GetRoleOrganizationIdName(roleName);
        if (Guid.Parse(roleOrganizationId) != organizationId)
        {
            throw new UserFriendlyException("Invalid organizational role.");
        }
    }

    public static bool IsOwner(string roleName)
    {
        return roleName.Split("_")[1] == AevatarConsts.OrganizationOwnerRoleName;
    }
}