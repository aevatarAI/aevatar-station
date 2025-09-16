using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Application.Grains.Agents.AI;
using Aevatar.Application.Grains.Agents.Configuration;
using Aevatar.Application.Grains.Agents.Creator;
using Aevatar.Application.Grains.Subscription;
using Aevatar.Common;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS;
using Aevatar.Exceptions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.Options;
using Aevatar.Query;
using Aevatar.Schema;
using Aevatar.Provider;
using Aevatar.Station.Feature.CreatorGAgent;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceProvider _serviceProvider;
    private readonly IDocumentLinkService _documentLinkService;

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
        IServiceProvider serviceProvider,
        IDocumentLinkService documentLinkService)
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
        _serviceProvider = serviceProvider;
        _documentLinkService = documentLinkService;
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

                    try
                    {
                        paramDto.PropertyJsonSchema =
                            await EnhanceSchemaWithDefaults(kvp.Value.InitializationData.DtoType);
                        
                        if (string.IsNullOrWhiteSpace(paramDto.PropertyJsonSchema))
                        {
                            _logger.LogError("PropertyJsonSchema is null or empty for agent {AgentType} with DtoType {DtoType}", 
                                kvp.Key, kvp.Value.InitializationData.DtoType.Name);
                            paramDto.PropertyJsonSchema = "{}"; // Fallback to empty JSON object
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to generate PropertyJsonSchema for agent {AgentType} with DtoType {DtoType}", 
                            kvp.Key, kvp.Value.InitializationData.DtoType.Name);
                        paramDto.PropertyJsonSchema = "{}"; // Fallback to empty JSON object
                    }

                    // Get default values for backward compatibility
                    paramDto.DefaultValues =
                        GetConfigurationDefaultValues(kvp.Value.InitializationData.DtoType);
                }
                else
                {
                    _logger.LogWarning("InitializationData is null for agent {AgentType}", kvp.Key);
                    paramDto.PropertyJsonSchema = "{}"; // Fallback for agents without initialization data
                }
            }
            else
            {
                _logger.LogWarning("Agent metadata is null for agent type {AgentType}", kvp.Key);
                paramDto.PropertyJsonSchema = "{}"; // Fallback for agents without metadata
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
            var context = await CreateSchemaContextAsync();
            
            // Generate base schema with context
            var schemaResult = _schemaProvider.GetTypeSchema(configurationType, context);
            if (schemaResult == null)
            {
                _logger.LogError("SchemaProvider returned null schema for type {TypeName}", configurationType.Name);
                return "{}";
            }
            
            var baseSchema = schemaResult.ToJson();
            if (string.IsNullOrWhiteSpace(baseSchema))
            {
                _logger.LogError("Schema ToJson() returned empty result for type {TypeName}", configurationType.Name);
                return "{}";
            }
            
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
                        
                        // Process DefaultValuesAttribute 
                        ProcessDefaultValuesAttribute(property, propertySchema, defaultValue);
                        
                        // Update the properties dictionary with enhanced schema
                        schemaProperties[propertyName] = propertySchema;
                        
                        _logger.LogDebug("Enhanced schema property {PropertyName} with default: {DefaultValue}",
                            property.Name, defaultValue);
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
            
            var fallbackSchema = _schemaProvider.GetTypeSchema(configurationType);
            if (fallbackSchema == null)
            {
                _logger.LogError("SchemaProvider returned null schema for type {TypeName}", configurationType.Name);
                return "{}"; // Return empty JSON object as fallback
            }
            
            var jsonResult = fallbackSchema.ToJson();
            if (string.IsNullOrWhiteSpace(jsonResult))
            {
                _logger.LogError("Schema ToJson() returned empty result for type {TypeName}", configurationType.Name);
                return "{}"; // Return empty JSON object as fallback
            }
            
            return jsonResult;
        }
    }

    /// <summary>
    /// Gets default values of configuration class properties (backward compatibility)
    /// </summary>
    private Dictionary<string, object?> GetConfigurationDefaultValues(Type configurationType)
    {
        var defaultValues = new Dictionary<string, object?>();

        var instance = CreateTypeInstance(configurationType);
        if (instance == null)
        {
            return defaultValues;
        }

        ExtractPropertyDefaultValues(instance, configurationType, defaultValues);
        return defaultValues;
    }

    /// <summary>
    /// Create an instance of the specified type with error handling
    /// </summary>
    private object CreateTypeInstance(Type configurationType)
    {
        try
        {
            return Activator.CreateInstance(configurationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create instance of {TypeName} for default values", configurationType.Name);
            return null;
        }
    }

    /// <summary>
    /// Extract default values from all properties of an instance
    /// </summary>
    private void ExtractPropertyDefaultValues(object instance, Type configurationType, Dictionary<string, object?> defaultValues)
    {
        var properties = configurationType.GetProperties(BindingFlags.Public | 
            BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (var property in properties)
        {
            var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
            var propertyValue = GetPropertyValueSafely(property, instance);
            defaultValues[propertyName] = propertyValue;
        }
    }

    /// <summary>
    /// Get property value (simplified - property access rarely fails on valid instances)
    /// </summary>
    private object GetPropertyValueSafely(PropertyInfo property, object instance)
    {
        return property.GetValue(instance);
    }

    /// <summary>
    /// Process DefaultValuesAttribute for a property and update its schema
    /// </summary>
    private void ProcessDefaultValuesAttribute(PropertyInfo property, Dictionary<string, object> propertySchema, object defaultValue)
    {
        var defaultValuesAttribute = property.GetCustomAttribute<DefaultValuesAttribute>();
        if (!ShouldProcessDefaultValuesAttribute(defaultValuesAttribute))
        {
            return;
        }

        // Add enum values
        propertySchema["enum"] = defaultValuesAttribute.Values;
        
        // Process descriptions
        ProcessAttributeDescriptions(property, propertySchema, defaultValuesAttribute);
        
        // Validate default value against enum
        ValidateDefaultValueAgainstEnum(property, defaultValue, defaultValuesAttribute);
    }

    /// <summary>
    /// Check if DefaultValuesAttribute should be processed
    /// </summary>
    private static bool ShouldProcessDefaultValuesAttribute(DefaultValuesAttribute attribute)
    {
        return attribute?.Values != null && attribute.Values.Length > 1;
    }

    /// <summary>
    /// Process descriptions from DefaultValuesAttribute
    /// </summary>
    private void ProcessAttributeDescriptions(PropertyInfo property, Dictionary<string, object> propertySchema, DefaultValuesAttribute attribute)
    {
        if (!HasValidDescriptions(attribute))
        {
            return;
        }

        var hasNonEmptyDescriptions = attribute.Descriptions.Any(d => !string.IsNullOrEmpty(d));
        if (hasNonEmptyDescriptions)
        {
            propertySchema["x-descriptions"] = attribute.Descriptions;
            _logger.LogDebug("Added x-descriptions for property {PropertyName}: {Descriptions}",
                property.Name, string.Join(", ", attribute.Descriptions));
        }
    }

    /// <summary>
    /// Check if attribute has valid descriptions
    /// </summary>
    private static bool HasValidDescriptions(DefaultValuesAttribute attribute)
    {
        return attribute.Descriptions != null && 
               attribute.Descriptions.Length == attribute.Values.Length;
    }

    /// <summary>
    /// Validate that default value matches the first enum value
    /// </summary>
    private void ValidateDefaultValueAgainstEnum(PropertyInfo property, object defaultValue, DefaultValuesAttribute attribute)
    {
        if (!Equals(defaultValue, attribute.Values[0]))
        {
            _logger.LogWarning("Property {PropertyName} default ({Default}) doesn't match first enum value ({EnumValue})",
                property.Name, defaultValue, attribute.Values[0]);
        }
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
    private async Task<DynamicDropDownContext> CreateSchemaContextAsync()
    {
        try
        {
            _logger.LogDebug("[AgentService] Starting schema context creation using plugin architecture");
            
            // 创建线程安全的并发字典用于多个processor并发写入
            var concurrentData = new ConcurrentDictionary<string, object>();
            var configurationProviders = _serviceProvider.GetServices<IDynamicConfigurationProvider>().ToList();
            
            if (!configurationProviders.Any())
            {
                _logger.LogError("[AgentService] No configuration providers found, plugin architecture not properly configured");
                throw new InvalidOperationException("Configuration provider plugin architecture not properly configured - no IDynamicConfigurationProvider implementations found");
            }

            _logger.LogInformation("[AgentService] Found {ProviderCount} configuration providers", configurationProviders.Count);

            // 为每个配置提供者执行处理逻辑
            var processingTasks = configurationProviders.Select(async provider =>
            {
                try
                {
                    _logger.LogDebug("[AgentService] Processing with provider: {ProviderType}", 
                        provider.GetType().Name);

                    // 直接调用provider的处理方法，传递concurrentData和clusterClient
                    await provider.ProcessSchemaAsync(concurrentData, _clusterClient);
                    
                    _logger.LogInformation("[AgentService] Successfully processed configuration with {ProviderType}", 
                        provider.GetType().Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AgentService] Failed to process with provider: {ProviderType}", 
                        provider.GetType().Name);
                }
            });

            // 等待所有配置提供者完成
            await Task.WhenAll(processingTasks);

            return new DynamicDropDownContext { AdditionalData = new Dictionary<string, object>(concurrentData) };;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AgentService] Failed to create schema context using plugin architecture");
            throw;
        }
    }

    /// <summary>
    /// Creates schema processing context by scanning configuration type for documentation links
    /// </summary>
    /// <param name="configurationType">The configuration type to scan</param>
    /// <returns>Schema processing context with invalid URLs</returns>
    private async Task<SchemaProcessingContext> CreateSchemaContextAsync(Type configurationType)
    {
        var context = new SchemaProcessingContext();
        var properties = configurationType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            var docLinkAttributes = property.GetCustomAttributes<Aevatar.GAgents.Basic.Common.DocumentationLinkAttribute>(true);
            
            foreach (var attribute in docLinkAttributes)
            {
                var url = attribute.DocumentationUrl;
                if (string.IsNullOrWhiteSpace(url)) continue;

                var isValid = await _documentLinkService.GetDocumentLinkStatusAsync(url);
                if (!isValid)
                {
                    context.InvalidUrls.Add(url);
                }
            }
        }

        return context;
    }
}