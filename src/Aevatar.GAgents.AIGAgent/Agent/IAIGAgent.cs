using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.MCP.Core.Model;
using Aevatar.GAgents.MCP.Options;
using Orleans.Runtime;

namespace Aevatar.GAgents.AIGAgent.Agent;

// ReSharper disable InconsistentNaming
public interface IAIGAgent
{
    Task<bool> InitializeAsync(InitializeDto dto);

    Task<bool> UploadKnowledge(List<BrainContentDto>? knowledgeList);

    /// <summary>
    /// Gets the currently resolved LLM configuration with priority order:
    /// 1. LLMConfigKey (new reference format)
    /// 2. SystemLLM (existing reference format)
    /// 3. LLM (old resolved format - backwards compatibility)
    /// </summary>
    Task<LLMConfig?> GetLLMConfigAsync();

    /// <summary>
    /// Sets the LLM configuration key using the centralized configuration approach
    /// </summary>
    Task SetLLMConfigKeyAsync(string llmConfigKey);

    /// <summary>
    /// Sets the SystemLLM configuration for testing purposes (does not trigger brain initialization)
    /// </summary>
    Task SetSystemLLMAsync(string systemLLM);

    /// <summary>
    /// Sets the LLM configuration for testing purposes (does not trigger brain initialization)
    /// </summary>
    Task SetLLMAsync(LLMConfig llmConfig, string? systemLLM);

    /// <summary>
    /// Triggers the automatic migration logic for testing purposes
    /// </summary>
    Task TriggerMigrationAsync();

    // MCP tool methods

    /// <summary>
    /// Configure MCP servers for this agent
    /// </summary>
    Task<bool> ConfigureMCPServersAsync(List<MCPServerConfig> servers);

    /// <summary>
    /// Get available MCP tools from all configured servers
    /// </summary>
    Task<List<MCPToolInfo>> GetAvailableMCPToolsAsync();

    /// <summary>
    /// Configure selected GAgent tools
    /// </summary>
    Task<bool> ConfigureGAgentToolsAsync(List<GrainType> toolGAgentTypes);

    /// <summary>
    /// Configure selected GAgent as GAgent tools
    /// </summary>
    /// <param name="toolGAgents"></param>
    /// <returns></returns>
    Task<bool> ConfigureToolGAgentsAsync(List<GrainId> toolGAgents);

    /// <summary>
    /// Get configured tool GAgents
    /// </summary>
    /// <returns></returns>
    Task<List<GrainId>> GetToolGAgentsAsync();

    /// <summary>
    /// Clears all registered GAgent tools
    /// </summary>
    Task<bool> ClearGAgentToolsAsync();
}