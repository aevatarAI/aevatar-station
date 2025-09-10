using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.PermissionManagement;

namespace Aevatar.Permissions;

public class PermissionHelper
{
    public static ConcurrentDictionary<string, string> GetAllStatePermissionInfos()
    {
        var permissionMap = new ConcurrentDictionary<string, string>();
        var baseType = typeof(StateBase);

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypesIgnoringLoadException()
                         .Where(t => t.IsSubclassOf(baseType) && !t.IsAbstract))
            {
                var attr = type.GetCustomAttribute<PermissionAttribute>();
                if (attr != null)
                {
                    permissionMap[type.Name] = attr.Name;
                }
            }
        }

        return permissionMap;
    }
}