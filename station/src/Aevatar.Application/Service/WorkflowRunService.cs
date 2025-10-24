using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.AgentValidation;
using Aevatar.Application.Grains.Agents.Creator;
using Aevatar.Common;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Core;
// using Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView.Dto; // Removed due to type conflicts with Workflow.Core.Configs
using Aevatar.GAgents.Workflow.Core;
using Aevatar.GAgents.Workflow.Core.Configs;
using Aevatar.Schema;
using Aevatar.Subscription;
using Aevatar.WorkflowRun;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NJsonSchema.Validation;
using Orleans;
using Orleans.Metadata;
using Orleans.Runtime;
using Volo.Abp.Application.Services;
using Volo.Abp;

namespace Aevatar.Service;


public interface IWorkflowRunService
{
    Task<WorkflowRunResultDto> RunWorkflowAsync(WorkflowRunRequestDto request);
    Task<List<AgentTypeDto>> GetAllWorkflowAgents();
}

[RemoteService(IsEnabled = false)]
public class WorkflowRunService : ApplicationService, IWorkflowRunService
{
    private readonly IClusterClient _clusterClient;
    private readonly IWorkflowViewService _workflowViewService;
    private readonly ISubscriptionAppService _subscriptionAppService;
    private readonly IAgentService _agentService;
    private readonly IGAgentFactory<IBusinessAgentBase> _gAgentFactory;
    private readonly ISchemaProvider _schemaProvider;
    private readonly ILogger<WorkflowRunService> _logger;
    private readonly IGAgentManager _gAgentManager;
    private readonly GrainTypeResolver _grainTypeResolver;

    public WorkflowRunService(
        IWorkflowViewService workflowViewService,
        ISubscriptionAppService subscriptionAppService,
        IAgentService agentService,
        IGAgentFactory<IBusinessAgentBase> gAgentFactory,
        ISchemaProvider schemaProvider,
        ILogger<WorkflowRunService> logger,
        IClusterClient clusterClient,
        IGAgentManager gAgentManager,
        GrainTypeResolver grainTypeResolver)
    {
        _workflowViewService = workflowViewService;
        _subscriptionAppService = subscriptionAppService;
        _agentService = agentService;
        _gAgentFactory = gAgentFactory;
        _schemaProvider = schemaProvider;
        _logger = logger;
        _clusterClient = clusterClient;
        _gAgentManager = gAgentManager;
        _grainTypeResolver = grainTypeResolver;
    }

