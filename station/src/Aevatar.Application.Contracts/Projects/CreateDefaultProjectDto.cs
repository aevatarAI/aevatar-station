using System;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Projects;

public class CreateDefaultProjectDto
{
    [Required]
    public Guid OrganizationId { get; set; }
}