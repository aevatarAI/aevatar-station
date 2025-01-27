using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Agents.Creator.Models;
using Aevatar.Application.Grains.Agents.Creator;
using Aevatar.Application.Grains.Subscription;
using Aevatar.Common;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Aevatar.CQRS.Provider;
using Aevatar.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Orleans.Metadata;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Aevatar.Service;


[RemoteService(IsEnabled = false)]
public class AgentService : ApplicationService, IAgentService
{
    private readonly IClusterClient _clusterClient;
    private readonly ICQRSProvider _cqrsProvider;
    private readonly ILogger<AgentService> _logger;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IGAgentManager _gAgentManager;
    private readonly IUserAppService _userAppService;
    private readonly IOptionsMonitor<AgentOptions> _agentOptions;
    private readonly GrainTypeResolver _grainTypeResolver;
    
    private const string IndexSuffix = "index";
    private const string IndexPrefix = "aevatar";

    public AgentService(
        IClusterClient clusterClient, 
        ICQRSProvider cqrsProvider, 
        ILogger<AgentService> logger,
        IGAgentFactory gAgentFactory, 
        IGAgentManager gAgentManager, 
        IUserAppService userAppService, 
        IOptionsMonitor<AgentOptions> agentOptions, 
        GrainTypeResolver grainTypeResolver)
    {
        _clusterClient = clusterClient;
        _cqrsProvider = cqrsProvider;
        _logger = logger;
        _gAgentFactory = gAgentFactory;
        _gAgentManager = gAgentManager;
        _userAppService = userAppService;
        _agentOptions = agentOptions;
        _grainTypeResolver = grainTypeResolver;
    }
    
    

    // public async Task<List<AtomicAgentDto>> GetAtomicAgentsAsync(string userAddress, int pageIndex, int pageSize)
    // {
    //     if (userAddress.IsNullOrEmpty())
    //     {
    //         _logger.LogInformation("GetAgentAsync Invalid userAddress: {userAddress}", userAddress);
    //         throw new UserFriendlyException("Invalid userAddress");
    //     }
    //
    //     if (pageIndex <= 0 || pageSize <= 1)
    //     {
    //         _logger.LogInformation("GetAgentAsync Invalid pageIndex: {pageIndex} pageSize:{pageSize}", pageIndex, pageSize);
    //         throw new UserFriendlyException("Invalid pageIndex pageSize");
    //     }
    //
    //     var index = IndexPrefix + nameof(AtomicGAgentState).ToLower() + IndexSuffix;
    //     var result = await _cqrsProvider.QueryStateAsync(index,
    //         q => q.Term(t => t.Field("userAddress").Value(userAddress)), 
    //         (pageIndex-1)*pageSize,
    //         pageSize
    //     );
    //     if (result == null)
    //     {
    //         return null;
    //     }
    //     
    //     var atomicGAgentStateDtoList = JsonConvert.DeserializeObject<List<AtomicGAgentStateDto>>(result);
    //
    //     return atomicGAgentStateDtoList.Select(stateDto => new AtomicAgentDto { Id = stateDto.Id.ToString(), Type = stateDto.Type, Name = stateDto.Name, Properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(stateDto.Properties) }).ToList();
    // }
    

    private Guid ParseGuid(string id)
    {
        if (!Guid.TryParse(id, out Guid validGuid))
        {
            _logger.LogInformation("Invalid id: {id}", id);
            throw new UserFriendlyException("Invalid id");
        }
        return validGuid;
    }
    
