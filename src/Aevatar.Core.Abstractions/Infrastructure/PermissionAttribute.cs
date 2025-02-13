// ReSharper disable once CheckNamespace
namespace Aevatar.Core.Abstractions;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class PermissionAttribute : Attribute
{
    public string Name { get; }

    public PermissionAttribute(string name)
    {
        Name = name;
    }
}