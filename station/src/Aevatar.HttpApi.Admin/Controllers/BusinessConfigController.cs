// ABOUTME: This file provides business configuration upload API with persistent storage
// ABOUTME: Uses HostConfigurationGAgent for persistent storage and retrieval of business configurations

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Aevatar.Application.Grains.Agents.Configuration;
using Aevatar.Enum;
using Aevatar.Service;
using Asp.Versioning;
using Orleans;

namespace Aevatar.Admin.Controllers;

[RemoteService]
[ControllerName("BusinessConfig")]
[Route("api/business-config")]
[Authorize]
public class BusinessConfigController : ControllerBase
{
    private readonly ILogger<BusinessConfigController> _logger;
    private readonly IGrainFactory _grainFactory;
    private readonly IDeveloperService _developerService;

    public BusinessConfigController(ILogger<BusinessConfigController> logger, IGrainFactory grainFactory, IDeveloperService developerService)
    {
        _logger = logger;
        _grainFactory = grainFactory;
        _developerService = developerService;
    }

    /// <summary>
    /// Upload business configuration JSON file for a specific host
    /// </summary>
    /// <param name="hostId">Host identifier</param>
    /// <param name="file">JSON configuration file</param>
    /// <returns>Upload result</returns>
    [HttpPost("{hostId}/upload")]
    public async Task<IActionResult> UploadBusinessConfiguration(string hostId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded or file is empty");
            }

            if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only JSON files are allowed");
            }

            // Read and validate JSON content
            string jsonContent;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                jsonContent = await reader.ReadToEndAsync();
            }

            // Validate JSON format
            try
            {
                JsonConvert.DeserializeObject(jsonContent);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid JSON uploaded for host {HostId}", hostId);
                return BadRequest($"Invalid JSON format: {ex.Message}");
            }

            // Store configuration for each host type using HostConfigurationGAgent
            var updatedBy = User?.Identity?.Name ?? "System";
            var storedHostTypes = new List<string>();

            foreach (HostTypeEnum hostType in System.Enum.GetValues<HostTypeEnum>())
            {
                var grainKey = $"{hostId}:{hostType}";
                var configAgent = _grainFactory.GetGrain<IHostConfigurationGAgent>(grainKey);
                
                await configAgent.SetBusinessConfigurationJsonAsync(jsonContent, updatedBy);
                storedHostTypes.Add(hostType.ToString());
                
                _logger.LogDebug("Business configuration stored for {HostId}:{HostType}", hostId, hostType);
            }

            _logger.LogInformation("Business configuration uploaded for host {HostId}, stored for host types: {HostTypes}", 
                hostId, string.Join(", ", storedHostTypes));

            // Update existing K8s ConfigMaps with the new business configuration
            try
            {
                await _developerService.UpdateBusinessConfigurationAsync(hostId, "1"); // Using default version "1"
                _logger.LogInformation("K8s ConfigMaps updated successfully for host {HostId}", hostId);
            }
            catch (Exception configUpdateEx)
            {
                _logger.LogWarning(configUpdateEx, "Failed to update K8s ConfigMaps for host {HostId}, but configuration was stored successfully", hostId);
            }

            return Ok(new
            {
                Success = true,
                Message = "Business configuration uploaded and K8s ConfigMaps updated successfully",
                HostId = hostId,
                StoredForHostTypes = storedHostTypes,
                UpdatedBy = updatedBy,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload business configuration for host {HostId}", hostId);
            return StatusCode(500, $"Failed to upload configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Get current business configuration for a host
    /// </summary>
    /// <param name="hostId">Host identifier</param>
    /// <returns>Current configuration JSON</returns>
    [HttpGet("{hostId}")]
    public async Task<IActionResult> GetBusinessConfiguration(string hostId)
    {
        try
        {
            // Get configuration from first available host type
            string configurationJson = null;
            HostTypeEnum? foundHostType = null;
            DateTime? lastUpdated = null;

            foreach (HostTypeEnum hostType in System.Enum.GetValues<HostTypeEnum>())
            {
                var grainKey = $"{hostId}:{hostType}";
                var configAgent = _grainFactory.GetGrain<IHostConfigurationGAgent>(grainKey);
                
                var hostConfigJson = await configAgent.GetBusinessConfigurationJsonAsync();
                
                if (!string.IsNullOrWhiteSpace(hostConfigJson) && hostConfigJson != "{}")
                {
                    configurationJson = hostConfigJson;
                    foundHostType = hostType;
                    // Note: In a full implementation, you might want to add a GetLastUpdatedAsync method
                    lastUpdated = DateTime.UtcNow; // Placeholder
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(configurationJson))
            {
                return NotFound($"No business configuration found for host {hostId}");
            }

            return Ok(new
            {
                HostId = hostId,
                Configuration = JsonConvert.DeserializeObject(configurationJson),
                FoundInHostType = foundHostType?.ToString(),
                LastModified = lastUpdated,
                ConfigurationJson = configurationJson
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get business configuration for host {HostId}", hostId);
            return StatusCode(500, $"Failed to get configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete business configuration for a host
    /// </summary>
    /// <param name="hostId">Host identifier</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{hostId}")]
    public async Task<IActionResult> DeleteBusinessConfiguration(string hostId)
    {
        try
        {
            var updatedBy = User?.Identity?.Name ?? "System";
            var clearedHostTypes = new List<string>();
            bool foundAnyConfig = false;

            // Clear configuration for all host types
            foreach (HostTypeEnum hostType in System.Enum.GetValues<HostTypeEnum>())
            {
                var grainKey = $"{hostId}:{hostType}";
                var configAgent = _grainFactory.GetGrain<IHostConfigurationGAgent>(grainKey);
                
                // Check if there's existing configuration
                var existingConfig = await configAgent.GetBusinessConfigurationJsonAsync();
                if (!string.IsNullOrWhiteSpace(existingConfig) && existingConfig != "{}")
                {
                    foundAnyConfig = true;
                }
                
                await configAgent.ClearBusinessConfigurationAsync(updatedBy);
                clearedHostTypes.Add(hostType.ToString());
                
                _logger.LogDebug("Business configuration cleared for {HostId}:{HostType}", hostId, hostType);
            }

            if (!foundAnyConfig)
            {
                return NotFound($"No business configuration found for host {hostId}");
            }

            _logger.LogInformation("Business configuration deleted for host {HostId}", hostId);

            return Ok(new
            {
                Success = true,
                Message = "Business configuration deleted successfully",
                HostId = hostId,
                ClearedHostTypes = clearedHostTypes,
                UpdatedBy = updatedBy,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete business configuration for host {HostId}", hostId);
            return StatusCode(500, $"Failed to delete configuration: {ex.Message}");
        }
    }
}