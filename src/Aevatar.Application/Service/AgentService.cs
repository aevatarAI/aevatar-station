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
using Aevatar.Application.Grains.Agents.Investment;
using Aevatar.AtomicAgent;
using Aevatar.CombinationAgent;
using Aevatar.Core;
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
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IGAgentManager _gAgentManager;
    
    private const string GroupAgentName = "GroupAgent";
    private const string IndexSuffix = "index";
    private const string IndexPrefix = "aevatar";

    public AgentService(
        IClusterClient clusterClient, 
        ICQRSProvider cqrsProvider, 
        ILogger<AgentService> logger,  
        IObjectMapper objectMapper, 
        IGAgentFactory gAgentFactory, 
        IGAgentManager gAgentManager)
    {
        _clusterClient = clusterClient;
        _cqrsProvider = cqrsProvider;
        _logger = logger;
        _objectMapper = objectMapper;
        _gAgentFactory = gAgentFactory;
        _gAgentManager = gAgentManager;
    }
    
    public async Task<AtomicAgentDto> GetAtomicAgentAsync(string id)
    {
        var validGuid = ParseGuid(id);
        var atomicAgent = _clusterClient.GetGrain<IAtomicGAgent>(validGuid);
        var agentData = await atomicAgent.GetAgentAsync();
        
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
        
        var components = await SetGroupAsync(combineAgentDto.AgentComponent, guid);
        var data = _objectMapper.Map<CombineAgentDto, CombinationAgentData>(combineAgentDto);
        data.GroupId = guid.ToString();
        data.UserAddress = address;
        data.AgentComponent = components;
        await combinationAgent.CombineAgentAsync(data);

        var resp = new CombinationAgentDto
        {
            Id = guid.ToString(),
            Name = data.Name,
            AgentComponent = components
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
    
    private async Task<Dictionary<string, string>> SetGroupAsync(List<string> agentComponent, Guid guid)
    {
        var combinationAgent = _clusterClient.GetGrain<ICombinationGAgent>(guid);
  
        foreach (var agentId in agentComponent)
        {
            var validGuid = ParseGuid(agentId);
            await CheckAtomicAgentValid(validGuid);
        }
        
        var components = new Dictionary<string, string>();
        foreach (var agentId in agentComponent)
        {
            var validGuid = ParseGuid(agentId);
            var atomicAgent = _clusterClient.GetGrain<IAtomicGAgent>(validGuid);
            var agentData = await atomicAgent.GetAgentAsync();
            
            var businessAgent = await _gAgentFactory.GetGAgentAsync(agentData.Type, initializeDto: new InitializeDto
            {
                Properties = agentData.Properties
            });
            // var publishAgent =  _clusterClient.GetGrain<IPublishingGAgent>(Guid.NewGuid());
            await combinationAgent.RegisterAsync(businessAgent);

            var primaryKey = businessAgent.GetPrimaryKey().ToString();
            components.Add(agentId, primaryKey);
            await atomicAgent.AddToGroupAsync(guid.ToString());
        }

        return components;
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
            var oldAgentList = combinationData.AgentComponent.Keys.ToList();
            var newAgentList = updateDto.AgentComponent;
            
            var newIncludedAgent = updateDto.AgentComponent.Where(i => !oldAgentList.Contains(i)).ToList();
            _logger.LogInformation("UpdateCombinationAsync newIncludedAgent: {newIncludedAgent}", newIncludedAgent);
            var newComponents = await SetGroupAsync(newIncludedAgent, validGuid);
             
            var excludedAgent = combinationData.AgentComponent.Where(kv => !newAgentList.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
            _logger.LogInformation("UpdateCombinationAsync excludeAgent: {excludedAgent}", excludedAgent);
            await ExcludeFromGroupAsync(excludedAgent, validGuid);
            
            var currentComponents =  combinationData.AgentComponent.Where(kv => !excludedAgent.ContainsKey(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
            foreach (var component in newComponents)
            {
                currentComponents[component.Key] = component.Value;
            }
            combinationData.AgentComponent = currentComponents;
            _logger.LogInformation("UpdateCombinationAsync currentComponents: {currentComponents}", combinationData.AgentComponent);
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
    
    private async Task ExcludeFromGroupAsync(Dictionary<string, string> excludedAgent, Guid guid)
    {
        var combinationAgent = _clusterClient.GetGrain<ICombinationGAgent>(guid);
        foreach (var agentId in excludedAgent.Keys.ToList())
        {
            var atomicAgent = _clusterClient.GetGrain<IAtomicGAgent>(Guid.Parse(agentId));
            await atomicAgent.RemoveFromGroupAsync(guid.ToString());
            var agentData = await atomicAgent.GetAgentAsync();
            var businessAgentGuid = ParseGuid(excludedAgent[agentId]);
            var businessAgent = await _gAgentFactory.GetGAgentAsync(agentData.Type, businessAgentGuid);
            await combinationAgent.UnregisterAsync(businessAgent);
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
        if (agentData.Properties.IsNullOrEmpty())
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

    public async Task<List<CombinationAgentDto>> GetCombinationAgentsAsync(string userAddress, string groupId, int pageIndex, int pageSize)
    {
        if (userAddress.IsNullOrEmpty())
        {
            _logger.LogInformation("GetAgentAsync Invalid userAddress: {userAddress} ", userAddress);
            throw new UserFriendlyException("Invalid userAddress");
        }
        if (pageIndex <= 0 || pageSize <= 1)
        {
            _logger.LogInformation("GetAgentAsync Invalid pageIndex: {pageIndex} pageSize:{pageSize}", pageIndex, pageSize);
            throw new UserFriendlyException("Invalid pageIndex pageSize");
        }
        
        var index = IndexPrefix + nameof(CombinationGAgentState).ToLower() + IndexSuffix;
        var filters = new List<Func<QueryContainerDescriptor<object>, QueryContainer>> { m => m.Term(t => t.Field("userAddress").Value(userAddress)) };
        if (!groupId.IsNullOrEmpty())
        {
            filters.Add(m => m.Term(t => t.Field("groupId").Value(groupId)));
        }

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
    
        return combinationGAgentStateDtoList.Select(stateDto => new CombinationAgentDto { Id = stateDto.Id.ToString(), Name = stateDto.Name, AgentComponent = JsonConvert.DeserializeObject<Dictionary<string, string>>(stateDto.AgentComponent) }).ToList();
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
        var result = new List<string> { agentId };
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
    
    public async Task RunAgentAsync(string agentId)
    {
        var gAgent = _clusterClient.GetGrain<ICombinationGAgent>(Guid.Parse(agentId));
        await gAgent.PublishEventAsync(new InvestmentEvent { Content = "test"});
    }

    public async Task<List<string>> GetAllAgents()
    {
        var systemAgents = new List<string>()
        {
            "GroupGAgent",
            "PublishingGAgent",
            "SubscriptionGAgent",
            "AtomicGAgent",
            "CombinationGAgent",
        };
        var availableGAgents = _gAgentManager.GetAvailableGAgentTypes();
        var validAgent = availableGAgents.Where(a => a.Namespace.StartsWith("Aevatar")).ToList();
        var businessAgent = validAgent.Where(a => !systemAgents.Contains(a.Name)).ToList();
        foreach (var type in businessAgent)
        {
            var agent = await _gAgentFactory.GetGAgentAsync(type.Name);
            var dto = await agent.GetInitializeDtoTypeAsync();
            _logger.LogInformation("GetAllAgents: {agent}", JsonConvert.SerializeObject(dto));
        }
        return businessAgent.Select(a => a.Name).ToList();
    }
}