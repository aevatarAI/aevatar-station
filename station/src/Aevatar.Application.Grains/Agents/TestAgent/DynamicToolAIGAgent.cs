using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.Executor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Aevatar.Application.Grains.Agents.TestAgent;

public interface IDynamicToolAIGAgent : IAIGAgent, IStateGAgent<DynamicToolAIGAgentState>
{
    Task<bool> ConfigureBrainAsync(string systemLLM);
    Task<List<GAgentDetailInfo>> GetAvailableGAgentsAsync();
    Task<string> ChatAsync(string message);
    Task<ChatWithDetailsResponse> ChatWithDetailsAsync(string message);
}

[GenerateSerializer]
public class DynamicToolAIGAgentState : AIGAgentStateBase
{
    // All MCP and GAgent state is now in the base class
}

[GenerateSerializer]
public class DynamicToolAIGAgentStateLogEvent : StateLogEventBase<DynamicToolAIGAgentStateLogEvent>
{
}

[GAgent("dynamictoolai", "demo")]
public class DynamicToolAIGAgent : AIGAgentBase<DynamicToolAIGAgentState, DynamicToolAIGAgentStateLogEvent>,
    IDynamicToolAIGAgent
{
    private readonly ILogger<DynamicToolAIGAgent> _logger;
    private readonly IGAgentService _gAgentService;

    public DynamicToolAIGAgent()
    {
        _logger = ServiceProvider.GetRequiredService<ILogger<DynamicToolAIGAgent>>();
        _gAgentService = ServiceProvider.GetRequiredService<IGAgentService>();
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Dynamic AI agent that can use MCP tools at runtime");
    }

    /// <summary>
    /// Configure the brain with a specific LLM system - Simplified version
    /// </summary>
    public async Task<bool> ConfigureBrainAsync(string systemLLM)
    {
        try
        {
            Logger.LogInformation("Configuring brain with system LLM: {SystemLLM}", systemLLM);

            // Use the base class method to set system LLM
            await SetSystemLLMAsync(systemLLM);

            // Set a default prompt template if not already set
            if (string.IsNullOrEmpty(State.PromptTemplate))
            {
                var initDto = new InitializeDto
                {
                    LLMConfig = new LLMConfigDto { SystemLLM = systemLLM },
                    Instructions =
                        @"You are a helpful AI assistant with access to various tools through MCP (Model Context Protocol) and other GAgent tools.

When asked to perform tasks, use the available tools to help provide accurate and complete responses.

Before using any tool:
- Check the tool's description to understand what it does
- Review the parameter descriptions to understand what inputs are required
- Use the exact tool names and parameter names as provided

Always explain what tools you're using and why.
When using tools, be clear about the results and how they help answer the user's question.",
                    EnableMCPTools = true,
                    EnableGAgentTools = true
                };

                // Use base class InitializeAsync which handles everything
                return await InitializeAsync(initDto);
            }

            // If prompt template exists, just trigger brain initialization
            await OnAIGAgentActivateAsync(CancellationToken.None);
            return GetKernelFromBrain() != null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to configure brain");
            return false;
        }
    }

    public async Task<List<GAgentDetailInfo>> GetAvailableGAgentsAsync()
    {
        try
        {
            // Get all available GAgents
            var allGAgentInfos = await _gAgentService.GetAllAvailableGAgentInformation();
            var gAgentList = new List<GAgentDetailInfo>();

            // Filter out self and MCP-related agents
            var selfGrainType = this.GetGrainId().Type;

            foreach (var (grainType, eventTypes) in allGAgentInfos)
            {
                // Skip self and MCP agents
                var grainTypeString = grainType.ToString();
                if (grainType.Equals(selfGrainType) ||
                    grainTypeString.Contains("MCP", StringComparison.OrdinalIgnoreCase) ||
                    grainTypeString.Contains("DynamicTool", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Get detailed info
                var detailInfo = await _gAgentService.GetGAgentDetailInfoAsync(grainType);
                if (detailInfo != null)
                {
                    gAgentList.Add(detailInfo);
                }
            }

            Logger.LogInformation($"Found {gAgentList.Count} available GAgents for tool usage");
            return gAgentList;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get available GAgents");
            return new List<GAgentDetailInfo>();
        }
    }

    // Override the base class method to add logging
    public override async Task<bool> ConfigureGAgentToolsAsync(List<GrainType> selectedGAgents)
    {
        try
        {
            Logger.LogInformation("Configuring GAgent tools with {Count} selected agents", selectedGAgents.Count);

            // Use base class implementation which handles everything
            var result = await base.ConfigureGAgentToolsAsync(selectedGAgents);

            if (result)
            {
                Logger.LogInformation("Successfully configured {Count} GAgent tools", selectedGAgents.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to configure GAgent tools");
            return false;
        }
    }

    public async Task<string> ChatAsync(string message)
    {
        var detailedResponse = await ChatWithDetailsAsync(message);
        return detailedResponse.Response;
    }

    public async Task<ChatWithDetailsResponse> ChatWithDetailsAsync(string message)
    {
        var response = new ChatWithDetailsResponse();
        var overallStartTime = DateTime.UtcNow;

        // Clear tool calls from previous request using base class method
        ClearToolCalls();

        try
        {
            Logger.LogInformation("Processing chat message with details: {Message}", message);

            // Get the kernel from brain
            var kernel = GetKernelFromBrain();
            if (kernel == null)
            {
                Logger.LogWarning("Kernel not available, falling back to base implementation");
                var fallbackHistory = await ChatWithHistory(message);
                response.Response = fallbackHistory?.LastOrDefault()?.Content ?? "No response generated.";
                response.TotalDurationMs = (long)(DateTime.UtcNow - overallStartTime).TotalMilliseconds;
                return response;
            }

            // Get chat completion service from kernel
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // Create chat history
            var chatHistory = new ChatHistory();

            // Add system message
            var systemMessage = State.PromptTemplate ??
                                "You are a helpful AI assistant with access to various tools through MCP (Model Context Protocol). " +
                                "When asked to perform tasks, use the available tools to help provide accurate and complete responses. " +
                                "Always explain what tools you're using and why. " +
                                "When using tools, be clear about the results and how they help answer the user's question.";

            chatHistory.AddSystemMessage(systemMessage);

            // Add user message
            chatHistory.AddUserMessage(message);

            Logger.LogInformation("Available tools in kernel: {Tools}",
                string.Join(", ", kernel.Plugins.SelectMany(p => p.Select(f => f.Name))));

            // Configure execution settings for automatic tool calling
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions, // Auto-invoke tools
                Temperature = 0.1,
                MaxTokens = 2000
            };

            // Get response with automatic tool invocation
            Logger.LogInformation("[{Timestamp}] Starting LLM call with auto tool invocation",
                DateTime.UtcNow.ToString("HH:mm:ss.fff"));

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var chatResponse = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                kernel,
                cts.Token);

            response.Response = chatResponse.Content ?? "I couldn't generate a response.";

            // Copy collected tool calls from base class tracking
            response.ToolCalls = new List<ToolCallDetail>(CurrentToolCalls);

            response.TotalDurationMs = (long)(DateTime.UtcNow - overallStartTime).TotalMilliseconds;

            Logger.LogInformation("[{Timestamp}] Chat completed with {ToolCount} tool calls in {Duration}ms",
                DateTime.UtcNow.ToString("HH:mm:ss.fff"),
                response.ToolCalls.Count,
                response.TotalDurationMs);

            return response;
        }
        catch (TaskCanceledException)
        {
            var timeoutDuration = DateTime.UtcNow - overallStartTime;
            Logger.LogError("Chat completion timed out after {Duration}ms", timeoutDuration.TotalMilliseconds);
            response.Response = $"Request timed out after {timeoutDuration.TotalSeconds:F1} seconds. Please try again.";
            response.TotalDurationMs = (long)timeoutDuration.TotalMilliseconds;
            return response;
        }
        catch (Exception ex) when (IsNetworkRelatedError(ex))
        {
            Logger.LogError(ex, "Network error during chat completion");
            response.Response = "⚠️ Network connection error. Please check your internet connection and try again.";
            response.TotalDurationMs = (long)(DateTime.UtcNow - overallStartTime).TotalMilliseconds;
            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during chat with details");
            response.Response = $"Error: {ex.Message}";
            response.TotalDurationMs = (long)(DateTime.UtcNow - overallStartTime).TotalMilliseconds;
            return response;
        }
    }

    private bool IsNetworkRelatedError(Exception ex)
    {
        return ex is TaskCanceledException ||
               ex is HttpRequestException ||
               ex is IOException ||
               (ex.InnerException != null && IsNetworkRelatedError(ex.InnerException));
    }
}