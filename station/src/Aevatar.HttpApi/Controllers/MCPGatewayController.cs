using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.MCPGateway;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace Aevatar.Controllers;

/// <summary>
/// Controller for MCP Gateway management operations
/// </summary>
[ApiController]
[Route("api/mcp-gateway")]
//[Authorize]
public class MCPGatewayController : AbpControllerBase, IMCPGatewayAppService
{
    private readonly IMCPGatewayAppService _mcpGatewayAppService;
    private readonly ILogger<MCPGatewayController> _logger;

    public MCPGatewayController(
        IMCPGatewayAppService mcpGatewayAppService,
        ILogger<MCPGatewayController> logger)
    {
        _mcpGatewayAppService = mcpGatewayAppService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new MCP adapter
    /// </summary>
    /// <param name="input">Adapter creation parameters</param>
    /// <returns>Created adapter information</returns>
    [HttpPost("adapters")]
    [ProducesResponseType(typeof(MCPAdapterDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<MCPAdapterDto> CreateAdapterAsync(CreateMCPAdapterDto input)
    {
        _logger.LogInformation("API request to create adapter {AdapterName}", input.Name);
        
        var result = await _mcpGatewayAppService.CreateAdapterAsync(input);
        
        // Set 201 Created status
        Response.StatusCode = StatusCodes.Status201Created;
        
        return result;
    }

    /// <summary>
    /// Update an existing MCP adapter
    /// </summary>
    /// <param name="name">Adapter name</param>
    /// <param name="input">Update parameters</param>
    /// <returns>Updated adapter information</returns>
    [HttpPut("adapters/{name}")]
    [ProducesResponseType(typeof(MCPAdapterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<MCPAdapterDto> UpdateAdapterAsync(string name, UpdateMCPAdapterDto input)
    {
        _logger.LogInformation("API request to update adapter {AdapterName}", name);
        
        return await _mcpGatewayAppService.UpdateAdapterAsync(name, input);
    }

    /// <summary>
    /// Delete an MCP adapter
    /// </summary>
    /// <param name="name">Adapter name</param>
    [HttpDelete("adapters/{name}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task DeleteAdapterAsync(string name)
    {
        _logger.LogInformation("API request to delete adapter {AdapterName}", name);
        
        await _mcpGatewayAppService.DeleteAdapterAsync(name);
        
        // Set 204 No Content status
        Response.StatusCode = StatusCodes.Status204NoContent;
    }

    /// <summary>
    /// Get all MCP adapters with pagination and filtering
    /// </summary>
    /// <param name="input">Query parameters</param>
    /// <returns>Paginated list of adapters</returns>
    [HttpGet("adapters")]
    [ProducesResponseType(typeof(PagedResultDto<MCPAdapterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<PagedResultDto<MCPAdapterDto>> GetAdaptersAsync([FromQuery] GetAdaptersInput input)
    {
        _logger.LogDebug("API request to get adapters with search: {Search}", input.Search);
        
        return await _mcpGatewayAppService.GetAdaptersAsync(input);
    }

    /// <summary>
    /// Get a specific MCP adapter by name
    /// </summary>
    /// <param name="name">Adapter name</param>
    /// <returns>Adapter information</returns>
    [HttpGet("adapters/{name}")]
    [ProducesResponseType(typeof(MCPAdapterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<MCPAdapterDto> GetAdapterAsync(string name)
    {
        _logger.LogDebug("API request to get adapter {AdapterName}", name);
        
        return await _mcpGatewayAppService.GetAdapterAsync(name);
    }

    /// <summary>
    /// Get the status of a specific MCP adapter
    /// </summary>
    /// <param name="name">Adapter name</param>
    /// <returns>Adapter status information</returns>
    [HttpGet("adapters/{name}/status")]
    [ProducesResponseType(typeof(MCPAdapterStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<MCPAdapterStatusDto> GetAdapterStatusAsync(string name)
    {
        _logger.LogDebug("API request to get adapter status {AdapterName}", name);
        
        return await _mcpGatewayAppService.GetAdapterStatusAsync(name);
    }

    /// <summary>
    /// Get health status of the MCP Gateway
    /// </summary>
    /// <returns>Gateway health information</returns>
    [HttpGet("gateway/health")]
    [ProducesResponseType(typeof(MCPGatewayHealthDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<MCPGatewayHealthDto> GetGatewayHealthAsync()
    {
        _logger.LogDebug("API request to get gateway health");
        
        return await _mcpGatewayAppService.GetGatewayHealthAsync();
    }

    /// <summary>
    /// Test connection to a specific adapter
    /// </summary>
    /// <param name="name">Adapter name</param>
    /// <returns>Connection test results</returns>
    [HttpPost("adapters/{name}/test")]
    [ProducesResponseType(typeof(MCPConnectionTestResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<MCPConnectionTestResultDto> TestAdapterConnectionAsync(string name)
    {
        _logger.LogInformation("API request to test adapter connection {AdapterName}", name);
        
        return await _mcpGatewayAppService.TestAdapterConnectionAsync(name);
    }

    /// <summary>
    /// Get adapter metrics and usage statistics
    /// </summary>
    /// <param name="name">Adapter name</param>
    /// <param name="from">Start time for metrics (optional)</param>
    /// <param name="to">End time for metrics (optional)</param>
    /// <returns>Adapter metrics</returns>
    [HttpGet("adapters/{name}/metrics")]
    [ProducesResponseType(typeof(MCPAdapterMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<MCPAdapterMetricsDto> GetAdapterMetricsAsync(
        string name, 
        [FromQuery] DateTime? from = null, 
        [FromQuery] DateTime? to = null)
    {
        _logger.LogDebug("API request to get adapter metrics {AdapterName} from {From} to {To}", 
            name, from, to);
        
        return await _mcpGatewayAppService.GetAdapterMetricsAsync(name, from, to);
    }

    /// <summary>
    /// Get adapter logs
    /// </summary>
    /// <param name="name">Adapter name</param>
    /// <param name="lines">Number of lines to retrieve (optional)</param>
    /// <param name="follow">Whether to follow logs (optional)</param>
    /// <returns>List of log lines</returns>
    [HttpGet("adapters/{name}/logs")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<List<string>> GetAdapterLogsAsync(
        string name, 
        [FromQuery] int? lines = null, 
        [FromQuery] bool? follow = null)
    {
        _logger.LogDebug("API request to get adapter logs {AdapterName} (lines: {Lines}, follow: {Follow})", 
            name, lines, follow);
        
        return await _mcpGatewayAppService.GetAdapterLogsAsync(name, lines, follow);
    }
}
