using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Agents.Atomic;
using Aevatar.Agents.Atomic.Models;
using Aevatar.Agents.Combination;
using Aevatar.Agents.Combination.Models;
using Aevatar.Application.Grains.Agents.Atomic;
using Aevatar.Application.Grains.Agents.Combination;
using Aevatar.AtomicAgent;
using Aevatar.CombinationAgent;
using Aevatar.CQRS.Dto;
using Aevatar.CQRS.Provider;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.ObjectMapping;

namespace Aevatar.Service;

public class AgentService : ApplicationService, IAgentService
{
    private readonly IClusterClient _clusterClient;
    private readonly ICQRSProvider _cqrsProvider;
    private readonly ILogger<AgentService> _logger;
    private readonly IObjectMapper _objectMapper;
    private const string GroupAgentName = "GroupAgent";
    private const string IndexSuffix = "index";
    private const string IndexPrefix = "aevatar";

    public AgentService(
        IClusterClient clusterClient, 
        ICQRSProvider cqrsProvider, 
        ILogger<AgentService> logger,  
        IObjectMapper objectMapper)
    {
        _clusterClient = clusterClient;
        _cqrsProvider = cqrsProvider;
        _logger = logger;
        _objectMapper = objectMapper;
    }
    
    public async Task<AtomicAgentDto> GetAtomicAgentAsync(string id)
    {
        if (!Guid.TryParse(id, out Guid validGuid))
        {
            _logger.LogInformation("GetAgentAsync Invalid id: {id}", id);
            throw new UserFriendlyException("Invalid id");
        }
        
        var atomicAgent = _clusterClient.GetGrain<IAtomicGAgent>(validGuid);
        var agentData = await atomicAgent.GetAgentAsync();
        if (agentData == null)
        {
            _logger.LogInformation("GetAgentAsync agent is null: {id}", id);
            throw new UserFriendlyException("agent not exist");
        }
        
        if (agentData.Properties.IsNullOrEmpty())
        {
            _logger.LogInformation("GetAgentAsync agentProperty is null: {id}", id);
            throw new UserFriendlyException("agent not exist");
        }

        var resp = new AtomicAgentDto()
        {
            Id = id,
            Type = agentData.Type,
            Name = agentData.Name,
            Properties = JsonConvert.DeserializeObject<Dictionary<string, string>>(agentData.Properties)
        };
        
        return resp;
    }

    public async Task<AtomicAgentDto> CreateAtomicAgentAsync(CreateAtomicAgentDto createDto)
    {
        var address = GetCurrentUserAddress();
        var guid = Guid.NewGuid();
        var atomicAgent = _clusterClient.GetGrain<IAtomicGAgent>(guid);
        var agentData = _objectMapper.Map<CreateAtomicAgentDto, AtomicAgentData>(createDto);
        agentData.Properties = JsonConvert.SerializeObject(createDto.Properties);
        agentData.UserAddress = address;
        await atomicAgent.CreateAgentAsync(agentData);
        var resp = _objectMapper.Map<CreateAtomicAgentDto, AtomicAgentDto>(createDto);
        resp.Id = guid.ToString();
        return resp;
    }

    public async Task<AtomicAgentDto> UpdateAtomicAgentAsync(string id, UpdateAtomicAgentDto updateDto)
    {
        if (!Guid.TryParse(id, out Guid validGuid))
        {
            _logger.LogInformation("UpdateAgentAsync Invalid id: {id}", id);
            throw new UserFriendlyException("Invalid id");
        }
        
        var atomicAgent = _clusterClient.GetGrain<IAtomicGAgent>(validGuid);
        var agentData = await atomicAgent.GetAgentAsync();
        
        if (agentData == null)
        {
            _logger.LogInformation("UpdateAgentAsync agentProperty is null: {id}", id);
            throw new UserFriendlyException("agent not exist");
        }
        
        var resp = new AtomicAgentDto()
        {
            Id = id,
            Type = agentData.Type
        };
        
        if (!updateDto.Name.IsNullOrEmpty())
        {
            agentData.Name = updateDto.Name;
        }

        var newProperties = JsonConvert.DeserializeObject<Dictionary<string, string>>(agentData.Properties);
        if (newProperties != null)
        {
            foreach (var kvp in updateDto.Properties)
            {
                if (newProperties.ContainsKey(kvp.Key))
                {
                    newProperties[kvp.Key] = kvp.Value;
                }
            }
            
            agentData.Properties = JsonConvert.SerializeObject(newProperties);
        }
        
        await atomicAgent.UpdateAgentAsync(agentData);
        resp.Name = agentData.Name;
        resp.Properties = newProperties;

        return resp;
    }

