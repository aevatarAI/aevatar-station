using System;

namespace Aevatar.Notification.Parameters;

public class OrganizationVisitInfo
{
    public Guid Creator { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RoleId { get; set; }
    public Guid Vistor { get; set; }
}