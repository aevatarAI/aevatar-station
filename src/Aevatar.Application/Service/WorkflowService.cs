using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.GroupChat.Feature.Extension;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
using Aevatar.GAgents.Twitter.GEvents;
using Aevatar.Station.Feature.CreatorGAgent;
using Aevatar.Workflow;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Aevatar.Service;

[RemoteService(IsEnabled = false)]
public class WorkflowService : ApplicationService, IWorkflowService
{
    private readonly ILogger<WorkflowService> _logger;
    private readonly IAgentService _agentService;
    private readonly IClusterClient _clusterClient;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IConfiguration _configuration;
    private readonly IUserAppService _userAppService;

    public WorkflowService(
        ILogger<WorkflowService> logger,
        IAgentService agentService,
        IClusterClient clusterClient,
        IGAgentFactory gAgentFactory,
        IConfiguration configuration,
        IUserAppService userAppService)
    {
        _logger = logger;
        _agentService = agentService;
        _clusterClient = clusterClient;
        _gAgentFactory = gAgentFactory;
        _configuration = configuration;
        _userAppService = userAppService;
    }

    public async Task<WorkflowResponseDto> CreateWorkflowAsync(CreateWorkflowDto dto)
    {
        // Get current user information
        var currentUserId = _userAppService.GetCurrentUserId();
        _logger.LogInformation("Starting workflow creation: {workflowName}, userId: {userId}", dto.WorkflowName, currentUserId);

        try
        {
            // Step 1: Create GroupGAgent (workflow coordinator)
            _logger.LogInformation("Step 1: Creating GroupGAgent workflow coordinator");
            var groupAgentDto = await CreateGroupAgentAsync(dto.WorkflowName);

            // Step 2: Create AIGAgent
            _logger.LogInformation("Step 2: Creating AIGAgent");
            var aiAgentDto = await CreateAIAgentAsync(dto.AiConfig);

            // Step 3: Create TwitterGAgent
            _logger.LogInformation("Step 3: Creating TwitterGAgent");
            var twitterAgentDto = await CreateTwitterAgentAsync(dto.TwitterConfig);

            // Step 4: Create SocialGAgent
            _logger.LogInformation("Step 4: Creating SocialGAgent");
            var socialAgentDto = await CreateSocialAgentAsync();

            // Step 5: Establish relationships
            _logger.LogInformation("Step 5: Establishing agent relationships");
            await EstablishRelationshipsAsync(twitterAgentDto.Id, socialAgentDto.Id);

            // Step 6: Build workflow units
            _logger.LogInformation("Step 6: Building workflow units");
            var workflowUnits = BuildWorkflowUnits(aiAgentDto.BusinessAgentGrainId, twitterAgentDto.BusinessAgentGrainId);

            // Step 7: Register workflow to GroupGAgent
            _logger.LogInformation("Step 7: Registering workflow to GroupGAgent");
            await RegisterWorkflowAsync(groupAgentDto.Id, workflowUnits);

            _logger.LogInformation("Workflow creation completed: {workflowName}, userId: {userId}", dto.WorkflowName, currentUserId);

            return new WorkflowResponseDto
            {
                GroupAgentId = groupAgentDto.Id,
                AiAgentId = aiAgentDto.Id,
                TwitterAgentId = twitterAgentDto.Id,
                SocialAgentId = socialAgentDto.Id,
                WorkflowUnits = workflowUnits,
                Success = true,
                Message = "Workflow created successfully, all agents initialized"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow creation failed: {workflowName}, userId: {userId}", dto.WorkflowName, currentUserId);
            throw new UserFriendlyException($"Workflow creation failed: {ex.Message}");
        }
    }

    private async Task<AgentDto> CreateGroupAgentAsync(string workflowName)
    {
        var createAgentDto = new CreateAgentInputDto
        {
            Name = $"WorkflowCoordinator_{workflowName}",
            AgentType = "Aevatar.Application.Grains.Agents.Group.GroupGAgent",
            Properties = new Dictionary<string, object>
            {
                { "groupName", workflowName },
                { "description", "Workflow coordinator" },
                { "maxMembers", 10 }
            }
        };

        return await _agentService.CreateAgentAsync(createAgentDto);
    }

    private async Task<AgentDto> CreateAIAgentAsync(AIConfigDto aiConfig)
    {
        if (string.IsNullOrEmpty(aiConfig.ApiKey))
        {
            throw new UserFriendlyException("AIGAgent ApiKey is required");
        }

        var createAgentDto = new CreateAgentInputDto
        {
            Name = "WorkflowAIAgent",
            AgentType = "Aevatar.GAgents.AIGAgent",
            Properties = new Dictionary<string, object>
            {
                { "apiKey", aiConfig.ApiKey },
                { "model", aiConfig.Model },
                { "maxTokens", aiConfig.MaxTokens },
                { "temperature", aiConfig.Temperature }
            }
        };

        return await _agentService.CreateAgentAsync(createAgentDto);
    }

    private async Task<AgentDto> CreateTwitterAgentAsync(TwitterConfigDto twitterConfig)
    {
        var createAgentDto = new CreateAgentInputDto
        {
            Name = "WorkflowTwitterAgent",
            AgentType = "Aevatar.GAgents.Twitter.Agent.TwitterGAgent",
            Properties = new Dictionary<string, object>
            {
                { "consumerKey", twitterConfig.ConsumerKey },
                { "consumerSecret", twitterConfig.ConsumerSecret },
                { "bearerToken", twitterConfig.BearerToken },
                { "encryptionPassword", twitterConfig.EncryptionPassword },
                { "replyLimit", twitterConfig.ReplyLimit }
            }
        };

        var twitterAgent = await _agentService.CreateAgentAsync(createAgentDto);
        
        // Publish BindTwitterAccountGEvent to bind Twitter account if binding information is provided
        if (!string.IsNullOrEmpty(twitterConfig.UserName) || !string.IsNullOrEmpty(twitterConfig.UserId) || 
            !string.IsNullOrEmpty(twitterConfig.Token) || !string.IsNullOrEmpty(twitterConfig.TokenSecret))
        {
            _logger.LogInformation("Publishing BindTwitterAccountGEvent for Twitter account binding");
            
            var creatorAgent = _clusterClient.GetGrain<ICreatorGAgent>(twitterAgent.Id);
            var bindEvent = new BindTwitterAccountGEvent
            {
                UserName = twitterConfig.UserName,
                UserId = twitterConfig.UserId,
                Token = twitterConfig.Token,
                TokenSecret = twitterConfig.TokenSecret
            };
            
            await creatorAgent.PublishEventAsync(bindEvent);
            _logger.LogInformation("BindTwitterAccountGEvent published successfully");
        }

        return twitterAgent;
    }

    private async Task<AgentDto> CreateSocialAgentAsync()
    {
        var createAgentDto = new CreateAgentInputDto
        {
            Name = "WorkflowSocialAgent",
            AgentType = "Aevatar.GAgents.SocialGAgent",
            Properties = new Dictionary<string, object>
            {
                { "platform", "twitter" },
                { "autoPost", true },
                { "contentFilter", true }
            }
        };

        return await _agentService.CreateAgentAsync(createAgentDto);
    }

    private async Task EstablishRelationshipsAsync(Guid twitterAgentId, Guid socialAgentId)
    {
        // Set SocialGAgent as a sub-agent of TwitterGAgent
        var addSubAgentDto = new AddSubAgentDto
        {
            SubAgents = new List<Guid> { socialAgentId }
        };

        await _agentService.AddSubAgentAsync(twitterAgentId, addSubAgentDto);
    }

    private List<WorkflowUnitDto> BuildWorkflowUnits(string aiAgentGrainId, string twitterAgentGrainId)
    {
        return new List<WorkflowUnitDto>
        {
            new WorkflowUnitDto
            {
                GrainId = aiAgentGrainId,
                NextGrainId = twitterAgentGrainId
            },
            new WorkflowUnitDto
            {
                GrainId = twitterAgentGrainId,
                NextGrainId = "" // End node
            }
        };
    }

    private async Task RegisterWorkflowAsync(Guid groupAgentId, List<WorkflowUnitDto> workflowUnits)
    {
        // For now, simplify the implementation and log workflow unit information
        // In practice, GroupGAgent should be extended with AddWorkflowGroupChat method
        _logger.LogInformation("Registering workflow units to GroupAgent {groupAgentId}: {workflowUnits}", 
            groupAgentId, JsonConvert.SerializeObject(workflowUnits));
        
        var groupAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(groupAgentId);
        await groupAgent.AddWorkflowGroupChat(_gAgentFactory, workflowUnits);
    }
} 