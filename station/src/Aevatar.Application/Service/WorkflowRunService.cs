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
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView.Dto;
using Aevatar.Schema;
using Aevatar.Subscription;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NJsonSchema.Validation;
using Orleans;
using Orleans.Runtime;
using Volo.Abp.Application.Services;
using Volo.Abp;

namespace Aevatar.Service;

public interface IWorkflowRunService
{
    Task<WorkflowRunResultDto> RunWorkflowAsync(WorkflowRunRequestDto request);
}

[RemoteService(IsEnabled = false)]
public class WorkflowRunService : ApplicationService, IWorkflowRunService
{
    private readonly IClusterClient _clusterClient;
    private readonly IWorkflowViewService _workflowViewService;
    private readonly ISubscriptionAppService _subscriptionAppService;
    private readonly IAgentService _agentService;
    private readonly IGAgentManager _gAgentManager;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ISchemaProvider _schemaProvider;
    private readonly ILogger<WorkflowRunService> _logger;

    public WorkflowRunService(
        IWorkflowViewService workflowViewService,
        ISubscriptionAppService subscriptionAppService,
        IAgentService agentService,
        IGAgentManager gAgentManager,
        IGAgentFactory gAgentFactory,
        ISchemaProvider schemaProvider,
        ILogger<WorkflowRunService> logger,
        IClusterClient clusterClient)
    {
        _workflowViewService = workflowViewService;
        _subscriptionAppService = subscriptionAppService;
        _agentService = agentService;
        _gAgentManager = gAgentManager;
        _gAgentFactory = gAgentFactory;
        _schemaProvider = schemaProvider;
        _logger = logger;
        _clusterClient = clusterClient;
    }

    public async Task<WorkflowRunResultDto> RunWorkflowAsync(WorkflowRunRequestDto request)
    {
        _logger.LogInformation("Starting workflow run for ViewAgentId: {ViewAgentId}", request.ViewAgentId);

        // Step 1: Validate workflow configuration
        await ValidateWorkflowConfigurationAsync(request.ViewAgentId);

        // Step 2: Publish workflow
        var workflowCoordinatorAgentId = await PublishWorkflowAsync(request.ViewAgentId);

        // Step 3: Execute workflow
        var executionSuccess = await ExecuteWorkflowAsync(workflowCoordinatorAgentId, request);

        if (!executionSuccess)
        {
            _logger.LogWarning("Workflow execution failed for ViewAgentId: {ViewAgentId}, coordinator agent not ready",
                request.ViewAgentId);
            return new WorkflowRunResultDto
            {
                IsSuccess = false,
                WorkflowId = workflowCoordinatorAgentId,
                Message = "Workflow coordinator agent is not ready yet. Please retry the execution in a few moments."
            };
        }

        // All steps completed successfully
        _logger.LogInformation("Workflow run completed successfully for ViewAgentId: {ViewAgentId}",
            request.ViewAgentId);

        return new WorkflowRunResultDto
        {
            IsSuccess = true,
            WorkflowId = workflowCoordinatorAgentId,
            Message = "Workflow executed successfully"
        };
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

        if (viewConfigDto == null)
        {
            throw new UserFriendlyException("Workflow contains no nodes");
        }
        
        // var validationTasks = viewConfigDto.WorkflowNodeList.Select(ValidateWorkflowNodePropertiesAsync);
        // await Task.WhenAll(validationTasks);

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

        // 1. 先反序列化为Dictionary（与WorkflowViewService.PublishWorkflowAsync保持一致）
     
        var nodeAgentProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(workflowNode.JsonProperties);

        // 2. 重新序列化为JSON字符串（模拟AgentService.CreateAgentAsync的Properties处理）
        var initializationParam = JsonConvert.SerializeObject(nodeAgentProperties);

        // 3. 验证Agent配置
        await ValidateAgentConfigAsync(workflowNode.AgentType, initializationParam);

        _logger.LogDebug("Validation passed for node '{NodeName}' (AgentType: {AgentType})",
            workflowNode.Name, workflowNode.AgentType);
    }

    private async Task<Guid> PublishWorkflowAsync(Guid viewAgentId)
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

        return viewConfigDto.WorkflowCoordinatorGAgentId;
    }

    /// <summary>
    /// 执行工作流 - 检查事件是否可用后再发布
    /// </summary>
    private async Task<bool> ExecuteWorkflowAsync(Guid coordinatorAgentId, WorkflowRunRequestDto request)
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
                        EventProperties = request.EventProperties
                    });

                    _logger.LogInformation("Workflow event published successfully for agent: {AgentId}",
                        coordinatorAgentId);
                    return true;
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
        return false;
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
            // 1. 创建临时Agent实例获取配置类型（模拟AgentService.InitializeBusinessAgent）
            var tempGrainId = GrainId.Create(agentType, Guid.NewGuid().ToString());
            var agent = await _gAgentFactory.GetGAgentAsync(tempGrainId);
            
            // 2. 获取配置类型（模拟AgentService.GetAgentConfigurationAsync）
            var configurationType = ExtractConfigurationProperties(await agent.GetConfigurationTypeAsync());
            if (configurationType == null)
            {
                _logger.LogWarning("[AgentValidation] No configuration type found for agent: {AgentType}", agentType);
                throw new UserFriendlyException($"Agent type '{agentType}' has no configuration");
            }

            // 3. 使用与AgentService.SetupConfigurationData相同的验证流程
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

    private async Task ValidateConfigurationDataAsync(Configuration configType, string configJson)
    {
        try
        {
            _logger.LogDebug("[AgentValidation] Starting validation for {ConfigType} with JSON: {ConfigJson}", configType.DtoType, configJson);

            // 1. 创建配置实例（模拟AgentService.SetupConfigurationData）
            var actualDto = Activator.CreateInstance(configType.DtoType);
            var config = (ConfigurationBase)actualDto!;
            // if (actualDto == null)
            // {
            //     _logger.LogError("[AgentValidation] Failed to create instance of {ConfigType}", configType.Name);
            //     throw new UserFriendlyException($"Failed to create configuration instance for {configType.Name}");
            // }

            // 2. Schema验证（使用与AgentService相同的设置）
            var schema = _schemaProvider.GetTypeSchema(config.GetType());
            var validateResponse = schema.Validate(configJson, new JsonSchemaValidatorSettings { PropertyStringComparer = StringComparer.CurrentCultureIgnoreCase });
            if (validateResponse.Count > 0) throw new UserFriendlyException($"Schema validation failed for {configType.DtoType}");

            // 3. 自定义验证（IValidatableObject）
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

            // _logger.LogDebug("[AgentValidation] All validations passed for {ConfigType}", configType.Name);
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "[AgentValidation] Unexpected error during config validation for {ConfigType}", configType.Name);
            throw new UserFriendlyException($"Validation error: {ex.Message}");
        }
    }

    #endregion
}