    public async Task<WorkflowRunResultDto> RunWorkflowAsync(WorkflowRunRequestDto request)
    {
        _logger.LogInformation("Starting workflow run for ViewAgentId: {ViewAgentId}", request.ViewAgentId);

        // Step 1: Validate workflow configuration
        await ValidateWorkflowConfigurationAsync(request.ViewAgentId);

        // Step 2: Publish workflow
        var publishedAgent = await PublishWorkflowAsync(request.ViewAgentId);
        
        // Step 3: Execute workflow through IWorkflowViewGAgentPlus
        Guid executionEventId;
        try
        {
            executionEventId = await ExecuteWorkflowAsync(request.ViewAgentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute workflow for ViewAgentId: {ViewAgentId}", request.ViewAgentId);
            return new WorkflowRunResultDto
            {
                IsSuccess = false,
                WorkflowId = request.ViewAgentId,
                Message = $"Workflow execution failed: {ex.Message}",
                PublishedAgent = publishedAgent.Item1
            };
        }

        // All steps completed successfully
        _logger.LogInformation("Workflow run completed successfully for ViewAgentId: {ViewAgentId}, ExecutionEventId: {ExecutionEventId}",
            request.ViewAgentId, executionEventId);

        return new WorkflowRunResultDto
        {
            IsSuccess = true,
            WorkflowId = publishedAgent.Item2.WorkflowCoordinatorGAgentId,
            Message = $"Workflow executed successfully. Execution event ID: {executionEventId}",
            PublishedAgent = publishedAgent.Item1
        };
    }

    /// <summary>
    /// Get all workflow agents that inherit from BusinessAgentBase
    /// </summary>
    public async Task<List<AgentTypeDto>> GetAllWorkflowAgents()
    {
        _logger.LogInformation("Getting all workflow agents that inherit from BusinessAgentBase");
        
        var propertyDtos = await GetWorkflowAgentTypeDataMap();
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
                        paramDto.PropertyJsonSchema = await GenerateSchemaForConfigType(kvp.Value.InitializationData.DtoType);
                        
                        if (string.IsNullOrWhiteSpace(paramDto.PropertyJsonSchema))
                        {
                            _logger.LogError("PropertyJsonSchema is null or empty for workflow agent {AgentType} with DtoType {DtoType}", 
                                kvp.Key, kvp.Value.InitializationData.DtoType.Name);
                            paramDto.PropertyJsonSchema = "{}"; // Fallback to empty JSON object
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to generate PropertyJsonSchema for workflow agent {AgentType} with DtoType {DtoType}", 
                            kvp.Key, kvp.Value.InitializationData.DtoType.Name);
                        paramDto.PropertyJsonSchema = "{}"; // Fallback to empty JSON object
                    }
                }
            }

            resp.Add(paramDto);
        }

        _logger.LogInformation("Successfully retrieved {Count} workflow agent types", resp.Count);
        return resp;
    }
    
    private async Task ValidateWorkflowConfigurationAsync(Guid viewAgentId)
    {
        _logger.LogInformation("Starting workflow configuration validation for ViewAgentId: {ViewAgentId}",
            viewAgentId);
        
        var agentDto = await _agentService.GetAgentAsync(viewAgentId);
        var configJson = JsonConvert.SerializeObject(agentDto.Properties);
        WorkflowViewConfigDto? viewConfigDto;
        try
        {
            viewConfigDto = JsonConvert.DeserializeObject<WorkflowViewConfigDto>(configJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize workflow configuration for ViewAgentId: {ViewAgentId}",
                viewAgentId);
            throw new UserFriendlyException("Invalid workflow configuration format");
        }

        if (viewConfigDto == null || viewConfigDto.WorkflowNodeList == null || viewConfigDto.WorkflowNodeList.Count == 0)
        {
            throw new UserFriendlyException("Workflow contains no nodes");
        }
        
        var validationTasks = viewConfigDto.WorkflowNodeList.Select(ValidateWorkflowNodePropertiesAsync);
        await Task.WhenAll(validationTasks);

        _logger.LogInformation(
            "Workflow configuration validation passed for ViewAgentId: {ViewAgentId} with {NodeCount} nodes",
            viewAgentId, viewConfigDto.WorkflowNodeList.Count);
    }

    /// <summary>
    /// 验证单个工作流节点的属性配置 - 完全模拟WorkflowViewService.PublishWorkflowAsync处理节点的方式
    /// </summary>
    private async Task ValidateWorkflowNodePropertiesAsync(WorkflowNodeDto workflowNode)
    {
        _logger.LogInformation("Validating workflow node: {NodeName} (AgentType: {AgentType})", workflowNode.Name, workflowNode.AgentType);

        if (string.IsNullOrEmpty(workflowNode.AgentType))
        {
            throw new UserFriendlyException($"Node '{workflowNode.Name}': AgentType is missing");
        }

        if (string.IsNullOrEmpty(workflowNode.JsonProperties))
        {
            throw new UserFriendlyException($"Node '{workflowNode.Name}': JsonProperties is missing");
        }
        
        Dictionary<string, object> nodeAgentProperties;
        string initializationParam;
        
        try
        {
            nodeAgentProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(workflowNode.JsonProperties);
        }
        catch (JsonSerializationException ex)
        {
            throw new UserFriendlyException($"Node '{workflowNode.Name}': JSON deserialization failed. {ex.Message}");
        }
        
        try
        {
            initializationParam = JsonConvert.SerializeObject(nodeAgentProperties);
        }
        catch (JsonSerializationException ex)
        {
            throw new UserFriendlyException($"Node '{workflowNode.Name}': Failed to serialize agent properties. {ex.Message}");
        }
        
        await ValidateAgentConfigAsync(workflowNode.AgentType, initializationParam);

        _logger.LogDebug("Validation passed for node '{NodeName}' (AgentType: {AgentType})",
            workflowNode.Name, workflowNode.AgentType);
    }

    private async Task<(AgentDto,WorkflowViewConfigDto)> PublishWorkflowAsync(Guid viewAgentId)
    {
        _logger.LogInformation("[PublishWorkflow] Starting workflow publication for ViewAgentId: {ViewAgentId}", viewAgentId);

        _logger.LogDebug("[PublishWorkflow] Calling WorkflowViewService.PublishWorkflowAsync for ViewAgentId: {ViewAgentId}", viewAgentId);
        var publishedAgent = await _workflowViewService.PublishWorkflowAsync(viewAgentId);

        if (publishedAgent == null)
        {
            _logger.LogError("[PublishWorkflow] WorkflowViewService.PublishWorkflowAsync returned null for ViewAgentId: {ViewAgentId}", viewAgentId);
            throw new UserFriendlyException("Failed to publish workflow");
        }

        _logger.LogInformation("[PublishWorkflow] Successfully published workflow agent: {PublishedAgentId}, Name: {AgentName} for ViewAgentId: {ViewAgentId}", 
            publishedAgent.Id, publishedAgent.Name, viewAgentId);

        // Extract WorkflowCoordinatorGAgentId from publishedAgent properties
        if (publishedAgent.Properties == null)
        {
            _logger.LogError("[PublishWorkflow] Published agent has null Properties. AgentId: {PublishedAgentId}, ViewAgentId: {ViewAgentId}", 
                publishedAgent.Id, viewAgentId);
            throw new UserFriendlyException("Published workflow agent has no properties");
        }

        _logger.LogDebug("[PublishWorkflow] Published agent has {PropertiesCount} properties. AgentId: {PublishedAgentId}", 
            publishedAgent.Properties.Count, publishedAgent.Id);

        var configJson = JsonConvert.SerializeObject(publishedAgent.Properties);
        _logger.LogDebug("[PublishWorkflow] Serialized properties to JSON. Length: {JsonLength}, AgentId: {PublishedAgentId}", 
            configJson.Length, publishedAgent.Id);

        WorkflowViewConfigDto? viewConfigDto;
        try
        {
            viewConfigDto = JsonConvert.DeserializeObject<WorkflowViewConfigDto>(configJson);
            _logger.LogDebug("[PublishWorkflow] Successfully deserialized WorkflowViewConfigDto. AgentId: {PublishedAgentId}", publishedAgent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PublishWorkflow] Failed to deserialize workflow configuration from published agent: {PublishedAgentId}, ViewAgentId: {ViewAgentId}, ConfigJson: {ConfigJson}", 
                publishedAgent.Id, viewAgentId, configJson);
            throw new UserFriendlyException("Invalid workflow configuration in published agent");
        }

        if (viewConfigDto?.WorkflowCoordinatorGAgentId == null || viewConfigDto.WorkflowCoordinatorGAgentId == Guid.Empty)
        {
            _logger.LogError("[PublishWorkflow] WorkflowCoordinatorGAgentId is null or empty. ViewConfigDto: {ViewConfigDto}, PublishedAgentId: {PublishedAgentId}, ViewAgentId: {ViewAgentId}", 
                viewConfigDto, publishedAgent.Id, viewAgentId);
            throw new UserFriendlyException("WorkflowCoordinatorGAgentId not found in published workflow");
        }

        _logger.LogInformation("[PublishWorkflow] Successfully extracted WorkflowCoordinatorGAgentId: {CoordinatorId} from published workflow ViewAgentId: {ViewAgentId}, PublishedAgentId: {PublishedAgentId}", 
            viewConfigDto.WorkflowCoordinatorGAgentId, viewAgentId, publishedAgent.Id);

        return (publishedAgent,viewConfigDto);
    }



    /// <summary>
    /// Execute workflow directly through IWorkflowViewGAgentPlus.ExecuteWorkflowAsync()
    /// Execution name is automatically generated as: {WorkflowName}-Round{RoundId}
    /// </summary>
    /// <param name="viewAgentId">WorkflowViewAgent ID</param>
    /// <returns>Event ID of the workflow event</returns>
    private async Task<Guid> ExecuteWorkflowAsync(Guid viewAgentId)
    {
        _logger.LogInformation("[ExecuteWorkflow] Starting workflow execution for ViewAgentId: {ViewAgentId}", viewAgentId);
        
        // Get IWorkflowViewGAgentPlus grain
        var workflowViewAgent = _clusterClient.GetGrain<IWorkflowViewGAgentPlus>(viewAgentId);
        
        // Direct call to ExecuteWorkflowAsync (execution name generated internally as {Name}-Round{RoundId})
        var executionEventId = await workflowViewAgent.ExecuteWorkflowAsync();
        
        _logger.LogInformation("[ExecuteWorkflow] Workflow execution started successfully. ViewAgentId: {ViewAgentId}, EventId: {ExecutionEventId}", 
            viewAgentId, executionEventId);
        
        return executionEventId;
    }
    
    private async Task<Guid> ExecuteWorkflowAsync_Old(Guid coordinatorAgentId, WorkflowRunRequestDto request)
    {
        const string targetEventType = "Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent.StartWorkflowCoordinatorEvent";
        const int maxRetries = 20;
        const int retryDelayMs = 1000;

        _logger.LogInformation("Starting workflow execution for coordinator agent: {CoordinatorAgentId}", coordinatorAgentId);
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            _logger.LogInformation("Checking event availability, attempt {Attempt}/{MaxRetries} for agent: {AgentId}", 
                attempt, maxRetries, coordinatorAgentId);

            try
            {
                var agent = _clusterClient.GetGrain<ICreatorGAgent>(coordinatorAgentId);
                var agentState = await agent.GetAgentAsync();
                
                // Log all available events in EventInfoList
                _logger.LogInformation("Agent {AgentId} has {EventCount} events in EventInfoList: {EventList}", 
                    coordinatorAgentId, 
                    agentState.EventInfoList.Count,
                    string.Join(", ", agentState.EventInfoList.Select(e => e.EventType.FullName ?? "Unknown")));
                
                var targetEvent =  agentState.EventInfoList.Find(i => i.EventType.FullName == targetEventType);
                if (targetEvent != null)
                {
                    _logger.LogInformation("StartWorkflowCoordinatorEvent found on attempt {Attempt}, proceeding with workflow execution", attempt);

                    await _subscriptionAppService.PublishEventAsync(new PublishEventDto
                    {
                        AgentId = coordinatorAgentId,
                        EventType = targetEventType,
                        EventProperties = request.EventProperties as Dictionary<string, object> ?? new Dictionary<string, object>()
                    });

                    _logger.LogInformation("Workflow event published successfully for agent: {AgentId}",
                        coordinatorAgentId);
                    return Guid.NewGuid(); // Return a dummy event ID for old implementation
                }

                _logger.LogWarning("StartWorkflowCoordinatorEvent not found on attempt {Attempt}",attempt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking event availability on attempt {Attempt} for agent: {AgentId}", attempt, coordinatorAgentId);
            }

            if (attempt < maxRetries)
            {
                _logger.LogInformation("Waiting {DelayMs}ms before next attempt for agent: {AgentId}", retryDelayMs, coordinatorAgentId);
                await Task.Delay(retryDelayMs);
            }
        }

        _logger.LogError("Failed to find StartWorkflowCoordinatorEvent after {MaxRetries} attempts for agent: {AgentId}", maxRetries, coordinatorAgentId);
        return Guid.Empty;
    }

    #region Agent Validation Methods

    /// <summary>
    /// 验证Agent配置 - 完全模拟AgentService.InitializeBusinessAgent+SetupConfigurationData的流程
    /// </summary>
    private async Task ValidateAgentConfigAsync(string agentType, string configJson)
    {
        _logger.LogInformation("[AgentValidation] Validating {AgentType}", agentType);

        try
        {
            var tempGrainId = GrainId.Create(agentType, GuidUtil.GuidToGrainKey(Guid.NewGuid()));
            var agent = await _gAgentFactory.GetGAgentAsync(tempGrainId);
            var configurationType = ExtractConfigurationProperties(await agent.GetConfigurationTypeAsync());
            if (configurationType == null)
            {
                _logger.LogWarning("[AgentValidation] No configuration type found for agent: {AgentType}", agentType);
                throw new UserFriendlyException($"Agent type '{agentType}' has no configuration");
            }

            await ValidateConfigurationDataAsync(configurationType, configJson);
            _logger.LogInformation("[AgentValidation] Validation completed successfully for {AgentType}", agentType);
        }
        catch (Exception ex) when (!(ex is UserFriendlyException))
        {
            _logger.LogError(ex, "[AgentValidation] Failed to validate agent type: {AgentType}", agentType);
            throw new UserFriendlyException($"Invalid agent type '{agentType}': {ex.Message}");
        }
    }
    
    private Configuration? ExtractConfigurationProperties(Type? configurationType)
    {
        if (configurationType == null || configurationType.IsAbstract) return null;
        var properties = configurationType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var configuration = new Configuration { DtoType = configurationType };
        var propertyData = properties
            .Select(property => new PropertyData() { Name = property.Name, Type = property.PropertyType }).ToList();
        configuration.Properties = propertyData;
        return configuration;
    }

    /// <summary>
    /// Get workflow agents that inherit from BusinessAgentBase
    /// </summary>
    private async Task<Dictionary<string, AgentTypeData?>> GetWorkflowAgentTypeDataMap()
    {
        var availableGAgents = _gAgentManager.GetAvailableGAgentTypes();
        
        _logger.LogInformation("Total available GAgent types: {Count}", availableGAgents.Count());
        
        // Filter agents that:
        // 1. Don't start with OrleansCodeGen
        // 2. Inherit from BusinessAgentBase (new workflow agents)
        var workflowAgentTypes = availableGAgents
            .Where(a => !a.Namespace.StartsWith("OrleansCodeGen"))
            .Where(a => IsBusinessAgentType(a))
            .ToList();

        _logger.LogInformation("Found {Count} workflow agent types that inherit from BusinessAgentBase", workflowAgentTypes.Count);

        var dict = new Dictionary<string, AgentTypeData?>();

        foreach (var agentType in workflowAgentTypes)
        {
            try
            {
                var grainType = _grainTypeResolver.GetGrainType(agentType).ToString();

                if (grainType == null) continue;

                var agentTypeData = new AgentTypeData { FullName = agentType.FullName };
                var grainId = GrainId.Create(grainType,
                    GuidUtil.GuidToGrainKey(
                        GuidUtil.StringToGuid("WorkflowAgentDefaultId"))); // unique ID for workflow agents
                var agent = await _gAgentFactory.GetGAgentAsync(grainId);
                
                // Filter out workflow infrastructure agents (coordinator, execution record, etc.)
                // These are system agents that should not be exposed as user-facing workflow nodes
                var isWorkflowAgent = await agent.GetIsWorkflowAgentAsync();
                if (isWorkflowAgent)
                {
                    _logger.LogDebug("Skipping workflow infrastructure agent: {AgentType}", agentType.FullName);
                    continue;
                }
                
                var description = await agent.GetDescriptionAsync();
                agentTypeData.Description = description;

                var initializationData = await GetAgentConfigurationAsync(agent);

                agentTypeData.InitializationData = initializationData;
                dict[grainType] = agentTypeData;
                
                _logger.LogDebug("Processed workflow agent type: {AgentType}", agentType.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process workflow agent type {AgentType}: {ErrorMessage}",
                    agentType.FullName, ex.Message);
                continue;
            }
        }

        return dict;
    }
    
    /// <summary>
    /// Check if a type is a BusinessAgent type (inherits from BusinessAgentBase)
    /// </summary>
    private bool IsBusinessAgentType(Type type)
    {
        // Check if the type inherits from BusinessAgentBase class
        var currentType = type.BaseType;
        while (currentType != null)
        {
            if (currentType.Name.StartsWith("BusinessAgentBase"))
            {
                return true;
            }
            currentType = currentType.BaseType;
        }
        return false;
    }

    private async Task<Configuration?> GetAgentConfigurationAsync(IGAgentPlus agent)
        => ExtractConfigurationProperties(await agent.GetConfigurationTypeAsync());
    
    /// <summary>
    /// Generate JSON schema for configuration type
    /// </summary>
    private async Task<string> GenerateSchemaForConfigType(Type configurationType)
    {
        try
        {
            var schema = _schemaProvider.GetTypeSchema(configurationType);
            if (schema == null)
            {
                _logger.LogError("SchemaProvider returned null schema for type {TypeName}", configurationType.Name);
                return "{}";
            }
            
            var schemaJson = schema.ToJson();
            if (string.IsNullOrWhiteSpace(schemaJson))
            {
                _logger.LogError("Schema ToJson() returned empty result for type {TypeName}", configurationType.Name);
                return "{}";
            }
            
            return schemaJson;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate schema for type {TypeName}", configurationType.Name);
            return "{}";
        }
    }

    private async Task ValidateConfigurationDataAsync(Configuration configType, string configJson)
    {
        try
        {
            _logger.LogDebug("[AgentValidation] Starting validation for {ConfigType} with JSON: {ConfigJson}", configType.DtoType, configJson);

            var actualDto = Activator.CreateInstance(configType.DtoType);
            var config = (ConfigurationBase)actualDto!;
            var schema = _schemaProvider.GetTypeSchema(config.GetType());
            var validateResponse = schema.Validate(configJson, new JsonSchemaValidatorSettings { PropertyStringComparer = StringComparer.CurrentCultureIgnoreCase });
            if (validateResponse.Count > 0)
            {
                var errors = string.Join("; ", validateResponse.Select(e => $"{e.Path}: {e.Kind}"));
                _logger.LogWarning("[AgentValidation] Schema validation failed for {ConfigType}. Errors: {Errors}. JSON: {ConfigJson}", 
                    configType.DtoType, errors, configJson);
                throw new UserFriendlyException($"Schema validation failed for {configType.DtoType}: {errors}");
            }

            if (config is IValidatableObject validatableConfig)
            {
                var validationContext = new ValidationContext(config);
                var customResults = validatableConfig.Validate(validationContext).ToList();
                if (customResults.Any())
                {
                    var errors = string.Join("; ", customResults.Select(r => r.ErrorMessage));
                    _logger.LogWarning("[AgentValidation] Custom validation failed for {ConfigType}: {Errors}", configType.DtoType, errors);
                    throw new UserFriendlyException($"Custom validation failed for {configType.DtoType}: {errors}");
                }
            }

        }
        catch (Exception ex)
        {
            throw new UserFriendlyException($"Validation error: {ex.Message}");
        }
    }

    #endregion
}