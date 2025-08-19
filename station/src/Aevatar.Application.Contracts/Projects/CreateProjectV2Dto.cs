using System;
using System.ComponentModel.DataAnnotations;
using Aevatar.Organizations;

namespace Aevatar.Projects;

/// <summary>
/// 项目创建DTO - V2版本
/// 不包含DomainName字段，将基于项目名称自动生成域名
/// </summary>
public class CreateProjectV2Dto : CreateOrganizationDto
{
    [Required]
    public Guid OrganizationId { get; set; }
}