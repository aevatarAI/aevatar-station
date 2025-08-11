using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions.Plugin;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Identity;
using System.Linq;

namespace Aevatar.Plugins;

[RemoteService(IsEnabled = false)]
public class PluginService : AevatarAppService, IPluginService
{
    private readonly IPluginGAgentManager _pluginGAgentManager;
    private readonly IPluginRepository _pluginRepository;
    private readonly IdentityUserManager _identityUserManager;

    public PluginService(IPluginGAgentManager pluginGAgentManager, IPluginRepository pluginRepository,
        IdentityUserManager identityUserManager)
    {
        _pluginGAgentManager = pluginGAgentManager;
        _pluginRepository = pluginRepository;
        _identityUserManager = identityUserManager;
    }

    public async Task<ListResultDto<PluginDto>> GetListAsync(GetPluginDto input)
    {
        var query = await _pluginRepository.GetQueryableAsync();
        query = query.Where(o =>
                o.ProjectId == input.ProjectId)
            .OrderBy(o => o.CreationTime);
        var list = query.ToList();

        var status = await _pluginGAgentManager.GetPluginLoadStatusAsync();
        var pluginStatus = new Dictionary<Guid, PluginLoadStatus>();
        foreach (var item in status)
        {
            var key = Guid.Parse(item.Key.Split("_").Last());
            pluginStatus[key] = item.Value;
        }
        
        var pluginDtos = new List<PluginDto>();
        foreach (var plugin in list)
        {
            var dto = ObjectMapper.Map<Plugin, PluginDto>(plugin);

            if (plugin.CreatorId.HasValue)
            {
                var creator = await _identityUserManager.GetByIdAsync(plugin.CreatorId.Value);
                dto.CreatorName = creator.UserName;
            }

            if (plugin.LastModifierId.HasValue)
            {
                var lastModifier = await _identityUserManager.GetByIdAsync(plugin.LastModifierId.Value);
                dto.LastModifierName = lastModifier.UserName;
            }

            dto.LoadStatus = LoadStatus.Unload;
            if (pluginStatus.TryGetValue(plugin.Id, out var value))
            {
                dto.LoadStatus = value.Status;
                dto.Reason = value.Reason;
            }

            pluginDtos.Add(dto);
        }

        return new ListResultDto<PluginDto>
        {
            Items = pluginDtos
        };
    }

    public async Task<PluginDto> GetAsync(Guid id)
    {
        var plugin = await _pluginRepository.GetAsync(id);
        return ObjectMapper.Map<Plugin, PluginDto>(plugin);
    }

    public async Task<PluginDto> CreateAsync(Guid projectId, string name, byte[] code)
    {
        var id = await _pluginGAgentManager.AddPluginAsync(new AddPluginDto
        {
            Code = code,
            TenantId =projectId
        });

        var plugin = new Plugin(id)
        {
            ProjectId = projectId,
            Name = name
        };
        await _pluginRepository.InsertAsync(plugin);
        
        return ObjectMapper.Map<Plugin, PluginDto>(plugin);
    }

    public async Task<PluginDto> UpdateAsync(Guid id, string name, byte[] code)
    {
        var plugin = await _pluginRepository.GetAsync(id);
        
        await _pluginGAgentManager.UpdatePluginAsync(new Aevatar.Core.Abstractions.Plugin.UpdatePluginDto
        {
            Code = code,
            TenantId =plugin.ProjectId,
            PluginCodeId = plugin.Id
        });

        plugin.Name = name;
        await _pluginRepository.UpdateAsync(plugin);
        
        return ObjectMapper.Map<Plugin, PluginDto>(plugin);
    }

    public async Task DeleteAsync(Guid id)
    {
        var plugin = await _pluginRepository.GetAsync(id);
        
        await _pluginGAgentManager.RemovePluginAsync(new RemovePluginDto
        {
            TenantId =plugin.ProjectId,
            PluginCodeId = plugin.Id
        });

        await _pluginRepository.DeleteAsync(plugin);
    }
}