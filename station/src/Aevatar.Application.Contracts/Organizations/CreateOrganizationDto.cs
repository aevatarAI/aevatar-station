using System.ComponentModel.DataAnnotations;

namespace Aevatar.Organizations;

public class CreateOrganizationDto
{
    [Required]
    public string DisplayName { get; set; }
}