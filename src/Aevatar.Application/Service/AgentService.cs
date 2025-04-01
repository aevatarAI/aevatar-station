using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Agents;
using Aevatar.Agents.Creator;
using Aevatar.Agents.Creator.Models;
using Aevatar.Application.Grains.Agents.Creator;
using Aevatar.Application.Grains.Subscription;
using Aevatar.Common;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Aevatar.CQRS.Provider;
using Aevatar.Exceptions;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using Aevatar.Options;
using Aevatar.Schema;
using Aevatar.Sender;
using Aevatar.Workflow;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Blackboard;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Orleans.Metadata;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.Application.Services;
using ZstdSharp.Unsafe;

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
    private readonly IWorkflowRepository _workflowRepository;

    public AgentService(
        IClusterClient clusterClient,
        ICQRSProvider cqrsProvider,
        ILogger<AgentService> logger,
        IGAgentFactory gAgentFactory,
        IGAgentManager gAgentManager,
        IUserAppService userAppService,
        IOptionsMonitor<AgentOptions> agentOptions,
        GrainTypeResolver grainTypeResolver,
        ISchemaProvider schemaProvider, IWorkflowRepository workflowRepository)
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
        _workflowRepository = workflowRepository;
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
        var availableGAgents = _gAgentManager.GetAvailableGAgentTypes();
        var validAgent = availableGAgents.Where(a => !a.Namespace.StartsWith("OrleansCodeGen")).ToList();
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
                var grainId = GrainId.Create(grainType,
                    GuidUtil.GuidToGrainKey(
                        GuidUtil.StringToGuid("AgentDefaultId"))); // make sure only one agent instance for each type
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
        CheckCreateParam(dto);
        var userId = _userAppService.GetCurrentUserId();
        var guid = dto.AgentId ?? Guid.NewGuid();
        var agentData = new AgentData
        {
            UserId = userId,
            AgentType = dto.AgentType,
            Properties = JsonConvert.SerializeObject(dto.Properties),
            Name = dto.Name
        };

        var initializationParam =
            dto.Properties.IsNullOrEmpty() ? string.Empty : JsonConvert.SerializeObject(dto.Properties);
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
        var grainId = GrainId.Create(agentType, GuidUtil.GuidToGrainKey(primaryKey));
        var businessAgent = await _gAgentFactory.GetGAgentAsync(grainId);

        var initializationData = await GetAgentConfigurationAsync(businessAgent);
        if (initializationData != null && !agentProperties.IsNullOrEmpty())
        {
            var config = SetupConfigurationData(initializationData, agentProperties);
            await businessAgent.ConfigAsync(config);
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

    public async Task<string> SimulateWorkflowAsync(string workflowGrainId,
        List<WorkflowAgentDefinesDto> workUnitRelations)
    {
        return await CheckWorkflowWithGrainIdAsync(workflowGrainId, workUnitRelations);
    }

    public async Task<CreateWorkflowResponseDto> CreateWorkflowAsync(WorkflowAgentsDto workflowAgentDto)
    {
        var result = new CreateWorkflowResponseDto();
        var errorStr =
            await CheckWorkflowAsync(workflowAgentDto.WorkUnitRelations, new List<WorkflowAgentDefinesDto>());
        if (errorStr.IsNullOrEmpty() == false)
        {
            result.ErrorMessage = errorStr;
            return result;
        }

        var workflowAgent = _clusterClient.GetGrain<IWorkflowCoordinatorGAgent>(Guid.NewGuid());
        var blackboardAgent = _clusterClient.GetGrain<IBlackboardGAgent>(Guid.NewGuid());

        await workflowAgent.RegisterAsync(blackboardAgent);
        foreach (var item in workflowAgentDto.WorkUnitRelations)
        {
            var grainId = GrainId.Parse(item.GrainId);
            var gAgent = _clusterClient.GetGrain<IGAgent>(grainId);
            await workflowAgent.RegisterAsync(gAgent);
        }

        result.WorkflowGrainId = workflowAgent.GetGrainId().ToString();
        var workflowList = workflowAgentDto.WorkUnitRelations
            .Select(s => new WorkflowUnitDto() { GrainId = s.GrainId, NextGrainId = s.NextGrainId }).ToList();
        var publishGrain = _clusterClient.GetGrain<IPublishingGAgent>(workflowAgent.GetPrimaryKey());
        await workflowAgent.RegisterAsync(publishGrain);
        await workflowAgent.ConfigAsync(new WorkflowCoordinatorConfigDto() { WorkflowUnitList = workflowList });

        await _workflowRepository.InsertAsync(new WorkflowInfo()
        {
            WorkflowGrainId = workflowAgent.GetGrainId().ToString(), WorkUnitList = workflowAgentDto.WorkUnitRelations
                .Select(s => new WorkflowUintInfo()
                {
                    GrainId = s.GrainId,
                    NextGrainId = s.NextGrainId,
                    XPosition = s.XPosition,
                    YPosition = s.YPosition
                }).ToList()
        });

        return result;
    }

    public async Task<List<WorkflowAgentDefinesDto>> GetWorkflowUnitRelationsAsync(string workflowGrainId)
    {
        // var workflowCoordinator = GetWorkFlowGAgent(workflowGrainId);
        // var workflowState = await workflowCoordinator.GetStateAsync();
        var workflowInfo = await _workflowRepository.GetByWorkflowGrainId(workflowGrainId);
        if (workflowInfo == null)
        {
            return  new List<WorkflowAgentDefinesDto>();
        }

        return workflowInfo.WorkUnitList.Select(s => new WorkflowAgentDefinesDto()
        {
            GrainId= s.GrainId,
            NextGrainId = s.NextGrainId,
            XPosition = s.XPosition,
            YPosition = s.YPosition,
        }).ToList();
    }

    public async Task<string> EditWorkWorkflowAsync(string workflowGrainId,
        List<WorkflowAgentDefinesDto> workflowUnitList)
    {
        var errorMsg = await CheckWorkflowWithGrainIdAsync(workflowGrainId, workflowUnitList);
        if (errorMsg.IsNullOrEmpty() == false)
        {
            return errorMsg;
        }

        var workflowRelation = await GetWorkflowUnitRelationsAsync(workflowGrainId);
        var notExistWorkUnit = workflowUnitList
            .Where(w => workflowRelation.Exists(e => e.GrainId == w.GrainId) == false).ToList();

        var workflow = GetWorkFlowGAgent(workflowGrainId);
        if (notExistWorkUnit.Count > 0)
        {
            foreach (var item in notExistWorkUnit)
            {
                var grainId = GrainId.Parse(item.GrainId);
                var gAgent = _clusterClient.GetGrain<IGAgent>(grainId);
                await workflow.SubscribeToAsync(gAgent);
            }
        }

        var workUnit = await _workflowRepository.GetByWorkflowGrainId(workflowGrainId);
        if (workUnit == null)
        {
            return "workflow not found";
        }
        
        var workflowUnitDtoList = workflowUnitList
            .Select(s => new WorkflowUnitDto { GrainId = s.GrainId, NextGrainId = s.NextGrainId }).ToList();

        var publishGrain = _clusterClient.GetGrain<IPublishingGAgent>(workflow.GetPrimaryKey());
        if (await publishGrain.GetParentAsync() == default)
        {
            publishGrain = _clusterClient.GetGrain<IPublishingGAgent>(workflow.GetPrimaryKey());
            await workflow.RegisterAsync(publishGrain);
        }

        await publishGrain.PublishEventAsync(new ResetWorkflowEvent() { WorkflowUnitList = workflowUnitDtoList });
        workUnit.WorkUnitList = workflowUnitList.Select(s => new WorkflowUintInfo()
        {
            GrainId= s.GrainId,
            NextGrainId = s.NextGrainId,
            XPosition = s.XPosition,
            YPosition = s.YPosition,
        }).ToList();
        
        await _workflowRepository.UpdateAsync(workUnit);
        
        return string.Empty;
    }

    public async Task<string> CheckWorkflowWithGrainIdAsync(string workflowGrainId,
        List<WorkflowAgentDefinesDto> newWorkflowRelations)
    {
        if (workflowGrainId.IsNullOrEmpty())
        {
            return await CheckWorkflowAsync(newWorkflowRelations, new List<WorkflowAgentDefinesDto>());
        }

        var workflowRelation = await GetWorkflowUnitRelationsAsync(workflowGrainId);
        var notExistWorkUnit =
            newWorkflowRelations.Where(w => workflowRelation.Exists(e => e.GrainId == w.GrainId) == false).ToList();
        if (notExistWorkUnit.Count > 0)
        {
            foreach (var item in notExistWorkUnit)
            {
                var errorMsg = await CheckAgentCanJoinWorkflow(item.GrainId);
                if (errorMsg.IsNullOrEmpty() == false)
                {
                    return errorMsg;
                }
            }
        }

        return await CheckWorkflowAsync(newWorkflowRelations, workflowRelation);
    }

    private async Task<string> CheckWorkflowAsync(List<WorkflowAgentDefinesDto> workflowUnits,
        List<WorkflowAgentDefinesDto> existWorkflowRelation)
    {
        if (workflowUnits.Count == 0)
        {
            return "no work unit";
        }

        var groupCount = workflowUnits.GroupBy(f => f.GrainId);
        if (groupCount.Count() != workflowUnits.Count)
        {
            return "cannot input the same work unit";
        }

        if (ExistLoopAgents(workflowUnits) == true)
        {
            _logger.LogError($"[AgentService] exist cyclic agent:{JsonConvert.SerializeObject(workflowUnits)}");
            return "A workflow with cyclic workflows or non-existent nodes.";
        }

        foreach (var workUnit in workflowUnits)
        {
            if (existWorkflowRelation.Exists(e => e.GrainId == workUnit.GrainId) == true)
            {
                continue;
            }

            var errorMsg = await CheckAgentCanJoinWorkflow(workUnit.GrainId);
            if (errorMsg.IsNullOrEmpty() == false)
            {
                return errorMsg;
            }
        }

        return string.Empty;
    }

    private bool ExistLoopAgents(List<WorkflowAgentDefinesDto> workflowAgents)
    {
        var agentCount = workflowAgents.Count;
        var checkCycle = (WorkflowAgentDefinesDto workUnit) =>
        {
            var count = 0;
            var nextGrainId = workUnit.NextGrainId;
            while (true)
            {
                if (count >= agentCount)
                {
                    return true;
                }

                if (string.IsNullOrEmpty(nextGrainId))
                {
                    return false;
                }

                var nextWorkUnit = workflowAgents.FirstOrDefault(f => f.GrainId == nextGrainId);
                if (nextWorkUnit == null)
                {
                    return true;
                }

                if (nextWorkUnit.GrainId.Contains("/") == false)
                {
                    return true;
                }
                else
                {
                    if (Regex.IsMatch(nextWorkUnit.GrainId.Split("/")[1], @"^[a-zA-Z0-9]{32}$") == false)
                    {
                        return true;
                    }
                }

                count += 1;
                nextGrainId = nextWorkUnit.NextGrainId;
                if (nextGrainId == nextWorkUnit.GrainId)
                {
                    return true;
                }
            }
        };

        return workflowAgents.Any(workUnit => checkCycle(workUnit) == true);
    }

    private async Task<string> CheckAgentCanJoinWorkflow(string workUnitGrainId)
    {
        var grainId = GrainId.Parse(workUnitGrainId);
        var agent = _clusterClient.GetGrain<IGAgent>(grainId);

        var agentType = ReflectionUtil.GetTypeByFullName(grainId.Type.ToString()!);

        if (agentType == null ||
            ReflectionUtil.CheckInheritGenericClass(agentType, typeof(GroupMemberGAgentBase<,,,>)) == false)
        {
            return "Some agents are unable to orchestrate workflows";
        }

        var parent = await agent.GetParentAsync();
        if (parent != default)
        {
            return $"agent:{grainId.ToString()} cannot participate in the pipeline";
        }

        return string.Empty;
    }

    private IWorkflowCoordinatorGAgent GetWorkFlowGAgent(string workflowGrainId)
    {
        var grainId = GrainId.Parse(workflowGrainId);
        var workflowCoordinator = _clusterClient.GetGrain<IWorkflowCoordinatorGAgent>(grainId);

        return workflowCoordinator;
    }
}