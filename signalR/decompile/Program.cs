using System;
using System.IO;
using System.Reflection;
using System.Text;

class Program
{
    static void Main()
    {
        var assembly = Assembly.LoadFile("/Users/auric/.nuget/packages/aevatar.core.abstractions/1.4.12-optimize.5/lib/net9.0/Aevatar.Core.Abstractions.dll");
        
        foreach (var type in assembly.GetTypes())
        {
            if (type.Name == "IGAgentFactory")
            {
                Console.WriteLine($"Interface: {type.FullName}");
                Console.WriteLine("Methods:");
                
                foreach (var method in type.GetMethods())
                {
                    StringBuilder methodSignature = new StringBuilder();
                    
                    // Return type
                    methodSignature.Append(FormatTypeName(method.ReturnType));
                    methodSignature.Append(" ");
                    
                    // Method name
                    methodSignature.Append(method.Name);
                    
                    // Generic parameters
                    if (method.IsGenericMethod)
                    {
                        methodSignature.Append("<");
                        var genericArgs = method.GetGenericArguments();
                        for (int i = 0; i < genericArgs.Length; i++)
                        {
                            if (i > 0) methodSignature.Append(", ");
                            methodSignature.Append(FormatTypeName(genericArgs[i]));
                        }
                        methodSignature.Append(">");
                    }
                    
                    // Parameters
                    methodSignature.Append("(");
                    var parameters = method.GetParameters();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        if (i > 0) methodSignature.Append(", ");
                        
                        methodSignature.Append(FormatTypeName(param.ParameterType));
                        methodSignature.Append(" ");
                        methodSignature.Append(param.Name);
                        
                        if (param.HasDefaultValue)
                        {
                            methodSignature.Append(" = ");
                            methodSignature.Append(param.DefaultValue?.ToString() ?? "null");
                        }
                    }
                    methodSignature.Append(")");
                    
                    Console.WriteLine($"  {methodSignature}");
                }
                
                break;
            }
        }
    }
    
    static string FormatTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var genericType = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments();
            
            var baseName = genericType.Name;
            int tickIndex = baseName.IndexOf('`');
            if (tickIndex > 0)
            {
                baseName = baseName.Substring(0, tickIndex);
            }
            
            StringBuilder sb = new StringBuilder();
            sb.Append(baseName);
            sb.Append("<");
            
            for (int i = 0; i < genericArgs.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(FormatTypeName(genericArgs[i]));
            }
            
            sb.Append(">");
            return sb.ToString();
        }
        
        if (type == typeof(void)) return "void";
        if (type == typeof(int)) return "int";
        if (type == typeof(string)) return "string";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(Task<>)) return "Task";
        
        return type.Name;
    }
}
