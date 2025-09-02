using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Aevatar.GAgents.PsiOmni;

public partial class PsiOmniGAgent
{
    /// <summary>
    /// Create a new specialized agent with custom prompt and tools
    /// </summary>
    [KernelFunction("query_existing_agents")]
    [Description("Query what agents are available.")]
    public async Task<List<AgentWithUsage>> QueryAgentsAsync()
    {
        var result = State.ChildAgents.Values.Select(x => new AgentWithUsage
        {
            Name = x.Name,
            AgentId = x.AgentId,
            AgentType = x.AgentType,
            Description = x.Description,
            Examples = x.Examples,
            Tools = x.Tools,
            HandlingTask =
                State.TodoList.Find(y => y.AssigneeAgentName == x.Name && y.Status == TodoStatus.InProgress)?.Id ??
                string.Empty
        }).ToList();
        return await Task.FromResult(result);
    }

    [KernelFunction("write_artifact")]
    [Description("Write an artifact.")]
    public async Task<string> WriteArtifactAsync(
        [Description(
             "The name of the artifact. It has to be unique and must be a valid file name with a valid extension."),
         Required]
        string name,
        [Description("The format of the artifact. It has to be a valid file extension."), Required]
        string format,
        [Description("The content of the artifact."), Required]
        string content
    )
    {
        if (State.Artifacts.ContainsKey(name))
        {
            return await Task.FromResult<string>($"Failed to write artifact: name {name} exits. Pick another name.");
        }

        State.Artifacts.TryAdd(name, new Artifact
        {
            Name = name,
            Format = format,
            Content = content
        });
        RaiseEventWithTracing(new WriteArtifact
        {
            Name = name,
            Format = format,
            Content = content
        });
        return await Task.FromResult($"Written artifact {name}.");
    }

    [KernelFunction("read_artifact")]
    [Description("Read an artifact.")]
    public async Task<Artifact?> ReadArtifactAsync(
        [Description(
             "The name of the artifact. It has to be unique and must be a valid file name with a valid extension."),
         Required]
        string name
    )
    {
        if (!State.Artifacts.TryGetValue(name, out var artifact))
        {
            return null;
        }

        return await Task.FromResult(artifact);
    }

    [KernelFunction("list_artifacts")]
    [Description("List all artifacts.")]
    public async Task<List<string>> ListArtifactsAsync()
    {
        return await Task.FromResult(State.Artifacts.Keys.OrderBy(x => x).ToList());
    }

    [KernelFunction("write_task")]
    [Description("Rewrite the current task.")]
    public async Task<string> WriteTaskAsync(
        [Description("The details of the task."), Required]
        FramedTask task
    )
    {
        RaiseEventWithTracing(new WriteTask()
        {
            Task = task
        });
        return await Task.FromResult("Written task.");
    }

    [KernelFunction("read_task")]
    [Description("Read the current task.")]
    public async Task<FramedTask> ReadTaskAsync(
    )
    {
        return await Task.FromResult(State.CurrentTask);
    }

    [KernelFunction("response_draft_write")]
    [Description("Write the draft response.")]
    public async Task<string> WriteResponseProposalAsync(
        [Description("The draft response."), Required]
        string draftResponse
    )
    {
        RaiseEventWithTracing(new WriteDraftResponse()
        {
            DraftResponse = draftResponse
        });
        return await Task.FromResult("Written draft response.");
    }

