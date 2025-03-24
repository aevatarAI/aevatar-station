using System.ComponentModel.DataAnnotations;
using Aevatar.Organizations;

namespace Aevatar.Projects;

public class UpdateProjectDto : UpdateOrganizationDto
{
    [Required]
    public string DomainName { get; set; }
}