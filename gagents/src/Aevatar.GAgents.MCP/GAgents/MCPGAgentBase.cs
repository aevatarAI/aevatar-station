using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Core.GEvents;
using Aevatar.GAgents.MCP.Core.Model;
using Aevatar.GAgents.MCP.Core.State;
using Aevatar.GAgents.MCP.GEvents;
using Aevatar.GAgents.MCP.Options;
using GroupChat.GAgent;
using ModelContextProtocol.Client;
using System.Text.Json;
using Aevatar.GAgents.MCP.McpClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.MCP.GAgents;

/// <summary>
/// Base class for MCP GAgents using ModelContextProtocol.NET.Core SDK
/// </summary>
public abstract partial class MCPGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    MemberGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
    where TState : MCPGAgentState, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : MCPGAgentConfig
{
    protected IEnumerable<IMcpClientProvider> McpClientProviders =>
        ServiceProvider.GetServices<IMcpClientProvider>();

    protected IMcpClient? McpClient;

    protected override async Task PerformConfigAsync(TConfiguration configuration)
    {
        McpClient = await GetOrCreateMcpClientAsync(configuration.ServerConfig);
        var tools = await McpClient.ListToolsAsync();
        var serverName = configuration.ServerConfig.ServerName;
        await PublishAsync(new MCPToolsDiscoveredEvent
        {
            ServerName = serverName,
            Tools = tools.Select(t => ConvertToMCPToolInfo(t, serverName)).ToList()
        });
        RaiseEvent(new SetConfigurationLogEvent
        {
            RequestTimeout = configuration.RequestTimeout
        });
        RaiseEvent(new ConfigMCPServerLogEvent { ServerConfig = configuration.ServerConfig });
        await ConfirmEvents();
    }

    private async Task<IMcpClient> GetOrCreateMcpClientAsync(MCPServerConfig mcpConfig)
    {
        var providers = McpClientProviders.ToList();
        Logger.LogDebug("There are {Count} mcp client providers registered.", providers.Count);
        
        foreach (var provider in providers)
        {
            Logger.LogDebug("Provider found: Type={Type}, ClientType={ClientType}", 
                provider.GetType().Name, provider.ClientType);
        }
        
        if (mcpConfig.Url.IsNullOrEmpty())
        {
            var stdioProvider = providers.SingleOrDefault(p => p.ClientType == McpClientType.Stdio);
            if (stdioProvider == null)
            {
                throw new InvalidOperationException($"No MCP client provider found for ClientType={McpClientType.Stdio}. Available providers: {string.Join(", ", providers.Select(p => $"{p.GetType().Name}({p.ClientType})"))}");
            }
            return await stdioProvider.GetOrCreateClientAsync(mcpConfig);
        }

        var sseProvider = providers.SingleOrDefault(p => p.ClientType == McpClientType.Sse);
        if (sseProvider == null)
        {
            throw new InvalidOperationException($"No MCP client provider found for ClientType={McpClientType.Sse}. Available providers: {string.Join(", ", providers.Select(p => $"{p.GetType().Name}({p.ClientType})"))}");
        }
        return await sseProvider.GetOrCreateClientAsync(mcpConfig);
    }

    /// <summary>
    /// Call MCP tool
    /// </summary>
    public async Task<MCPToolResponseEvent> CallToolAsync(string serverName, string toolName,
        Dictionary<string, object> arguments)
    {
        var @event = new MCPToolCallEvent
        {
            RequestId = Guid.NewGuid(),
            ServerName = serverName,
            ToolName = $"{serverName}.{toolName}",
            Arguments = arguments
        };

        return await HandleEventAsync(@event);
    }

    /// <summary>
    /// Get available tools from specific server or all servers
    /// </summary>
    public async Task<List<MCPToolInfo>> GetAvailableToolsAsync(string? serverName = null)
    {
        var response = await HandleEventAsync(new MCPDiscoverToolsEvent());
        return response.Tools;
    }

    /// <summary>
    /// Convert McpClientTool to MCPToolInfo
    /// </summary>
    private MCPToolInfo ConvertToMCPToolInfo(McpClientTool tool, string serverName)
    {
        var parameters = new Dictionary<string, MCPParameterInfo>();

        // Extract parameters from tool's JsonSchema if available
        if (tool.JsonSchema.ValueKind != JsonValueKind.Undefined && tool.JsonSchema.ValueKind != JsonValueKind.Null)
        {
            parameters = MCPParameterInfo.FromMCPToolSchema(tool.JsonSchema);
        }
        
        Logger.LogInformation($"Tool: {JsonSerializer.Serialize(tool)}");
        Logger.LogInformation($"Parameters: {JsonSerializer.Serialize(parameters)}");

        return new MCPToolInfo
        {
            Name = tool.Name,
            Description = tool.Description,
            Parameters = parameters,
            ServerName = serverName
        };
    }

    /// <summary>
    /// Override GAgentTransitionState to handle MCP-specific state transitions
    /// </summary>
    protected override void GAgentTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
        switch (@event)
        {
            case SetConfigurationLogEvent configEvent:
                state.RequestTimeout = configEvent.RequestTimeout;
                break;

            case ConfigMCPServerLogEvent configServerEvent:
                state.MCPServerConfig = configServerEvent.ServerConfig;
                break;

            case UpdateLastToolCallLogEvent updateEvent:
                state.LastToolCall = updateEvent.LastToolCall;
                break;

            default:
                MCPTransitionState(state, @event);
                break;
        }
    }

    /// <summary>
    /// Virtual method for derived classes to override state transitions
    /// </summary>
    protected virtual void MCPTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
        // Derived classes can override this method
    }

    /// <summary>
    /// Log event for setting configuration
    /// </summary>
    [GenerateSerializer]
    public class SetConfigurationLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public TimeSpan RequestTimeout { get; set; }
    }

    /// <summary>
    /// Log event for adding MCP server
    /// </summary>
    [GenerateSerializer]
    public class ConfigMCPServerLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public MCPServerConfig ServerConfig { get; set; } = null!;
    }

    /// <summary>
    /// Log event for updating last tool call timestamp
    /// </summary>
    [GenerateSerializer]
    public class UpdateLastToolCallLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public DateTime LastToolCall { get; set; }
    }
}