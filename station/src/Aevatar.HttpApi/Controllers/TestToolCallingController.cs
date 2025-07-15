using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Controllers;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Executor;
using Aevatar.GAgents.MCP.Core;
using Aevatar.GAgents.MCP.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;

namespace Aevatar.HttpApi.Controllers;

[Route("api/test-tool-calling")]
[ApiController]
public class TestToolCallingController : AevatarController
{
    private readonly ILogger<TestToolCallingController> _logger;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IGAgentExecutor _gAgentExecutor;
    private readonly IClusterClient _clusterClient;

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

            var eventInstance = JsonConvert.DeserializeObject(request.EventData, eventType) as EventBase;
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
                        : ["-y", "@modelcontextprotocol/server-memory", "/tmp"],
                    Env = request.Environment ?? new Dictionary<string, string>(),
                }
            });

            // Get agent description to verify it's working
            var description = await mcpGAgent.GetDescriptionAsync();
            var tools = await mcpGAgent.GetAvailableToolsAsync();

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

    // /// <summary>
    // /// Create and test AIGAgent with tool integration
    // /// </summary>
    // [HttpPost("test-ai-gagent")]
    // public async Task<IActionResult> TestAIGAgent([FromBody] TestAIGAgentRequest request)
    // {
    //     try
    //     {
    //         _logger.LogInformation("Testing AIGAgent with tool configuration");
    //
    //         var aiGAgent = await _gAgentFactory.GetGAgentAsync<IDynamicToolAIGAgent>();
    //
    //         var configuration = new TestAIGAgentConfiguration
    //         {
    //             ModelProvider = request.ModelProvider ?? "openai",
    //             ModelName = request.ModelName ?? "gpt-4",
    //             ApiKey = request.ApiKey,
    //             SystemPrompt = request.SystemPrompt ?? "You are a helpful AI assistant."
    //         };
    //
    //         await aiGAgent.ConfigAsync(configuration);
    //
    //         // Configure MCP servers if provided
    //         if (request.MCPServers?.Any() == true)
    //         {
    //             await aiGAgent.ConfigureMCPServersAsync(request.MCPServers);
    //         }
    //
    //         // Configure GAgent tools if provided
    //         if (request.GAgentTypes?.Any() == true)
    //         {
    //             await aiGAgent.ConfigureGAgentToolsAsync(request.GAgentTypes);
    //         }
    //
    //         // Get description
    //         var description = await aiGAgent.GetDescriptionAsync();
    //
    //         // Get available tools
    //         var tools = await aiGAgent.GetAvailableToolsAsync();
    //
    //         // Test chat if prompt provided
    //         string? chatResponse = null;
    //         if (!string.IsNullOrEmpty(request.TestPrompt))
    //         {
    //             chatResponse = await aiGAgent.ChatAsync(request.TestPrompt);
    //         }
    //
    //         return Ok(new
    //         {
    //             Success = true,
    //             AgentId = aiGAgent.GetGrainId().ToString(),
    //             Description = description,
    //             AvailableTools = tools.Select(t => new
    //             {
    //                 t.Name,
    //                 t.Description,
    //                 t.Type,
    //                 t.Parameters
    //             }),
    //             ChatResponse = chatResponse,
    //             Configuration = configuration,
    //             Message = "AIGAgent created and configured successfully"
    //         });
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error testing AIGAgent");
    //         return StatusCode(500, new { Success = false, Error = ex.Message });
    //     }
    // }
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