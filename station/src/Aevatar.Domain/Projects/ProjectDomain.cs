using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Aevatar.Projects;

public class ProjectDomain : FullAuditedAggregateRoot<Guid>
{
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public string DomainName { get; set; }
    public string NormalizedDomainName { get; set; }
}