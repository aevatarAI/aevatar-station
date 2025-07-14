using System;
using Volo.Abp.Application.Dtos;

namespace Aevatar.Organizations;

public class OrganizationMemberDto : EntityDto<Guid>
{
    public string UserName { get; set; }
    public string Email { get; set; }
    public Guid? RoleId { get; set; }
}