using System;
using System.ComponentModel.DataAnnotations;
using Aevatar.Organizations;

namespace Aevatar.Projects;

public class CreateProjectDto : CreateOrganizationDto
{
    [Required]
    public Guid OrganizationId { get; set; }
    
    /// <summary>
    /// 域名（可选）
    /// 如果不提供，将基于项目名称自动生成
    /// </summary>
    [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "DomainName can only contain letters (A-Z, a-z) and numbers (0-9)")]
    public string? DomainName { get; set; }
}