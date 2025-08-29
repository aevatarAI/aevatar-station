using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Core.GEvents;
using Aevatar.GAgents.MCP.Core.Model;
using Aevatar.GAgents.MCP.GEvents;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace Aevatar.GAgents.MCP.GAgents;

// ReSharper disable MemberCanBePrivate.Global
public abstract partial class MCPGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
{
    [EventHandler]
    public async Task<MCPToolResponseEvent> HandleEventAsync(MCPToolCallEvent toolCallEvent)
    {
        Logger.LogInformation($"[MCPGAgent] HandleEventAsync called - ServerName: {toolCallEvent.ServerName}, ToolName: {toolCallEvent.ToolName}, Arguments: {JsonSerializer.Serialize(toolCallEvent.Arguments)}");
        Console.WriteLine($"[MCPGAgent] HandleEventAsync called - ServerName: {toolCallEvent.ServerName}, ToolName: {toolCallEvent.ToolName}, Arguments: {JsonSerializer.Serialize(toolCallEvent.Arguments)}");
        try
        {
            // Extract server name and actual tool name
            var parts = toolCallEvent.ToolName.Split('.');
            if (parts.Length < 2)
            {
                throw new ArgumentException($"Invalid tool name: {toolCallEvent.ToolName}");
            }

            var actualToolName = string.Join(".", parts.Skip(1));

            // Set timeout
            using var cts = new CancellationTokenSource(State.RequestTimeout);

            McpClient ??= await GetOrCreateMcpClientAsync(State.MCPServerConfig);

            // Call tool through the dynamically obtained client
            var arguments = toolCallEvent.Arguments.ToDictionary(kvp => kvp.Key, object? (kvp) => kvp.Value);
            var result = await McpClient.CallToolAsync(actualToolName, arguments, cancellationToken: cts.Token);

            // Update LastToolCall timestamp
            RaiseEvent(new UpdateLastToolCallLogEvent
            {
                LastToolCall = DateTime.UtcNow
            });
            await ConfirmEvents();

            var response = new MCPToolResponseEvent
            {
                RequestId = toolCallEvent.RequestId,
                Success = !result.IsError!.Value,
                ServerName = toolCallEvent.ServerName,
                ToolName = toolCallEvent.ToolName,
                ErrorMessage = result.IsError!.Value ? "Tool exection failed." : null,
                Result = ExtractContentFromMcpResult(result)
            };

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to call mcp tool: {ToolName}.", toolCallEvent.ToolName);

            var errorResponse = new MCPToolResponseEvent
            {
                RequestId = toolCallEvent.RequestId,
                Success = false,
                ServerName = toolCallEvent.ServerName,
                ToolName = toolCallEvent.ToolName,
                ErrorMessage = ex.Message,
                Result = null
            };

            return errorResponse;
        }
    }

    public async Task<MCPToolsDiscoveredEvent> HandleEventAsync(MCPDiscoverToolsEvent discoverEvent)
    {
        Logger.LogInformation("Start to execute event handler for MCPDiscoverToolsEvent.");
        try
        {
            var allTools = new List<MCPToolInfo>();
            McpClient ??= await GetOrCreateMcpClientAsync(State.MCPServerConfig);
            var tools = await McpClient.ListToolsAsync();
            allTools.AddRange(tools.Select(t => ConvertToMCPToolInfo(t, State.MCPServerConfig.ServerName)));

            return new MCPToolsDiscoveredEvent
            {
                ServerName = State.MCPServerConfig.ServerName,
                Tools = allTools
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to discover tools.");
            throw;
        }
    }

    /// <summary>
    /// Extract content text from MCP result
    /// </summary>
    private string ExtractContentFromMcpResult(object result)
    {
        try
        {
            // Use reflection to get Content property
            var resultType = result?.GetType();
            if (resultType == null)
            {
                return string.Empty;
            }

            var contentProperty = resultType.GetProperty("Content");
            if (contentProperty == null)
            {
                return string.Empty;
            }

            var contentValue = contentProperty.GetValue(result);
            if (contentValue == null)
            {
                return string.Empty;
            }

            // Try to get the first content item
            object? firstContent = null;
            if (contentValue is System.Collections.IEnumerable enumerable)
            {
                var enumerator = enumerable.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    firstContent = enumerator.Current;
                }
            }

            if (firstContent == null)
            {
                return string.Empty;
            }

            // Try to get Text property (TextContentBlock should have this property)
            var contentType = firstContent.GetType();
            var textProperty = contentType.GetProperty("Text");
            if (textProperty != null)
            {
                var textValue = textProperty.GetValue(firstContent);
                return textValue?.ToString() ?? string.Empty;
            }

            // If there's no Text property, try Value property
            var valueProperty = contentType.GetProperty("Value");
            if (valueProperty != null)
            {
                var value = valueProperty.GetValue(firstContent);
                return value?.ToString() ?? string.Empty;
            }

            // If neither exists, try to serialize the entire object as JSON
            var json = JsonSerializer.Serialize(firstContent);
            Logger.LogDebug("MCP content serialized as: {Json}", json);
            
            // Try to extract text field from JSON
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("text", out JsonElement textElement))
            {
                return textElement.GetString() ?? string.Empty;
            }
            if (doc.RootElement.TryGetProperty("Text", out JsonElement TextElement))
            {
                return TextElement.GetString() ?? string.Empty;
            }
            
            // If still not found, return the entire JSON
            return json;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to extract content from MCP result");
            // Fallback to ToString()
            return result?.ToString() ?? string.Empty;
        }
    }
}