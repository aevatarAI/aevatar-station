using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.MCPGateway;

namespace Aevatar.Application.MCPGateway;

/// <summary>
/// Interface for MCP Gateway management operations
/// </summary>
public interface IMCPGatewayManager
{
    /// <summary>
    /// Create a new adapter in the gateway
    /// </summary>
    Task<MCPAdapterDto> CreateAdapterAsync(CreateMCPAdapterDto input);

    /// <summary>
    /// Update an existing adapter
    /// </summary>
    Task<MCPAdapterDto> UpdateAdapterAsync(string name, UpdateMCPAdapterDto input);

    /// <summary>
    /// Delete an adapter from the gateway
    /// </summary>
    Task DeleteAdapterAsync(string name);

    /// <summary>
    /// Get all adapters from the gateway
    /// </summary>
    Task<List<MCPAdapterDto>> GetAllAdaptersAsync();

    /// <summary>
    /// Get a specific adapter by name
    /// </summary>
    Task<MCPAdapterDto?> GetAdapterAsync(string name);

    /// <summary>
    /// Get adapter status and health information
    /// </summary>
    Task<MCPAdapterStatusDto> GetAdapterStatusAsync(string name);

    /// <summary>
    /// Get gateway health status
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
    /// Get adapter logs (corresponds to GET /adapters/{name}/logs)
    /// </summary>
    Task<List<string>> GetAdapterLogsAsync(string name, int? lines = null, bool? follow = null);
}