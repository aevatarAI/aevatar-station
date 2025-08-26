using System;

namespace Aevatar.Organizations;

public class OrganizationWithDefaultProjectDto : OrganizationDto
{
    public Guid DefaultProjectId { get; set; }
}