    public async Task<List<AtomicAgentDto>> GetAtomicAgentsAsync(string userAddress, int pageIndex, int pageSize)
    {
        if (userAddress.IsNullOrEmpty())
        {
            _logger.LogInformation("GetAgentAsync Invalid userAddress: {userAddress}", userAddress);
            throw new UserFriendlyException("Invalid userAddress");
        }

        if (pageIndex <= 0 || pageSize <= 1)
        {
            _logger.LogInformation("GetAgentAsync Invalid pageIndex: {pageIndex} pageSize:{pageSize}", pageIndex, pageSize);
            throw new UserFriendlyException("Invalid pageIndex pageSize");
        }

        var index = IndexPrefix + nameof(AtomicGAgentState).ToLower() + IndexSuffix;
        var result = await _cqrsProvider.QueryStateAsync(index,
            q => q.Term(t => t.Field("userAddress").Value(userAddress)), 
            (pageIndex-1)*pageSize,
            pageSize
        );
        if (result == null)
        {
            return null;
        }
        
        var atomicGAgentStateDtoList = JsonConvert.DeserializeObject<List<AtomicGAgentStateDto>>(result);

        return atomicGAgentStateDtoList.Select(stateDto => new AtomicAgentDto { Id = stateDto.Id.ToString(), Type = stateDto.Type, Name = stateDto.Name, Properties = JsonConvert.DeserializeObject<Dictionary<string, string>>(stateDto.Properties) }).ToList();
    }

    public async Task DeleteAtomicAgentAsync(string id)
    {
        if (!Guid.TryParse(id, out Guid validGuid))
        {
            _logger.LogInformation("DeleteAgentAsync Invalid id: {id}", id);
            throw new UserFriendlyException("Invalid id");
        }
        
        var atomicAgent = _clusterClient.GetGrain<IAtomicGAgent>(validGuid);
        var data = await atomicAgent.GetAgentAsync();
        if (data == null)
        {
            _logger.LogInformation("DeleteAgentAsync agentProperty is null: {id}", id);
            throw new UserFriendlyException("agent not exist");
        }
        
        if (!data.GroupId.IsNullOrEmpty())
        {
            _logger.LogInformation("agent in group id: {id}", data.GroupId);
            throw new UserFriendlyException("agent in group!");
        }
        
        var address = GetCurrentUserAddress();
        if (data.UserAddress != address)
        {
            _logger.LogInformation("agent not belong to user address: {address}, owner: {owner}", 
                address, data.UserAddress);
            throw new UserFriendlyException("agent not belong to user!");
        }
        
        await atomicAgent.DeleteAgentAsync();
    }

    private string GetCurrentUserAddress()
    {
         // todo
        return "my_address";
    }
    
    public async Task<CombinationAgentDto> CombineAgentAsync(CombineAgentDto combineAgentDto)
    {
        if (combineAgentDto.AgentComponent.IsNullOrEmpty())
        {
            _logger.LogInformation("CombineAgentAsync agentComponent is null, name: {name}", combineAgentDto.Name);
            throw new UserFriendlyException("agentComponent is null");
        }
        
        if (combineAgentDto.Name.IsNullOrEmpty())
        {
            _logger.LogInformation("CombineAgentAsync name is null, name: {name}", combineAgentDto.Name);
            throw new UserFriendlyException("name is null");
        }
        
        var address = GetCurrentUserAddress();
        var guid = Guid.NewGuid();
        var combinationAgent = _clusterClient.GetGrain<ICombinationGAgent>(guid);
        var status = await combinationAgent.GetStatusAsync();
        if (status == AgentStatus.Running || status == AgentStatus.Stopped)
        {
            _logger.LogInformation("CombineAgentAsync agent exist, name: {name} status: {status}", 
                combineAgentDto.Name, status);
            throw new UserFriendlyException("agent already exist");
        }
        
        var groupId = await SetGroupAsync(combineAgentDto.AgentComponent, address, guid);
        var data = _objectMapper.Map<CombineAgentDto, CombinationAgentData>(combineAgentDto);
        data.GroupId = groupId;
        data.UserAddress = address;
        await combinationAgent.CombineAgentAsync(data);

        var resp = new CombinationAgentDto
        {
            Id = guid.ToString(),
            Name = data.Name,
            AgentComponent = data.AgentComponent
        };
   
        return resp;
    }
    
