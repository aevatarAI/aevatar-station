using System;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Projects;

public class RecentUsedProjectDto
{
    [Required]
    public Guid OrganizationId { get; set; }
    [Required]
    public Guid ProjectId { get; set; }
}