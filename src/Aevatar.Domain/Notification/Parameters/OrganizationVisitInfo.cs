using System;

namespace Aevatar.Notification.Parameters;

public class OrganizationVisitInfo
{
    public Guid OrganizationId { get; set; }
    public Guid RoleId { get; set; }
    public Guid UserId { get; set; }
}