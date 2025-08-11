using System;
using System.ComponentModel.DataAnnotations;
using Aevatar.Organizations;

namespace Aevatar.Projects;

public class CreateProjectDto : CreateOrganizationDto
{
    [Required]
    public Guid OrganizationId { get; set; }
    [Required]
    [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "DomainName can only contain letters (A-Z, a-z) and numbers (0-9)")]
    public string DomainName { get; set; }
}