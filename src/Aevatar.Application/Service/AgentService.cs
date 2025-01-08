using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Agents.Group;
using Aevatar.AtomicAgent.Agent;
using Aevatar.AtomicAgent.Dtos;
using Aevatar.AtomicAgent.Models;
using Aevatar.CombinationAgent.Agent;
using Aevatar.CombinationAgent.Dtos;
using Aevatar.CombinationAgent.Models;
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
        if (!Guid.TryParse(id, out Guid validGuid))
        {
            _logger.LogInformation("GetAgentAsync Invalid id: {id}", id);
            throw new UserFriendlyException("Invalid id");
        }
        
        var atomicAgent = _clusterClient.GetGrain<IAtomicGAgent>(validGuid);
        var agentData = await atomicAgent.GetAgentAsync();
        if (agentData == null)
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

    public async Task DeleteAtomicAgentAsync(string id)
    {
        if (!Guid.TryParse(id, out Guid validGuid))
        {
            _logger.LogInformation("DeleteAgentAsync Invalid id: {id}", id);
            throw new UserFriendlyException("Invalid id");
        }
        
        var atomicAgent = _clusterClient.GetGrain<IAtomicGAgent>(validGuid);
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
        
        var groupId = await SetGroupAsync(combineAgentDto.AgentComponent, combineAgentDto.Name, address);
        var data = _objectMapper.Map<CombineAgentDto, CombinationAgentData>(combineAgentDto);
        data.GroupId = groupId;
        data.UserAddress = address;
        await combinationAgent.CombineAgentAsync(data);
        var resp = _objectMapper.Map<CombineAgentDto, CombinationAgentDto>(combineAgentDto);
        resp.Id = guid.ToString();
        return resp;
    }
    
    private async Task<string> SetGroupAsync(List<string> agentComponent, string name, string address)
    {
        var groupGuid = Guid.NewGuid();
        var groupAgent = _clusterClient.GetGrain<IStateGAgent<GroupAgentState>>(groupGuid);
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
           await agent.RegisterToGroupAsync(groupGuid.ToString());
        }
        
        return groupGuid.ToString();
    }
    
}