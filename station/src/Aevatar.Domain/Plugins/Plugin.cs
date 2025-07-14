using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Aevatar.Plugins;

public class Plugin: FullAuditedAggregateRoot<Guid>
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; }
    
    public Plugin()
    {

    }

    public Plugin(Guid id)
        : base(id)
    {

    }
}