using System;
using Volo.Abp.Application.Dtos;

namespace Aevatar.Plugins.Dtos
{
    public class PluginDto : EntityDto<Guid>
    {
        public Guid ProjectId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime CreationTime { get; set; }
        public Guid? CreatorId { get; set; }
        public DateTime? LastModificationTime { get; set; }
        public Guid? LastModifierId { get; set; }
    }
} 