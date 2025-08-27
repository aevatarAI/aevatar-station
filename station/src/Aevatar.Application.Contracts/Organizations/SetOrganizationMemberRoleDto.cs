using System;

namespace Aevatar.Organizations;

public class SetOrganizationMemberRoleDto
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}