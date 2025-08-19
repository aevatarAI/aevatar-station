using System;
using System.ComponentModel.DataAnnotations;
using Aevatar.Organizations;

namespace Aevatar.Projects;

/// <summary>
/// 项目创建DTO
/// 不包含DomainName字段，将基于项目名称自动生成域名
/// </summary>
public class CreateProjectDto : CreateOrganizationDto
{
    [Required]
    public Guid OrganizationId { get; set; }
}