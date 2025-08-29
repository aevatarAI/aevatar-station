// ABOUTME: This file implements a test AIGAgent specifically for MCP tool testing
// ABOUTME: Provides minimal implementation focused on MCP functionality validation

using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Test.TestAgents.Events;
using Aevatar.GAgents.AIGAgent.Test.TestAgents.States;
using Orleans.Providers;

namespace Aevatar.GAgents.AIGAgent.Test.TestAgents;

// ReSharper disable InconsistentNaming
public interface ITestMCPAIGAgent : IAIGAgent, IStateGAgent<TestMCPAIGAgentState>
{
    Task<bool> TestMCPToolCallAsync(string serverName, string toolName, Dictionary<string, object> parameters);
    Task<List<ToolCallDetail>> GetCurrentToolCallsAsync();
    Task ClearToolCallsAsync();
}

[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class TestMCPAIGAgent : 
    AIGAgentBase<TestMCPAIGAgentState, TestMCPAIGAgentStateLogEvent, TestMCPAIGAgentEvent, ConfigurationBase>,
    ITestMCPAIGAgent
{
    public override async Task<string> GetDescriptionAsync()
    {
        return "Test MCP AIGAgent for unit testing";
    }

    public async Task<bool> TestMCPToolCallAsync(string serverName, string toolName, Dictionary<string, object> parameters)
    {
        try
        {
            var kernelArgs = new Microsoft.SemanticKernel.KernelArguments();
            foreach (var kvp in parameters)
            {
                kernelArgs[kvp.Key] = kvp.Value;
            }

            var result = await CallMCPToolAsync(serverName, toolName, kernelArgs);
            return !result.Contains("Error");
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<ToolCallDetail>> GetCurrentToolCallsAsync()
    {
        return CurrentToolCalls.ToList();
    }

    public async Task ClearToolCallsAsync()
    {
        ClearToolCalls();
    }

    [EventHandler]
    public async Task HandleTestEvent(TestMCPAIGAgentEvent testEvent)
    {
        var logEvent = new TestMCPAIGAgentStateLogEvent
        {
            PromptTemplate = testEvent.Message
        };

        RaiseEvent(logEvent);
        await ConfirmEvents();
    }
}