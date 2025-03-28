using System;
using System.Collections.Generic;
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

    public static bool CheckInheritClass(object obj, Type basicType)
    {
        var currentType = obj.GetType();
        while (currentType != null && currentType != typeof(object))
        {
            if (currentType.BaseType == basicType)
            {
                return true;
            }

            currentType = currentType.BaseType;
        }

        return false;
    }
}