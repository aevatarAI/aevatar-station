using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Aevatar.Permission;

public class StatePermission : FullAuditedAggregateRoot<Guid>
{
    public Guid Id { get; set; }

    public string HostId { get; set; }

    public string StateName { get; set; }

    public string Permission { get; set; }

    public DateTime createTime { get; set; }
}