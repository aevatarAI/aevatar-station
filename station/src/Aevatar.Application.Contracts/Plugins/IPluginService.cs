using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace Aevatar.Plugins;

public interface IPluginService
{
    Task<ListResultDto<PluginDto>> GetListAsync(GetPluginDto input);
    Task<PluginDto> GetAsync(Guid id);
    Task<PluginDto> CreateAsync(Guid projectId, string name, byte[] code);
    Task<PluginDto> UpdateAsync(Guid id, string name, byte[] code);
    Task DeleteAsync(Guid id);
}