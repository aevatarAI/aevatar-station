using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Aevatar.Application.Contracts.MCPGateway;

/// <summary>
/// Application service for MCP Gateway management
/// </summary>
public interface IMCPGatewayAppService : IApplicationService
{
    /// <summary>
    /// Create a new MCP adapter
    /// </summary>
    Task<MCPAdapterDto> CreateAdapterAsync(CreateMCPAdapterDto input);

    /// <summary>
    /// Update an existing MCP adapter
    /// </summary>
    Task<MCPAdapterDto> UpdateAdapterAsync(string name, UpdateMCPAdapterDto input);

    /// <summary>
    /// Delete an MCP adapter
    /// </summary>
    Task DeleteAdapterAsync(string name);

    /// <summary>
    /// Get all MCP adapters with pagination
    /// </summary>
    Task<PagedResultDto<MCPAdapterDto>> GetAdaptersAsync(GetAdaptersInput input);

    /// <summary>
    /// Get a specific MCP adapter by name
    /// </summary>
    Task<MCPAdapterDto> GetAdapterAsync(string name);

    /// <summary>
    /// Get the status of a specific MCP adapter
    /// </summary>
    Task<MCPAdapterStatusDto> GetAdapterStatusAsync(string name);

    /// <summary>
    /// Get health status of the MCP Gateway
    /// </summary>
    Task<MCPGatewayHealthDto> GetGatewayHealthAsync();

    /// <summary>
    /// Test connection to a specific adapter
    /// </summary>
    Task<MCPConnectionTestResultDto> TestAdapterConnectionAsync(string name);

    /// <summary>
    /// Get adapter metrics and usage statistics
    /// </summary>
    Task<MCPAdapterMetricsDto> GetAdapterMetricsAsync(string name, DateTime? from = null, DateTime? to = null);

    /// <summary>
    /// Get adapter logs
    /// </summary>
    Task<List<string>> GetAdapterLogsAsync(string name, int? lines = null, bool? follow = null);
}
