using System;
using System.ComponentModel.DataAnnotations;
using Aevatar.Organizations;

namespace Aevatar.Projects;

public class CreateProjectDto : CreateOrganizationDto
{
    [Required]
    public Guid OrganizationId { get; set; }
    [Required]
    public string DomainName { get; set; }
}