using System;
using Volo.Abp.Application.Dtos;

namespace Aevatar.Plugins;

public class PluginDto : EntityDto<Guid>
{
    public string Name { get; set; }
    public long CreationTime { get; set; }
    public string CreatorName { get; set; }
}