    /// <summary>
    /// Create a new specialized agent with custom prompt and tools
    /// </summary>
    [KernelFunction("create_agent")]
    [Description("Creates a new agent.")]
    public async Task<string> CreateAgentAsync(
        [Description("A unique name of this agent.")]
        string name,
        [Description(
            "The description of the agent. Include all necessary information such as the persona, knowledge and capabilities.")]
        string description,
        [Description("The example tasks that the agent can perform.")]
        string exampleTasks
    )
    {
        if (State.ChildAgents.ContainsKey(name))
        {
            return $"Failed. Agent with name {name} already exists. Please pick another name.";
        }

        var parentAgentId = this.GetGrainId().ToString();
        try
        {
            // Create agent configuration with custom prompt
            var agentConfig = State.Configuration;
            if (agentConfig == null)
            {
                return
                    $"❌ Error: AgentConfiguration not set.";
            }

            if (State.Depth >= 5)
            {
                return "❌ Error: Can't create agent with depth > 5.";
            }

            // Create and initialize the new agent
            var psi = await _gAgentFactory.GetGAgentAsync("omni", "psi", new PsiOmniGAgentConfig()
            {
                Name = name,
                ParentId = parentAgentId,
                Description = description,
                Examples = exampleTasks,
                Depth = State.Depth + 1
            });
            var agentId = psi.GetGrainId();
            // There's a publisher tied to each parent agent.

            var agent = await _gAgentFactory.GetGAgentAsync<IPsiOmniGAgent>(agentId);

            // Use the same priority system as AIGAgentBase.GetCurrentLLMConfigAsync()
            string? configKeyToPass = null;
            SelfLLMConfig? selfLlmConfig = null;

            // Priority 1: LLMConfigKey (if PsiOmni supported it)
            if (!State.LLMConfigKey.IsNullOrEmpty())
            {
                configKeyToPass = State.LLMConfigKey;
            }
            // Priority 2: SystemLLM 
            else if (!State.SystemLLM.IsNullOrEmpty())
            {
                configKeyToPass = State.SystemLLM;
            }
            // Priority 3: Fallback to resolved LLM
            else if (State.LLM != null)
            {
                selfLlmConfig = new SelfLLMConfig
                {
                    ProviderEnum = State.LLM.ProviderEnum,
                    ModelId = State.LLM.ModelIdEnum,
                    ModelName = State.LLM.ModelName,
                    ApiKey = State.LLM.ApiKey,
                    Endpoint = State.LLM.Endpoint,
                    Memo = State.LLM.Memo
                };
            }

            await agent.InitializeAsync(new InitializeDto()
            {
                LLMConfig = new LLMConfigDto()
                {
                    SystemLLM = configKeyToPass,  // Pass the key, not just State.SystemLLM
                    SelfLLMConfig = selfLlmConfig
                }
            });

            var configEvent = new AgentConfigEvent
            {
                Configuration = agentConfig,
                ParentAgentId = parentAgentId
            };
            await PublishAsyncWithTracing(agentId, configEvent);

            var descriptor = new AgentDescriptor
            {
                Name = name,
                AgentId = agentId.ToString(),
                Description = description
            };

            State.ChildAgents.Add(name, descriptor);

            RaiseEventWithTracing(new AddNewAgent()
            {
                NewAgent = descriptor
            });

            LogEventInfo("Agent created successfully: AgentId={AgentId}, Depth={Depth}",
                agentId, State.Depth + 1);
            return $"Created the following agent:\n{JsonSerializer.Serialize(descriptor)}";
        }
        catch (Exception ex)
        {
            var errorMessage = $"❌ Error creating agent for parent {parentAgentId}: {ex.Message}";
            Logger.LogError(ex, "❌ Error creating agent for parent {Parent}", parentAgentId);
            return errorMessage;
        }
    }

