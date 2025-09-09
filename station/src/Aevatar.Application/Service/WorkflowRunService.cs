using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.Creator;
using Aevatar.Subscription;
using Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView.Dto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
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
    private readonly IAgentValidationService _agentValidationService;
    private readonly IWorkflowViewService _workflowViewService;
    private readonly ISubscriptionAppService _subscriptionAppService;
    private readonly IAgentService _agentService;
    private readonly ILogger<WorkflowRunService> _logger;

    public WorkflowRunService(
        IAgentValidationService agentValidationService,
        IWorkflowViewService workflowViewService,
        ISubscriptionAppService subscriptionAppService,
        IAgentService agentService,
        ILogger<WorkflowRunService> logger,
        IClusterClient clusterClient)
    {
        _agentValidationService = agentValidationService;
        _workflowViewService = workflowViewService;
        _subscriptionAppService = subscriptionAppService;
        _agentService = agentService;
        _logger = logger;
        _clusterClient = clusterClient;
    }

    public async Task<WorkflowRunResultDto> RunWorkflowAsync(WorkflowRunRequestDto request)
    {
        _logger.LogInformation("Starting workflow run for ViewAgentId: {ViewAgentId}", request.ViewAgentId);

        // Step 1: Validate workflow configuration
        // await ValidateWorkflowConfigurationAsync(request.ViewAgentId);

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

    /// <summary>
    /// 验证工作流配置 - 遍历视图中的每个节点并验证用户设置的参数
    /// </summary>
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

        // Step 3: 并发验证每个工作流节点 - 类似PublishWorkflowAsync的逻辑
        var validationTasks = viewConfigDto.WorkflowNodeList
            .Select(workflowNode => ValidateWorkflowNodePropertiesAsync(workflowNode, viewAgentId));
        
        await Task.WhenAll(validationTasks);

        _logger.LogInformation(
            "Workflow configuration validation passed for ViewAgentId: {ViewAgentId} with {NodeCount} nodes",
            viewAgentId, viewConfigDto.WorkflowNodeList.Count);
    }

    /// <summary>
    /// 验证单个工作流节点的属性配置 - 类似PublishWorkflowAsync处理节点的方式
    /// </summary>
    private async Task ValidateWorkflowNodePropertiesAsync(WorkflowNodeDto workflowNode, Guid viewAgentId)
    {
        _logger.LogInformation("Validating workflow node: {NodeName} (AgentType: {AgentType}) for ViewAgentId: {ViewAgentId}",
            workflowNode.Name, workflowNode.AgentType, viewAgentId);

        if (string.IsNullOrEmpty(workflowNode.AgentType))
        {
            throw new UserFriendlyException($"Node '{workflowNode.Name}': AgentType is missing");
        }

        if (string.IsNullOrEmpty(workflowNode.JsonProperties))
        {
            throw new UserFriendlyException($"Node '{workflowNode.Name}': JsonProperties is missing");
        }

        // 获取AgentType的propertyJsonSchema并验证节点属性
        var validationResult = await _agentValidationService.ValidateConfigAsync(new()
        {
            GAgentNamespace = workflowNode.AgentType,
            ConfigJson = workflowNode.JsonProperties
        });

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for node '{NodeName}' (AgentType: {AgentType}): {Message}",
                workflowNode.Name, workflowNode.AgentType, validationResult.Message);
            throw new UserFriendlyException($"Node '{workflowNode.Name}' validation failed: {validationResult.Message}");
        }

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
}