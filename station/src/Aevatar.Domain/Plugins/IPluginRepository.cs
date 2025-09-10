using System;
using Volo.Abp.Domain.Repositories;

namespace Aevatar.Plugins;

public interface IPluginRepository : IRepository<Plugin, Guid>
{
    
}