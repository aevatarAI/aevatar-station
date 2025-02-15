namespace Aevatar.PermissionManagement;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class PermissionAttribute : Attribute
{
    public string Name { get; }
    public string? DisplayName { get; }

    public PermissionAttribute(string name, string? displayName = null)
    {
        Name = name;
        DisplayName = displayName;
    }
}