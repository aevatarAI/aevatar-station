using System;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Service;

public class DeveloperServiceOperationDto
{
    [Required(ErrorMessage = "ProjectId is required")]
    public Guid ProjectId { get; set; }

    [Required(ErrorMessage = "DomainName is required")]
    [StringLength(200, ErrorMessage = "DomainName cannot exceed 200 characters")]
    public string DomainName { get; set; } = string.Empty;
}

 