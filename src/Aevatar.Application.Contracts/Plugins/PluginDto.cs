using Volo.Abp.Application.Dtos;

namespace Aevatar.Plugins;

public class PluginDto : EntityDto
{
    public string Name { get; set; }
    public long CreationTime { get; set; }
    public string CreatorName { get; set; }
}