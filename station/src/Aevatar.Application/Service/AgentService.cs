using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Application.Grains.Agents.Creator;
using Aevatar.Application.Grains.Subscription;
using Aevatar.Common;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS;
using Aevatar.CQRS.Provider;
using Aevatar.Exceptions;
using Aevatar.Options;
using Aevatar.Query;
using Aevatar.Schema;
using Aevatar.Station.Feature.CreatorGAgent;
using Aevatar.GAgents.AIGAgent.Agent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Validation;
using Orleans;
using Orleans.Metadata;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using ICreatorGAgent = Aevatar.Application.Grains.Agents.Creator.ICreatorGAgent;

namespace Aevatar.Service;

[RemoteService(IsEnabled = false)]
public class AgentService : ApplicationService, IAgentService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<AgentService> _logger;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IGAgentManager _gAgentManager;
    private readonly IUserAppService _userAppService;
    private readonly IOptionsMonitor<AgentOptions> _agentOptions;
    private readonly IOptionsMonitor<AgentDefaultValuesOptions> _agentDefaultValuesOptions;
    private readonly GrainTypeResolver _grainTypeResolver;
    private readonly ISchemaProvider _schemaProvider;
    private readonly IIndexingService _indexingService;

    public AgentService(
        IClusterClient clusterClient,
        ILogger<AgentService> logger,
        IGAgentFactory gAgentFactory,
        IGAgentManager gAgentManager,
        IUserAppService userAppService,
        IOptionsMonitor<AgentOptions> agentOptions,
        IOptionsMonitor<AgentDefaultValuesOptions> agentDefaultValuesOptions,
        GrainTypeResolver grainTypeResolver,
        ISchemaProvider schemaProvider,
        IIndexingService indexingService)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _gAgentFactory = gAgentFactory;
        _gAgentManager = gAgentManager;
        _userAppService = userAppService;
        _agentOptions = agentOptions;
        _agentDefaultValuesOptions = agentDefaultValuesOptions;
        _grainTypeResolver = grainTypeResolver;
        _schemaProvider = schemaProvider;
        _indexingService = indexingService;
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
            try
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
            catch (Exception ex)
            {
                // Log and skip problematic grain types (e.g., generic types with invalid arity)
                _logger.LogWarning(ex, "Failed to process agent type {AgentType}: {ErrorMessage}", 
                    agentType.FullName, ex.Message);
                continue;
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

                    // Get default values
                    paramDto.DefaultValues =
                        await GetConfigurationDefaultValuesAsync(kvp.Value.InitializationData.DtoType);
                }
            }

            resp.Add(paramDto);
        }

        return resp;
    }

    /// <summary>
    /// Gets default values of configuration class properties (returns in list format to prepare for default value list functionality)
    /// </summary>
    private async Task<Dictionary<string, object?>> GetConfigurationDefaultValuesAsync(Type configurationType)
    {
        var defaultValues = new Dictionary<string, object?>();

        try
        {
            // Create configuration instance to get default values
            var instance = Activator.CreateInstance(configurationType);
            if (instance != null)
            {
                var properties =
                    configurationType.GetProperties(BindingFlags.Public | BindingFlags.Instance |
                                                    BindingFlags.DeclaredOnly);

                foreach (var property in properties)
                {

                    var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
                    try
                    {
                        var value = property.GetValue(instance);

                        // Special handling for AIGAgent systemLLM property
                        if (configurationType != null && typeof(IAIGAgent).IsAssignableFrom(configurationType) && 
                            string.Equals(property.Name, "systemLLM", StringComparison.OrdinalIgnoreCase))
                        {
                            var systemLLMList = _agentDefaultValuesOptions.CurrentValue.SystemLLMConfigs;
                            defaultValues[propertyName] = systemLLMList;
                            _logger.LogInformation("AIGAgent detected, setting systemLLM default values: {SystemLLMList}", 
                                string.Join(", ", systemLLMList));
                        }
                        else
                        {
                            // Convert all default values to list format
                            if (value == null)
                            {
                                // Convert null values to empty list
                                defaultValues[propertyName] = new List<object>();
                            }
                            else
                            {
                                // Convert non-null values to single-item list
                                defaultValues[propertyName] = new List<object> { value };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to get default value for property {PropertyName} in {ConfigType}",
                            property.Name, configurationType.Name);
                        // Return empty list for exception cases
                        defaultValues[propertyName] = new List<object>();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create instance of configuration type {ConfigType}",
                configurationType.Name);
        }

        return defaultValues;
    }

    private ConfigurationBase SetupConfigurationData(Configuration configuration,
        string propertiesString)
    {
        var actualDto = Activator.CreateInstance(configuration.DtoType);

        var config = (ConfigurationBase)actualDto!;
        var schema = _schemaProvider.GetTypeSchema(config.GetType());
        var validateResponse = schema.Validate(propertiesString, new JsonSchemaValidatorSettings
        {
            PropertyStringComparer = StringComparer.CurrentCultureIgnoreCase
        });
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
            Name = dto.Name
        };

        var initializationParam =
            dto.Properties.IsNullOrEmpty() ? string.Empty : JsonConvert.SerializeObject(dto.Properties);
        var initialization = await InitializeBusinessAgent(guid, dto.AgentType, initializationParam);
        var businessAgent = initialization.Item1;

        var creatorAgent = _clusterClient.GetGrain<ICreatorGAgent>(guid);
        agentData.BusinessAgentGrainId = businessAgent.GetGrainId();
        agentData.Properties = JsonConvert.SerializeObject(initialization.Item2, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        });

        await creatorAgent.CreateAgentAsync(agentData);

        var resp = new AgentDto
        {
            Id = guid,
            AgentType = dto.AgentType,
            Name = dto.Name,
            GrainId = businessAgent.GetGrainId(),
            Properties = agentData.Properties.IsNullOrWhiteSpace()
                ? null
                : JsonConvert.DeserializeObject<Dictionary<string, object>>(agentData.Properties),
            AgentGuid = businessAgent.GetPrimaryKey(),
            BusinessAgentGrainId = businessAgent.GetGrainId().ToString()
        };
        
        var configuration = await GetAgentConfigurationAsync(businessAgent);
        if (configuration != null)
        {
            resp.PropertyJsonSchema = _schemaProvider.GetTypeSchema(configuration.DtoType).ToJson();
        }

        return resp;
    }

    public async Task<List<AgentInstanceDto>> GetAllAgentInstances(GetAllAgentInstancesQueryDto queryDto)
    {
        var result = new List<AgentInstanceDto>();
        var currentUserId = _userAppService.GetCurrentUserId();
        
        // Build query conditions
        var queryString = "userId.keyword:" + currentUserId;
        
        // Add agentType fuzzy query condition
        if (!string.IsNullOrWhiteSpace(queryDto.AgentType))
        {
            // Use fuzzy query with ~ operator for better matching
            queryString += " AND agentType:(" + queryDto.AgentType + "~ OR " + queryDto.AgentType + "*)";
        }
        
        PagedResultDto<Dictionary<string, object>> response;
        try
        {
            response =
                await _indexingService.QueryWithLuceneAsync(new LuceneQueryDto()
                {
                    QueryString = queryString,
                    StateName = nameof(CreatorGAgentState),
                    PageSize = queryDto.PageSize,
                    PageIndex = queryDto.PageIndex
                });
        }
        catch (UserFriendlyException e)
        {
            if (e.Code == "index_not_found_exception")
            {
                return result;
            }

            throw;
        }
        if (response.TotalCount == 0)
        {
            return result;
        }

        result.AddRange(response.Items.Select(state => new AgentInstanceDto()
        {
            Id = (string)state["id"],
            Name = (string)state["name"],
            Properties = state.TryGetValue("properties", out var properties)
                ? JsonConvert.DeserializeObject<Dictionary<string, object>>((string)properties)
                : null,
            AgentType = (string)state["agentType"],
            BusinessAgentGrainId =
                state.TryGetValue("formattedBusinessAgentGrainId", out var value) ? (string)value : null
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

    private async Task<Tuple<IGAgent, ConfigurationBase>> InitializeBusinessAgent(Guid primaryKey, string agentType,
        string agentProperties)
    {
        var grainId = GrainId.Create(agentType, GuidUtil.GuidToGrainKey(primaryKey));
        var businessAgent = await _gAgentFactory.GetGAgentAsync(grainId);

        var initializationData = await GetAgentConfigurationAsync(businessAgent);
        if (initializationData != null && !agentProperties.IsNullOrEmpty())
        {
            var config = SetupConfigurationData(initializationData, agentProperties);
            await businessAgent.ConfigAsync(config);
            
            return new Tuple<IGAgent, ConfigurationBase>(businessAgent, config);
        }

        return new Tuple<IGAgent, ConfigurationBase>(businessAgent, null);
    }

    public async Task<AgentDto> UpdateAgentAsync(Guid guid, UpdateAgentInputDto dto)
    {
        var creatorAgent = _clusterClient.GetGrain<ICreatorGAgent>(guid);
        var agentState = await creatorAgent.GetAgentAsync();

        EnsureUserAuthorized(agentState.UserId);

        var businessAgent = await _gAgentFactory.GetGAgentAsync(agentState.BusinessAgentGrainId);

        string properties = null;
        if (!dto.Properties.IsNullOrEmpty())
        {
            var updatedParam = JsonConvert.SerializeObject(dto.Properties);
            var configuration = await GetAgentConfigurationAsync(businessAgent);
            if (configuration != null && !updatedParam.IsNullOrEmpty())
            {
                var config = SetupConfigurationData(configuration, updatedParam);
                await businessAgent.ConfigAsync(config);
                properties = JsonConvert.SerializeObject(config, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore
                });
            }
            else
            {
                _logger.LogError("no properties to be updated, id: {id}", guid);
            }
        }
        
        await creatorAgent.UpdateAgentAsync(new UpdateAgentInput
        {
            Name = dto.Name,
            Properties = properties
        });

        var resp = new AgentDto
        {
            Id = guid,
            AgentType = agentState.AgentType,
            Name = dto.Name,
            GrainId = agentState.BusinessAgentGrainId,
            Properties = properties.IsNullOrWhiteSpace()
                ? null
                : JsonConvert.DeserializeObject<Dictionary<string, object>>(properties),
            BusinessAgentGrainId = agentState.BusinessAgentGrainId.ToString()
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
            Properties = string.IsNullOrWhiteSpace(agentState.Properties)
                ? null
                : JsonConvert.DeserializeObject<Dictionary<string, object>>(agentState.Properties),
            AgentGuid = agentState.BusinessAgentGrainId.GetGuidKey(),
            BusinessAgentGrainId = agentState.BusinessAgentGrainId.ToString()
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

        var agent = await _gAgentFactory.GetGAgentAsync<IExtGAgent>(agentState.BusinessAgentGrainId);

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
        await agent.RegisterAsync(creatorAgent);
        var parentEventData = await agent.GetAllSubscribedEventsAsync();
        if (parentEventData != null)
        {
            allEventsHandled.AddRange(parentEventData);
        }

        // register sub agent and add their events to parent agent
        var subAgentGuids = subAgentGrainIds.Select(x => x.GetGuidKey()).ToList();
        var businessAgents = new List<IGAgent>();
        foreach (var grainId in newSubAgentGrainIds)
        {
            if (subAgentGrainIds.Contains(grainId))
            {
                continue;
            }

            var businessAgent = await _gAgentFactory.GetGAgentAsync(grainId);
            businessAgents.Add(businessAgent);
            subAgentGuids.Add(grainId.GetGuidKey());
        }
        
        await agent.RegisterManyAsync(businessAgents);
        
        foreach (var businessAgent in businessAgents)
        {
            var eventsHandledByAgent = await businessAgent.GetAllSubscribedEventsAsync();
            if (eventsHandledByAgent != null)
            {
                _logger.LogInformation("all events for agent {agentId}, events: {events}",
                    businessAgent.GetGrainId().GetGuidKey(), JsonConvert.SerializeObject(eventsHandledByAgent));
                var eventsToAdd = eventsHandledByAgent.Except(allEventsHandled).ToList();
                _logger.LogInformation("Adding events for agent {agentId}, events: {events}",
                    businessAgent.GetGrainId().GetGuidKey(), JsonConvert.SerializeObject(eventsToAdd));
                allEventsHandled.AddRange(eventsToAdd);
            }
            else
            {
                _logger.LogInformation("No events handled by agent {agentId}", businessAgent.GetGrainId().GetGuidKey());
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