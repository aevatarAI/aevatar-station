using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;

namespace Aevatar.Common;

public class ReflectionUtil
{
    public static object ConvertValue(Type targetType, object? value)
    {
        if (value == null)
        {
            return null;
        }

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = targetType.GetGenericArguments()[0];
            var list = Activator.CreateInstance(targetType) as System.Collections.IList;

            foreach (var item in (IEnumerable<object>)value)
            {
                list.Add(ConvertValue(elementType, item));
            }

            return list;
        }

        return Convert.ChangeType(value, targetType);
    }

    public static bool CheckInheritClass(Type currentType, Type basicType)
    {
        while (currentType != null && currentType != typeof(object))
        {
            var baseType = currentType.BaseType;
            var data = baseType.GetType();
            if (currentType.BaseType == basicType)
            {
                return true;
            }

            currentType = currentType.BaseType;
        }

        return false;
    }

    public static bool CheckInheritGenericClass(Type? targetType, Type? abstractGenericType)
    {
        if (targetType == null || abstractGenericType == null || !abstractGenericType.IsAbstract)
        {
            return false;
        }

        Type? currentType = targetType;
        while (currentType != null && currentType != typeof(object))
        {
            if (currentType.IsGenericType)
            {
                Type genericDef = currentType.GetGenericTypeDefinition();
                if (genericDef == abstractGenericType)
                {
                    return true;
                }
            }

            currentType = currentType.BaseType;
        }

        return false;
    }

    public static Type? GetTypeByFullName(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .FirstOrDefault(t => t.FullName == fullName);
    }
}