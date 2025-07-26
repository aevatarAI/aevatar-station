using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Aevatar.Projects;

public class ProjectCorsOrigin: FullAuditedAggregateRoot<Guid>
{
    public Guid ProjectId { get; set; }
    public string Domain { get; set; }
}