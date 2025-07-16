using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Domain.Shared.Util;
using Aevatar.GAgents.Executor;
using Aevatar.GAgents.MCP.Core;
using Aevatar.GAgents.MCP.Core.GEvents;
using Aevatar.GAgents.MCP.GEvents;
using Aevatar.GAgents.MCP.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Aevatar.HttpApi.Controllers;

[ApiController]
[Route("api/mcp-demo")]
public class MCPDemoController : ControllerBase
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IGAgentExecutor _gAgentExecutor;
    private readonly ILogger<MCPDemoController> _logger;

    // Store MCP GAgent instances by ID and their grain IDs
    private static readonly Dictionary<Guid, (IMCPGAgent agent, GrainId grainId)> _mcpAgents = new();

    // Store tool call history
    private static readonly Dictionary<Guid, List<MCPToolCallHistory>> _toolCallHistory = new();

    public MCPDemoController(
        IClusterClient clusterClient,
        IGAgentService gAgentService,
        ILogger<MCPDemoController> logger)
    {
        _gAgentFactory = new GAgentFactory(clusterClient);
        _gAgentExecutor = new GAgentExecutor(clusterClient, gAgentService);
        _logger = logger;
    }

    /// <summary>
    /// Initialize MCP GAgent with configured servers
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize([FromBody] MCPInitRequest request)
    {
        try
        {
            _logger.LogInformation("Initializing MCP GAgent with {Count} servers", request.Servers.Count);

            // Create MCP configuration
            var config = new MCPGAgentConfig
            {
                EnableToolDiscovery = true,
                RequestTimeout = TimeSpan.FromSeconds(request.TimeoutSeconds ?? 30),
                Server = request.Servers.Select(s => new MCPServerConfig
                {
                    ServerName = s.ServerName ?? string.Empty,
                    Command = s.Command ?? string.Empty,
                    Args = s.Args?.Select(arg => arg?.ToString() ?? string.Empty).ToList() ?? new List<string>(),
                    Env = ConvertEnvironmentDictionary(s.Environment)
                }).First()
            };

            // Create MCP GAgent
            var mcpAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(config);
            var agentId = mcpAgent.GetPrimaryKey();
            var grainId = mcpAgent.GetGrainId();

            // Store the agent and grain ID
            _mcpAgents[agentId] = (mcpAgent, grainId);
            _toolCallHistory[agentId] = new List<MCPToolCallHistory>();

            // Get available tools after initialization
            var availableTools = await mcpAgent.GetAvailableToolsAsync();
            var serverStates = await mcpAgent.GetServerStatesAsync();

            _logger.LogInformation("MCP GAgent initialized with {ToolCount} tools from {ServerCount} servers",
                availableTools.Count, serverStates.Count);

            return Ok(new
            {
                success = true,
                agentId = agentId,
                availableTools = availableTools.Select(t => new
                {
                    name = t.Key,
                    description = t.Value.Description,
                    serverName = t.Value.ServerName,
                    parameters = t.Value.Parameters.Select(p => new
                    {
                        name = p.Key,
                        type = p.Value.Type,
                        description = p.Value.Description,
                        required = p.Value.Required
                    })
                }),
                serverStates = serverStates.Select(s => new
                {
                    serverName = s.ServerName,
                    isConnected = s.IsConnected,
                    lastConnectedTime = s.LastConnectedTime,
                    registeredTools = s.RegisteredTools
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MCP GAgent");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Call a tool on MCP server
    /// </summary>
    [HttpPost("tool-call")]
    public async Task<IActionResult> CallTool([FromBody] MCPToolCallRequest request)
    {
        try
        {
            if (!Guid.TryParse(request.AgentId, out var agentId))
            {
                return BadRequest(new { success = false, error = "Invalid agent ID" });
            }

            if (!_mcpAgents.TryGetValue(agentId, out var agentInfo))
            {
                return NotFound(new { success = false, error = "MCP agent not found" });
            }

            _logger.LogInformation("Calling tool {Tool} on server {Server}", request.ToolName, request.ServerName);

            // Create tool call event with converted arguments
            var toolCallEvent = new MCPToolCallEvent
            {
                ServerName = request.ServerName,
                ToolName = request.ToolName,
                Arguments = JsonConversionHelper.ConvertToBasicTypes(
                    request.Arguments ??
                    new Dictionary<string, object>())
            };

            try
            {
                // Execute the event through GAgentExecutor
                var resultJson = await _gAgentExecutor.ExecuteGAgentEventHandler(agentInfo.grainId, toolCallEvent, typeof(MCPToolResponseEvent));

                // Try to parse the result as MCPToolResponseEvent
                MCPToolResponseEvent? response;
                try
                {
                    _logger.LogInformation("Raw result JSON: {Json}", resultJson);
                    response = JsonSerializer.Deserialize<MCPToolResponseEvent>(resultJson);
                    _logger.LogInformation("Parsed response. Success: {Success}, Result type: {Type}, Result: {Result}",
                        response?.Success, response?.Result?.GetType().Name, response?.Result);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize MCPToolResponseEvent, using raw result");
                    // If deserialization fails, create a response with the raw result
                    response = new MCPToolResponseEvent
                    {
                        Success = true,
                        Result = resultJson
                    };
                }

                // Record the tool call in history
                var historyEntry = new MCPToolCallHistory
                {
                    ServerName = request.ServerName,
                    ToolName = request.ToolName,
                    Arguments = request.Arguments,
                    Success = response?.Success ?? false,
                    Result = response?.Result,
                    ErrorMessage = response?.ErrorMessage,
                    Timestamp = DateTime.UtcNow
                };
                _toolCallHistory[agentId].Add(historyEntry);

                _logger.LogInformation("Tool call completed. Success: {Success}", response?.Success ?? false);

                // Format the response with result in a separate section
                var formattedResponse = new
                {
                    success = response?.Success ?? false,
                    errorMessage = response?.ErrorMessage,
                    toolInfo = new
                    {
                        serverName = request.ServerName,
                        toolName = request.ToolName,
                        executedAt = DateTime.UtcNow
                    }
                };

                // Add result in a highlighted section if successful
                if (response?.Success == true && response.Result != null)
                {
                    _logger.LogInformation("Tool call successful. Result type: {Type}, Result: {Result}",
                        response.Result?.GetType().Name, response.Result);

                    return Ok(new
                    {
                        success = true,
                        toolInfo = formattedResponse.toolInfo,
                        resultDisplay = new
                        {
                            hasResult = true,
                            resultType = response.Result?.GetType().Name ?? "Unknown",
                            resultContent = response.Result,
                            formattedResult = FormatResultForDisplay(response.Result)
                        }
                    });
                }

                return Ok(new
                {
                    success = false,
                    errorMessage = response?.ErrorMessage ?? "Unknown error",
                    toolInfo = formattedResponse.toolInfo,
                    resultDisplay = new
                    {
                        hasResult = false,
                        errorDetails = response?.ErrorMessage
                    }
                });
            }
            catch (TimeoutException tex)
            {
                _logger.LogError(tex, "Tool call timed out");
                return StatusCode(504, new { success = false, error = "Tool call timed out" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call MCP tool");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get tool call history
    /// </summary>
    [HttpGet("history/{agentId}")]
    public IActionResult GetHistory(string agentId)
    {
        if (!Guid.TryParse(agentId, out var id))
        {
            return BadRequest(new { success = false, error = "Invalid agent ID" });
        }

        var history = _toolCallHistory.GetValueOrDefault(id) ?? new List<MCPToolCallHistory>();

        return Ok(new
        {
            success = true,
            history = history.OrderByDescending(h => h.Timestamp).Take(50).Select(h => new
            {
                serverName = h.ServerName,
                toolName = h.ToolName,
                arguments = h.Arguments,
                success = h.Success,
                result = h.Result,
                errorMessage = h.ErrorMessage,
                timestamp = h.Timestamp
            })
        });
    }

    /// <summary>
    /// Discover tools from a specific server
    /// </summary>
    [HttpPost("discover-tools")]
    public async Task<IActionResult> DiscoverTools([FromBody] DiscoverToolsRequest request)
    {
        try
        {
            if (!Guid.TryParse(request.AgentId, out var agentId))
            {
                return BadRequest(new { success = false, error = "Invalid agent ID" });
            }

            if (!_mcpAgents.TryGetValue(agentId, out var agentInfo))
            {
                return NotFound(new { success = false, error = "MCP agent not found" });
            }

            _logger.LogInformation("Discovering tools from server {Server}", request.ServerName);

            // Create discover tools event
            var discoverEvent = new MCPDiscoverToolsEvent
            {
                ServerName = request.ServerName
            };

            try
            {
                // Execute the event through GAgentExecutor
                var resultJson = await _gAgentExecutor.ExecuteGAgentEventHandler(agentInfo.grainId, discoverEvent);

                // Try to parse the result as MCPToolsDiscoveredEvent
                MCPToolsDiscoveredEvent? response = null;
                try
                {
                    response = JsonSerializer.Deserialize<MCPToolsDiscoveredEvent>(resultJson);
                }
                catch
                {
                    _logger.LogWarning("Failed to deserialize MCPToolsDiscoveredEvent from: {Json}", resultJson);
                }

                return Ok(new
                {
                    success = response?.Tools != null,
                    tools = response?.Tools?.Select(t => new
                    {
                        name = t.Name,
                        description = t.Description,
                        serverName = t.ServerName,
                        parameters = t.Parameters.Select(p => new
                        {
                            name = p.Key,
                            type = p.Value.Type,
                            description = p.Value.Description,
                            required = p.Value.Required
                        })
                    })
                });
            }
            catch (TimeoutException tex)
            {
                _logger.LogError(tex, "Tool discovery timed out");
                return StatusCode(504, new { success = false, error = "Tool discovery timed out" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover tools");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get server states
    /// </summary>
    [HttpGet("server-states/{agentId}")]
    public async Task<IActionResult> GetServerStates(string agentId)
    {
        if (!Guid.TryParse(agentId, out var id))
        {
            return BadRequest(new { success = false, error = "Invalid agent ID" });
        }

        if (!_mcpAgents.TryGetValue(id, out var agentInfo))
        {
            return NotFound(new { success = false, error = "MCP agent not found" });
        }

        var serverStates = await agentInfo.agent.GetServerStatesAsync();

        return Ok(new
        {
            success = true,
            serverStates = serverStates.Select(s => new
            {
                serverName = s.ServerName,
                isConnected = s.IsConnected,
                lastConnectedTime = s.LastConnectedTime,
                registeredTools = s.RegisteredTools
            })
        });
    }

    // DTOs
    public class MCPInitRequest
    {
        public List<MCPServerRequest> Servers { get; set; } = new();
        public int? TimeoutSeconds { get; set; }
    }

    public class MCPServerRequest
    {
        public string ServerName { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public List<string>? Args { get; set; }
        public Dictionary<string, string>? Environment { get; set; }
    }

    public class MCPToolCallRequest
    {
        public string AgentId { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public Dictionary<string, object>? Arguments { get; set; }
    }

    public class DiscoverToolsRequest
    {
        public string AgentId { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
    }

    public class MCPToolCallHistory
    {
        public string ServerName { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public Dictionary<string, object>? Arguments { get; set; }
        public bool Success { get; set; }
        public object? Result { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Convert environment dictionary ensuring all values are strings
    /// </summary>
    private Dictionary<string, string> ConvertEnvironmentDictionary(Dictionary<string, string>? input)
    {
        if (input == null) return new Dictionary<string, string>();

        var result = new Dictionary<string, string>();
        foreach (var kvp in input)
        {
            // The value should already be a string, but ensure it's not null
            result[kvp.Key] = kvp.Value ?? string.Empty;
        }

        return result;
    }

    /// <summary>
    /// Format result for better display in UI
    /// </summary>
    private object FormatResultForDisplay(object? result)
    {
        _logger.LogInformation("FormatResultForDisplay called with type: {Type}, value: {Value}",
            result?.GetType().Name ?? "null", result);

        if (result == null) return new { type = "null", value = "" };

        // Handle different result types
        if (result is string str)
        {
            // Check if it's JSON string
            if ((str.StartsWith("{") && str.EndsWith("}")) || (str.StartsWith("[") && str.EndsWith("]")))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<JsonElement>(str);
                    var formatted =
                        JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = true });
                    return new
                    {
                        type = "json",
                        value = str,
                        raw = str,
                        formatted = formatted
                    };
                }
                catch
                {
                    // Not valid JSON, treat as regular string
                }
            }

            // For multiline strings, provide line count
            var lines = str.Split('\n');
            if (lines.Length > 1)
            {
                return new
                {
                    type = "multiline_string",
                    value = str,
                    lineCount = lines.Length,
                    preview = lines.Length > 5 ? string.Join("\n", lines.Take(5)) + "\n..." : str
                };
            }

            return new { type = "string", value = str };
        }

        if (result is IList list)
        {
            var items = new List<object>();
            foreach (var item in list)
            {
                items.Add(FormatResultForDisplay(item));
            }

            return new
            {
                type = "array",
                count = list.Count,
                items = items
            };
        }

        if (result is Dictionary<string, object> dict)
        {
            return new
            {
                type = "object",
                propertyCount = dict.Count,
                properties = dict.ToDictionary(kvp => kvp.Key, kvp => FormatResultForDisplay(kvp.Value))
            };
        }

        if (result is bool || result is int || result is long || result is double || result is decimal)
        {
            return new
            {
                type = result.GetType().Name.ToLower(),
                value = result
            };
        }

        // For complex objects, try to serialize to JSON
        try
        {
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return new
            {
                type = "complex_object",
                typeName = result.GetType().Name,
                value = json,
                formatted = json,
                parsed = JsonSerializer.Deserialize<JsonElement>(json)
            };
        }
        catch
        {
            return new
            {
                type = "unknown",
                typeName = result.GetType().Name,
                value = result.ToString() ?? ""
            };
        }
    }
}