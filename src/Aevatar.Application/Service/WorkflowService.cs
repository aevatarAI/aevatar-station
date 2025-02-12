using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.AI.Dtos;
using Aevatar.Application.Grains.Agents.BasicAI;
using Aevatar.Application.Grains.Workflow;
using Aevatar.Workflow;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Aevatar.Service;

public class WorkflowService : ApplicationService, IWorkflowService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<WorkflowService> _logger;
    private readonly IAgentService _agentService;

    public WorkflowService(
        IClusterClient clusterClient,
        ILogger<WorkflowService> logger,
        IAgentService agentService
    )
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _agentService = agentService;
    }
    
    public async Task<WorkflowDto> GenerateWorkflowAsync(string taskDescription)
    {
        var agentDescriptions = await GetAllAgentDescriptionsAsync();
        string prompt = GeneratePrompt(taskDescription, agentDescriptions);
        
        var initializeDto = new InitializeDto
        {
            LLM = "AzureOpenAI",
            Instructions = "",
            Files = new List<FileDto>()
        };

        prompt = "tell me about yourself";
        var basicAIGAgent = _clusterClient.GetGrain<IBasicAIGAgent>(Guid.NewGuid());
        var initializeResult = await basicAIGAgent.InitializeAsync(initializeDto);
        var response = await basicAIGAgent.InvokeLLMAsync(prompt);
        if (response.IsNullOrEmpty())
        {
            _logger.LogError("invoke llm failed, task: {task}", taskDescription);
            throw new UserFriendlyException("invoke llm failed");
        }
        
        var workflow = JsonConvert.DeserializeObject<WorkflowDto>(response);
        if (workflow == null)
        {
            _logger.LogError("parse workflow failed, task: {task}, workflow: {workflow}", taskDescription, response);
            throw new UserFriendlyException("parse workflow failed");
        }

        var workflowGAgent = _clusterClient.GetGrain<IWorkflowGAgent>(Guid.NewGuid());
        await workflowGAgent.SetWorkflowAsync(workflow);
        
        return workflow;
    }

    public async Task<WorkflowDto> GetWorkflow(Guid workflowId)
    {
        var workflowGAgent = _clusterClient.GetGrain<IWorkflowGAgent>(workflowId);
        var workflow = await workflowGAgent.GetWorkflowAsync();
        return new WorkflowDto()
        {
            WorkflowId = workflowId,
            WorkflowName = workflow.WorkflowName,
            TriggerEvent = workflow.TriggerEvent,
            EventFlow = workflow.EventFlow
        };
    }
    
    public async Task<WorkflowDto> UpdateWorkflow(WorkflowDto updatedWorkflow)
    {
        var workflowGAgent = _clusterClient.GetGrain<IWorkflowGAgent>(updatedWorkflow.WorkflowId);
        await workflowGAgent.SetWorkflowAsync(updatedWorkflow);
        return updatedWorkflow;
    }
    
    
    public static string GeneratePrompt(string taskDescription, Dictionary<string, string> agentDescriptions)
    {
        if (string.IsNullOrWhiteSpace(taskDescription))
        {
            throw new ArgumentException("Task description cannot be empty.", nameof(taskDescription));
        }

        if (agentDescriptions == null || agentDescriptions.Count == 0)
        {
            throw new ArgumentException("Agent descriptions cannot be null or empty.", nameof(agentDescriptions));
        }

        // Convert agent descriptions to a formatted string
        var agentDescriptionsText = string.Join("\n", agentDescriptions.Select(kv => 
            $"- Agent: {kv.Key}\n  Description: {kv.Value}\n"));

        // Replace placeholders in the template
        var finalPrompt = Template.PromptTemplate
            .Replace("{TASK}", taskDescription)
            .Replace("{AGENT_DESCRIPTIONS}", agentDescriptionsText);

        return finalPrompt;
    }
    
    private async Task<Dictionary<string, string>> GetAllAgentDescriptionsAsync()
    {
        var agentDescriptions = new Dictionary<string, string>();
        var availableAgents = await _agentService.GetAllAgents();

        foreach (var agent in availableAgents)
        {
            var description = agent.Description;
            agentDescriptions[agent.AgentType] = description;
        }

        return agentDescriptions;
    }
    
}