    // public async Task<List<CombinationAgentDto>> GetCombinationAgentsAsync(string userAddress, string groupId, int pageIndex, int pageSize)
    // {
    //     if (userAddress.IsNullOrEmpty())
    //     {
    //         _logger.LogInformation("GetAgentAsync Invalid userAddress: {userAddress} ", userAddress);
    //         throw new UserFriendlyException("Invalid userAddress");
    //     }
    //     if (pageIndex <= 0 || pageSize <= 1)
    //     {
    //         _logger.LogInformation("GetAgentAsync Invalid pageIndex: {pageIndex} pageSize:{pageSize}", pageIndex, pageSize);
    //         throw new UserFriendlyException("Invalid pageIndex pageSize");
    //     }
    //     
    //     var index = IndexPrefix + nameof(CombinationGAgentState).ToLower() + IndexSuffix;
    //     var filters = new List<Func<QueryContainerDescriptor<object>, QueryContainer>> { m => m.Term(t => t.Field("userAddress").Value(userAddress)) };
    //     if (!groupId.IsNullOrEmpty())
    //     {
    //         filters.Add(m => m.Term(t => t.Field("groupId").Value(groupId)));
    //     }
    //
    //     var result = await _cqrsProvider.QueryStateAsync(index,
    //         q => q.Bool(b => b.Must(filters)),
    //         (pageIndex-1)*pageSize,
    //         pageSize
    //     );
    //     if (result == null)
    //     {
    //         return null;
    //     }
    //     
    //     var combinationGAgentStateDtoList = JsonConvert.DeserializeObject<List<CombinationGAgentStateDto>>(result);
    //
    //     return combinationGAgentStateDtoList.Select(stateDto => new CombinationAgentDto { Id = stateDto.Id.ToString(), Name = stateDto.Name, AgentComponent = JsonConvert.DeserializeObject<Dictionary<string, string>>(stateDto.AgentComponent) }).ToList();
    // }

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
        var gAgent = _clusterClient.GetGrain<ICreatorGAgent>(Guid.Parse(agentId));
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
    
    public async Task<Dictionary<string, AgentTypeData?>> GetAgentTypeDataMap()
    {
        var systemAgents = _agentOptions.CurrentValue.SystemAgentList;
        var availableGAgents = _gAgentManager.GetAvailableGAgentTypes();
        var validAgent = availableGAgents.Where(a => a.Namespace.StartsWith("Aevatar")).ToList();
        var businessAgentTypes = validAgent.Where(a => !systemAgents.Contains(a.Name)).ToList();

        var dict = new Dictionary<string, AgentTypeData?>();
        
        foreach (var agentType in businessAgentTypes)
        {
            var grainType = _grainTypeResolver.GetGrainType(agentType).ToString();
            if (grainType != null)
            {
                var agentTypeData = new AgentTypeData
                {
                    FullName = agentType.FullName,
                };
                var grainId = GrainId.Create(grainType, Guid.NewGuid().ToString());
                var agent = await _gAgentFactory.GetGAgentAsync(grainId);
                var initializeDtoType = await agent.GetConfigurationTypeAsync();
                if (initializeDtoType == null || initializeDtoType.IsAbstract)
                {
                    dict[grainType] = agentTypeData;
                    continue;
                }
            
                PropertyInfo[] properties = initializeDtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            
                var initializationData = new InitializationData
                {
                    DtoType = initializeDtoType
                };
            
                var propertyDtos = new List<PropertyData>();
                foreach (PropertyInfo property in properties)
                {
                    var propertyDto = new PropertyData()
                    {
                        Name = property.Name,
                        Type = property.PropertyType
                    };
                    propertyDtos.Add(propertyDto);
                }
                initializationData.Properties = propertyDtos;
                agentTypeData.InitializationData = initializationData;
                dict[grainType] = agentTypeData;
            }
        }

        return dict;
    }
    
    public async Task<List<AgentTypeDto>> GetAllAgents()
    {
        var propertyDtos = await GetAgentTypeDataMap();
        var resp = new List<AgentTypeDto>();
        foreach (var kvp in propertyDtos)
        {
            var paramDto = new AgentTypeDto
            {
                AgentType = kvp.Key,
                FullName = kvp.Value?.FullName ?? kvp.Key,
            };
            
            if (kvp.Value != null )
            {
                paramDto.FullName = kvp.Value.FullName ?? "";
                if (kvp.Value.InitializationData != null)
                {
                    paramDto.AgentParams = kvp.Value.InitializationData.Properties.Select(p => new ParamDto
                    {
                        Name = p.Name,
                        Type = p.Type.ToString()
                    }).ToList();
                }
            }
            
            resp.Add(paramDto);
        }
        return resp;
    }

