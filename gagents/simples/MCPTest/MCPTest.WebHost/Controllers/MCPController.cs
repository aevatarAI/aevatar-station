using MCPTest.WebHost.Services;
using Microsoft.AspNetCore.Mvc;

namespace MCPTest.WebHost.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MCPController : ControllerBase
{
    private readonly RealMCPTestService _mcpTestService;
    private readonly EnvironmentVariableService _envService;
    private readonly ILogger<MCPController> _logger;

    public MCPController(
        RealMCPTestService mcpTestService,
        EnvironmentVariableService envService,
        ILogger<MCPController> logger)
    {
        _mcpTestService = mcpTestService;
        _envService = envService;
        _logger = logger;
    }

    /// <summary>
    /// Get all available MCP servers
    /// </summary>
    [HttpGet("servers")]
    public ActionResult<List<MCPServerInfo>> GetServers()
    {
        try
        {
            var servers = _mcpTestService.GetAvailableServers();
            return Ok(servers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting MCP servers");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get available tools for a specific MCP server
    /// </summary>
    [HttpGet("servers/{serverName}/tools")]
    public async Task<ActionResult<List<MCPToolInfo>>> GetTools(string serverName)
    {
        try
        {
            var tools = await _mcpTestService.GetAvailableToolsAsync(serverName);
            return Ok(tools);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tools for server {ServerName}", serverName);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Call a specific tool on an MCP server
    /// </summary>
    [HttpPost("servers/{serverName}/tools/{toolName}/call")]
    public async Task<ActionResult<MCPToolResult>> CallTool(
        string serverName, 
        string toolName, 
        [FromBody] Dictionary<string, object> arguments)
    {
        try
        {
            _logger.LogInformation("Calling tool {ToolName} on server {ServerName} with arguments: {Arguments}", 
                toolName, serverName, string.Join(", ", arguments.Select(kv => $"{kv.Key}={kv.Value}")));

            var result = await _mcpTestService.CallToolAsync(serverName, toolName, arguments);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling tool {ToolName} on server {ServerName}", toolName, serverName);
            return StatusCode(500, new MCPToolResult
            {
                Success = false,
                Error = "Internal server error: " + ex.Message,
                ExecutionTime = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Test endpoint to verify API is working
    /// </summary>
    [HttpGet("health")]
    public ActionResult<object> Health()
    {
        return Ok(new
        {
            Status = "OK",
            Timestamp = DateTime.UtcNow,
            Message = "MCP Test API is running"
        });
    }

    /// <summary>
    /// Get available environment variables for debugging
    /// </summary>
    [HttpGet("debug/environment")]
    public ActionResult<object> GetEnvironmentVariables()
    {
        try
        {
            var envVars = _envService.GetAvailableEnvironmentVariables();
            return Ok(new
            {
                AvailableVariables = envVars,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting environment variables");
            return StatusCode(500, "Internal server error");
        }
    }
}
