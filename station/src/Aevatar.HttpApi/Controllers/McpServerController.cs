using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGAgents;
using Aevatar.GAgents.MCP.Core.Extensions;
using Aevatar.GAgents.MCP.Options;
using Aevatar.Mcp;
using Aevatar.Permissions;
using Aevatar.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Mcp")]
[Route("api/mcp/servers")]
[Authorize]
public class McpServerController : AevatarController
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IMcpExtensionWrapper _mcpExtensionWrapper;
    private readonly ILogger<McpServerController> _logger;

    public McpServerController(
        ILogger<McpServerController> logger,
        IGAgentFactory gAgentFactory,
        IMcpExtensionWrapper mcpExtensionWrapper)
    {
        _logger = logger;
        _gAgentFactory = gAgentFactory;
        _mcpExtensionWrapper = mcpExtensionWrapper;
    }

    /// <summary>
    /// Get a list of MCP servers with filtering and pagination
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AevatarPermissions.McpServers.Default)]
    public async Task<PagedResultDto<McpServerDto>> GetListAsync([FromQuery] GetMcpServerListDto input)
    {
        // Validate pagination parameters
        if (input.MaxResultCount <= 0 || input.MaxResultCount > 100)
        {
            throw new UserFriendlyException("Page size must be between 1 and 100");
        }

        if (input.SkipCount < 0)
        {
            throw new UserFriendlyException("Skip count cannot be negative");
        }

        _logger.LogInformation(
            "Getting MCP server list with filter: {filter}, Page: {page}, PageSize: {pageSize}, Sort: {sort}",
            JsonConvert.SerializeObject(input), input.PageNumber, input.MaxResultCount, input.GetSortingDescription());

        var mcpServerConfigs = await GetMCPServerConfigsAsync();
        var serverList = mcpServerConfigs.Select(kvp => ConvertToDto(kvp.Key, kvp.Value)).ToList();

        // Apply filters
        if (!string.IsNullOrEmpty(input.ServerName))
        {
            serverList = serverList
                .Where(s => s.ServerName.Contains(input.ServerName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrEmpty(input.ServerType))
        {
            serverList = serverList
                .Where(s => s.ServerType.Equals(input.ServerType, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrEmpty(input.SearchTerm))
        {
            serverList = serverList.Where(s =>
                s.ServerName.Contains(input.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                s.Command.Contains(input.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains(input.SearchTerm, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        // Apply sorting
        if (!string.IsNullOrEmpty(input.Sorting))
        {
            serverList = ApplySorting(serverList, input.Sorting);
        }
        else
        {
            // Default sorting by ServerName ascending
            serverList = serverList.OrderBy(s => s.ServerName).ToList();
        }

        // Apply pagination
        var totalCount = serverList.Count;
        var pagedList = serverList.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        return new PagedResultDto<McpServerDto>(totalCount, pagedList);
    }

    /// <summary>
    /// Get a specific MCP server by name
    /// </summary>
    [HttpGet("{serverName}")]
    [Authorize(Policy = AevatarPermissions.McpServers.Default)]
    public async Task<McpServerDto> GetAsync(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new UserFriendlyException("Server name is required");
        }

        _logger.LogInformation("Getting MCP server: {serverName}", serverName);

        var mcpServerConfigs = await GetMCPServerConfigsAsync();

        if (!mcpServerConfigs.TryGetValue(serverName, out var config))
        {
            throw new UserFriendlyException($"MCP server '{serverName}' not found");
        }

        return ConvertToDto(serverName, config);
    }

    /// <summary>
    /// Create a new MCP server
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AevatarPermissions.McpServers.Create)]
    public async Task<McpServerDto> CreateAsync([FromBody] CreateMcpServerDto input)
    {
        if (input == null)
        {
            throw new UserFriendlyException("Invalid input data");
        }

        if (string.IsNullOrWhiteSpace(input.ServerName))
        {
            throw new UserFriendlyException("Server name is required");
        }

        if (string.IsNullOrWhiteSpace(input.Command))
        {
            throw new UserFriendlyException("Server command is required");
        }

        _logger.LogInformation("Creating MCP server: {serverName}", input.ServerName);

        var mcpServerConfigs = await GetMCPServerConfigsAsync();

        if (mcpServerConfigs.ContainsKey(input.ServerName))
        {
            throw new UserFriendlyException($"MCP server '{input.ServerName}' already exists");
        }

        var newConfig = new MCPServerConfig
        {
            ServerName = input.ServerName,
            Command = input.Command,
            Args = input.Args ?? [],
            Env = input.Env,
            Description = input.Description,
            Url = input.Url
        };

        // Add the new server to the configuration
        mcpServerConfigs[input.ServerName] = newConfig;

        // Update the whitelist
        var configJson = JsonConvert.SerializeObject(mcpServerConfigs);
        var success = await ConfigWhitelistAsync(configJson);

        if (!success)
        {
            throw new UserFriendlyException("Failed to create MCP server configuration");
        }

        _logger.LogInformation("Successfully created MCP server: {serverName}", input.ServerName);

        return ConvertToDto(input.ServerName, newConfig);
    }

    /// <summary>
    /// Update an existing MCP server
    /// </summary>
    [HttpPut("{serverName}")]
    [Authorize(Policy = AevatarPermissions.McpServers.Edit)]
    public async Task<McpServerDto> UpdateAsync(string serverName, [FromBody] UpdateMcpServerDto input)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new UserFriendlyException("Server name is required");
        }

        if (input == null)
        {
            throw new UserFriendlyException("Invalid input data");
        }

        _logger.LogInformation("Updating MCP server: {serverName}", serverName);

        var mcpServerConfigs = await GetMCPServerConfigsAsync();

        if (!mcpServerConfigs.TryGetValue(serverName, out var existingConfig))
        {
            throw new UserFriendlyException($"MCP server '{serverName}' not found");
        }

        // Update the configuration with non-null values from input
        var updatedConfig = new MCPServerConfig
        {
            ServerName = existingConfig.ServerName, // Keep the original server name
            Command = input.Command ?? existingConfig.Command,
            Args = input.Args ?? existingConfig.Args,
            Env = input.Env ?? existingConfig.Env,
            Description = input.Description ?? existingConfig.Description,
            Url = input.Url ?? existingConfig.Url
        };

        // Update the whitelist
        mcpServerConfigs[serverName] = updatedConfig;
        var configJson = JsonConvert.SerializeObject(mcpServerConfigs);
        var success = await ConfigWhitelistAsync(configJson);

        if (!success)
        {
            throw new UserFriendlyException("Failed to update MCP server configuration");
        }

        _logger.LogInformation("Successfully updated MCP server: {serverName}", serverName);

        return ConvertToDto(serverName, updatedConfig);
    }

    /// <summary>
    /// Delete an MCP server
    /// </summary>
    [HttpDelete("{serverName}")]
    [Authorize(Policy = AevatarPermissions.McpServers.Delete)]
    public async Task DeleteAsync(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new UserFriendlyException("Server name is required");
        }

        _logger.LogInformation("Deleting MCP server: {serverName}", serverName);

        var mcpServerConfigs = await GetMCPServerConfigsAsync();

        if (!mcpServerConfigs.ContainsKey(serverName))
        {
            throw new UserFriendlyException($"MCP server '{serverName}' not found");
        }

        // Remove the server from configuration
        mcpServerConfigs.Remove(serverName);

        // Update the whitelist
        var configJson = JsonConvert.SerializeObject(mcpServerConfigs);
        var success = await ConfigWhitelistAsync(configJson);

        if (!success)
        {
            throw new UserFriendlyException("Failed to delete MCP server configuration");
        }

        _logger.LogInformation("Successfully deleted MCP server: {serverName}", serverName);
    }

    /// <summary>
    /// Get all MCP server names
    /// </summary>
    [HttpGet("names")]
    [Authorize(Policy = AevatarPermissions.McpServers.Default)]
    public async Task<List<string>> GetServerNamesAsync()
    {
        var mcpServerConfigs = await GetMCPServerConfigsAsync();
        return mcpServerConfigs.Keys.ToList();
    }

    /// <summary>
    /// Get raw MCP server configurations (for backward compatibility)
    /// </summary>
    [HttpGet("raw")]
    [Authorize(Policy = AevatarPermissions.McpServers.Default)]
    public async Task<Dictionary<string, MCPServerConfig>> GetRawConfigurationsAsync()
    {
        return await GetMCPServerConfigsAsync();
    }

    /// <summary>
    /// Apply sorting to the server list based on the sorting parameter
    /// </summary>
    /// <param name="serverList">List of servers to sort</param>
    /// <param name="sorting">Sorting parameter (e.g., "serverName desc", "command asc", "description")</param>
    /// <returns>Sorted list of servers</returns>
    private static List<McpServerDto> ApplySorting(List<McpServerDto> serverList, string sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting))
        {
            return serverList.OrderBy(s => s.ServerName).ToList();
        }

        var sortParts = sorting.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sortField = sortParts[0].ToLower();
        var sortDirection = sortParts.Length > 1 && sortParts[1].ToLower() == "desc" ? "desc" : "asc";

        return sortField switch
        {
            "servername" => sortDirection == "desc"
                ? serverList.OrderByDescending(s => s.ServerName).ToList()
                : serverList.OrderBy(s => s.ServerName).ToList(),

            "command" => sortDirection == "desc"
                ? serverList.OrderByDescending(s => s.Command).ToList()
                : serverList.OrderBy(s => s.Command).ToList(),

            "description" => sortDirection == "desc"
                ? serverList.OrderByDescending(s => s.Description).ToList()
                : serverList.OrderBy(s => s.Description).ToList(),

            "servertype" => sortDirection == "desc"
                ? serverList.OrderByDescending(s => s.ServerType).ToList()
                : serverList.OrderBy(s => s.ServerType).ToList(),

            "createdat" => sortDirection == "desc"
                ? serverList.OrderByDescending(s => s.CreatedAt).ToList()
                : serverList.OrderBy(s => s.CreatedAt).ToList(),

            "modifiedat" => sortDirection == "desc"
                ? serverList.OrderByDescending(s => s.ModifiedAt ?? DateTime.MinValue).ToList()
                : serverList.OrderBy(s => s.ModifiedAt ?? DateTime.MinValue).ToList(),

            _ => serverList.OrderBy(s => s.ServerName).ToList() // Default fallback
        };
    }

    /// <summary>
    /// Convert MCPServerConfig to McpServerDto
    /// </summary>
    private static McpServerDto ConvertToDto(string serverName, MCPServerConfig config)
    {
        return new McpServerDto
        {
            ServerName = serverName,
            Command = config.Command,
            Args = config.Args ?? [],
            Env = config.Env ?? new(),
            Description = config.Description,
            Url = config.Url,
            CreatedAt = DateTime.UtcNow, // Placeholder - consider adding timestamp to MCPServerConfig
            ModifiedAt = null // No modification timestamp available in MCPServerConfig
        };
    }

    private IConfigManagerGAgent _mcpServerConfigGAgent;

    private async Task<Dictionary<string, MCPServerConfig>> GetMCPServerConfigsAsync()
    {
        _mcpServerConfigGAgent = await _mcpExtensionWrapper.GetMcpServerConfigManagerAsync();
        return await _mcpExtensionWrapper.GetMCPWhiteListAsync(_mcpServerConfigGAgent);
    }

    private async Task<bool> ConfigWhitelistAsync(string configJson)
    {
        return await _mcpExtensionWrapper.ConfigMCPWhitelistAsync(_mcpServerConfigGAgent, configJson);
    }
}