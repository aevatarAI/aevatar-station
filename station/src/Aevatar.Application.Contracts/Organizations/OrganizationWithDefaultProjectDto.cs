using System;
using Aevatar.Projects;

namespace Aevatar.Organizations;

public class OrganizationWithDefaultProjectDto : OrganizationDto
{
    public ProjectDto Project { get; set; }
}