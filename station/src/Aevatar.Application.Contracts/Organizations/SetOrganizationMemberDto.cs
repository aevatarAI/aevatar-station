using System;

namespace Aevatar.Organizations;

public class SetOrganizationMemberDto
{
    public string Email { get; set; }
    public bool Join { get; set; }
    public Guid? RoleId { get; set; }
}