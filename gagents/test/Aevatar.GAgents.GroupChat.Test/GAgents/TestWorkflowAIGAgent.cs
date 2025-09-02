using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.State;
using Orleans.Providers;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.GroupChat.Test.GAgents;

/// <summary>
/// 测试用的 Workflow AIGAgent，用于验证 MCP 工具注册功能
/// </summary>
public interface ITestWorkflowAIGAgent : IStateGAgent<TestWorkflowAIGAgentState>, IAIGAgent
{
    Task<List<string>> GetAvailableKernelFunctionsAsync();
    Task<bool> HasMCPFunctionAsync(string serverName, string functionName);
    Task<int> GetRegisteredToolsCountAsync();
}

[GAgent("test-workflow-ai-agent", "test")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class TestWorkflowAIGAgent : 
    AIGAgentBase<TestWorkflowAIGAgentState, TestWorkflowAIGAgentStateLogEvent>,
    ITestWorkflowAIGAgent
{
    public override async Task<string> GetDescriptionAsync()
    {
        return "Test Workflow AIGAgent for unit testing MCP tool registration";
    }

    public Task<List<string>> GetAvailableKernelFunctionsAsync()
    {
        try
        {
            // Use the protected method from AIGAgentBase to get the kernel
            var kernel = GetKernelFromBrain();
            
            if (kernel != null)
            {
                var functions = kernel.Plugins.GetFunctionsMetadata();
                return Task.FromResult(functions.Select(f => $"{f.PluginName}.{f.Name}").ToList());
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Failed to access kernel functions: {Exception}", ex.Message);
        }

        return Task.FromResult(new List<string>());
    }

    public async Task<bool> HasMCPFunctionAsync(string serverName, string functionName)
    {
        var functions = await GetAvailableKernelFunctionsAsync();
        var expectedFunctionName = $"{serverName}.{functionName}";
        return functions.Any(f => f.Contains(expectedFunctionName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<int> GetRegisteredToolsCountAsync()
    {
        var functions = await GetAvailableKernelFunctionsAsync();
        return functions.Count;
    }

    /// <summary>
    /// AI GAgent 状态转换 - 根据 gagent-implementation-v1 规范
    /// </summary>
    protected override void AIGAgentTransitionState(TestWorkflowAIGAgentState state, StateLogEventBase<TestWorkflowAIGAgentStateLogEvent> @event)
    {
        switch (@event)
        {
            case TestWorkflowInitializedLogEvent initializedEvent:
                state.IsInitialized = initializedEvent.IsInitialized;
                state.InitializedAt = initializedEvent.InitializedAt;
                break;
        }
    }
}

/// <summary>
/// 测试用 AIGAgent 状态 - 继承 AIGAgentStateBase
/// </summary>
[GenerateSerializer]
public class TestWorkflowAIGAgentState : AIGAgentStateBase
{
    [Id(0)] public bool IsInitialized { get; set; } = false;
    [Id(1)] public DateTime InitializedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 测试用 AIGAgent 状态日志事件基类
/// </summary>
[GenerateSerializer]
public abstract class TestWorkflowAIGAgentStateLogEvent : StateLogEventBase<TestWorkflowAIGAgentStateLogEvent>
{
}

/// <summary>
/// 测试用初始化日志事件
/// </summary>
[GenerateSerializer]
public class TestWorkflowInitializedLogEvent : TestWorkflowAIGAgentStateLogEvent
{
    [Id(0)] public bool IsInitialized { get; set; } = true;
    [Id(1)] public DateTime InitializedAt { get; set; } = DateTime.UtcNow;
}