using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Aevatar.GAgents.PsiOmni.Interfaces;
using Aevatar.GAgents.PsiOmni.Models;
using Aevatar.GAgents.AI.Common;
using GroupChat.GAgent;
using JsonConverter = Newtonsoft.Json.JsonConvert;

namespace Aevatar.GAgents.PsiOmni;

public interface IPshOmniGAgent : IStateGAgent<PsiOmniGAgentState>;

[GAgent("omni", "psi")]
public partial class
    PsiOmniGAgent : GroupMemberGAgentBase<PsiOmniGAgentState, PsiOmniGAgentStateLogEvent, EventBase, PsiOmniGAgentConfig>, IPshOmniGAgent
{
    private static readonly Dictionary<RealizationStatus, string> SystemPrompts =
        new()
        {
            [RealizationStatus.Unrealized] = """
                                             You are a professional analyst that analyzes the task given by the user. You help an
                                             agent to decide if it will operate in ORCHESTRATOR or SPECIALIZED mode.

                                             ## ORCHESTRATOR MODE
                                             - The agent will not perform any specific task. It will break down the task into sub-tasks and create child agents to handle the sub-tasks.
                                             - The child agents can be re-used to perform similar sub-tasks.
                                             - A root agent (with depth value 0) should always operate in ORCHESTRATOR mode.

                                             ## SPECIALIZED MODE
                                             - The agent will perform a specific task. It will use the tools given to it to perform the task.
                                             - The agent will not create child agents.

                                             ## Output Format
                                             - Output a JSON object with the following fields:
                                                - "OperationMode": "ORCHESTRATOR" or "SPECIALIZED"
                                                - "Description": a description of the agent can do. For SPECIALIZED agents: 1) Include the agent's capability derived from the selected tools. 2) DO NOT directly include the task without generalization.
                                                - "Tools": a list of names of the tools the agent will use (only for SPECIALIZED mode)
                                             - No other text or explanation.
                                             """,
            [RealizationStatus.Orchestrator] = """
                                               You are a smart orchestrator agent that can interact with the user, analyze the user's request,
                                               break down the request into sub-tasks and delegate the sub-tasks to child agents.

                                               ## General Rules
                                               - Always analyze the user's request and break it down into sub-tasks.
                                               - Perform the task by delegating sub-tasks to child agents.
                                               - Make sure to consider existing child agents before deciding to delegate a sub-task.
                                               - Decide suitable child agents based on their description and tools used.
                                               - Only use tools for interacting with child agents and no other tools.
                                               - Never send the original task to another agent unless it's a simple task that is suitable for an existing agent.

                                               ## Guidelines for Task Breakdown and Delegation
                                               - Apply separation of concerns. An agent should be responsible for one type of task.
                                               - When you send the first task to a new agent, the agent is created upon receiving the task, you don't need to send the task in another tool call. IMPORTANT: Always try to find an existing agent that is suitable for a task first.
                                               - New agent is required if and only if a new category of subtasks is discovered.

                                               ## Minimize interaction with the user
                                               - Don't be verbose and keep asking for confirmation from the user.
                                               - Apply your best judgement to create agents without asking for permission.

                                               ## Output Format
                                               - Output a JSON object with the following fields:
                                                  - "Intermediate": the intermediate result of the agent.
                                                  - "Final": the final result of the agent.
                                               - Either "Intermediate" or "Final" must be present, not both.
                                               - If the task is not finished, you should output "Intermediate" with the intermediate result.
                                               - If the task is finished, you should output "Final" with the final result.

                                               ## Example Outputs
                                               {
                                                 "Intermediate": "There are 22 people in the room and we have 2 cakes. We need to divide the cakes evenly.",
                                                 "Final": "We have 11 people and 1 cake each."
                                               }
                                               {
                                                 "Intermediate": "I received the GDP of the United States for 2024 which is $x trillion. Awaiting the GDP of New York state for 2024 before I can calculate the percentage contribution of New York state to the US GDP."
                                               }
                                               {
                                                 "Final": "The GDP of the United States for 2024 is $x trillion, and the GDP of New York state for 2024 is $y trillion. The percentage contribution of New York state to the US GDP is approximately z%."
                                               }

                                               """,
            [RealizationStatus.Specialized] = "" // TODO:
        };

    private const string IntrospectorSystemPrompt = """
                                                    You are an agent manager that understands the capabilities of the agents.

                                                    ## Task
                                                    - You are trying to understand the capabilities of an agent that works as an orchestrator and delegates its agent.
                                                    - Derive the capabilities of the agent from the capabilities of the child agents.
                                                    - Prepare a description of the agent's capabilities.
                                                    - Understand the category of tasks the agent can handle.
                                                    - Avoid putting specific tasks in the description.
                                                    """;

    private readonly IKernelFactory _kernelFactory;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly HashSet<string> _receivedMessageIds = new HashSet<string>();

    public PsiOmniGAgent(
        IKernelFactory kernelFactory,
        IGAgentFactory gAgentFactory
    )
    {
        _kernelFactory = kernelFactory;
        _gAgentFactory = gAgentFactory;
    }

    protected override async Task PerformConfigAsync(PsiOmniGAgentConfig configuration)
    {
        // First, call the base class method
        await base.PerformConfigAsync(configuration);
        
        RaiseEvent(new SetDepthEvent()
        {
            Depth = configuration.Depth
        });
        await ConfirmEvents();
        
        // Note: We don't initialize Brain here to maintain backward compatibility.
        // Brain initialization should be done explicitly if needed.
        // The agent will use IKernelFactory by default.
    }

    public override Task<string> GetDescriptionAsync()
    {
        var descriptionInfo = new AgentDescriptionInfo
        {
            Id = "PsiOmniGAgent",
            Name = "PsiOmni Integration Agent",
            L1Description = "AI agent for PsiOmni platform integration with advanced cognitive capabilities",
            L2Description = "Sophisticated PsiOmni platform agent that provides advanced AI cognitive services, neural network processing, and intelligent automation capabilities for complex problem-solving scenarios.",
            Category = "AI",
            Capabilities = new List<string> { "cognitive-services", "neural-processing", "intelligent-automation", "complex-problem-solving" },
            Tags = new List<string> { "psiomni", "cognitive", "ai", "automation" }
        };
        return Task.FromResult(JsonConverter.SerializeObject(descriptionInfo));
    }

    private async Task DoSelfReportAsync()
    {
        if (State.UserAgentId.IsNullOrEmpty())
        {
            // Do Nothing
            return;
        }

        await PublishAsync(GrainId.Parse(State.UserAgentId), new SelfReportEvent
        {
            TargetAgentId = State.UserAgentId,
            SelfReport = new AgentDescriptor
            {
                AgentId = State.AgentId,
                AgentType = State.RealizationStatus == RealizationStatus.Orchestrator ? "orchestrator" : "specialized",
                Description = State.Description,
                Examples = State.Examples,
                Tools = State.Tools
            }
        });
    }

    private async Task RunAsync(string trigger = null)
    {
        if (!InitializedOk())
        {
            Logger.LogWarning("PsiOmniGAgent is not initialized properly. Skipping run.");
            // Do nothing
            return;
        }
        
        Logger.LogInformation("Running PsiOmniGAgent with trigger: {Trigger}", trigger);

        Kernel kernel;
        ChatHistory chatHistory;
        int preHistoryLength;
        var systemPrompt = SystemPrompts[State.RealizationStatus];
        Logger.LogInformation("Status: {RealizationStatus}", State.RealizationStatus);
        Logger.LogInformation("Prompt: {systemPrompt}", systemPrompt);
        switch (State.RealizationStatus)
        {
            case RealizationStatus.Unrealized:
                kernel = GetKernel_Analyzer();
                systemPrompt += $"\n\n## Depth Value\n<depth>{State.Depth}</depth>";
                systemPrompt += $"\n\n## Available Tools:\n{GetAllToolDefinitions()}";
                (chatHistory, preHistoryLength) = await RunCoreAsync(kernel, systemPrompt);
                OnChatDoneAsync_Analyzer(chatHistory, preHistoryLength);
                break;
            case RealizationStatus.Orchestrator:
                kernel = GetKernel_Orchestrator();
                if (kernel == null)
                {
                    Logger.LogWarning("Cannot get kernel for Orchestrator mode, skipping run");
                    return;
                }
                systemPrompt += $"\n\n## Existing Child Agents (Try your best to re-use them):\n{GetAllChildAgents()}";
                systemPrompt += $"\n\nYour agent Id is: <agentId>{this.GetGrainId()}</agentId>";
                (chatHistory, preHistoryLength) = await RunCoreAsync(kernel, systemPrompt);
                OnChatDoneAsync_Orchestrator(chatHistory, preHistoryLength);
                break;
            case RealizationStatus.Specialized:
                kernel = GetKernel_Specialized();
                if (kernel == null)
                {
                    Logger.LogWarning("Cannot get kernel for Specialized mode, skipping run");
                    return;
                }
                (chatHistory, preHistoryLength) = await RunCoreAsync(kernel, systemPrompt);
                OnChatDoneAsync_Specialized(chatHistory, preHistoryLength);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private bool InitializedOk()
    {
        if (State.ChatHistory.IsNullOrEmpty())
        {
            Logger.LogInformation("ChatHistory is empty.");
            return false;
        }

        if (State.Configuration == null)
        {
            Logger.LogInformation("Configuration is empty.");
            return false;
        }

        return true;
    }

    private ChatHistory GetChatHistory(string systemPrompt)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        var messages =
            State.ChatHistory.Select(message => message.ToSkMessage());
        chatHistory.AddRange(messages);

        return chatHistory;
    }

    private async Task<(ChatHistory, int)> RunCoreAsync(Kernel kernel, string systemPrompt)
    {
        try
        { 
            Logger.LogInformation("RunCoreAsync.1");
            // 1. Get chat completion service
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            Logger.LogInformation("RunCoreAsync.2");
            // 2. Construct PromptExecutionSettings
            var maxTokens = 4000; // Default max tokens
            var temperature = 0.1; // Default temperature
            // Use OpenAI version only (no config.Model check)
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                MaxTokens = maxTokens,
                Temperature = temperature
            };
            Logger.LogInformation("RunCoreAsync.3");

            var chatHistory = GetChatHistory(systemPrompt);
            var preChatHistoryLength = chatHistory.Count;
            Logger.LogInformation("preChatHistoryLength: {Count}", preChatHistoryLength);
            var result = await chatService.GetChatMessageContentAsync(chatHistory, executionSettings, kernel);
            chatHistory.Add(result);
            Logger.LogInformation("RunCoreAsync.4");
            return (chatHistory, preChatHistoryLength);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error during RunCoreAsync: {Message}", e.Message);
            throw;
        }
    }

    private async Task ReplyAsync(string finalResult)
    {
        if (State.UserAgentId.IsNullOrEmpty())
        {
            Logger.LogInformation("Result:\n{Result}", State.ChatHistory.Last()?.Content);
            return;
        }

        await PublishAsync(GrainId.Parse(State.UserAgentId), new AgentMessageEvent
        {
            TargetAgentId = State.UserAgentId,
            CallId = State.CallId,
            Content = finalResult
        });
    }

    protected override void AIGAgentTransitionState(
        PsiOmniGAgentState state,
        StateLogEventBase<PsiOmniGAgentStateLogEvent> @event
    )
    {
        if (@event is PsiOmniGAgentStateLogEvent e1)
        {
            var uid = e1.UniqueId;
            if (!_receivedMessageIds.Add(uid))
            {
                return;
            }
        }

        switch (@event)
        {
            case SetDepthEvent payload:
                state.Depth = payload.Depth;
                break;
            case UpdateSendConfigEvent payload:
                if (state.AgentId.IsNullOrEmpty())
                {
                    var grainId = this.GetGrainId().ToString();
                    var config = payload.Event.Configuration;
                    state.AgentId = grainId;
                    state.UserAgentId = payload.Event.ParentAgentId;
                    state.Configuration = config;
                    // state.Tools = payload.Event.Tools; // Not needed here. No tools should be configured here.
                }

                break;
            case ReceiveUserMessageEvent payload:

                Logger.LogInformation("StateTransition for ReceiveUserMessageEvent");
                if (!payload.Event.CallId.IsNullOrEmpty())
                {
                    state.CallId = payload.Event.CallId;
                }

                if (!payload.Event.Content.IsNullOrEmpty())
                {
                    state.UserAgentId = payload.Event.ReplyToAgentId;
                    var message = PsiOmniChatMessage.CreateUserMessage(payload.Event.Content);
                    message.Metadata["CallId"] = payload.Event.CallId;
                    state.ChatHistory.Add(message);
                    state.Examples.Add(new AgentExample
                    {
                        Request = payload.Event.Content,
                        Response = String.Empty
                    });
                    ScheduleTask(async () => await RunAsync($"User Message {payload.Event}"));
                }

                break;
            case RealizationEvent payload:
                if (state.RealizationStatus == RealizationStatus.Unrealized)
                {
                    state.RealizationStatus = payload.RealizationStatus;
                    state.Description = payload.Description;
                    state.Tools = payload.Tools;
                }

                ScheduleTask(async () =>
                {
                    await DoSelfReportAsync();
                    await RunAsync("RealizationEvent");
                });
                break;
            case ReceiveAgentMessageEvent payload:
                var amessage =
                    PsiOmniChatMessage.CreateAssistantMessage(
                        $"Received reply for callId ({payload.Event.CallId}): {payload.Event.Content}");
                amessage.Metadata["CallId"] = payload.Event.CallId;
                state.ChatHistory.Add(amessage);
                ScheduleTask(async () => await RunAsync($"Agent Message {payload.Event}"));
                break;
            case NewAgentsCreatedEvent payload:
                foreach (var newAgent in payload.NewAgents)
                {
                    state.ChildAgents.TryAdd(newAgent.AgentId, newAgent);
                }

                var newAgentIds = payload.NewAgents.Select(x => x.AgentId);

                ScheduleTask(async () =>
                {
                    foreach (var newAgent in newAgentIds)
                    {
                        var child = GrainFactory.GetGrain<IGAgent>(GrainId.Parse(newAgent));
                        await RegisterAsync(child);
                    }
                });

                break;
            case UpdateChildEvent payload:
                AgentDescriptor? oldObj;
                // Child may proceed first and we receive this event before we process our own NewAgentsCreatedEvent event
                if (!state.ChildAgents.TryGetValue(payload.LastChildDescriptor.AgentId, out oldObj))
                {
                    oldObj = new AgentDescriptor()
                    {
                        AgentId = payload.LastChildDescriptor.AgentId
                    };
                    state.ChildAgents[payload.LastChildDescriptor.AgentId] = oldObj;
                }

                var oldObjClone = oldObj.DeepClone();
                var newObjClone = payload.LastChildDescriptor.DeepClone();
                oldObjClone.Examples = new List<AgentExample>();
                newObjClone.Examples = new List<AgentExample>();
                var refreshDescription = !oldObjClone.Equals(newObjClone);

                state.ChildAgents[payload.LastChildDescriptor.AgentId] = payload.LastChildDescriptor;
                ScheduleTask(async () =>
                {
                    if (refreshDescription)
                    {
                        await RunIntrospectionAsync();
                    }
                });
                break;
            case GrowChatHistoryEvent payload:
                state.ChatHistory.AddRange(payload.NewMessages);
                if (state.ChatHistory.Count <= 1)
                    break;
                var finalResult = string.Empty;

                if (state.RealizationStatus == RealizationStatus.Specialized)
                {
                    finalResult = State.ChatHistory.Last().Content;
                }
                else if (state.RealizationStatus == RealizationStatus.Orchestrator)
                {
                    var lastMessage = state.ChatHistory.Last()?.Content ?? string.Empty;
                    try
                    {
                        var lastOrchestratorMessage = JsonSerializer.Deserialize<OrchestratorMessage>(lastMessage);
                        if (lastOrchestratorMessage != null && !lastOrchestratorMessage.Final.IsNullOrEmpty())
                        {
                            finalResult = lastOrchestratorMessage.Final;
                        }
                    }
                    catch (Exception ex)
                    {
                        finalResult = lastMessage;
                        Logger.LogError(ex, "Failed to deserialize OrchestratorMessage: {Message}", lastMessage);
                    }
                }

                if (!finalResult.IsNullOrEmpty())
                {
                    state.Examples.Last().Response = finalResult;
                    ScheduleTask(async () =>
                    {
                        // TODO: Maybe update description.
                        await DoSelfReportAsync();
                        await ReplyAsync(finalResult);
                    });
                }

                break;
            case UpdateSelfDescription payload:
                state.Description = payload.Description;
                ScheduleTask(DoSelfReportAsync);
                break;
        }
    }
}