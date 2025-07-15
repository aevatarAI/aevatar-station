using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Controllers;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Executor;
using Aevatar.GAgents.MCP.Core;
using Aevatar.GAgents.MCP.Core.GEvents;
using Aevatar.GAgents.MCP.GEvents;
using Aevatar.GAgents.MCP.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using System.Text.Json;

namespace Aevatar.HttpApi.Controllers;

[Route("api/test-tool-calling")]
[ApiController]
public class TestToolCallingController : AevatarController
{
    private readonly ILogger<TestToolCallingController> _logger;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IGAgentExecutor _gAgentExecutor;
    private readonly IClusterClient _clusterClient;

    private static readonly Dictionary<Guid, (IMCPGAgent agent, GrainId grainId)> _mcpAgents = new();

    public TestToolCallingController(
        ILogger<TestToolCallingController> logger,
        IGAgentFactory gAgentFactory,
        IGAgentExecutor gAgentExecutor,
        IClusterClient clusterClient)
    {
        _logger = logger;
        _gAgentFactory = gAgentFactory;
        _gAgentExecutor = gAgentExecutor;
        _clusterClient = clusterClient;
    }

    /// <summary>
    /// Execute any GAgent's event handler
    /// </summary>
    [HttpPost("execute-gagent-event")]
    public async Task<IActionResult> ExecuteGAgentEvent([FromBody] ExecuteGAgentEventRequest request)
    {
        try
        {
            _logger.LogInformation($"Executing GAgent event: {request.GAgentType}, Event: {request.EventType}");

            // Get the GAgent instance
            var gAgentType = Type.GetType(request.GAgentType);
            if (gAgentType == null)
            {
                return BadRequest($"GAgent type {request.GAgentType} not found");
            }

            var gAgent = await _gAgentFactory.GetGAgentAsync(gAgentType);

            // Create event instance
            var eventType = Type.GetType(request.EventType);
            if (eventType == null)
            {
                return BadRequest($"Event type {request.EventType} not found");
            }

            var eventInstance = JsonSerializer.Deserialize(request.EventData, eventType) as EventBase;
            if (eventInstance == null)
            {
                return BadRequest("Failed to deserialize event data");
            }

            var result = await _gAgentExecutor.ExecuteGAgentEventHandler(gAgent, eventInstance);

            return Ok(new
            {
                Success = true,
                GAgentId = gAgent.GetGrainId().ToString(),
                Result = result,
                Message = "Event executed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GAgent event");
            return StatusCode(500, new { Success = false, Error = ex.Message });
        }
    }

    /// <summary>
    /// Create and test MCPGAgent with Docker-based MCP server
    /// </summary>
    [HttpPost("test-mcp-gagent")]
    public async Task<IActionResult> TestMCPGAgent([FromBody] TestMCPGAgentRequest request)
    {
        try
        {
            _logger.LogInformation($"Testing MCPGAgent with server: {request.MCPServerName}");

            var mcpGAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(new MCPGAgentConfig
            {
                EnableToolDiscovery = true,
                MemberName = request.MCPServerName,
                Server = new MCPServerConfig
                {
                    ServerName = request.MCPServerName ?? "memory",
                    Command = request.NpmCommand ?? "npx",
                    Args = request.DockerImage != null
                        ? ["-y", request.DockerImage]
                        : ["-y", "@modelcontextprotocol/server-memory"],
                    Env = request.Environment ?? new Dictionary<string, string>(),
                }
            });

            var agentId = mcpGAgent.GetPrimaryKey();
            var grainId = mcpGAgent.GetGrainId();

            // Store the agent and grain ID
            _mcpAgents[agentId] = (mcpGAgent, grainId);

            // Get agent description to verify it's working
            var description = await mcpGAgent.GetDescriptionAsync();
            var tools = await mcpGAgent.GetAvailableToolsAsync();

            Logger.LogInformation($"MCPGAgent description: {description}");
            Logger.LogInformation($"MCPGAgent tools count: {tools.Count}");

            return Ok(new
            {
                Success = true,
                AgentId = mcpGAgent.GetGrainId().ToString(),
                Description = description,
                ToolsCount = tools.Count,
                Tools = tools,
                Message = "MCPGAgent created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing MCPGAgent");
            return StatusCode(500, new { Error = ex.Message });
        }
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
                    response = System.Text.Json.JsonSerializer.Deserialize<MCPToolsDiscoveredEvent>(resultJson);
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
                Arguments = ConvertJsonElementToBasicTypes(request.Arguments ?? new Dictionary<string, object>())
            };

            try
            {
                // Execute the event through GAgentExecutor
                var resultJson = await _gAgentExecutor.ExecuteGAgentEventHandler(agentInfo.grainId, toolCallEvent);

                // Try to parse the result as MCPToolResponseEvent
                MCPToolResponseEvent? response = null;
                try
                {
                    _logger.LogInformation("Raw result JSON: {Json}", resultJson);
                    response = System.Text.Json.JsonSerializer.Deserialize<MCPToolResponseEvent>(resultJson);
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
                else
                {
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

    private Dictionary<string, object> ConvertJsonElementToBasicTypes(Dictionary<string, object> input)
    {
        var result = new Dictionary<string, object>();

        foreach (var kvp in input)
        {
            result[kvp.Key] = ConvertValue(kvp.Value);
        }

        return result;
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

    private object? ConvertValue(object? value)
    {
        if (value == null) return null;

        if (value is JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intValue))
                        return intValue;
                    if (element.TryGetInt64(out var longValue))
                        return longValue;
                    if (element.TryGetDouble(out var doubleValue))
                        return doubleValue;
                    return element.GetDecimal();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(ConvertValue(item));
                    }

                    return list;
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in element.EnumerateObject())
                    {
                        dict[prop.Name] = ConvertValue(prop.Value);
                    }

                    return dict;
                default:
                    return element.ToString();
            }
        }

        // If it's already a basic type, return as-is
        return value;
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

// Request DTOs
public class ExecuteGAgentEventRequest
{
    public string GAgentType { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = "{}";
}

public class TestMCPGAgentRequest
{
    public Guid? AgentId { get; set; }
    public string? MCPServerName { get; set; } = string.Empty;
    public string? DockerImage { get; set; }
    public string? NpmCommand { get; set; }
    public bool? AutoStartServer { get; set; }
    public Dictionary<string, string>? Environment { get; set; }
    public string? TestToolName { get; set; }
    public Dictionary<string, object>? TestToolParameters { get; set; }
    public string SystemLLM { get; set; }
}

public class TestAIGAgentRequest
{
    public Guid? AgentId { get; set; }
    public string? ModelProvider { get; set; }
    public string? ModelName { get; set; }
    public string? ApiKey { get; set; }
    public string? SystemPrompt { get; set; }
    public List<MCPServerConfig>? MCPServers { get; set; }
    public List<string>? GAgentTypes { get; set; }
    public string? TestPrompt { get; set; }
}

public class DiscoverToolsRequest
{
    public string AgentId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
}

public class MCPToolCallRequest
{
    public string AgentId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, object>? Arguments { get; set; }
}