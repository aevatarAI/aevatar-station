using System.ComponentModel.DataAnnotations;

namespace Aevatar.Organizations;

public class UpdateOrganizationDto
{
    [Required]
    public string DisplayName { get; set; }
}