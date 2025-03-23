using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Agents.Creator;
using Aevatar.Agents.Creator.Models;
using Aevatar.Application.Grains.Agents.Creator;
using Aevatar.Application.Grains.Subscription;
using Aevatar.Common;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Aevatar.CQRS.Provider;
using Aevatar.Exceptions;
using Aevatar.Options;
using Aevatar.Schema;
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
    private readonly ISchemaProvider _schemaProvider;

    public AgentService(
        IClusterClient clusterClient,
        ICQRSProvider cqrsProvider,
        ILogger<AgentService> logger,
        IGAgentFactory gAgentFactory,
        IGAgentManager gAgentManager,
        IUserAppService userAppService,
        IOptionsMonitor<AgentOptions> agentOptions,
        GrainTypeResolver grainTypeResolver,
        ISchemaProvider schemaProvider)
    {
        _clusterClient = clusterClient;
        _cqrsProvider = cqrsProvider;
        _logger = logger;
        _gAgentFactory = gAgentFactory;
        _gAgentManager = gAgentManager;
        _userAppService = userAppService;
        _agentOptions = agentOptions;
        _grainTypeResolver = grainTypeResolver;
        _schemaProvider = schemaProvider;
    }

    public async Task<Tuple<long, List<AgentGEventIndex>>> GetAgentEventLogsAsync(string agentId, int pageNumber,
        int pageSize)
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

    private async Task<Dictionary<string, AgentTypeData?>> GetAgentTypeDataMap()
    {
        var systemAgents = _agentOptions.CurrentValue.SystemAgentList;
        Stopwatch stopwatch = Stopwatch.StartNew();
        var availableGAgents = _gAgentManager.GetAvailableGAgentTypes();
        stopwatch.Stop();
        _logger.LogInformation("GetAgentTypeDataMap GetAvailableGAgentTypes {Time}",stopwatch.ElapsedMilliseconds);
        var validAgent = availableGAgents.Where(a => !a.Namespace.StartsWith("OrleansCodeGen")).ToList();
        var businessAgentTypes = validAgent.Where(a => !systemAgents.Contains(a.Name)).ToList();
        _logger.LogInformation("GetAgentTypeDataMap businessAgentTypes {businessAgentTypesCount}",businessAgentTypes.Count);

        var dict = new Dictionary<string, AgentTypeData?>();
       
        foreach (var agentType in businessAgentTypes)
        {
            stopwatch = Stopwatch.StartNew();
            var grainType = _grainTypeResolver.GetGrainType(agentType).ToString();
            _logger.LogInformation("GetAgentTypeDataMap GetGrainType agentType {agentType} ,{Time}",agentType.Name,stopwatch.ElapsedMilliseconds);
            stopwatch.Stop();
            if (grainType != null)
            {
                var agentTypeData = new AgentTypeData
                {
                    FullName = agentType.FullName,
                };
                var grainId = GrainId.Create(grainType, GuidUtil.GuidToGrainKey(GuidUtil.StringToGuid("AgentDefaultId"))); // make sure only one agent instance for each type
                var agent = await _gAgentFactory.GetGAgentAsync(grainId);
                var initializeDtoType = await agent.GetConfigurationTypeAsync();
                if (initializeDtoType == null || initializeDtoType.IsAbstract)
                {
                    dict[grainType] = agentTypeData;
                    continue;
                }

                PropertyInfo[] properties =
                    initializeDtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance |
                                                    BindingFlags.DeclaredOnly);

                var initializationData = new Configuration
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

    private async Task<Configuration?> GetAgentConfigurationAsync(IGAgent agent)
    {
        var configurationType = await agent.GetConfigurationTypeAsync();
        if (configurationType == null || configurationType.IsAbstract)
        {
            return null;
        }

        PropertyInfo[] properties =
            configurationType.GetProperties(BindingFlags.Public | BindingFlags.Instance |
                                            BindingFlags.DeclaredOnly);

        var configuration = new Configuration
        {
            DtoType = configurationType
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

        configuration.Properties = propertyDtos;
        return configuration;
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

            if (kvp.Value != null)
            {
                paramDto.FullName = kvp.Value.FullName ?? "";
                if (kvp.Value.InitializationData != null)
                {
                    paramDto.AgentParams = kvp.Value.InitializationData.Properties.Select(p => new ParamDto
                    {
                        Name = p.Name,
                        Type = p.Type.ToString()
                    }).ToList();

                    paramDto.PropertyJsonSchema =
                        _schemaProvider.GetTypeSchema(kvp.Value.InitializationData.DtoType).ToJson();
                }
            }

            resp.Add(paramDto);
        }

        return resp;
    }

    private ConfigurationBase SetupConfigurationData(Configuration configuration,
        string propertiesString)
    {
        var actualDto = Activator.CreateInstance(configuration.DtoType);

        var config = (ConfigurationBase)actualDto!;
        var schema = _schemaProvider.GetTypeSchema(config.GetType());
        var validateResponse = schema.Validate(propertiesString);
        if (validateResponse.Count > 0)
        {
            var validateDic = _schemaProvider.ConvertValidateError(validateResponse);
            throw new ParameterValidateException(validateDic);
        }

        config = JsonConvert.DeserializeObject(propertiesString, configuration.DtoType) as ConfigurationBase;
        if (config == null)
        {
            throw new BusinessException("[AgentService][SetupInitializedConfig] config convert error");
        }
        
        return config;
    }

    public async Task<AgentDto> CreateAgentAsync(CreateAgentInputDto dto)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        CheckCreateParam(dto);
        stopwatch.Stop();
        _logger.LogInformation("CreateAgentAsync CheckCreateParam {Time}",stopwatch.ElapsedMilliseconds);
        stopwatch = Stopwatch.StartNew();
        var userId = _userAppService.GetCurrentUserId();
        stopwatch.Stop();
        _logger.LogInformation("CreateAgentAsync GetCurrentUserId {Time}",stopwatch.ElapsedMilliseconds);
       
        var guid = dto.AgentId ?? Guid.NewGuid();
        var agentData = new AgentData
        {
            UserId = userId,
            AgentType = dto.AgentType,
            Properties = !dto.Properties.IsNullOrEmpty()?JsonConvert.SerializeObject(dto.Properties):"",
            Name = dto.Name
        };

        var initializationParam = JsonConvert.SerializeObject(dto.Properties);
        stopwatch = Stopwatch.StartNew();
        var businessAgent = await InitializeBusinessAgent(guid, dto.AgentType, initializationParam);
        stopwatch.Stop();
        _logger.LogInformation("CreateAgentAsync InitializeBusinessAgent {Time}",stopwatch.ElapsedMilliseconds);
        stopwatch = Stopwatch.StartNew();
        var creatorAgent = _clusterClient.GetGrain<ICreatorGAgent>(guid);
        agentData.BusinessAgentGrainId = businessAgent.GetGrainId();
        await creatorAgent.CreateAgentAsync(agentData);
        stopwatch.Stop();
        _logger.LogInformation("CreateAgentAsync CreateAgentAsync {Time}",stopwatch.ElapsedMilliseconds);
        var resp = new AgentDto
        {
            Id = guid,
            AgentType = dto.AgentType,
            Name = dto.Name,
            GrainId = businessAgent.GetGrainId(),
            Properties = dto.Properties,
            AgentGuid = businessAgent.GetPrimaryKey()
        };

        return resp;
    }

    public async Task<List<AgentInstanceDto>> GetAllAgentInstances(int pageIndex, int pageSize)
    {
        var result = new List<AgentInstanceDto>();
        var currentUserId = _userAppService.GetCurrentUserId();
        var response =
            await _cqrsProvider.GetUserInstanceAgent<CreatorGAgentState, AgentInstanceSQRSDto>(currentUserId, pageIndex,
                pageSize);
        if (response.Item1 == 0)
        {
            return result;
        }

        result.AddRange(response.Item2.Select(state => new AgentInstanceDto()
        {
            Id = state.Id, Name = state.Name,
            Properties = state.Properties == null
                ? null
                : JsonConvert.DeserializeObject<Dictionary<string, object>>(state.Properties),
            AgentType = state.AgentType,
        }));

        return result;
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

    private async Task<IGAgent> InitializeBusinessAgent(Guid primaryKey, string agentType,
        string agentProperties)
    {
        Stopwatch  stopwatch = Stopwatch.StartNew();
        var grainId = GrainId.Create(agentType, GuidUtil.GuidToGrainKey(primaryKey));
        var businessAgent = await _gAgentFactory.GetGAgentAsync(grainId);
        stopwatch.Stop();
        _logger.LogInformation("CreateAgentAsync InitializeBusinessAgent GetGAgentAsync{Time}",stopwatch.ElapsedMilliseconds);
        
        stopwatch = Stopwatch.StartNew();
        var initializationData = await GetAgentConfigurationAsync(businessAgent);
        stopwatch.Stop();
        _logger.LogInformation("CreateAgentAsync InitializeBusinessAgent GetAgentConfigurationAsync{Time}",stopwatch.ElapsedMilliseconds);
        
        if (initializationData != null && !agentProperties.IsNullOrEmpty())
        {
            stopwatch = Stopwatch.StartNew();
            var config = SetupConfigurationData(initializationData, agentProperties);
            await businessAgent.ConfigAsync(config);
            stopwatch.Stop();
            _logger.LogInformation("CreateAgentAsync InitializeBusinessAgent ConfigAsync{Time}",stopwatch.ElapsedMilliseconds);
        }
        
        return businessAgent;
    }

    public async Task<AgentDto> UpdateAgentAsync(Guid guid, UpdateAgentInputDto dto)
    {
        var creatorAgent = _clusterClient.GetGrain<ICreatorGAgent>(guid);
        var agentState = await creatorAgent.GetAgentAsync();

        EnsureUserAuthorized(agentState.UserId);

        var businessAgent = await _gAgentFactory.GetGAgentAsync(agentState.BusinessAgentGrainId);

        if (!dto.Properties.IsNullOrEmpty())
        {
            var updatedParam = JsonConvert.SerializeObject(dto.Properties);
            var configuration = await GetAgentConfigurationAsync(businessAgent);
            if (configuration != null && !updatedParam.IsNullOrEmpty())
            {
                var config = SetupConfigurationData(configuration, updatedParam);
                await businessAgent.ConfigAsync(config);
                await creatorAgent.UpdateAgentAsync(new UpdateAgentInput
                {
                    Name = dto.Name, 
                    Properties = JsonConvert.SerializeObject(dto.Properties)
                });
            
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
        _logger.LogInformation("GetAgentAsync id: {id} state: {state}", guid, JsonConvert.SerializeObject(agentState));

        EnsureUserAuthorized(agentState.UserId);

        var resp = new AgentDto
        {
            Id = guid,
            AgentType = agentState.AgentType,
            Name = agentState.Name,
            GrainId = agentState.BusinessAgentGrainId,
            Properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(agentState.Properties),
            AgentGuid = agentState.BusinessAgentGrainId.GetGuidKey()
        };
        
        var businessAgent = await _gAgentFactory.GetGAgentAsync(agentState.BusinessAgentGrainId);
        
        var configuration = await GetAgentConfigurationAsync(businessAgent);
        if (configuration != null)
        {
            resp.PropertyJsonSchema = _schemaProvider.GetTypeSchema(configuration.DtoType).ToJson();
        }

        return resp;
    }

    public async Task<SubAgentDto> AddSubAgentAsync(Guid guid, AddSubAgentDto addSubAgentDto)
    {
        _logger.LogInformation("Add sub Agent: {agent}", JsonConvert.SerializeObject(addSubAgentDto));
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
            var parentEventData = await agent.GetAllSubscribedEventsAsync();
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
                _logger.LogInformation("all events for agent {agentId}, events: {events}",
                    grainId.GetGuidKey(), JsonConvert.SerializeObject(eventsHandledByAgent));
                var eventsToAdd = eventsHandledByAgent.Except(allEventsHandled).ToList();
                _logger.LogInformation("Adding events for agent {agentId}, events: {events}",
                    grainId.GetGuidKey(), JsonConvert.SerializeObject(eventsToAdd));
                allEventsHandled.AddRange(eventsToAdd);
            }
            else
            {
                _logger.LogInformation("No events handled by agent {agentId}", grainId.GetGuidKey());
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
        var parentEventData = await agent.GetAllSubscribedEventsAsync();
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
        if (!subAgentGrainIds.IsNullOrEmpty() &&
            (subAgentGrainIds.Count > 1 || subAgentGrainIds[0] != creatorAgent.GetGrainId()))
        {
            _logger.LogInformation("Agent {agentId} has subagents, please remove them first.", guid);
            throw new UserFriendlyException("Agent has subagents, please remove them first.");
        }

        var parentGrainId = await agent.GetParentAsync();
        if (parentGrainId.IsDefault)
        {
            if (subAgentGrainIds.Any())
            {
                await agent.UnregisterAsync(creatorAgent);
            }

            await creatorAgent.DeleteAgentAsync();
        }
        else
        {
            _logger.LogInformation("Agent {agentId} has parent, please remove from it first.", guid);
            throw new UserFriendlyException("Agent has parent, please remove from it first.");
        }
    }
}