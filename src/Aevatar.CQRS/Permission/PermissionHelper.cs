using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aevatar.Core.Abstractions;
using Aevatar.PermissionManagement;

namespace Aevatar.Permissions;

public class PermissionHelper
{
    public static ConcurrentDictionary<string, string> GetAllStatePermissionInfos()
    {
        var concurrentMap = new ConcurrentDictionary<string, string>();
        Type agentType = typeof(StateBase);
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        List<Type> typeList = new List<Type>();
        foreach (Assembly assembly in assemblies)
        {
            IEnumerable<Type> collection =
                ((IEnumerable<Type>)assembly.GetTypes()).Where<Type>((Func<Type, bool>)(t =>
                    agentType.IsSubclassOf(t)));
            typeList.AddRange(collection);
        }

        foreach (Type type in typeList)
        {
            Type gAgentStateType = type;
            var customAttributes =
                type.GetCustomAttributes(typeof(PermissionAttribute), false)
                    .FirstOrDefault() as PermissionAttribute;
            if (customAttributes != null)
            {
                concurrentMap[gAgentStateType.Name] = customAttributes.Name;
            }
        }

        return concurrentMap;
    }
}