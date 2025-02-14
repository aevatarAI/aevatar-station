using Orleans;

namespace Aevatar.PermissionManagement;

[GenerateSerializer]
public class UserContext
{
    [Id(0)] public required Guid UserId { get; set; }
    [Id(1)] public required string Role { get; set; }
}