    private ConfigurationBase SetupInitializedConfig(InitializationData initializationData,  string propertiesString)
    {
        var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(propertiesString);
        var actualDto = Activator.CreateInstance(initializationData.DtoType);
        var config = (ConfigurationBase)actualDto!;
                    
        foreach (var kvp in properties)
        {
            var propertyName = kvp.Key; 
            var propertyValue = kvp.Value;
            var propertyType = initializationData.Properties.FirstOrDefault(x => x.Name == propertyName)?.Type;
            if (propertyType == null)
            {
                continue;
            }
                        
            var propertyInfo = initializationData.DtoType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (propertyInfo == null || !propertyInfo.CanWrite)
            {
                _logger.LogInformation("Property {propertyName} not found or cannot be written.", propertyName);
                throw new UserFriendlyException("property could not be found or cannot be written");
            }
            
            object convertedValue = ReflectionUtil.ConvertValue(propertyType, propertyValue);
            propertyInfo?.SetValue(config, convertedValue);
            _logger.LogInformation("SetGroupAsync propertyName: {propertyName}, propertyValue: {propertyValue}, propertyType: {propertyType}", propertyName, propertyValue, propertyType);
        }

        return config;
    }

    public async Task<AgentDto> CreateAgentAsync(CreateAgentInputDto dto)
    {
        CheckCreateParam(dto);
        var userId = _userAppService.GetCurrentUserId();
        var guid = Guid.NewGuid();
        var agentData = new AgentData
        {
            UserId = userId,
            AgentType = dto.AgentType,
            Properties = JsonConvert.SerializeObject(dto.Properties),
            Name = dto.Name
        };
        
        var initializationParam = JsonConvert.SerializeObject(dto.Properties);
        var businessAgent = await InitializeBusinessAgent(guid, dto.AgentType, initializationParam);
        var creatorAgent = _clusterClient.GetGrain<ICreatorGAgent>(guid);
        agentData.BusinessAgentGrainId = businessAgent.GetGrainId();
        await creatorAgent.CreateAgentAsync(agentData);
        
        var resp = new AgentDto
        {
            Id = guid,
            AgentType = dto.AgentType,
            Name = dto.Name,
            GrainId = businessAgent.GetGrainId(),
            Properties = dto.Properties
        };
        
        
        return resp;
    }

    private void CheckCreateParam(CreateAgentInputDto createDto)
    {
        if (createDto.AgentType.IsNullOrEmpty())
        {
            _logger.LogInformation("CreateAgentAsync type is null");
            throw new UserFriendlyException("Agent type is null");
        }
        
        if (createDto.Name.IsNullOrEmpty())
        {
            _logger.LogInformation("CreateAgentAsync name is null");
            throw new UserFriendlyException("name is null");
        }
    }
    
    private async Task<IGAgent> InitializeBusinessAgent(Guid primaryKey, string agentType, string agentProperties)
    {
        var agentTypeDataMap = await GetAgentTypeDataMap();
        ConfigurationBase? config = null;
        
        if (agentTypeDataMap.TryGetValue(agentType, out var agentTypeData) && !agentProperties.IsNullOrEmpty()) 
        {
            if (agentTypeData != null && agentTypeData.InitializationData != null)
            {
                config = SetupInitializedConfig(agentTypeData.InitializationData, agentProperties);
            }
        }
        
        var grainId = GrainId.Create(agentType, primaryKey.ToString("N"));
        var businessAgent = await _gAgentFactory.GetGAgentAsync(grainId, config);
        return businessAgent;
    }
    
    public async Task<AgentDto> UpdateAgentAsync(Guid guid, UpdateAgentInputDto dto)
    {
        var creatorAgent = _clusterClient.GetGrain<ICreatorGAgent>(guid);
        var agentState = await creatorAgent.GetAgentAsync();

        EnsureUserAuthorized(agentState.UserId);
        
        await creatorAgent.UpdateAgentAsync(dto);
        var businessAgent = await _gAgentFactory.GetGAgentAsync(agentState.BusinessAgentGrainId);

        if (!dto.Properties.IsNullOrEmpty())
        {
            var updatedParam = JsonConvert.SerializeObject(dto.Properties);
            var agentTypeDataMap = await GetAgentTypeDataMap();
            ConfigurationBase? config = null;
            if (agentTypeDataMap.TryGetValue(agentState.AgentType, out var agentTypeData) && !updatedParam.IsNullOrEmpty()) 
            {
                if (agentTypeData != null && agentTypeData.InitializationData != null)
                {
                    config = SetupInitializedConfig(agentTypeData.InitializationData, updatedParam);
                }
            }

            if (config != null)
            {
                await businessAgent.ConfigAsync(config);
            }
            else
            {
                _logger.LogError("no properties to be updated, id: {id}", guid);
            }
        }
        
        var resp = new AgentDto
        {
            Id = guid,
            AgentType = agentState.AgentType,
            Name = dto.Name,
            GrainId = agentState.BusinessAgentGrainId,
            Properties = dto.Properties
        };
       
        return resp;
    }

