using System;
using Aevatar.Core.Abstractions.Plugin;
using Volo.Abp.Application.Dtos;

namespace Aevatar.Plugins;

public class PluginDto : EntityDto<Guid>
{
    public string Name { get; set; }
    public Guid ProjectId { get; set; }
    public long CreationTime { get; set; }
    public string CreatorName { get; set; }
    public long LastModificationTime { get; set; }
    public string LastModifierName { get; set; }
    public LoadStatus LoadStatus { get; set; }
    public string? Reason { get; set; }
}