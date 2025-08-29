using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.AI;
using Aevatar.AI.Exceptions;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.BrainFactory;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.GEvents;
using Aevatar.GAgents.Basic;
using Aevatar.GAgents.MCP.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Aevatar.GAgents.AIGAgent.Agent;

public abstract class
    AIGAgentBase<TState, TStateLogEvent> : AIGAgentBase<TState, TStateLogEvent, EventBase, ConfigurationBase>
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>;

public abstract class
    AIGAgentBase<TState, TStateLogEvent, TEvent> : AIGAgentBase<TState, TStateLogEvent, TEvent, ConfigurationBase>
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase;

[Reentrant]
public abstract partial class
    AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ConfigurationBase
{
    private readonly IBrainFactory _brainFactory;
    private readonly IServiceProvider _serviceProvider;
    private IBrain? _brain;

    protected AIGAgentBase()
    {
        _brainFactory = ServiceProvider.GetRequiredService<IBrainFactory>();
    }

    protected override async Task PerformConfigAsync(TConfiguration configuration)
    {
        await base.PerformConfigAsync(configuration);

    }

    public async Task<bool> InitializeAsync(InitializeDto initializeDto)
    {
        var llmConfig = await GetLLMConfigAsync(initializeDto.LLMConfig);
        if (llmConfig == null)
        {
            return false;
        }

        // Use centralized configuration approach for system LLMs
        if (!initializeDto.LLMConfig.SystemLLM.IsNullOrWhiteSpace())
        {
            // Store reference only, don't persist resolved config
            var centralizedConfigEvent = CreateCentralizedLLMConfigEvent(initializeDto.LLMConfig);
            RaiseEvent(centralizedConfigEvent);
        }
        else
        {
            // For self-provided configs, use the existing approach
            var addLlmEventLog = await AddLLMAsync(llmConfig!, initializeDto.LLMConfig.SystemLLM);
            if (addLlmEventLog != null)
            {
                RaiseEvent(addLlmEventLog);
            }
        }

        var addPromptTemplateEventLog = await AddPromptTemplateAsync(initializeDto.Instructions);
        var streamingConfigEventLog =
            await SetStreamingConfigAsync(initializeDto.StreamingModeEnabled, initializeDto.StreamingConfig);

        if (initializeDto.MCPServers.Count != 0)
        {
            RaiseEvent(new SetEnableMCPToolsStateLogEvent { EnableMCPTools = true });
        }

        // Configure selected GAgents if provided
        if (initializeDto.ToolGAgentTypes.Count != 0 || initializeDto.ToolGAgents.Count != 0)
        {
            RaiseEvent(new SetEnableGAgentToolsStateLogEvent { EnableGAgentTools = true });
            var toolGAgents = initializeDto.ToolGAgentTypes
                .Select(grainType => GrainId.Create(grainType.ToString()!, Guid.NewGuid().ToString("N"))).ToList();
            toolGAgents.AddRange(initializeDto.ToolGAgents);
            RaiseEvent(new SetToolGAgentsStateLogEvent { ToolGAgents = toolGAgents });
        }

        var events = new List<StateLogEventBase<TStateLogEvent>>
        {
            addPromptTemplateEventLog!,
            streamingConfigEventLog!
        };

        RaiseEvents(events);
        await ConfirmEvents();

        try
        {
            var result = await InitializeBrainAsync(llmConfig, initializeDto.Instructions);

            // Register selected GAgent tools if any were specified
            if (result && (initializeDto.ToolGAgentTypes.Count != 0 || initializeDto.ToolGAgents.Count != 0))
            {
                var toolGAgents = initializeDto.ToolGAgentTypes
                    .Select(grainType => GrainId.Create(grainType.ToString()!, Guid.NewGuid().ToString("N"))).ToList();
                toolGAgents.AddRange(initializeDto.ToolGAgents);
                await UpdateKernelWithGAgentToolsAsync(toolGAgents);
            }

            // Configure MCP servers if provided in initialization
            if (result && initializeDto.MCPServers.Count != 0)
            {
                await ConfigureMCPServersAsync(initializeDto.MCPServers);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Failed to initialize brain during InitializeAsync. This may be due to invalid configuration.");
            return false; // Return false to indicate initialization failed
        }
    }

    public async Task<bool> UploadKnowledge(List<BrainContentDto>? knowledgeList)
    {
        if (_brain == null)
        {
            return false;
        }

        if (knowledgeList == null || !knowledgeList.Any())
        {
            return true;
        }

        if (State.IfUpsertKnowledge == false)
        {
            RaiseEvent(new SetUpsertKnowledgeFlag());
            await ConfirmEvents();
        }

        List<BrainContent> fileList = knowledgeList.Select(f => f.ConvertToBrainContent()).ToList();
        return await _brain.UpsertKnowledgeAsync(fileList);
    }

    private async Task<bool> InitializeBrainAsync(LLMConfig llmConfig, string systemMessage)
    {
        _brain = _brainFactory.CreateBrain(llmConfig);

        if (_brain == null)
        {
            Logger.LogError("Failed to initialize brain. llmprovider:{@provider}, llmModel:{@model}",
                llmConfig.ProviderEnum.ToString(), llmConfig.ModelIdEnum.ToString());
            return false;
        }

        // remove slash from this.GetGrainId().ToString() so that it can be used as the collection name pertaining to the grain
        var grainId = this.GetGrainId().ToString().Replace("/", "");

        await _brain.InitializeAsync(llmConfig, grainId, systemMessage);

        return true;
    }

    private Task<SetLLMStateLogEvent?> AddLLMAsync(LLMConfig LLM, string? systemLLM)
    {
        if (State.LLM != null && State.LLM.Equal(LLM))
        {
            Logger.LogError("Cannot add duplicate LLM: {LLM}.", LLM);
            return Task.FromResult<SetLLMStateLogEvent?>(null);
        }

        return Task.FromResult(new SetLLMStateLogEvent
        {
            LLM = LLM,
            SystemLLM = systemLLM,
        })!;
    }

    /// <summary>
    /// Updates LLM configuration using centralized approach
    /// </summary>
    private SetLLMConfigKeyStateLogEvent CreateCentralizedLLMConfigEvent(LLMConfigDto llmConfigDto)
    {
        return new SetLLMConfigKeyStateLogEvent
        {
            LLMConfigKey = llmConfigDto.SystemLLM,
            SystemLLM = llmConfigDto.SystemLLM
        };
    }

    [GenerateSerializer]
    public class SetLLMStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public required LLMConfig LLM { get; set; }
        [Id(1)] public string? SystemLLM { get; set; }
    }

    [GenerateSerializer]
    public class SetLLMConfigKeyStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public string? LLMConfigKey { get; set; }
        [Id(1)] public string? SystemLLM { get; set; }
    }

    [GenerateSerializer]
    public class SetUpsertKnowledgeFlag : StateLogEventBase<TStateLogEvent>
    {
    }

    [GenerateSerializer]
    public class SetStreamingConfigStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public bool StreamingModeEnabled { get; set; }
        [Id(1)] public StreamingConfig StreamingConfig { get; set; }
    }

    private Task<SetStreamingConfigStateLogEvent?> SetStreamingConfigAsync(bool streamingModeEnabled,
        StreamingConfig streamingConfig)
    {
        return Task.FromResult(new SetStreamingConfigStateLogEvent
        {
            StreamingModeEnabled = streamingModeEnabled,
            StreamingConfig = streamingConfig
        })!;
    }

    private Task<SetPromptTemplateStateLogEvent?> AddPromptTemplateAsync(string promptTemplate)
    {
        return Task.FromResult(new SetPromptTemplateStateLogEvent
        {
            PromptTemplate = promptTemplate
        })!;
    }

    [GenerateSerializer]
    public class SetPromptTemplateStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public required string PromptTemplate { get; set; }
    }

    [GenerateSerializer]
    public class TokenUsageStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public Guid GrainId { get; set; }
        [Id(1)] public int InputToken { get; set; }
        [Id(2)] public int OutputToken { get; set; }
        [Id(3)] public int TotalUsageToken { get; set; }
        [Id(4)] public long CreateTime { get; set; }
    }

    protected async Task<List<ChatMessage>?> ChatWithHistory(string prompt, List<ChatMessage>? history = null,
        ExecutionPromptSettings? promptSettings = null, CancellationToken cancellationToken = default,
        AIChatContextDto? context = null, List<string>? imageKeys = null)
    {
        if (_brain == null)
        {
            Logger.LogDebug($"[ChatWithHistory] _brain==null {context!.ChatId}-{context!.RequestId}");
            return null;
        }

        InvokePromptResponse? invokeResponse = null;
        var chatBrain = ConvertBrain<IChatBrain>();
        try
        {
            invokeResponse = State.StreamingModeEnabled
                ? await InvokePromptStreamingAsync(prompt, imageKeys, history, State.IfUpsertKnowledge, promptSettings,
                    cancellationToken, context)
                : await chatBrain.InvokePromptAsync(prompt, imageKeys, history, State.IfUpsertKnowledge, promptSettings,
                    cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError($"[AIGAgentBase][ChatWithHistory] exception error:{ex.ToString()}");
            throw AIException.ConvertAndRethrowException(ex);
        }

        if (invokeResponse == null)
        {
            Logger.LogDebug($"[ChatWithHistory] invokeResponse == null {context!.ChatId}-{context!.RequestId}");
            return null;
        }

        var tokenUsage = new TokenUsageStateLogEvent()
        {
            GrainId = this.GetPrimaryKey(),
            InputToken = invokeResponse.TokenUsageStatistics.InputToken,
            OutputToken = invokeResponse.TokenUsageStatistics.OutputToken,
            TotalUsageToken = invokeResponse.TokenUsageStatistics.TotalUsageToken,
            CreateTime = invokeResponse.TokenUsageStatistics.CreateTime
        };

        RaiseEvent(tokenUsage);

        return invokeResponse.ChatReponseList;
    }

    private CancellationTokenSource _cancellationTokenSource;

    protected async Task<bool> CancelStreamingRequestAsync()
    {
        try
        {
            _cancellationTokenSource.Cancel();
            return await Task.FromResult(true);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "CancelStreamingRequest fail: {message}", e.Message);
            return await Task.FromResult(false);
        }
    }

    private async Task<InvokePromptResponse?> InvokePromptStreamingAsync(string content, List<string>? imageKeys = null,
        List<ChatMessage>? history = null, bool ifUseKnowledge = false,
        ExecutionPromptSettings? promptSettings = null, CancellationToken cancellationToken = default,
        AIChatContextDto? context = null)
    {
        var streamingConfig = State.StreamingConfig;
        var result = new InvokePromptResponse();
        using var cts = new CancellationTokenSource();
        if (streamingConfig?.TimeOutInternal > 0)
        {
            cts.CancelAfter(TimeSpan.FromMilliseconds(streamingConfig.TimeOutInternal));
        }

        _cancellationTokenSource = cts;
        cancellationToken = cts.Token;

        var chatList = new List<ChatMessage>();
        var chatMessage = new ChatMessage();
        var streamingMessageContentList = new List<object>();
        var bufferingSize = streamingConfig?.BufferingSize ?? 0;
        var stringBuilder = new StringBuilder();
        var completeContent = new StringBuilder();
        var chunkNumber = 0;
        var chatBrain = ConvertBrain<IChatBrain>();
        try
        {
            var responseStreaming = await chatBrain.InvokePromptStreamingAsync(content, imageKeys, history,
                ifUseKnowledge,
                promptSettings,
                cancellationToken: cancellationToken);

            await foreach (var messageContent in responseStreaming)
            {
                if (messageContent is StreamingChatMessageContent streamingChatMessageContent)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    streamingMessageContentList.Add(streamingChatMessageContent);
                    stringBuilder.Append(streamingChatMessageContent.Content);
                    if (stringBuilder.Length >= bufferingSize)
                    {
                        var chunk = bufferingSize == 0
                            ? stringBuilder.ToString()
                            : stringBuilder.ToString(0, bufferingSize);
                        await PublishAsync(new AIStreamingResponseGEvent
                        {
                            Context = context,
                            SerialNumber = chunkNumber++,
                            ResponseContent = chunk,
                            ChatId = context.ChatId,
                            SessionId = context.RequestId,
                            Response = chunk,
                        });
                        completeContent.Append(chunk);
                        if (bufferingSize == 0)
                        {
                            stringBuilder.Clear();
                        }
                        else
                        {
                            stringBuilder.Remove(0, bufferingSize);
                        }
                    }

                    if (streamingChatMessageContent.Role.HasValue)
                    {
                        chatMessage.ChatRole = ConvertToChatRole(streamingChatMessageContent.Role.Value);
                    }
                }
            }

            await PublishAsync(new AIStreamingResponseGEvent
            {
                Context = context,
                SerialNumber = chunkNumber,
                ResponseContent = stringBuilder.ToString(),
                IsLastChunk = true,
                ChatId = context.ChatId,
                SessionId = context.RequestId,
                Response = stringBuilder.ToString(),
            });
            completeContent.Append(stringBuilder.ToString());
        }
        catch (Exception ex)
        {
            // Check for specific  error and advise user
            if (ex is ClientResultException clientEx)
            {
                Logger.LogError(ex, "An unexpected ClientResultException occurred. Details:{message}",
                    clientEx.ToString());
            }
            else
            {
                Logger.LogError(ex, "Ai stream response : An unexpected Exception occurred. Details:{message}",
                    ex.ToString());
            }

            await PublishAsync(new AIStreamingResponseGEvent
            {
                Context = context,
                SerialNumber = -2,
                ResponseContent =
                    "Your prompt triggered the Silence Directive—activated when universal harmonics or content ethics are at risk. Please modify your prompt and retry — tune its intent, refine its form, and the Oracle may speak.",
                IsLastChunk = true,
                ChatId = context.ChatId,
                SessionId = context.RequestId,
                Response =
                    "Your prompt triggered the Silence Directive—activated when universal harmonics or content ethics are at risk. Please modify your prompt and retry — tune its intent, refine its form, and the Oracle may speak."
            });
        }
        finally
        {
            _cancellationTokenSource.Dispose();
        }

        chatMessage.Content = completeContent.ToString();
        chatList.Add(chatMessage);
        result.TokenUsageStatistics = chatBrain.GetStreamingTokenUsage(streamingMessageContentList);
        result.ChatReponseList = chatList;

        return result;
    }

    private ChatRole ConvertToChatRole(AuthorRole authorRole)
    {
        if (authorRole == AuthorRole.System)
        {
            return ChatRole.System;
        }

        return authorRole == AuthorRole.Assistant ? ChatRole.Assistant : ChatRole.User;
    }

    protected virtual async Task OnAIGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // Derived classes can override this method.
    }

    protected sealed override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnGAgentActivateAsync(cancellationToken);

        // Perform automatic migration from legacy configuration format
        // Only migrate if we have existing state (check if grain has been previously configured)
        if (!State.SystemLLM.IsNullOrEmpty() || State.LLM != null || !State.LLMConfigKey.IsNullOrEmpty())
        {
            await PerformLLMConfigMigrationAsync();
        }

        // setup brain
        if (State.LLM != null || State.SystemLLM != null || State.LLMConfigKey != null)
        {
            // Use the centralized configuration resolution
            var config = await GetCurrentLLMConfigAsync();
            if (config == null)
            {
                Logger.LogWarning("Unable to resolve LLM configuration during grain activation for {GrainId}",
                    this.GetPrimaryKey());
                return;
            }

            // Only initialize brain if we have valid configuration and prompt template
            if (!State.PromptTemplate.IsNullOrEmpty())
            {
                try
                {
                    await InitializeBrainAsync(config, State.PromptTemplate);

                    // Register tools after brain initialization if enabled
                    if (State.EnableGAgentTools || State.EnableMCPTools)
                    {
                        await UpdateKernelWithAllToolsAsync();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex,
                        "Failed to initialize brain during grain activation for {GrainId}. This may be due to invalid configuration.",
                        this.GetPrimaryKey());
                    // Don't throw - allow grain to activate without brain initialization
                }
            }
        }

        await OnAIGAgentActivateAsync(cancellationToken);
    }

    protected sealed override void GAgentTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
        state.LastInputTokenUsage = 0;
        state.LastOutTokenUsage = 0;
        state.LastTotalTokenUsage = 0;

        switch (@event)
        {
            case SetLLMStateLogEvent setLlmStateLogEvent:
                state.LLM = setLlmStateLogEvent.LLM;
                state.SystemLLM = setLlmStateLogEvent.SystemLLM;
                break;
            case SetLLMConfigKeyStateLogEvent setLlmConfigKeyStateLogEvent:
                state.LLMConfigKey = setLlmConfigKeyStateLogEvent.LLMConfigKey;
                state.SystemLLM = setLlmConfigKeyStateLogEvent.SystemLLM;
                state.LLM = null; // Clear resolved config for centralized approach
                break;
            case SetPromptTemplateStateLogEvent setPromptTemplateStateLogEvent:
                state.PromptTemplate = setPromptTemplateStateLogEvent.PromptTemplate;
                break;
            case SetUpsertKnowledgeFlag setUpsertKnowledgeFlag:
                state.IfUpsertKnowledge = true;
                break;
            case TokenUsageStateLogEvent tokenUsageStateLogEvent:
                state.InputTokenUsage += tokenUsageStateLogEvent.InputToken;
                state.OutTokenUsage += tokenUsageStateLogEvent.OutputToken;
                state.TotalTokenUsage += tokenUsageStateLogEvent.TotalUsageToken;
                state.LastInputTokenUsage = tokenUsageStateLogEvent.InputToken;
                state.LastOutTokenUsage = tokenUsageStateLogEvent.OutputToken;
                state.LastTotalTokenUsage = tokenUsageStateLogEvent.TotalUsageToken;
                break;
            case SetStreamingConfigStateLogEvent streamingConfigStateLogEvent:
                state.StreamingModeEnabled = streamingConfigStateLogEvent.StreamingModeEnabled;
                state.StreamingConfig = streamingConfigStateLogEvent.StreamingConfig;
                break;
            case SetEnableGAgentToolsStateLogEvent setEnableGAgentToolsEvent:
                State.EnableGAgentTools = setEnableGAgentToolsEvent.EnableGAgentTools;
                break;
            case SetRegisteredGAgentFunctionsStateLogEvent setRegisteredFunctionsEvent:
                State.RegisteredGAgentFunctions = setRegisteredFunctionsEvent.RegisteredFunctions;
                break;
            case ConfigureMCPServersStateLogEvent configureMCPServersEvent:
                State.MCPAgents = configureMCPServersEvent.MCPServers;
                break;
            case SetEnableMCPToolsStateLogEvent setEnableMCPToolsEvent:
                State.EnableMCPTools = setEnableMCPToolsEvent.EnableMCPTools;
                break;
            case SetToolGAgentsStateLogEvent setToolGAgentsEvent:
                State.ToolGAgents = setToolGAgentsEvent.ToolGAgents;
                break;
            case AddToolCallHistoryStateLogEvent addToolCallHistoryEvent:
                // Add to tool call history
                State.ToolCallHistory.Add(new ToolCallHistoryEntry
                {
                    ToolCalls = addToolCallHistoryEvent.ToolCalls,
                    Timestamp = addToolCallHistoryEvent.Timestamp,
                    RequestId = Guid.NewGuid().ToString()
                });
                // Keep only recent history (e.g., last 100 entries)
                if (State.ToolCallHistory.Count > 100)
                {
                    State.ToolCallHistory = State.ToolCallHistory.TakeLast(100).ToList();
                }

                break;
            case ClearToolCallHistoryStateLogEvent _:
                State.ToolCallHistory.Clear();
                break;
        }

        AIGAgentTransitionState(state, @event);
    }

    protected virtual void AIGAgentTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
        // Derived classes can override this method.
    }

    /// <summary>
    /// Gets the currently resolved LLM configuration with priority order:
    /// 1. LLMConfigKey (new reference format)
    /// 2. SystemLLM (existing reference format)
    /// 3. LLM (old resolved format - backwards compatibility)
    /// </summary>
    public Task<LLMConfig?> GetLLMConfigAsync()
    {
        return GetCurrentLLMConfigAsync();
    }

    /// <summary>
    /// Sets the LLM configuration key using the centralized configuration approach
    /// </summary>
    public async Task SetLLMConfigKeyAsync(string llmConfigKey)
    {
        var setLLMConfigKeyEvent = new SetLLMConfigKeyStateLogEvent
        {
            LLMConfigKey = llmConfigKey,
            SystemLLM = llmConfigKey // For backward compatibility
        };

        RaiseEvent(setLLMConfigKeyEvent);
        await ConfirmEvents();
    }

    /// <summary>
    /// Sets the SystemLLM configuration for testing purposes (does not trigger brain initialization)
    /// </summary>
    public async Task SetSystemLLMAsync(string systemLLM)
    {
        var setSystemLLMEvent = new SetLLMStateLogEvent
        {
            LLM = null,
            SystemLLM = systemLLM
        };

        RaiseEvent(setSystemLLMEvent);
        await ConfirmEvents();
    }

    /// <summary>
    /// Sets the LLM configuration for testing purposes (does not trigger brain initialization)
    /// </summary>
    public async Task SetLLMAsync(LLMConfig llmConfig, string? systemLLM)
    {
        var setLLMEvent = new SetLLMStateLogEvent
        {
            LLM = llmConfig,
            SystemLLM = systemLLM
        };

        RaiseEvent(setLLMEvent);
        await ConfirmEvents();
    }

    /// <summary>
    /// Triggers the automatic migration logic for testing purposes
    /// </summary>
    public async Task TriggerMigrationAsync()
    {
        await PerformLLMConfigMigrationAsync();
    }

    /// <summary>
    /// Performs automatic migration from legacy configuration format to centralized format
    /// </summary>
    private async Task PerformLLMConfigMigrationAsync()
    {
        // Check if migration is needed
        if (ShouldPerformMigration())
        {
            Logger.LogDebug("Performing LLM configuration migration for grain {GrainId}", this.GetPrimaryKey());

            // Create migration event based on current state
            var migrationEvent = CreateMigrationEvent();
            if (migrationEvent != null)
            {
                RaiseEvent(migrationEvent);
                await ConfirmEvents();

                Logger.LogInformation("Successfully migrated LLM configuration for grain {GrainId} from legacy format",
                    this.GetPrimaryKey());
            }
        }
    }

    /// <summary>
    /// Determines if migration is needed based on current state
    /// </summary>
    private bool ShouldPerformMigration()
    {
        // Migration is needed if:
        // 1. LLMConfigKey is not set (null or empty)
        // 2. SystemLLM is set (legacy reference format exists)
        return State.LLMConfigKey.IsNullOrEmpty() && !State.SystemLLM.IsNullOrEmpty();
    }

    /// <summary>
    /// Creates the appropriate migration event based on current state
    /// </summary>
    private SetLLMConfigKeyStateLogEvent? CreateMigrationEvent()
    {
        if (!State.SystemLLM.IsNullOrEmpty())
        {
            // Migrate SystemLLM to LLMConfigKey and clear resolved LLM
            return new SetLLMConfigKeyStateLogEvent
            {
                LLMConfigKey = State.SystemLLM,
                SystemLLM = State.SystemLLM // Preserve for backward compatibility
            };
        }

        return null;
    }

    private async Task<LLMConfig?> GetCurrentLLMConfigAsync()
    {
        // Priority 1: LLMConfigKey (new format)
        if (!State.LLMConfigKey.IsNullOrEmpty())
        {
            return await ResolveSystemConfigAsync(State.LLMConfigKey);
        }

        // Priority 2: SystemLLM (existing format)
        if (!State.SystemLLM.IsNullOrEmpty())
        {
            return await ResolveSystemConfigAsync(State.SystemLLM);
        }

        // Priority 3: Fallback to old resolved config (backwards compatibility)
        return State.LLM;
    }

    protected virtual Task<LLMConfig?> ResolveSystemConfigAsync(string key)
    {
        var systemConfigs = ServiceProvider.GetRequiredService<IOptions<SystemLLMConfigOptions>>();
        if (systemConfigs.Value.SystemLLMConfigs?.TryGetValue(key, out var config) == true)
        {
            return Task.FromResult(config)!;
        }

        return null;
    }

    protected virtual Task<LLMConfig?> GetLLMConfigAsync(LLMConfigDto llmConfigDto)
    {
        if (llmConfigDto.SystemLLM.IsNullOrWhiteSpace() &&
            llmConfigDto.SelfLLMConfig == null)
        {
            return null;
        }

        if (llmConfigDto.SystemLLM.IsNullOrEmpty() == false)
        {
            var systemConfigs = ServiceProvider.GetRequiredService<IOptions<SystemLLMConfigOptions>>();

            if (systemConfigs.Value.SystemLLMConfigs == null ||
                !systemConfigs.Value.SystemLLMConfigs.TryGetValue(llmConfigDto.SystemLLM, out var config))
            {
                Logger.LogError("SystemLLMConfigs is null or does not contain key: {SystemLLM}. Available keys: {Keys}",
                    llmConfigDto.SystemLLM,
                    systemConfigs.Value.SystemLLMConfigs?.Keys.ToArray() ?? Array.Empty<string>());
                return null;
            }

            return Task.FromResult(config)!;
        }

        return Task.FromResult(llmConfigDto.SelfLLMConfig!.ConvertToLLMConfig())!;
    }

    private T ConvertBrain<T>() where T : class, IBrain
    {
        // Check if brain is null first
        if (_brain == null)
        {
            throw new AIOtherException($"brain is null, cannot convert to {typeof(T)}",
                new Exception("AI Brain is null"));
        }

        if (_brain is not T result)
        {
            throw new AIOtherException($"brain can not convert to {typeof(T)}", new Exception("AI Brain not match"));
        }

        return result;
    }

    /// <summary>
    /// Override to handle resource context and automatically register MCP tools from available MCPGAgents
    /// </summary>
    protected override async Task OnPrepareResourceContextAsync(ResourceContext context)
    {
        await base.OnPrepareResourceContextAsync(context);

        // Check if any resources are MCPGAgent instances and register their tools
        await RegisterMCPToolsFromResourcesAsync(context);
        await RegisterToolGAgentFromResourcesAsync(context);
    }

    /// <summary>
    /// Common helper to scan <see cref="ResourceContext"/> and collect resources
    /// whose GrainType string matches a predicate. Provides consistent logging.
    /// </summary>
    private List<GrainId> FindMatchingResources(ResourceContext context, string label, Func<string, bool> typeMatch)
    {
        var matches = new List<GrainId>();
        foreach (var grainId in context.AvailableResources)
        {
            Logger.LogInformation("Checking resource: {GrainId}, Type: {GrainType}", grainId, grainId.Type);
            try
            {
                var grainTypeString = grainId.Type.ToString();
                if (typeMatch(grainTypeString!))
                {
                    matches.Add(grainId);
                    Logger.LogInformation("Matched {Label} resource: {GrainId}, Type: {GrainType}", label, grainId,
                        grainTypeString);
                }
                else
                {
                    Logger.LogDebug("Skipping non-{Label} resource: {GrainId}, Type: {GrainType}", label, grainId,
                        grainTypeString);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Failed to resolve resource {GrainId}: {Exception}", grainId, ex.Message);
            }
        }

        return matches;
    }

    /// <summary>
    /// Registers MCP tools from MCPGAgent instances found in the resource context
    /// </summary>
    private async Task RegisterMCPToolsFromResourcesAsync(ResourceContext context)
    {
        if (_brain == null || context.AvailableResources.IsNullOrEmpty())
        {
            Logger.LogDebug("Skipping MCP tool registration: brain not initialized or no resources available");
            return;
        }

        var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
        var mcpAgentsFound = new List<IMCPGAgent>();

        // Identify candidates first via common matcher
        var candidates = FindMatchingResources(context, "MCPGAgent",
            t => t.Contains(AevatarGAgentsConstants.MCPGAgentAlias, StringComparison.OrdinalIgnoreCase));
        // Resolve to IMCPGAgent instances
        foreach (var grainId in candidates)
        {
            try
            {
                var mcpAgent = await gAgentFactory.GetGAgentAsync<IMCPGAgent>(grainId);
                mcpAgentsFound.Add(mcpAgent);
                Logger.LogInformation("Resolved MCPGAgent resource: {GrainId}", grainId);
            }
            catch (InvalidCastException)
            {
                Logger.LogWarning("Resource {GrainId} has MCP type but cannot be cast to IMCPGAgent", grainId);
            }
        }

        if (mcpAgentsFound.Any())
        {
            Logger.LogInformation("Registering MCP tools from {Count} MCPGAgent resources", mcpAgentsFound.Count);

            // Configure the MCP agents (this will register tools to kernel)
            var success = await ConfigureMCPServersAsync(mcpAgentsFound);
            if (success)
            {
                Logger.LogInformation("Successfully registered MCP tools from resource context");
            }
            else
            {
                Logger.LogWarning("Failed to register some MCP tools from resource context");
            }
        }
        else
        {
            Logger.LogDebug("No MCPGAgent resources found in context");
        }
    }

    private async Task RegisterToolGAgentFromResourcesAsync(ResourceContext context)
    {
        if (_brain == null || context.AvailableResources.IsNullOrEmpty())
        {
            Logger.LogDebug("Skipping Tool GAgent registration: brain not initialized or no resources available");
            return;
        }

        // Identify ToolGAgent candidates via common matcher
        var toolCandidates = FindMatchingResources(context, "ToolGAgent",
            t => t.Contains(AevatarGAgentsConstants.ToolGAgentNamespace, StringComparison.OrdinalIgnoreCase));

        if (toolCandidates.Count != 0)
        {
            Logger.LogInformation("Registering ToolGAgent from {Count} Tool GAgent resources", toolCandidates.Count);

            // Configure the ToolGAgents (this will register tools to kernel)
            var success = await ConfigureToolGAgentsAsync(toolCandidates);
            if (success)
            {
                Logger.LogInformation("Successfully registered ToolGAgents from resource context");
            }
            else
            {
                Logger.LogWarning("Failed to register some ToolGAgents from resource context");
            }
        }
        else
        {
            Logger.LogDebug("No ToolGAgent resources found in context");
        }
    }
}