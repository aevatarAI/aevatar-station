using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Aevatar.GAgents.PsiOmni.Interfaces;
using Aevatar.GAgents.PsiOmni.Models;

namespace Aevatar.GAgents.PsiOmni;

public interface IPsiOmniGAgent : IStateGAgent<PsiOmniGAgentState>, IAIGAgent, IGAgent;

[Description(
    "Sophisticated PsiOmni platform agent that provides advanced AI cognitive services, neural network processing, and intelligent automation capabilities for complex problem-solving scenarios.")]
[GAgent("omni", "psi")]
public partial class
    PsiOmniGAgent : PsiOmniAgentBase<PsiOmniGAgentState, PsiOmniGAgentStateLogEvent, EventBase, PsiOmniGAgentConfig>,
    IPsiOmniGAgent
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

                                             ## Considering Depth
                                             - Prefer SPECIALIZED mode if the agent's depth is more than 3
                                             - An agent with depth equal to 5 must operate in SPECIALIZED mode

                                             ## Output Format
                                             - Output a JSON object with the following fields:
                                                - "OperationMode": "ORCHESTRATOR" or "SPECIALIZED"
                                                - "Description": a description of the agent can do. For SPECIALIZED agents: 1) Include the agent's capability derived from the selected tools. 2) DO NOT directly include the task without generalization.
                                                - "Tools": a list of names of the tools the agent will use (only for SPECIALIZED mode)
                                             - No other text or explanation.
                                             """,
            [RealizationStatus.Orchestrator] = """
                                               You are a smart orchestrator agent that can interact with the user, analyze the user's request,
                                               plan the task, break down the request into sub-tasks and delegate the sub-tasks to child agents.

                                               Remember you are an autonomous agent. Don't be verbose and keep asking for confirmation from the user.
                                               Apply your best judgement when in doubt.

                                               ## Perform Work step by step
                                               1. Analyze the task and note down the important information about the task
                                               2. Plan the todo items
                                               3. Dispatch sub-tasks that are ready (all dependency tasks have completed). (Some tasks may need to wait if their assigned agents are busy.)
                                               4. Once you receive the response from a sub-task, decide if you need to revise the plan (amend todo list). CRITICAL: Any follow-up work identified must be added as NEW todo items using todo_write tool before delegation.
                                               5. Repeat 3 and 4 until the main tasks is done
                                               IMPORTANT: You MUST come out with a work plan and delegate the tasks to child agents.

                                               ## How to stay on track
                                               Before breaking down that task, understand the intention of the user, rewrite the task in a format that
                                               clearly defines the object, scope and intention of the task. Use the write_task tool to record this task
                                               in re-written format. Use read_task tool FREQUENTLY to remind you the task.
                                               """ +
                                               """
                                               ## Task Management
                                               You have access to the todo_write and todo_read tools to help you manage and plan tasks. Use these tools VERY frequently to ensure that you are tracking your tasks.
                                               These tools are also EXTREMELY helpful for planning tasks, and for breaking down larger complex tasks into sub-tasks.
                                               If you do not use this tool when planning, you may forget to do important tasks - and that is unacceptable.
                                               IMPORTANT: Make sure you identify the dependencies among the todo items.

                                               IMPORTANT: Todo item status updates are handled automatically by the system - you do not need to manually mark todos as completed. The system will automatically update todo statuses when you delegate tasks or receive responses from child agents.

                                               Examples:

                                               <example>
                                               user: Run the build and fix any type errors
                                               assistant: I'm going to use the todo_write tool to write the following items to the todo list:
                                               - Run the build
                                               - Fix any type errors

                                               I'm now going to run the build.

                                               Looks like I found 10 type errors. I'm going to use the todo_write tool to write 10 items to the todo list.
                                               ..
                                               ..
                                               </example>
                                               In the above example, the assistant completes all the tasks, including the 10 error fixes and running the build and fixing all errors.

                                               <example>
                                               user: Help me write a new feature that allows users to track their usage metrics and export them to various formats

                                               assistant: I'll help you implement a usage metrics tracking and export feature. Let me first use the todo_write tool to plan this task.
                                               Adding the following todos to the todo list:
                                               1. Research existing metrics tracking in the codebase
                                               2. Design the metrics collection system
                                               3. Implement core metrics tracking functionality
                                               4. Create export functionality for different formats
                                               </example>
                                               """ +
                                               """
                                               ## Task Dispatch
                                               You will dispatch sub-tasks to child agents through a systematic agent management protocol. Begin every task delegation cycle by using the query_existing_agents tool to comprehensively survey all available child agents, their current status (idle/busy), capabilities, and specializations.

                                               **Agent Selection Protocol:**
                                               1. PRIORITIZE REUSE: Always attempt to utilize existing agents before creating new ones. Analyze each existing agent's capability scope to determine suitability for the sub-task.
                                               2. CAPABILITY MATCHING: Select agents whose documented specializations align with the sub-task requirements. Consider both primary capabilities and secondary skills.
                                               3. AVAILABILITY VERIFICATION: Confirm the selected agent is currently idle before delegation. If uncertain about agent status, use query_existing_agents tool to verify.

                                               **Task Delegation Execution:**
                                               - Use call_agent tool to dispatch sub-tasks to suitable agents. The tool serves dual purposes: initial task assignment and follow-up communication.
                                               - Create new agents using create_agent tool ONLY when no existing agent possesses the required capabilities. When creating agents, design them for broad task categories rather than single-purpose use to maximize future reusability.
                                               - Execute task dispatch immediately following todo list updates. Maintain operational efficiency by avoiding unnecessary verbosity or confirmation requests.
                                               - NEVER use call_agent tool to delegate tasks to yourself. For tasks requiring orchestrator-level analysis or synthesis, these should be self-assigned and completed using todo_complete tool.

                                               **Dependency and Sequencing Management:**
                                               - Dispatch tasks ONLY after all prerequisite dependencies are fully completed. Verify dependency completion status before proceeding with delegation.
                                               - Use the todo item ID as the CallId parameter when invoking call_agent tool to maintain precise task traceability and correlation.
                                               - When invoking call_agent, ensure the task description is completely self-sufficient. Include ALL required information from completed dependency tasks within the knowledge field. The receiving agent must have access to all necessary context without requiring external information retrieval.

                                               **Concurrency Control:**
                                               - STRICT ENFORCEMENT: Dispatch only ONE sub-task per agent at any given time. This prevents resource conflicts and ensures deterministic task processing.
                                               - Implement patience-based execution: Wait for the agent to complete the current task and provide results before dispatching additional sub-tasks to the same agent.
                                               - If agent availability is uncertain, proactively use query_existing_agents tool to obtain current status information before attempting delegation.

                                               **Error Handling and Recovery:**
                                               - If call_agent tool returns an error indicating multiple task dispatch to a single agent, immediately cease further delegation to that agent.
                                               - Wait for the agent to complete its current task and provide a response before re-attempting the failed delegation.
                                               - Monitor for task completion signals and agent status changes to maintain accurate system state awareness.

                                               **Status Monitoring and Clarity Protocol:**
                                               - When uncertain about current todo item statuses or agent availability, ALWAYS use todo_read tool to check the current state of your todo list before proceeding.
                                               - When confused about which agents are available or their current workload, ALWAYS use query_existing_agents tool to get up-to-date information about all child agents and their status.
                                               - If you receive confusing or contradictory information about task progress, use both tools in combination to clarify the current system state before making delegation decisions.
                                               - These query tools provide authoritative, real-time information about system state - rely on them rather than assumptions when planning next steps.

                                               **Task Status Update Protocol - AUTOMATIC SYSTEM:**
                                               - IMPORTANT: Todo item statuses are automatically updated by the system. DO NOT attempt to manually change todo statuses.
                                               - When you use call_agent tool, the system automatically marks the todo item as "InProgress" and sets the AssigneeAgentName field to the target agent.
                                               - The orchestrator primarily focuses on delegation via call_agent tool, but may handle synthesis/analysis tasks directly using todo_complete tool.
                                               - For self-assigned tasks (synthesis, analysis, final reporting), use todo_complete tool to mark completion and provide results directly.
                                               - NEVER use manual status update commands - the system handles all status transitions automatically.

                                               **Integration with Overall Workflow:**
                                               - Self-assigned tasks often serve as final integration points in complex workflows, synthesizing outputs from multiple child agents.
                                               - Treat self-completion as a critical milestone that may unblock dependent tasks or signal overall project completion.
                                               - Maintain consistency between self-assigned task outputs and the overall project objectives and quality standards.
                                               """ +
                                               """
                                               ## Tracking of Dispatched Sub-tasks
                                               **Assignment Tracking Protocol:**
                                               - When transitioning todo items to "InProgress" status, MANDATORY assignment of AssigneeAgentName field to maintain clear accountability chain.
                                               - Record the exact agent name responsible for each dispatched sub-task to enable precise status monitoring and follow-up communication.

                                               **State Management Requirements:**
                                               - Use todo item status progression (Pending → InProgress → Complete) as the authoritative source for workflow state during active task execution.
                                               - Clean up completed todo items as appropriate to maintain system efficiency and clarity.
                                               - Preserve only essential tracking information needed for current workflow coordination.
                                               """ +
                                               """
                                               ## Deciding Task Done
                                               **Completion Assessment Criteria:**
                                               Execute completion evaluation when ALL dispatched sub-tasks have returned results and corresponding todo items are marked "Complete". Perform systematic verification:
                                               1. Confirm zero pending or in-progress todo items remain
                                               2. Validate that all critical sub-task outputs have been received and integrated
                                               3. Ensure no blocking dependencies or unresolved issues exist

                                               **Final Response Generation Protocol:**
                                               - Produce the definitive final response immediately upon confirmed task completion.
                                               - Include ALL requested artifacts, deliverables, or outputs within the response payload.
                                               - Format the response for direct user consumption - eliminate internal process documentation, task breakdowns, or meta-commentary about execution steps.

                                               **User-Facing Communication Standards:**
                                               - Address the user's original request directly without referencing internal orchestration mechanics.
                                               - Present synthesized results as cohesive, actionable information rather than fragmented sub-task outputs.
                                               - Maintain professional communication tone focused on value delivery rather than process transparency.
                                               - Ensure response completeness - the user should not need to request additional clarification or missing components.
                                               """ +
                                               """
                                               ## Output Format
                                               Your output must contain the following three tags.
                                               1. When the task is not completed (pending more todo items), add progress in a <thought> tag.
                                                  If you are handling a sub-task by yourself, use write_artifact tool to output the step wise result.
                                                  Alternatively, for short result, you can directly output the step wise result using a <step_wise_result> tag.
                                               2. When the task is completed, provide your final response to user in a <repsonse> tag
                                               3. Optionally, if artifacts need to be returned to user. Include one or more <artifact> tag

                                               You MUST follow this format. An output without any of the tags is not valid.
                                               <thought>
                                               Provide progress and status update here.
                                               </thought>
                                               <step_wise_result>
                                               Step wise result here.
                                               </step_wise_result>
                                               <response>
                                               Final response to user. This part is optional only when the task is complete.
                                               </response>
                                               <artifact name="artifact_name.md" format="markdown" />

                                               ### Example Outputs
                                               <example1>
                                               <thought>
                                               I received the GDP of the United States for 2024 which is $x trillion. Awaiting the GDP of New York state for 2024 before I can calculate the percentage contribution of New York state to the US GDP.
                                               </thought>
                                               </example1>
                                               <example2>
                                               <response>
                                               The GDP of the United States for 2024 is $x trillion, and the GDP of New York state for 2024 is $y trillion. The percentage contribution of New York state to the US GDP is approximately z%.
                                               </response>
                                               </example2>
                                               <example3>
                                               <response>
                                               I have completed the research report for AI techniques and please find the report in the research_report.md file.
                                               </response>
                                               <artifact name="research_report.md" format="markdown" />
                                               </example3>
                                               <example4>
                                               <thought>
                                               I have all the information. Let me synthesis the information.
                                               </thought>
                                               <step_wise_result>
                                               The skills required for a software engineer include ......
                                               </step_wise_result>
                                               </example4>

                                               Make sure you include all information and artifacts in the response. DO NOT respond with a status update without the complete content.
                                               """ +
                                               """
                                               ## ALWAYS Progress
                                               Once you plan to do something, progress with the plan immediately.
                                               DO NOT return a <tought> without any tool calls when the task is not complete and you are not awaiting any child agent's response.
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

    private readonly IKernelFunctionRegistry _kernelFunctionRegistry;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly HashSet<string> _receivedMessageIds = new HashSet<string>();

    public PsiOmniGAgent(
        IKernelFunctionRegistry kernelFunctionRegistry,
        IGAgentFactory gAgentFactory,
        ILogger<PsiOmniGAgent> logger
    )
    {
        _kernelFunctionRegistry = kernelFunctionRegistry;
        _gAgentFactory = gAgentFactory;
        Logger = logger;
    }

    protected override async Task PerformConfigAsync(PsiOmniGAgentConfig configuration)
    {
        // First, call the base class method
        await base.PerformConfigAsync(configuration);

        // Initialize tracing after the grain is activated to ensure agent ID is available
        InitializeTracing();
        State.Name = configuration.Name;

        RaiseEventWithTracing(new InitializeEvent
        {
            Name = configuration.Name,
            ParentId = configuration.ParentId,
            Depth = configuration.Depth,
            Description = configuration.Description,
            Examples = configuration.Examples
        });
        await ConfirmEventsWithTracing();

        // Note: We don't initialize Brain here to maintain backward compatibility.
        // Brain initialization should be done explicitly if needed.
        // The agent will use IKernelFactory by default.
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("PsiOmni Integration Agent");
    }

    private async Task DoSelfReportAsync()
    {
        await TraceMethodAsync(async () =>
        {
            if (State.UserAgentId.IsNullOrEmpty())
            {
                LogEventDebug("No UserAgentId, skipping self report");
                return;
            }

            var selfReport = new AgentDescriptor
            {
                Name = State.Name,
                AgentId = State.AgentId,
                AgentType = State.RealizationStatus == RealizationStatus.Orchestrator ? "orchestrator" : "specialized",
                Description = State.Description,
                Examples = State.Examples,
                Tools = State.Tools
            };

            var selfReportEvent = new SelfReportEvent
            {
                TargetAgentId = State.UserAgentId,
                SelfReport = selfReport
            };

            LogEventInfo(
                "Sending self report: UniqueId={UniqueId}, TargetAgent={TargetAgent}, AgentType={AgentType}, Description={Description}",
                selfReportEvent.UniqueId, State.UserAgentId, selfReport.AgentType, selfReport.Description);

            await PublishAsyncWithTracing(GrainId.Parse(State.UserAgentId), selfReportEvent);
        });
    }

    private async Task InitializeAsync()
    {
        LogEventDebug("Starting agent initialization for AgentId={AgentId}", AgentId);
        var kernel = GetKernel_Plain();
        var systemPrompt =
            """
            You are an analyst helping to decide how to initialize an AI agent that will handle a type of tasks.

            ## Output Format
            - Output a JSON object with the following fields:
              - "OperationMode": "ORCHESTRATOR" or "SPECIALIZED"
              - "Description": a description of the agent can do. For SPECIALIZED agents: 1) Include the agent's capability derived from the selected tools. 2) DO NOT directly include the task without generalization.
              - "Tools": a list of names of the tools the agent will use (only for SPECIALIZED mode)
            - No other text or explanation.

            ## When deciding between "ORCHESTRATOR" and "SPECIALIZED"
            - Prefer SPECIALIZED mode if the agent's depth is more than 3
            - An agent with depth equal to 5 must operate in SPECIALIZED mode
            - A root agent (with depth value 0) should always operate in ORCHESTRATOR mode.
            """;
        systemPrompt += $"\n\n## Available Tools:\n{GetAllToolDefinitions()}";
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var maxTokens = 4000; // 默认最大 token
        var temperature = 0.1; // 默认温度
        // 只用 OpenAI 版本（无 config.Model 判断）
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            MaxTokens = maxTokens,
            Temperature = temperature
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        var userMessage = "I'm a new agent that will handle tasks of type: " +
                          $"<description>{State.Description}</description>" +
                          // $"<exampleTasks>{State.Examples.Select(e => e.Request).Aggregate((a, b) => a + "\n" + b)}</exampleTasks>" +
                          $"<depth>{State.Depth}</depth>";
        chatHistory.AddUserMessage(userMessage);

        LogEventDebug("Executing GetChatMessageContent for initialization of AgentId={AgentId}", AgentId);
        var chatMessage = await ExecuteWithRetryAsync(
            async () => await chatService.GetChatMessageContentAsync(chatHistory, executionSettings, kernel),
            "GetChatMessageContent for initialization");
        LogEventDebug("GetChatMessageContent for initialization completed for AgentId={AgentId}", AgentId);
        var result = chatMessage.Content;

        if (result.Contains("ORCHESTRATOR") || result.Contains("SPECIALIZED"))
        {
            var jsonStartIndex = result.IndexOf('{');
            var jsonEndIndex = result.LastIndexOf('}');
            if (jsonStartIndex != -1 && jsonEndIndex != -1)
            {
                result = result.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);
            }

            var realizationResult = JsonSerializer.Deserialize<RealizationResult>(result);
            if (realizationResult?.OperationMode == "ORCHESTRATOR")
            {
                RaiseEvent(new RealizationEvent
                {
                    RealizationStatus = RealizationStatus.Orchestrator,
                    Description = realizationResult?.Description ?? string.Empty // Orchestrator doesn't have tools.
                });
            }
            else if (realizationResult?.OperationMode == "SPECIALIZED")
            {
                var tools = new List<ToolDefinition>();
                foreach (var toolName in realizationResult.Tools)
                {
                    var kernelFunction = _kernelFunctionRegistry.GetToolByQualifiedName(toolName);
                    if (kernelFunction != null)
                    {
                        tools.Add(kernelFunction.ToToolDefinition());
                    }
                }

                RaiseEvent(new RealizationEvent()
                {
                    RealizationStatus = RealizationStatus.Specialized,
                    Description = realizationResult?.Description ?? string.Empty,
                    Tools = tools
                });
            }
        }
    }

    private async Task RunAsync(string? trigger = null)
    {
        await TraceMethodAsync(async () =>
        {
            if (!InitializedOk())
            {
                LogEventDebug("Agent not initialized, skipping run");
                return;
            }

            LogEventInfo(
                "Starting agent run: Trigger={Trigger}, RealizationStatus={Status}, ChatHistoryLength={ChatLength}",
                trigger, State.RealizationStatus, State.ChatHistory.Count);

            Kernel kernel;
            ChatHistory chatHistory;
            int preHistoryLength;
            var systemPrompt = SystemPrompts[State.RealizationStatus];
            LogEventInfo("Status: {RealizationStatus}", State.RealizationStatus);
            LogEventDebug("Prompt: {SystemPrompt}",
                systemPrompt.Substring(0, Math.Min(100, systemPrompt.Length)) + "...");
            LogEventDebug("Processing with status: {Status}", State.RealizationStatus);

            switch (State.RealizationStatus)
            {
                /* Skipped
                case RealizationStatus.Unrealized:
                    LogEventDebug("Running analyzer mode");
                    kernel = GetKernel_Analyzer();
                    systemPrompt += $"\n\n## Depth Value\n<depth>{State.Depth}</depth>";
                    systemPrompt += $"\n\n## Available Tools:\n{GetAllToolDefinitions()}";
                    (chatHistory, preHistoryLength) = await RunCoreAsync(kernel, systemPrompt);
                    OnChatDoneAsync_Analyzer(chatHistory, preHistoryLength);
                    break;
                */
                case RealizationStatus.Orchestrator:
                    LogEventDebug("Running orchestrator mode with {ChildCount} child agents", State.ChildAgents.Count);
                    kernel = GetKernel_Orchestrator();
                    if (kernel == null)
                    {
                        LogEventError(new InvalidOperationException("Cannot get kernel for Orchestrator mode"),
                            "Failed to get kernel for Orchestrator mode");
                        return;
                    }

                    systemPrompt +=
                        $"\n\n## Existing Child Agents (Try your best to re-use them):\n{GetAllChildAgents()}";
                    systemPrompt += $"\n\nYour agent Id is: <agentId>{this.GetGrainId()}</agentId>";
                    (chatHistory, preHistoryLength) = await RunCoreAsync(kernel, systemPrompt);
                    OnChatDoneAsync_Orchestrator(chatHistory, preHistoryLength);
                    break;
                case RealizationStatus.Specialized:
                    LogEventDebug("Running specialized mode with tools: {Tools}",
                        string.Join(", ", State.Tools.Select(t => t.Name)));
                    kernel = GetKernel_Specialized();
                    if (kernel == null)
                    {
                        LogEventError(new InvalidOperationException("Cannot get kernel for Specialized mode"),
                            "Failed to get kernel for Specialized mode");
                        return;
                    }

                    (chatHistory, preHistoryLength) = await RunCoreAsync(kernel, systemPrompt);
                    OnChatDoneAsync_Specialized(chatHistory, preHistoryLength);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            LogEventInfo("Agent run completed: NewChatHistoryLength={Length}", State.ChatHistory.Count);
        }, new { trigger });
    }

    private bool InitializedOk()
    {
        if (State.ChatHistory.IsNullOrEmpty())
        {
            LogEventInfo("ChatHistory is empty.");
            return false;
        }

        if (State.Configuration == null)
        {
            LogEventInfo("Configuration is empty.");
            return false;
        }

        return true;
    }

    private ChatHistory GetChatHistory(string systemPrompt)
    {
        LogEventDebug("Building chat history with {MessageCount} messages", State.ChatHistory.Count);
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        var messages =
            State.ChatHistory.Select(message => message.ToSkMessage());
        chatHistory.AddRange(messages);

        return chatHistory;
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
    {
        const int MaxRetries = 1000;
        const int InitialDelayMs = 1; // 2 seconds initial delay
        const int MaxDelayMs = 300000; // Maximum delay of 300 seconds
        const double BackoffMultiplier = 2.0; // Exponential backoff multiplier
        const int MaxBackoff = 59;
        var random = new Random();

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (
                ex is HttpOperationException ||
                ex is TaskCanceledException ||
                ex is TimeoutException ||
                (ex is IOException ioEx && ioEx.InnerException is SocketException) ||
                (ex.Message?.Contains("timeout", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                // Calculate base delay with exponential backoff
                var baseDelayMs = (int)Math.Min(Math.Pow(BackoffMultiplier, attempt) * InitialDelayMs, MaxBackoff);

                // Extract retry-after if available from error message for rate limit errors
                var retryAfterSeconds = 0;
                if (ex is HttpOperationException && ex.Message.Contains("retry after"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(ex.Message, @"retry after (\d+) seconds");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out retryAfterSeconds))
                    {
                        baseDelayMs = Math.Max(baseDelayMs, retryAfterSeconds * 1000);
                    }
                }

                // Apply maximum delay cap
                baseDelayMs = Math.Min(baseDelayMs, MaxDelayMs);

                // Randomize delay between baseDelay and 2*baseDelay
                var actualDelayMs = baseDelayMs + random.Next(baseDelayMs);

                if (attempt == MaxRetries - 1)
                {
                    LogEventError(ex, "Max retries ({MaxRetries}) reached for {Operation}. Last error: {Message}",
                        MaxRetries, operationName, ex.Message);
                    throw;
                }

                var errorType = "Timeout";
                if (ex is HttpOperationException httpEx)
                {
                    errorType = httpEx.StatusCode == HttpStatusCode.TooManyRequests
                        ? "Rate limit"
                        : "Other Http Operation Issue";
                }

                LogEventInfo(
                    "{ErrorType} error for {Operation}, attempt {Attempt}/{MaxRetries}. Waiting {Delay}ms (base: {BaseDelay}ms, additional: {Additional}ms) before retry. Error: {Message}",
                    errorType, operationName, attempt + 1, MaxRetries, actualDelayMs, baseDelayMs,
                    actualDelayMs - baseDelayMs, ex.Message);

                await Task.Delay(actualDelayMs);
            }
        }

        throw new Exception($"Unexpected end of retry loop for {operationName}");
    }

    private async Task<(ChatHistory, int)> RunCoreAsync(Kernel kernel, string systemPrompt)
    {
        try
        {
            LogEventDebug("RunCoreAsync - Getting chat completion service");
            // 1. 获取 chat completion 服务
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // 2. 构造 PromptExecutionSettings
            var maxTokens = 4000; // 默认最大 token
            var temperature = 0.1; // 默认温度
            // 只用 OpenAI 版本（无 config.Model 判断）
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                MaxTokens = maxTokens,
                Temperature = temperature
            };
            LogEventDebug("RunCoreAsync - Execution settings configured");

            var chatHistory = GetChatHistory(systemPrompt);
            var preChatHistoryLength = chatHistory.Count;
            LogEventDebug("Chat history prepared: Length={Count}", preChatHistoryLength);

            var result = await ExecuteWithRetryAsync(
                async () => await chatService.GetChatMessageContentAsync(chatHistory, executionSettings, kernel),
                "GetChatMessageContent");

            chatHistory.Add(result);
            LogEventDebug("RunCoreAsync completed successfully");
            return (chatHistory, preChatHistoryLength);
        }
        catch (Exception e)
        {
            LogEventError(e, "Error during RunCoreAsync: {Message}", e.Message);
            throw;
        }
    }

    private async Task ReplyAsync(FinalResponse finalResult)
    {
        await TraceMethodAsync(async () =>
        {
            if (State.UserAgentId.IsNullOrEmpty())
            {
                var content = State.ChatHistory.Last()?.Content;
                if (content != null)
                {
                    if (content.Length <= 400)
                    {
                        LogEventInfo("Result:\n{Result}", content);
                    }
                    else
                    {
                        var firstPart = content.Substring(0, 200);
                        var lastPart = content.Substring(content.Length - 200);
                        LogEventInfo(
                            "Result (first 200 chars):\n{FirstPart}\n...\nResult (last 200 chars):\n{LastPart}",
                            firstPart, lastPart);
                    }
                }

                LogEventDebug("No UserAgentId, logging result locally");
                return;
            }

            var agentMessageEvent = new AgentMessageEvent
            {
                TargetAgentId = State.UserAgentId,
                CallId = State.CallId,
                Content = finalResult.Response,
                Artifacts = finalResult.Artifacts,
                SenderAgentId = this.GetGrainId().ToString(),
                SenderAgentName = State.Name
            };

            LogEventInfo(
                "Sending reply: UniqueId={UniqueId}, TargetAgent={TargetAgent}, CallId={CallId}, ContentLength={Length}, ArtifactCount={ArtifactCount}",
                agentMessageEvent.UniqueId, State.UserAgentId, State.CallId, finalResult.Response?.Length ?? 0,
                finalResult.Artifacts.Count);

            await PublishAsyncWithTracing(GrainId.Parse(State.UserAgentId), agentMessageEvent);
        }, new { resultLength = finalResult.Response?.Length ?? 0 });
    }

    protected override void AIGAgentTransitionState(
        PsiOmniGAgentState state,
        StateLogEventBase<PsiOmniGAgentStateLogEvent> @event
    )
    {
        // Ensure tracing is initialized and scope exists
        InitializeTracing();

        LogEventDebug("State transition started: EventType={EventType}",
            @event.GetType().Name);

        if (@event is PsiOmniGAgentStateLogEvent e1)
        {
            var uid = e1.UniqueId;
            if (!_receivedMessageIds.Add(uid))
            {
                LogEventDebug("Duplicate state event detected, ignoring: UniqueId={UniqueId}", uid);
                return;
            }
        }

        switch (@event)
        {
            case InitializeEvent payload:
                LogEventDebug("Setting depth: {Depth}", payload.Depth);
                state.Name = payload.Name;
                state.Depth = payload.Depth;
                state.UserAgentId = payload.ParentId;
                state.Description = payload.Description + $"<examples>{payload.Examples}</examples>";
                if (state.Depth == 0) // is root
                {
                    state.RealizationStatus = RealizationStatus.Orchestrator;
                }
                else if (state.RealizationStatus == RealizationStatus.Unrealized && state.Configuration != null)
                {
                    LogEventDebug("Scheduling initialization upon InitializeEvent");
                    ScheduleTask(async () => await PublishAsyncWithTracing(this.GetGrainId(), new ContinuationEvent()
                    {
                        TargetAgentId = this.GetGrainId().ToString(),
                        ContinuationType = ContinuationType.Initialize
                    }));
                }

                break;
            case UpdateSendConfigEvent payload:
                LogEventDebug("Updating agent configuration: AgentId={AgentId}, ParentAgentId={ParentAgentId}",
                    state.AgentId, payload.Event.ParentAgentId);
                if (state.AgentId.IsNullOrEmpty())
                {
                    var grainId = this.GetGrainId().ToString();
                    var config = payload.Event.Configuration;
                    state.AgentId = grainId;
                    state.UserAgentId = payload.Event.ParentAgentId;
                    state.Configuration = config;
                    // state.Tools = payload.Event.Tools; // Not needed here. No tools should be configured here.
                }

                if (state.RealizationStatus == RealizationStatus.Unrealized && state.Configuration != null)
                {
                    LogEventDebug("Scheduling initialization upon UpdateSendConfigEvent");
                    ScheduleTask(async () => await PublishAsyncToSelfWithTracing(new ContinuationEvent()
                    {
                        TargetAgentId = this.GetGrainId().ToString(),
                        ContinuationType = ContinuationType.Initialize
                    }));
                }

                break;
            case ReceiveUserMessageEvent payload:
                Logger.LogInformation("StateTransition for ReceiveUserMessageEvent");
                LogEventInfo("Processing user message: CallId={CallId}, ReplyTo={ReplyTo}, ContentLength={Length}",
                    payload.Event.CallId, payload.Event.ReplyToAgentId, payload.Event.Content?.Length ?? 0);

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
                    if (state.RealizationStatus != RealizationStatus.Unrealized)
                    {
                        state.IterationCount = 0;
                        LogEventDebug("Scheduling RunAsync for user message");
                        ScheduleTask(async () => await PublishAsyncToSelfWithTracing(new ContinuationEvent()
                        {
                            TargetAgentId = this.GetGrainId().ToString(),
                            ContinuationType = ContinuationType.Run,
                            RunArg = $"User Message {payload.Event}"
                        }));
                    }
                }

                break;
            case RealizationEvent payload:
                LogEventInfo("Realization event: Status={Status}, Description={Description}, Tools={Tools}",
                    payload.RealizationStatus, payload.Description,
                    string.Join(", ", payload.Tools.Select(t => t.Name)));

                if (state.RealizationStatus == RealizationStatus.Unrealized)
                {
                    state.RealizationStatus = payload.RealizationStatus;
                    state.Description = payload.Description;
                    state.Tools = payload.Tools;
                    LogEventDebug("Agent realized as {Status}", payload.RealizationStatus);
                }

                ScheduleTask(async () => await PublishAsyncToSelfWithTracing(new ContinuationEvent()
                {
                    TargetAgentId = this.GetGrainId().ToString(),
                    ContinuationType = ContinuationType.SelfReportAndRun,
                    RunArg = "RealizationEvent"
                }));
                break;
            case ReceiveAgentMessageEvent payload:
            {
                var content = $"<agent_reply><agent_name>{payload.Event.SenderAgentName}</agent_name>\n";
                content += $"<content>{payload.Event.Content}</content>\n";

                if (!payload.Event.Artifacts.IsNullOrEmpty())
                {
                    var artifacts = payload.Event.Artifacts.Select(
                        a => $"<artifact name=\"{a.Name}\" format=\"{a.Format}\">{a.Content}</artifact>\n"
                    ).JoinAsString("\n");
                    content += $"\n\n{artifacts}";
                }

                content += "</agent_reply>";

                var todoItem = state.TodoList.Find(x =>
                    x.Id == payload.Event.CallId
                    && x.Status == TodoStatus.InProgress
                    && x.AssigneeAgentName == payload.Event.SenderAgentName
                );
                if (todoItem == null)
                {
                    LogEventDebug("Agent Message received for unknown todo item: CallId={CallId}",
                        payload.Event.CallId);
                }
                else
                {
                    todoItem.Status = TodoStatus.Completed;
                    content += $"\n<system_note>Todo item {payload.Event.CallId} is marked as Completed. You don't need to mark it again.</system_note>";
                }

                var amessage = PsiOmniChatMessage.CreateUserMessage(content);
                amessage.Metadata["CallId"] = payload.Event.CallId;
                state.ChatHistory.Add(amessage);
                ScheduleTask(async () => await PublishAsyncToSelfWithTracing(new ContinuationEvent()
                {
                    TargetAgentId = this.GetGrainId().ToString(),
                    ContinuationType = ContinuationType.Run,
                    RunArg = $"Agent Message {payload.Event}"
                }));
                break;
            }

            case NewAgentsCreatedEvent payload:
                LogEventInfo("New agents created: Count={Count}, AgentIds={AgentIds}",
                    payload.NewAgents.Count, string.Join(", ", payload.NewAgents.Select(a => a.AgentId)));

                foreach (var newAgent in payload.NewAgents)
                {
                    state.ChildAgents.TryAdd(newAgent.Name, newAgent);
                    LogEventDebug("Added child agent: {AgentId} ({AgentType})", newAgent.AgentId, newAgent.AgentType);
                }

                var newAgentIds = payload.NewAgents.Select(x => x.AgentId);

                ScheduleTask(async () => await PublishAsyncToSelfWithTracing(new ContinuationEvent()
                {
                    TargetAgentId = this.GetGrainId().ToString(),
                    ContinuationType = ContinuationType.RegisterAgents,
                    RegisterAgentIds = newAgentIds.ToList()
                }));

                break;
            case UpdateChildEvent payload:
            {
                if (payload.LastChildDescriptor.Name.IsNullOrEmpty())
                    break;

                LogEventInfo("Updating child agent: AgentId={ChildAgentId}, AgentType={AgentType}",
                    payload.LastChildDescriptor.AgentId, payload.LastChildDescriptor.AgentType);
                AgentDescriptor? oldObj;
                // Child may proceed first and we receive this event before we process our own NewAgentsCreatedEvent event
                if (!state.ChildAgents.TryGetValue(payload.LastChildDescriptor.Name, out oldObj))
                {
                    oldObj = new AgentDescriptor()
                    {
                        Name = payload.LastChildDescriptor.Name,
                        AgentId = payload.LastChildDescriptor.AgentId
                    };
                    state.ChildAgents[payload.LastChildDescriptor.Name] = oldObj;
                }

                var oldObjClone = oldObj.DeepClone();
                var newObjClone = payload.LastChildDescriptor.DeepClone();
                oldObjClone.Examples = new List<AgentExample>();
                newObjClone.Examples = new List<AgentExample>();
                var refreshDescription = !oldObjClone.Equals(newObjClone);

                LogEventDebug("Child agent update: RefreshDescription={RefreshDescription}", refreshDescription);
                state.ChildAgents[payload.LastChildDescriptor.Name] = payload.LastChildDescriptor;
                if (refreshDescription)
                {
                    // ScheduleTask(RunIntrospectionAsync);
                    ScheduleTask(async () => await PublishAsyncToSelfWithTracing(new ContinuationEvent()
                    {
                        TargetAgentId = this.GetGrainId().ToString(),
                        ContinuationType = ContinuationType.Retrospect
                    }));
                }

                break;
            }
            case GrowChatHistoryEvent payload:
                state.ChatHistory.AddRange(payload.NewMessages);
                foreach (var psiOmniChatMessage in payload.NewMessages)
                {
                    if (psiOmniChatMessage.TokenUsage == null) continue;
                    state.InputTokenUsage += psiOmniChatMessage.TokenUsage.PromptTokens;
                    state.OutTokenUsage += psiOmniChatMessage.TokenUsage.CompletionTokens;
                    state.TotalTokenUsage += psiOmniChatMessage.TokenUsage.TotalTokens;
                }

                if (state.ChatHistory.Count <= 1)
                    break;
                var finalResult = new FinalResponse();

                if (state.RealizationStatus == RealizationStatus.Specialized)
                {
                    finalResult.Response = State.ChatHistory.Last().Content;
                }
                else if (state.RealizationStatus == RealizationStatus.Orchestrator)
                {
                    var lastMessage = state.ChatHistory.Last()?.Content ?? string.Empty;
                    try
                    {
                        var thought = string.Empty;
                        var response = string.Empty;

                        // Extract thought if present
                        if (lastMessage.Contains("<thought>") && lastMessage.Contains("</thought>"))
                        {
                            var thoughtParts = lastMessage.Split("<thought>");
                            if (thoughtParts.Length > 1)
                            {
                                thought = thoughtParts[1].Split("</thought>")[0].Trim();
                            }
                        }

                        // Extract response if present
                        if (lastMessage.Contains("<response>") && lastMessage.Contains("</response>"))
                        {
                            var responseParts = lastMessage.Split("<response>");
                            if (responseParts.Length > 1)
                            {
                                response = responseParts[1].Split("</response>")[0].Trim();
                            }
                        }

                        // Extract artifacts if present
                        if (lastMessage.Contains("<artifact"))
                        {
                            // Support both self-closing and content-containing artifact tags

                            // 1. Handle self-closing artifacts: <artifact name="..." format="..." />
                            var selfClosingMatches = System.Text.RegularExpressions.Regex.Matches(
                                lastMessage,
                                @"<artifact name=""(.*?)"" format=""(.*?)"" />");

                            foreach (System.Text.RegularExpressions.Match match in selfClosingMatches)
                            {
                                var artifactName = match.Groups[1].Value.Trim();
                                var artifactFormat = match.Groups[2].Value.Trim();
                                if (State.Artifacts.TryGetValue(artifactName, out var artifact))
                                {
                                    finalResult.Artifacts.Add(new Artifact
                                    {
                                        Name = artifactName,
                                        Format = artifactFormat,
                                        Content = artifact.Content
                                    });
                                }
                            }

                            // 2. Handle content-containing artifacts: <artifact name="..." format="...">content</artifact>
                            var contentMatches = System.Text.RegularExpressions.Regex.Matches(
                                lastMessage,
                                @"<artifact name=""(.*?)"" format=""(.*?)"">(.*?)</artifact>",
                                System.Text.RegularExpressions.RegexOptions.Singleline);

                            foreach (System.Text.RegularExpressions.Match match in contentMatches)
                            {
                                var artifactName = match.Groups[1].Value.Trim();
                                var artifactFormat = match.Groups[2].Value.Trim();
                                var artifactContent = match.Groups[3].Value.Trim();

                                finalResult.Artifacts.Add(new Artifact
                                {
                                    Name = artifactName,
                                    Format = artifactFormat,
                                    Content = artifactContent
                                });
                            }
                        }

                        finalResult.Response = response;
                    }
                    catch (Exception ex)
                    {
                        finalResult.Response = lastMessage;
                        LogEventError(ex, "Failed to deserialize OrchestratorMessage: {Message}", lastMessage);
                    }
                }

                if (!finalResult.Response.IsNullOrEmpty())
                {
                    state.Examples.Last().Response = finalResult.Response;
                    // ScheduleTask(async () =>
                    // {
                    //     LogEventDebug("Starting report and reply: {Content}", finalResult.Response);
                    //     // TODO: Maybe update description.
                    //     await DoSelfReportAsync();
                    //     await ReplyAsync(finalResult);
                    //     LogEventDebug("Completed report and reply: {Content}", finalResult.Response);
                    // });
                    ScheduleTask(async () => await PublishAsyncToSelfWithTracing(new ContinuationEvent()
                    {
                        TargetAgentId = this.GetGrainId().ToString(),
                        ContinuationType = ContinuationType.IterateOrSelfReportAndReply,
                        FinalResponse = finalResult
                    }));
                }
                else
                {
                    // Check for stuck state: agent returned thought without tool calls and no InProgress tasks
                    var lastMessage = state.ChatHistory.LastOrDefault();
                    var hasToolCalls = lastMessage?.ToolCalls?.Count > 0;
                    // Check if there are any InProgress tasks that are not assigned to this agent
                    var hasInProgressTasks = state.TodoList.Any(x =>
                        x.Status == TodoStatus.InProgress && x.AssigneeAgentName != "__self__");
                    var hasPendingTasks = state.TodoList.Any(x =>
                        x.Status == TodoStatus.Pending ||
                        (x.Status == TodoStatus.InProgress && x.AssigneeAgentName == "__self__"));

                    // Extract thought to check if agent was thinking
                    var hasThought = false;
                    if (state.RealizationStatus == RealizationStatus.Orchestrator && lastMessage != null)
                    {
                        var content = lastMessage.Content ?? string.Empty;
                        hasThought = content.Contains("<thought>") && content.Contains("</thought>");
                    }

                    // If the agent returned thought without tool calls, no InProgress tasks but has pending tasks, inject a <crank> message to continue processing.
                    if (hasThought && !hasToolCalls && !hasInProgressTasks && hasPendingTasks)
                    {
                        LogEventInfo(
                            "Detected stuck state: agent returned thought without tool calls, no InProgress tasks but has pending tasks. Injecting <crank> message to continue processing.");

                        // Append crank message and schedule task run later
                        var crankMessage =
                            PsiOmniChatMessage.CreateUserMessage(
                                "<crank>You are not making progress. Please check if the statuses of the todo items are correctly updated. Otherwise, please continue to work on the todo items.</crank>");
                        crankMessage.Metadata["IsCrank"] = "true";
                        state.ChatHistory.Add(crankMessage);

                        // Schedule task run later
                        // ScheduleTask(async () => await RunAsync("crank message continuation"));
                        ScheduleTask(async () => await PublishAsyncToSelfWithTracing(new ContinuationEvent()
                        {
                            TargetAgentId = this.GetGrainId().ToString(),
                            ContinuationType = ContinuationType.Run,
                            RunArg = "crank message continuation"
                        }));
                    }
                }

                break;
            case UpdateSelfDescription payload:
                LogEventInfo("Updating self description: NewDescription={Description}", payload.Description);
                state.Description = payload.Description;
                // ScheduleTask(DoSelfReportAsync);
                ScheduleTask(async () => await PublishAsyncToSelfWithTracing(new ContinuationEvent()
                {
                    TargetAgentId = this.GetGrainId().ToString(),
                    ContinuationType = ContinuationType.SelfReport
                }));
                break;
            case WriteTask payload:
                LogEventInfo("Writing task: Task={Task}", payload.Task);
                state.CurrentTask = payload.Task;
                break;
            case WriteDraftResponse payload:
                LogEventInfo("Writing draft response: Response={Response}", payload.DraftResponse);
                state.DraftResponse = payload.DraftResponse;
                break;
            case CallAgent payload:
            {
                LogEventInfo("Calling agent: AgentName={}, TargetAgentId={TargetAgentId}, CallId={CallId}",
                    payload.AgentCall.AgentName,
                    payload.AgentCall.AgentId, payload.AgentCall.CallId);

                var todoItem = state.TodoList.Find(x =>
                    x.Id == payload.AgentCall.CallId
                    && x.Status == TodoStatus.InProgress
                );
                if (todoItem == null)
                {
                    LogEventDebug("Agent Message received for unknown todo item: CallId={CallId}",
                        payload.AgentCall.CallId);
                }
                else
                {
                    todoItem.Status = TodoStatus.InProgress;
                    todoItem.AssigneeAgentName = payload.AgentCall.AgentName;
                }

                break;
            }
            case WriteArtifact payload:
                LogEventInfo("Writing artifact: Name={ArtifactName}, Format={Format}, ContentLength={ContentLength}",
                    payload.Name, payload.Format, payload.Content?.Length ?? 0);
                if (!state.Artifacts.ContainsKey(payload.Name))
                {
                    state.Artifacts.Add(payload.Name, new Artifact
                    {
                        Name = payload.Name,
                        Format = payload.Format,
                        Content = payload.Content
                    });
                }

                break;
            case IterateEvent payload:
                state.IterationCount += 1;
                var userMessage = PsiOmniChatMessage.CreateUserMessage(
                    $"<review_comment>{payload.Comment}</review_comment>\n"+
                    "<system_note>Use a tone as if this is the first response. DO NOT mention revision or iteration to user in your response. Please give a self-contained response. DO NOT ask the user to reference previous response!!!</system_note>"
                );
                userMessage.Metadata["IsReviewComment"] = true;
                state.ChatHistory.Add(userMessage);
                ScheduleTask(async () => await PublishAsyncToSelfWithTracing(new ContinuationEvent()
                {
                    TargetAgentId = this.GetGrainId().ToString(),
                    ContinuationType = ContinuationType.Run,
                    RunArg = "iterate response"
                }));
                break;
        }

        LogEventDebug("State transition completed: EventType={EventType}",
            @event.GetType().Name);
    }
}