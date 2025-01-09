using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Agents.Atomic.Models;
using Aevatar.Agents.Combination.Models;
using Aevatar.Agents.Group;
using Aevatar.Application.Grains.Agents.Atomic;
using Aevatar.Application.Grains.Agents.Combination;
using Aevatar.AtomicAgent;
using Aevatar.CombinationAgent;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Provider;
using Microsoft.Extensions.Logging;
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
        var validGuid = ParseGuid(id);
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
        CheckCreateParam(createDto);
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

    private void CheckCreateParam(CreateAtomicAgentDto createDto)
    {
        if (createDto.Type.IsNullOrEmpty())
        {
            _logger.LogInformation("CreateAtomicAgentAsync type is null");
            throw new UserFriendlyException("type is null");
        }
        
        if (createDto.Name.IsNullOrEmpty())
        {
            _logger.LogInformation("CreateAtomicAgentAsync name is null");
            throw new UserFriendlyException("name is null");
        }
        
        if (createDto.Properties.IsNullOrEmpty())
        {
            _logger.LogInformation("CreateAtomicAgentAsync properties is null");
            throw new UserFriendlyException("properties is null");
        }
    }

    public async Task<AtomicAgentDto> UpdateAtomicAgentAsync(string id, UpdateAtomicAgentDto updateDto)
    {
        var validGuid = ParseGuid(id);
        await CheckAtomicAgentValid(validGuid);
        
        var atomicAgent = _clusterClient.GetGrain<IAtomicGAgent>(validGuid);
        var agentData = await atomicAgent.GetAgentAsync();
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

    public async Task DeleteAtomicAgentAsync(string id)
    {
        var validGuid = ParseGuid(id);
        await CheckAtomicAgentValid(validGuid);
        
        var atomicAgent = _clusterClient.GetGrain<IAtomicGAgent>(validGuid);
        var agentData = await atomicAgent.GetAgentAsync();
        
        if (!agentData.Groups.IsNullOrEmpty())
        {
            _logger.LogInformation("agent in group: {group}", agentData.Groups);
            throw new UserFriendlyException("agent in group!");
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
        CheckCombineParam(combineAgentDto);
        
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
        
        var groupId = await SetGroupAsync(combineAgentDto.AgentComponent, guid);
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

    private void CheckCombineParam(CombineAgentDto combineAgentDto)
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
    }
    
    public async Task<CombinationAgentDto> GetCombinationAsync(string id)
    {
        var validGuid = ParseGuid(id);
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
    
    private async Task<string> SetGroupAsync(List<string> agentComponent, Guid guid)
    {
        var combinationAgent = _clusterClient.GetGrain<ICombinationGAgent>(guid);
        var agentList = new List<IAtomicGAgent>();
        foreach (var agentId in agentComponent)
        {
            var validGuid = ParseGuid(agentId);
            await CheckAtomicAgentValid(validGuid);
            var atomicAgent = _clusterClient.GetGrain<IAtomicGAgent>(validGuid);
            agentList.Add(atomicAgent);
        }
    
        foreach (var agent in agentList)
        {
            // todo: initialize actual agent, and add to BusinessAgents map
            await agent.AddToGroupAsync(guid.ToString());
        }
        
        return guid.ToString();
    }
    
    public async Task<CombinationAgentDto> UpdateCombinationAsync(string id, UpdateCombinationDto updateDto)
    {
        var validGuid = ParseGuid(id);
        await CheckCombinationValid(validGuid);
        var combinationAgent = _clusterClient.GetGrain<ICombinationGAgent>(validGuid);
        var combinationData = await combinationAgent.GetCombinationAsync();
        
        if (!updateDto.Name.IsNullOrEmpty())
        {
            combinationData.Name = updateDto.Name;
        }

        if (!updateDto.AgentComponent.IsNullOrEmpty())
        {
            var newIncludedAgent = updateDto.AgentComponent.Except(combinationData.AgentComponent).ToList();
            await SetGroupAsync(newIncludedAgent, validGuid);
             
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
            await atomicAgent.RemoveFromGroupAsync(guid.ToString());
        }
    }

    public async Task DeleteCombinationAsync(string id)
    {
        var validGuid = ParseGuid(id);
        await CheckCombinationValid(validGuid);
        
        var combinationAgent = _clusterClient.GetGrain<ICombinationGAgent>(validGuid);
        var combinationData = await combinationAgent.GetCombinationAsync();
        await ExcludeFromGroupAsync(combinationData.AgentComponent, validGuid);
        await combinationAgent.DeleteCombinationAsync();
    }

    private Guid ParseGuid(string id)
    {
        if (!Guid.TryParse(id, out Guid validGuid))
        {
            _logger.LogInformation("Invalid id: {id}", id);
            throw new UserFriendlyException("Invalid id");
        }
        return validGuid;
    }

    private async Task CheckAtomicAgentValid(Guid guid)
    {
        var atomicAgent = _clusterClient.GetGrain<IAtomicGAgent>(guid);
        var agentData = await atomicAgent.GetAgentAsync();
        if (agentData == null || agentData.Properties.IsNullOrEmpty())
        {
            _logger.LogInformation("agent not exist, id: {id}", guid);
            throw new UserFriendlyException("agent not exist");
        }
        
        var address = GetCurrentUserAddress();
        if (agentData.UserAddress != address)
        {
            _logger.LogInformation("agent not belong to user, id: {id}, ownerAddress: {ownerAddress}", 
                guid, agentData.UserAddress);
            throw new UserFriendlyException("agent not belong to user");
        }
    }
    
    private async Task CheckCombinationValid(Guid guid)
    {
        var combinationAgent = _clusterClient.GetGrain<ICombinationGAgent>(guid);
        var combinationData = await combinationAgent.GetCombinationAsync();
        if (combinationData.AgentComponent.IsNullOrEmpty())
        {
            _logger.LogInformation("combinationData is null: {id}", guid);
            throw new UserFriendlyException("combination not exist");
        }
        
        var address = GetCurrentUserAddress();
        if (combinationData.UserAddress != address)
        {
            _logger.LogInformation("combination not belong to user address: {address}, owner: {owner}", 
                address, combinationData.UserAddress);
            throw new UserFriendlyException("combination not belong to user!");
        }
    }


}