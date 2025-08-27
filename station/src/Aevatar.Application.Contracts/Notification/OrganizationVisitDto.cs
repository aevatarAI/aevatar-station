using System;

namespace Aevatar.Notification;

public class OrganizationVisitDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; }
}