    public async Task<CombinationAgentDto> GetCombinationAsync(string id)
    {
        if (!Guid.TryParse(id, out Guid validGuid))
        {
            _logger.LogInformation("GetAgentAsync Invalid id: {id}", id);
            throw new UserFriendlyException("Invalid id");
        }
        
        var combinationAgent = _clusterClient.GetGrain<ICombinationGAgent>(validGuid);
        var combinationData = await combinationAgent.GetCombinationAsync();
        if (combinationData == null || combinationData.Status == AgentStatus.Undefined)
        {
            _logger.LogInformation("GetCombinationAsync combinationData is null: {id}", id);
            throw new UserFriendlyException("combination not exist");
        }
        
        if (combinationData.Status == AgentStatus.Deleted)
        {
            _logger.LogInformation("GetCombinationAsync combinationData is deleted: {id}", id);
            throw new UserFriendlyException("combination deleted");
        }

        var resp = new CombinationAgentDto()
        {
            Id = id,
            Name = combinationData.Name,
            AgentComponent = combinationData.AgentComponent
        };
        
        return resp;
    }
    
    private async Task<string> SetGroupAsync(List<string> agentComponent, string address, Guid guid)
    {
        var combinationAgent = _clusterClient.GetGrain<ICombinationGAgent>(guid);
        var agentList = new List<IAtomicGAgent>();
        foreach (var agentId in agentComponent)
        {
            if (!Guid.TryParse(agentId, out Guid validGuid))
            {
                _logger.LogInformation("SetGroupAsync invalid id: {id}", agentId);
                throw new UserFriendlyException("Invalid id");
            }
            
            var atomicAgent = _clusterClient.GetGrain<IAtomicGAgent>(validGuid);
            var agentData = await atomicAgent.GetAgentAsync();
            if (agentData == null)
            {
                _logger.LogInformation("SetGroupAsync agent not exist, id: {id}", agentId);
                throw new UserFriendlyException("agent not exist");
            }
            
            if (agentData.UserAddress != address)
            {
                _logger.LogInformation("SetGroupAsync agent not belong to user, id: {id}, ownerAddress: {ownerAddress}", 
                    agentId, agentData.UserAddress);
                throw new UserFriendlyException("agent not belong to user");
            }
            
            var inUse = !agentData.GroupId.IsNullOrEmpty();
            if (inUse)
            {
                _logger.LogInformation("SetGroupAsync agent in use, id: {id}", agentId);
                throw new UserFriendlyException("agent in use");
            }
            
            agentList.Add(atomicAgent);
        }
    
        foreach (var agent in agentList)
        {
            // todo: get business agent and register to group
            await agent.SetGroupAsync(guid.ToString());
        }
        
        return guid.ToString();
    }
    
    public async Task<CombinationAgentDto> UpdateCombinationAsync(string id, UpdateCombinationDto updateDto)
    {
        if (!Guid.TryParse(id, out Guid validGuid))
        {
            _logger.LogInformation("UpdateCombinationAsync Invalid id: {id}", id);
            throw new UserFriendlyException("Invalid id");
        }
        
        var combinationAgent = _clusterClient.GetGrain<ICombinationGAgent>(validGuid);
        var combinationData = await combinationAgent.GetCombinationAsync();
        if (combinationData == null)
        {
            _logger.LogInformation("GetCombinationAsync combinationData is null: {id}", id);
            throw new UserFriendlyException("combination not exist");
        }
        
        if (!updateDto.Name.IsNullOrEmpty())
        {
            combinationData.Name = updateDto.Name;
        }

        if (!updateDto.AgentComponent.IsNullOrEmpty())
        {
            var newIncludedAgent = updateDto.AgentComponent.Except(combinationData.AgentComponent).ToList();
            await SetGroupAsync(newIncludedAgent, combinationData.UserAddress, validGuid);
             
            var excludedAgent = combinationData.AgentComponent.Except(updateDto.AgentComponent).ToList();
            _logger.LogInformation("UpdateCombinationAsync excludeAgent: {excludedAgent}", excludedAgent);
            await ExcludeFromGroupAsync(excludedAgent, validGuid);
            
            combinationData.AgentComponent = updateDto.AgentComponent;
        }
        
        await combinationAgent.UpdateCombinationAsync(combinationData);
        
        var resp = new CombinationAgentDto
        {
            Id = id,
            Name = combinationData.Name,
            AgentComponent = combinationData.AgentComponent
        };

        return resp;
    }
    