    public async Task<AgentDto> GetAgentAsync(Guid guid)
    {
        var creatorAgent = _clusterClient.GetGrain<ICreatorGAgent>(guid);
        var agentState = await creatorAgent.GetAgentAsync();

        EnsureUserAuthorized(agentState.UserId);
        
        var resp = new AgentDto
        {
            Id = guid,
            AgentType = agentState.AgentType,
            Name = agentState.Name,
            GrainId = agentState.BusinessAgentGrainId,
            Properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(agentState.Properties)
        };
        
        return resp;
    }
    
    public async Task<SubAgentDto> AddSubAgentAsync(Guid guid, AddSubAgentDto addSubAgentDto)
    {
        var creatorAgent = _clusterClient.GetGrain<ICreatorGAgent>(guid);
        var agentState = await creatorAgent.GetAgentAsync();
        
        EnsureUserAuthorized(agentState.UserId);
        
        var agent = await _gAgentFactory.GetGAgentAsync(agentState.BusinessAgentGrainId);
        
        // check if all sub agent can be added 
        var newSubAgentGrainIds = new List<GrainId>();
        foreach (var subAgentGuid in addSubAgentDto.SubAgents)
        {
            var subAgent = _clusterClient.GetGrain<ICreatorGAgent>(subAgentGuid);
            var subAgentState = await subAgent.GetAgentAsync();
            EnsureUserAuthorized(subAgentState.UserId);
            
            newSubAgentGrainIds.Add(subAgentState.BusinessAgentGrainId);
        }
        
        var allEventsHandled = agentState.EventInfoList.Select(x => x.EventType).ToList();
        var subAgentGrainIds = await GetSubAgentGrainIds(agent);
        
        // add parent events and make creator agent child of business agent in order to publish events
        var children = await agent.GetChildrenAsync();
        if (children.IsNullOrEmpty())
        {
            await agent.RegisterAsync(creatorAgent);
            var parentEventData =  await agent.GetAllSubscribedEventsAsync();
            if (parentEventData != null)
            {
                allEventsHandled.AddRange(parentEventData);
            }
        }
        
        // register sub agent and add their events to parent agent
        var subAgentGuids = subAgentGrainIds.Select(x => x.GetGuidKey()).ToList();
        foreach (var grainId in newSubAgentGrainIds)
        {
            if (subAgentGrainIds.Contains(grainId))
            {
                continue;
            }
            
            var businessAgent = await _gAgentFactory.GetGAgentAsync(grainId);
            await agent.RegisterAsync(businessAgent);
            subAgentGuids.Add(grainId.GetGuidKey());
            
            var eventsHandledByAgent = await businessAgent.GetAllSubscribedEventsAsync();
            if (eventsHandledByAgent != null)
            {
                var eventsToAdd = eventsHandledByAgent.Except(allEventsHandled).ToList(); 
                allEventsHandled.AddRange(eventsToAdd);
            }
        }
        await creatorAgent.UpdateAvailableEventsAsync(allEventsHandled);
        
        var resp = new SubAgentDto
        {
            SubAgents = subAgentGuids
        };

        return resp;
    }
    
    private void EnsureUserAuthorized(Guid userId)
    {
        var currentUserId = _userAppService.GetCurrentUserId();
        if (currentUserId != userId)
        {
            _logger.LogInformation("User {userId} is not allowed.", currentUserId); 
            throw new UserFriendlyException("You are not the owner of this agent");
        }
        
    }
    
