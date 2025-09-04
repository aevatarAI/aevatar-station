// namespace Aevatar.GAgents.Basic;

// [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
// public class GenericMetaAttribute : Attribute
// {
//     public string? Category { get; set; }
//     public string Key { get; set; }
//     public object Value { get; set; }
//     public string[]? PathLevels { get; set; }

//     public GenericMetaAttribute(string key, object value)
//     {
//         Category = null;
//         Key = key;
//         Value = value;
//         PathLevels = null;
//     }

//     public GenericMetaAttribute(string category, string key, object value)
//     {
//         Category = category;
//         Key = key;
//         Value = value;
//         PathLevels = new[] { category };
//     }

//     public GenericMetaAttribute(params object[] pathAndValue)
//     {
//         if (pathAndValue.Length < 2) throw new ArgumentException("至少需要key和value两个参数");

//         Value = pathAndValue[pathAndValue.Length - 1];
//         Key = pathAndValue[pathAndValue.Length - 2].ToString()!;
//         if (pathAndValue.Length > 2)
//         {
//             PathLevels = pathAndValue.Take(pathAndValue.Length - 2)
//                 .Select(p => p.ToString()!)
//                 .ToArray();
//             Category = PathLevels[0]; // 保持兼容性
//         }
//         else
//         {
//             PathLevels = null;
//             Category = null;
//         }
//     }
// }