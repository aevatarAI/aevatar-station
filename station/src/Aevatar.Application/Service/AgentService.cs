using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Application.Grains.Agents.Creator;
using Aevatar.Application.Grains.Subscription;
using Aevatar.Common;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS;
using Aevatar.Exceptions;
using Aevatar.GAgents.AI.Common;
using Aevatar.Options;
using Aevatar.Query;
using Aevatar.Schema;
using Aevatar.Station.Feature.CreatorGAgent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NJsonSchema;
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
    private readonly GrainTypeResolver _grainTypeResolver;
    private readonly ISchemaProvider _schemaProvider;
    private readonly IIndexingService _indexingService;
    private readonly IOptionsMonitor<SystemLLMMetaInfoOptions> _systemLLMConfigOptions;

    public AgentService(
        IClusterClient clusterClient,
        ILogger<AgentService> logger,
        IGAgentFactory gAgentFactory,
        IGAgentManager gAgentManager,
        IUserAppService userAppService,
        IOptionsMonitor<AgentOptions> agentOptions,
        GrainTypeResolver grainTypeResolver,
        ISchemaProvider schemaProvider,
        IIndexingService indexingService,
        IOptionsMonitor<SystemLLMMetaInfoOptions> systemLLMConfigOptions)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _gAgentFactory = gAgentFactory;
        _gAgentManager = gAgentManager;
        _userAppService = userAppService;
        _agentOptions = agentOptions;
        _grainTypeResolver = grainTypeResolver;
        _schemaProvider = schemaProvider;
        _indexingService = indexingService;
        _systemLLMConfigOptions = systemLLMConfigOptions;
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
                Description = kvp.Value?.Description
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
                        await EnhanceSchemaWithDefaults(kvp.Value.InitializationData.DtoType);

                    // Get default values for backward compatibility
                    paramDto.DefaultValues =
                        GetConfigurationDefaultValues(kvp.Value.InitializationData.DtoType);

                    // Check if agent has SystemLLMConfig and add it
                    paramDto.SystemLLMConfigs = GetSystemLLMConfigsForAgent(kvp.Value.InitializationData);
                }
            }

            resp.Add(paramDto);
        }

        return resp;
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
            resp.PropertyJsonSchema = await EnhanceSchemaWithDefaults(configuration.DtoType);
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
            response = await _indexingService.QueryWithLuceneAsync(new LuceneQueryDto()
            {
                QueryString = queryString,
                StateName = nameof(CreatorGAgentState),
                PageSize = queryDto.PageSize,
                PageIndex = queryDto.PageIndex
            });
        }
        catch (UserFriendlyException e)
        {
            if (e.Code == "index_not_found_exception") return result;

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

    public async Task<AgentDto> UpdateAgentAsync(Guid guid, UpdateAgentInputDto dto)
    {
        var creatorAgent = _clusterClient.GetGrain<ICreatorGAgent>(guid);
        var agentState = await creatorAgent.GetAgentAsync();

        EnsureUserAuthorized(agentState.UserId);

        var businessAgent = await _gAgentFactory.GetGAgentAsync(agentState.BusinessAgentGrainId);

        string properties = null;
        if (!dto.Properties.IsNullOrEmpty())
        {
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };
            var configuration = await GetAgentConfigurationAsync(businessAgent);
            var updatedParam = JsonConvert.SerializeObject(dto.Properties);
            if (configuration != null && !updatedParam.IsNullOrEmpty())
            {
                var config = SetupConfigurationData(configuration, updatedParam);
                await businessAgent.ConfigAsync(config);
                properties = JsonConvert.SerializeObject(config, jsonSerializerSettings);
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
            resp.PropertyJsonSchema = await EnhanceSchemaWithDefaults(configuration.DtoType);
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
            if (subAgentGrainIds.Contains(grainId)) continue;

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
            else _logger.LogInformation("No events handled by agent {agentId}", businessAgent.GetGrainId().GetGuidKey());
        }

        await creatorAgent.UpdateAvailableEventsAsync(allEventsHandled);

        var resp = new SubAgentDto
        {
            SubAgents = subAgentGuids
        };

        return resp;
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

            if (removeSubAgentDto.RemovedSubAgents.Contains(subAgentGuid)) await agent.UnregisterAsync(subAgent);
            else
            {
                remainSubAgentGuids.Add(subAgentGuid);
                var eventsHandledByAgent = await subAgent.GetAllSubscribedEventsAsync();
                if (eventsHandledByAgent == null) continue;
                var eventsToAdd = eventsHandledByAgent.Except(allEventsHandled).ToList();
                allEventsHandled.AddRange(eventsToAdd);
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
            if (subAgentGrainIds.Any()) await agent.UnregisterAsync(creatorAgent);

            await creatorAgent.DeleteAgentAsync();
        }
        else
        {
            _logger.LogInformation("Agent {agentId} has parent, please remove from it first.", guid);
            throw new UserFriendlyException("Agent has parent, please remove from it first.");
        }
    }

    // Private methods moved to the end of the class

    /// <summary>
    /// Extracts configuration properties from a configuration type using reflection
    /// </summary>
    private Configuration? ExtractConfigurationProperties(Type? configurationType)
    {
        if (configurationType == null || configurationType.IsAbstract)
        {
            return null;
        }

        var properties = configurationType.GetProperties(BindingFlags.Public | BindingFlags.Instance |
                                                         BindingFlags.DeclaredOnly);

        var configuration = new Configuration { DtoType = configurationType };

        var propertyData = properties
            .Select(property => new PropertyData() { Name = property.Name, Type = property.PropertyType }).ToList();

        configuration.Properties = propertyData;
        return configuration;
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

                if (grainType == null) continue;

                var agentTypeData = new AgentTypeData { FullName = agentType.FullName, };
                var grainId = GrainId.Create(grainType,
                    GuidUtil.GuidToGrainKey(
                        GuidUtil.StringToGuid("AgentDefaultId"))); // make sure only one agent instance for each type
                var agent = await _gAgentFactory.GetGAgentAsync(grainId);
                var description = await agent.GetDescriptionAsync();
                agentTypeData.Description = description;

                var initializationData = await GetAgentConfigurationAsync(agent);

                agentTypeData.InitializationData = initializationData;
                dict[grainType] = agentTypeData;
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
        => ExtractConfigurationProperties(await agent.GetConfigurationTypeAsync());

    /// <summary>
    /// Enhances JSON Schema with default values and enum options from DefaultValuesAttribute
    /// </summary>
    private async Task<string> EnhanceSchemaWithDefaults(Type configurationType)
    {
        try
        {
            // Generate base schema
            var baseSchema = _schemaProvider.GetTypeSchema(configurationType).ToJson();
            var schemaDoc = JsonDocument.Parse(baseSchema);
            
            // Create instance to get default values
            var instance = Activator.CreateInstance(configurationType);
            if (instance == null)
            {
                return baseSchema;
            }
            
            var properties = configurationType.GetProperties(BindingFlags.Public | 
                BindingFlags.Instance | BindingFlags.DeclaredOnly);
            
            // Parse schema as mutable JSON
            using var jsonDoc = JsonDocument.Parse(baseSchema);
            var schemaObject = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(baseSchema);
            
            if (schemaObject != null && 
                schemaObject.TryGetValue("properties", out var propertiesObj) &&
                propertiesObj is JsonElement propertiesElement)
            {
                var schemaProperties = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(propertiesElement.GetRawText());
                
                foreach (var property in properties)
                {
                    var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
                    
                    if (schemaProperties != null && schemaProperties.TryGetValue(propertyName, out var propertySchemaObj))
                    {
                        try
                        {
                            Dictionary<string, object> propertySchema;
                            
                            // Handle JsonElement objects (preserve all properties including x-enumNames)
                            if (propertySchemaObj is JsonElement jsonElement)
                            {
                                propertySchema = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetRawText()) ?? new Dictionary<string, object>();
                            }
                            else if (propertySchemaObj is Dictionary<string, object> dict)
                            {
                                propertySchema = new Dictionary<string, object>(dict);
                            }
                            else
                            {
                                propertySchema = new Dictionary<string, object>();
                            }
                            
                            // Get default value
                            var defaultValue = property.GetValue(instance);
                            if (defaultValue != null)
                            {
                                propertySchema["default"] = defaultValue;
                            }
                            
                            // Check for DefaultValuesAttribute
                            var defaultValuesAttribute = property.GetCustomAttribute<DefaultValuesAttribute>();
                            if (defaultValuesAttribute?.Values != null && defaultValuesAttribute.Values.Length > 1)
                            {
                                // Only create enum if there are multiple values (single values don't make sense for enums)
                                propertySchema["enum"] = defaultValuesAttribute.Values;
                                
                                // Log warning if default doesn't match first enum value
                                if (!Equals(defaultValue, defaultValuesAttribute.Values[0]))
                                {
                                    _logger.LogWarning("Property {PropertyName} default ({Default}) doesn't match first enum value ({EnumValue})",
                                        property.Name, defaultValue, defaultValuesAttribute.Values[0]);
                                }
                            }
                            
                            // Update the properties dictionary with enhanced schema
                            schemaProperties[propertyName] = propertySchema;
                            
                            _logger.LogDebug("Enhanced schema property {PropertyName} with default: {DefaultValue}",
                                property.Name, defaultValue);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to enhance schema for property {PropertyName}", property.Name);
                        }
                    }
                }
                
                // Update the schema object with enhanced properties
                schemaObject["properties"] = schemaProperties;
            }
            
            return System.Text.Json.JsonSerializer.Serialize(schemaObject, new JsonSerializerOptions { WriteIndented = false });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enhance schema for type {TypeName}, returning base schema", configurationType.Name);
            return _schemaProvider.GetTypeSchema(configurationType).ToJson();
        }
    }

    /// <summary>
    /// Gets default values of configuration class properties (backward compatibility)
    /// </summary>
    private Dictionary<string, object?> GetConfigurationDefaultValues(Type configurationType)
    {
        var defaultValues = new Dictionary<string, object?>();

        try
        {
            // Create configuration instance to get default values
            var instance = Activator.CreateInstance(configurationType);
            if (instance != null)
            {
                var properties = configurationType.GetProperties(BindingFlags.Public | 
                    BindingFlags.Instance | BindingFlags.DeclaredOnly);

                foreach (var property in properties)
                {
                    var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
                    try
                    {
                        var defaultValue = property.GetValue(instance);
                        defaultValues[propertyName] = defaultValue;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get default value for property {PropertyName} on type {TypeName}", 
                            property.Name, configurationType.Name);
                        defaultValues[propertyName] = null;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create instance of {TypeName} for default values", configurationType.Name);
        }

        return defaultValues;
    }

    private ConfigurationBase SetupConfigurationData(Configuration configuration,
        string propertiesString)
    {
        var actualDto = Activator.CreateInstance(configuration.DtoType);

        var config = (ConfigurationBase)actualDto!;
        var schema = _schemaProvider.GetTypeSchema(config.GetType());
        var validateResponse = schema.Validate(propertiesString, new JsonSchemaValidatorSettings { PropertyStringComparer = StringComparer.CurrentCultureIgnoreCase });
        if (validateResponse.Count > 0) throw new UserFriendlyException("[AgentService][SetupInitializedConfig] Setup configuration data error");

        config = JsonConvert.DeserializeObject(propertiesString, configuration.DtoType) as ConfigurationBase;
        if (config == null) throw new UserFriendlyException("[AgentService][SetupInitializedConfig] config convert error");

        return config;
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

    private void EnsureUserAuthorized(Guid userId)
    {
        var currentUserId = _userAppService.GetCurrentUserId();
        if (currentUserId != userId)
        {
            _logger.LogInformation("User {userId} is not allowed.", currentUserId);
            throw new UserFriendlyException("You are not the owner of this agent");
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
    private DynamicDropDownContext CreateSchemaContextAsync() => new () { AIModelConfigs = _systemLLMConfigOptions.CurrentValue.SystemLLMConfigs };

    /// <summary>
    /// Gets SystemLLM configurations for agent based on SystemLLM or modelId properties
    /// </summary>
    private List<SystemLLMConfig>? GetSystemLLMConfigsForAgent(Configuration initializationData)
    {
        if (initializationData?.DtoType == null)
        {
            return null;
        }

        // Check for SystemLLM or modelId properties (case insensitive)
        var properties = initializationData.DtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var hasRelevantProperty = properties.Any(p => 
            string.Equals(p.Name, "SystemLLM", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.Name, "modelId", StringComparison.OrdinalIgnoreCase));

        if (hasRelevantProperty)
        {
            return _systemLLMConfigOptions.CurrentValue.SystemLLMConfigs;
        }

        return null;
    }
}