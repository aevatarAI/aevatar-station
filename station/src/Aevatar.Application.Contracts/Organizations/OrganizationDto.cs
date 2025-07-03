using System;
using Volo.Abp.Application.Dtos;

namespace Aevatar.Organizations;

public class OrganizationDto : EntityDto<Guid>
{
    public string DisplayName { get; set; }
    public int MemberCount { get; set; }
    public long CreationTime { get; set; }
}