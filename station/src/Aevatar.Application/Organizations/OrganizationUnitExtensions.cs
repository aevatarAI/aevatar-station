using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.Identity;

namespace Aevatar.Organizations;

public static class OrganizationUnitExtensions
{
    public static bool TryGetExtraPropertyValue<T>(this OrganizationUnit organizationUnit, string key, out T? value)
    {
        if (organizationUnit.ExtraProperties.TryGetValue(key, out var valueObject))
        {
            value = (T)valueObject!;
            return true;
        }

        value = default;
        return false;
    }
    
    public static bool TryGetOrganizationRoles(this OrganizationUnit organizationUnit, string key, out List<Guid> value)
    {
        if (organizationUnit.TryGetExtraPropertyValue<List<object>>(key, out var valueObject))
        {
            value = valueObject.OfType<Guid>().ToList();
            return true;
        }

        value = default;
        return false;
    }
}