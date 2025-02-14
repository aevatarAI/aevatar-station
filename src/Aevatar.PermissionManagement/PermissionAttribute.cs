namespace Aevatar.PermissionManagement;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class PermissionAttribute : Attribute
{
    public string Name { get; }

    public PermissionAttribute(string name)
    {
        Name = name;
    }
}