    private async Task ExcludeFromGroupAsync(List<string> excludedAgent, Guid guid)
    {
        var combinationAgent = _clusterClient.GetGrain<ICombinationGAgent>(guid);
        foreach (var agentId in excludedAgent)
        {
            var atomicAgent = _clusterClient.GetGrain<IAtomicGAgent>(Guid.Parse(agentId));
            // todo: get business agent and unregister from group
            await atomicAgent.SetGroupAsync("");
        }
    }

    public async Task DeleteCombinationAsync(string id)
    {
        if (!Guid.TryParse(id, out Guid validGuid))
        {
            _logger.LogInformation("DeleteCombinationAsync Invalid id: {id}", id);
            throw new UserFriendlyException("Invalid id");
        }
        
        var combinationAgent = _clusterClient.GetGrain<ICombinationGAgent>(validGuid);
        var combinationData = await combinationAgent.GetCombinationAsync();
        if (combinationData == null)
        {
            _logger.LogInformation("DeleteCombinationAsync combinationData is null: {id}", id);
            throw new UserFriendlyException("combination not exist");
        }
        
        var address = GetCurrentUserAddress();
        if (combinationData.UserAddress != address)
        {
            _logger.LogInformation("combination not belong to user address: {address}, owner: {owner}", 
                address, combinationData.UserAddress);
            throw new UserFriendlyException("combination not belong to user!");
        }
        
        await ExcludeFromGroupAsync(combinationData.AgentComponent, validGuid);
        await combinationAgent.DeleteCombinationAsync();
    }

    public async Task<List<CombinationAgentDto>> GetCombinationAgentsAsync(string userAddress, string groupId, int pageIndex, int pageSize)
    {
        if (userAddress.IsNullOrEmpty() || groupId.IsNullOrEmpty())
        {
            _logger.LogInformation("GetAgentAsync Invalid userAddress: {userAddress} groupId:{groupId}", userAddress, groupId);
            throw new UserFriendlyException("Invalid userAddress groupId");
        }
        if (pageIndex <= 0 || pageSize <= 1)
        {
            _logger.LogInformation("GetAgentAsync Invalid pageIndex: {pageIndex} pageSize:{pageSize}", pageIndex, pageSize);
            throw new UserFriendlyException("Invalid pageIndex pageSize");
        }
        
        var index = IndexPrefix + nameof(CombinationGAgentState).ToLower() + IndexSuffix;
        var filters = new List<Func<QueryContainerDescriptor<object>, QueryContainer>>();
        filters.Add(m => m.Term(t => t.Field("userAddress").Value(userAddress)));
        filters.Add(m => m.Term(t => t.Field("groupId").Value(groupId)));

        var result = await _cqrsProvider.QueryStateAsync(index,
            q => q.Bool(b => b.Must(filters)),
            (pageIndex-1)*pageSize,
            pageSize
        );
        if (result == null)
        {
            return null;
        }
        
        var combinationGAgentStateDtoList = JsonConvert.DeserializeObject<List<CombinationGAgentStateDto>>(result);

        return combinationGAgentStateDtoList.Select(stateDto => new CombinationAgentDto { Id = stateDto.Id.ToString(), Name = stateDto.Name, AgentComponent = JsonConvert.DeserializeObject<List<string>>(stateDto.AgentComponent) }).ToList();
    }

    public async Task<Tuple<long, List<AgentGEventIndex>>> GetAgentEventLogsAsync(string agentId, int pageNumber, int pageSize)
    {
        if (!Guid.TryParse(agentId, out var validGuid))
        {
            _logger.LogInformation("GetAgentAsync Invalid id: {id}", agentId);
            throw new UserFriendlyException("Invalid id");
        }
        var agentIds = await ViewGroupTreeAsync(agentId);

        return await _cqrsProvider.QueryGEventAsync("", agentIds, pageNumber, pageSize);
    }
    
    private async Task<List<string>> ViewGroupTreeAsync(string agentId)
    {
        var result = new List<string>();
        await BuildGroupTreeAsync(agentId, result);
        return result;
    }

    private async Task BuildGroupTreeAsync(string agentId, List<string> result)
    {
        var gAgent = _clusterClient.GetGrain<ICombinationGAgent>(Guid.Parse(agentId));
        var childrenAgentIds = await gAgent.GetChildrenAsync();
        if (childrenAgentIds.IsNullOrEmpty())
        {
            return;
        }

        var childrenIds = childrenAgentIds.Select(s => s.Key.ToString()).ToList();
        result.AddRange(childrenIds);

        foreach (var childrenId in childrenIds)
        {
            await BuildGroupTreeAsync(childrenId, result);
        }
    }
    
}