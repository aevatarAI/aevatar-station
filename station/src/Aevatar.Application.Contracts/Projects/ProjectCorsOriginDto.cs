using System;
using Volo.Abp.Application.Dtos;

namespace Aevatar.Projects;

public class ProjectCorsOriginDto : EntityDto<Guid>
{
    public Guid ProjectId { get; set; }
    public string Domain { get; set; }
    public long CreationTime { get; set; }
    public Guid CreatorId { get; set; }
    public string CreatorName { get; set; }
}
