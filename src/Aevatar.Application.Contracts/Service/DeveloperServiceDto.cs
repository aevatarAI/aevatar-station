using System;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Service;

public class DeveloperServiceOperationDto
{
    [Required(ErrorMessage = "ProjectId is required")]
    public Guid ProjectId { get; set; }

    [Required(ErrorMessage = "ClientId is required")]
    [StringLength(200, ErrorMessage = "ClientId cannot exceed 200 characters")]
    public string ClientId { get; set; } = string.Empty;
}

 