    [KernelFunction("call_agent")]
    [Description("Calls any ConfigurableAgentGrain by its ID with a natural language query")]
    public async Task<string> CallAgentAsync(
        [Description("The name of the agent to call. Don't use agentId here."), Required]
        string name,
        [Description("The call ID of this call.")]
        string callId,
        [Description("The task to be sent.")] TaskDispatch task
    )
    {
        // We match both agent id and name just in case the LLM confuses (we observed this)
        var agentDescriptor = State.ChildAgents.Values.FirstOrDefault(a => a.AgentId == name || a.Name == name);
        if (agentDescriptor == null)
        {
            return $"Failed to call agent with name {name}: agent is not found.";
        }

        var agentId = agentDescriptor.AgentId;
        var message = $"Task: {task.Task}\n\nBackground: {task.Background}";
        if (!task.Knowledge.IsNullOrEmpty())
        {
            var knowledge = task.Knowledge.Select(x => $"<knowledge>{x}</knowledge>").JoinAsString("\n");
            message += $"\n\nKnowledge:\n{knowledge}";
        }

        var parentAgentId = this.GetGrainId().ToString();
        if (parentAgentId == agentId)
        {
            return "Failed to call agent: Calling self is disallowed.";
        }

        return await TraceMethodAsync(async () =>
        {
            var nowHandling =
                State.TodoList.Find(x => x.AssigneeAgentName == name && x.Status == TodoStatus.InProgress);
            if (nowHandling != null)
            {
                return $"Failed to call agent {name}. Agent is handling call {nowHandling.Id}.";
            }

            var thisTodo = State.TodoList.Find(x => x.Id == callId);
            if (thisTodo == null)
            {
                return $"Failed to call agent {name}. Todo item {callId} is not found.";
            }

            thisTodo.Status = TodoStatus.InProgress;
            thisTodo.AssigneeAgentName = name;

            Logger.LogInformation("🔗 Generic agent proxy called for {AgentId} with message: {Message}", agentId,
                message);
            LogEventInfo(
                "Calling agent: ParentAgent={ParentAgent}, TargetAgent={TargetAgent}, CallId={CallId}, MessageLength={Length}",
                parentAgentId, agentId, callId, message?.Length ?? 0);

            try
            {
                var targetAgent = await _gAgentFactory.GetGAgentAsync(GrainId.Parse(agentId));

                var userMessageEvent = new UserMessageEvent
                {
                    TargetAgentId = agentId,
                    CallId = callId,
                    Content = message,
                    ReplyToAgentId = parentAgentId
                };
                await PublishAsyncWithTracing(GrainId.Parse(agentId), userMessageEvent);

                var call = new AgentCall
                {
                    AgentName = name,
                    AgentId = agentId,
                    CallId = callId,
                    Message = message
                };

                LogEventInfo("Agent call sent successfully: TargetAgent={TargetAgent}, CallId={CallId}",
                    agentId, callId);
                RaiseEventWithTracing(new CallAgent()
                {
                    AgentCall = call
                });
                return $"Made a call to agent {name}: {JsonSerializer.Serialize(call)}";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "❌ Error in generic agent proxy for {AgentId}", agentId);
                LogEventError(ex, "Failed to call agent: TargetAgent={TargetAgent}, CallId={CallId}",
                    agentId, callId);
                return $"Error calling agent '{agentId}': {ex.Message}";
            }
        }, new { parentAgentId, agentId, callId, messageLength = message?.Length });
    }

    [KernelFunction("todo_complete")]
    [Description("Make todo item as complete. Only applicable to task that can be handled by self.")]
    public async Task<string> MarkTodoCompleteAsync(
        [Description("The id of the todo item.")]
        string todoId
    )
    {
        var otherAgentsTodo = State.TodoList.Find(x => x.Id == todoId && !x.AssigneeAgentName.IsNullOrEmpty());
        if (otherAgentsTodo != null)
        {
            return
                $"Use todo_complete tool only for self assigned task. The todo item {todoId} is assigned to agent {otherAgentsTodo.AssigneeAgentName}";
        }

        var todo = State.TodoList.Find(x => x.Id == todoId && x.Status == TodoStatus.Pending);
        if (todo == null)
        {
            return $"Failed to start self handling todo item {todoId}: Todo item is not found or not pending.";
        }

        todo.Status = TodoStatus.Completed;
        todo.AssigneeAgentName = "__self__";
        RaiseEventWithTracing(new CallAgent()
        {
            AgentCall = new AgentCall()
            {
                AgentName = "__self__",
                AgentId = this.GetGrainId().ToString(),
                CallId = todoId
            }
        });
        return $"Completed todo item {todoId}.";
    }

    [KernelFunction("todo_read")]
    [Description(
        @"Read the current todo list.

Use this tool to read the current to-do list for the session. This tool should be used proactively and frequently to ensure that you are aware of
the status of the current task list. You should make use of this tool as often as possible, especially in the following situations:
    - At the beginning of conversations to see what's pending
    - Before starting new tasks to prioritize work
    - When the user asks about previous tasks or plans
    - Whenever you're uncertain about what to do next
    - After completing tasks to update your understanding of remaining work
    - After every few messages to ensure you're on track

    Usage:
    - This tool takes in no parameters. So leave the input blank or empty. DO NOT include a dummy object, placeholder string or a key like ""input"" or ""empty"". LEAVE IT BLANK.
    - Returns a list of todo items with their status, priority, and content
    - Use this information to track progress and plan next steps
    - If no todos exist yet, an empty list will be returned"
    )]
    public async Task<IReadOnlyList<TodoItem>> ReadTodosAsync()
    {
        return await Task.FromResult(State.TodoList);
    }

    [KernelFunction("todo_write")]
    [Description(
        @"Update the todo list for the current session. To be used proactively and often to keep track of the work plan.
Use this tool to create and manage a structured task list for your current session. This helps you track progress, organize complex tasks, and demonstrate thoroughness to the user.

    ## When to Use This Tool
    Use this tool proactively in these scenarios:

    1. Task planning - When a task needs to be broken down into sub-tasks
    2. After a sub-task is completed and work plan needs to be updated

    ## When NOT to Use This Tool

    Skip using this tool when:
    1. The task is purely conversational or informational
")
    ]
    public async Task<string> WriteTodosAsync(
        [Description(
            "The list of todo items to add. The ids of the todo items to add must be unique and must not be in the current todo list.")]
        List<TodoItem> newTodos,
        [Description("The ids of the todo items to cancel.")]
        List<string> todoIdsToRemove
    )
    {
        LogEventInfo("Updating todo list: OldCount={OldCount}, NewCount={NewCount}, Changes={Changes}",
            State.TodoList.Count, newTodos.Count,
            GetTodoChanges(State.TodoList, newTodos));

        var newTodoIds = newTodos.Select(x => x.Id).ToHashSet();
        var oldTodoIds = State.TodoList.Select(x => x.Id).ToHashSet();

        if (newTodoIds.Intersect(oldTodoIds).Any())
        {
            return "Failed to update todo list: Cannot add todo items with the same id.";
        }

        State.TodoList.AddRange(newTodos);

        var inProgressTodos = newTodos.Where(x => x.Status == TodoStatus.InProgress).Select(x => x.Id).ToList();
        var cancelInProgressTodos = inProgressTodos.Intersect(todoIdsToRemove).Any();

        if (cancelInProgressTodos)
        {
            return "Failed to update todo list: Cannot cancel in-progress todo items.";
        }

        foreach (var todoId in todoIdsToRemove)
        {
            var todo = State.TodoList.Find(x => x.Id == todoId);
            if (todo != null)
            {
                todo.Status = TodoStatus.Canceled;
            }
        }

        RaiseEventWithTracing(new UpdateTodoList()
        {
            AddedTodos = newTodos,
            RemovedTodoIds = todoIdsToRemove
        });
        // await ConfirmEventsWithTracing();
        return "Successfully updated todo list";
    }

    private string GetTodoChanges(List<TodoItem> oldList, List<TodoItem> newList)
    {
        var changes = new List<string>();
        var newIds = newList.Select(t => t.Id).ToHashSet();
        var oldIds = oldList.Select(t => t.Id).ToHashSet();

        // Find new todos
        var added = newIds.Except(oldIds).Count();
        if (added > 0) changes.Add($"{added} added");

        // Find removed todos
        var removed = oldIds.Except(newIds).Count();
        if (removed > 0) changes.Add($"{removed} removed");

        // Find status changes
        var statusChanges = 0;
        foreach (var newTodo in newList)
        {
            var oldTodo = oldList.FirstOrDefault(t => t.Id == newTodo.Id);
            if (oldTodo != null && oldTodo.Status != newTodo.Status)
            {
                statusChanges++;
            }
        }

        if (statusChanges > 0) changes.Add($"{statusChanges} status changes");

        return changes.Count > 0 ? string.Join(", ", changes) : "no changes";
    }
}