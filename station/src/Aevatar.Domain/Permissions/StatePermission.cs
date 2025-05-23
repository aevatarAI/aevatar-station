using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Aevatar.Permissions;

public class StatePermission : FullAuditedAggregateRoot<Guid>
{
    public string HostId { get; set; }

    public string StateName { get; set; }

    public string Permission { get; set; }

    public DateTime createTime { get; set; }
}