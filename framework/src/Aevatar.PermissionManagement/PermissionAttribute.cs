namespace Aevatar.PermissionManagement;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class PermissionAttribute : Attribute
{
    public string Name { get; }
    public string? GroupName { get; }
    public string? DisplayName { get; }

    public PermissionAttribute(string name, string? groupName = null, string? displayName = null)
    {
        Name = name;
        GroupName = groupName;
        DisplayName = displayName;
    }
}