    public async Task<SubAgentDto> RemoveSubAgentAsync(Guid guid, RemoveSubAgentDto removeSubAgentDto)
    {
        var creatorAgent = _clusterClient.GetGrain<ICreatorGAgent>(guid);
        var agentState = await creatorAgent.GetAgentAsync();
        
        EnsureUserAuthorized(agentState.UserId);
        
        var agent = await _gAgentFactory.GetGAgentAsync(agentState.BusinessAgentGrainId);
        
        var subAgentGrainIds = await GetSubAgentGrainIds(agent);
        var allEventsHandled = new List<Type>();
        var parentEventData =  await agent.GetAllSubscribedEventsAsync();
        if (parentEventData != null)
        {
            allEventsHandled.AddRange(parentEventData);
        }
        
        var remainSubAgentGuids = new List<Guid>();
        foreach (var subAgentGrainId in subAgentGrainIds)
        {
            var subAgent = await _gAgentFactory.GetGAgentAsync(subAgentGrainId);
            var subAgentGuid = subAgent.GetPrimaryKey();

            if (removeSubAgentDto.RemovedSubAgents.Contains(subAgentGuid))
            {
                await agent.UnregisterAsync(subAgent);
            }
            else
            {
                remainSubAgentGuids.Add(subAgentGuid);
                var eventsHandledByAgent = await subAgent.GetAllSubscribedEventsAsync();
                if (eventsHandledByAgent != null)
                {
                    var eventsToAdd = eventsHandledByAgent.Except(allEventsHandled).ToList(); 
                    allEventsHandled.AddRange(eventsToAdd);
                }
            }
        }
        await creatorAgent.UpdateAvailableEventsAsync(allEventsHandled);
        
        return new SubAgentDto
        {
            SubAgents = remainSubAgentGuids
        };
    }

    public async Task<AgentRelationshipDto> GetAgentRelationshipAsync(Guid guid)
    {
        var creatorAgent = _clusterClient.GetGrain<ICreatorGAgent>(guid);
        var agentState = await creatorAgent.GetAgentAsync();
        var agent = await _gAgentFactory.GetGAgentAsync(agentState.BusinessAgentGrainId);
        
      
        var parentGrainId = await agent.GetParentAsync();
        var subAgentGrainIds = await GetSubAgentGrainIds(agent);
        var subAgentGuids = subAgentGrainIds.Select(x => x.GetGuidKey()).ToList();
        
        return new AgentRelationshipDto
        {
            Parent = parentGrainId.IsDefault ? null : parentGrainId.GetGuidKey(),
            SubAgents = subAgentGuids
        };
    }
    
    
    public async Task RemoveAllSubAgentAsync(Guid guid)
    {
        var creatorAgent = _clusterClient.GetGrain<ICreatorGAgent>(guid);
        var agentState = await creatorAgent.GetAgentAsync();
        
        var agent = await _gAgentFactory.GetGAgentAsync(agentState.BusinessAgentGrainId);
        var subAgentGrainIds = await GetSubAgentGrainIds(agent);
        await RemoveSubAgentAsync(guid,
            new RemoveSubAgentDto { RemovedSubAgents = subAgentGrainIds.Select(x => x.GetGuidKey()).ToList() });
    }

    private async Task<List<GrainId>> GetSubAgentGrainIds(IGAgent agent)
    {
        var children = await agent.GetChildrenAsync();
        var subAgentGrainIds = new List<GrainId>();
        var creatorGAgentType = _grainTypeResolver.GetGrainType(typeof(CreatorGAgent));
        var subscriptionGAgentType = _grainTypeResolver.GetGrainType(typeof(SubscriptionGAgent));
        foreach (var grainId in children)
        {
            var grainType = grainId.Type;
            if (grainType == creatorGAgentType || grainType == subscriptionGAgentType)
            {
                continue;
            }
            subAgentGrainIds.Add(grainId);
        }

        return subAgentGrainIds;
    }

    public async Task DeleteAgentAsync(Guid guid)
    {
        var creatorAgent = _clusterClient.GetGrain<ICreatorGAgent>(guid);
        var agentState = await creatorAgent.GetAgentAsync();
        
        EnsureUserAuthorized(agentState.UserId);
        
        var agent = await _gAgentFactory.GetGAgentAsync(agentState.BusinessAgentGrainId);
        var subAgentGrainIds = await agent.GetChildrenAsync();
        if (!subAgentGrainIds.IsNullOrEmpty())
        {
            _logger.LogInformation("Agent {agentId} has subagents, please remove them first.", guid);
            throw new UserFriendlyException("Agent has subagents, please remove them first.");
        }
        
        var parentGrainId = await agent.GetParentAsync();
        if (!parentGrainId.IsDefault)
        {
            var parentAgent = await _gAgentFactory.GetGAgentAsync(parentGrainId);
            await parentAgent.UnregisterAsync(agent);
            await creatorAgent.DeleteAgentAsync();
        }
        else
        {
            _logger.LogInformation("Agent {agentId} has parent, please remove from it first.", guid);
            throw new UserFriendlyException("Agent has parent, please remove from it first.");
        }
        

       
    }
    
}