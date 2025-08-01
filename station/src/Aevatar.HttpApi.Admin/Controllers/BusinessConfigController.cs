using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Aevatar.Application.Grains.Agents.Configuration;
using Aevatar.Common;
using Aevatar.Controllers;
using Aevatar.Enum;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Orleans;

namespace Aevatar.Admin.Controllers;

[RemoteService]
[ControllerName("BusinessConfig")]
[Route("api/business-config")]
[Authorize]
public class BusinessConfigController : AevatarController
{
    private readonly ILogger<BusinessConfigController> _logger;
    private readonly IGrainFactory _grainFactory;
    private readonly IDeveloperService _developerService;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public BusinessConfigController(
        ILogger<BusinessConfigController> logger, 
        IGrainFactory grainFactory, 
        IDeveloperService developerService,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _grainFactory = grainFactory;
        _developerService = developerService;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    /// <summary>
    /// Upload business configuration JSON file for a specific host and host type
    /// </summary>
    /// <param name="hostType">Host type enum</param>
    /// <param name="file">JSON configuration file</param>
    /// <returns>Upload result</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadBusinessConfiguration( [FromForm] HostTypeEnum hostType, IFormFile file)
    {
        var hostId = GetHostId();
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

            // Store configuration for the specified host type using HostConfigurationGAgent
            var updatedBy = User?.Identity?.Name ?? "System";
            var grainKey = $"{hostId}:{hostType}";
            var configAgent = _grainFactory.GetGrain<IHostConfigurationGAgent>(GuidUtil.StringToGuid(grainKey));
            
            await configAgent.SetBusinessConfigurationJsonAsync(jsonContent, updatedBy);
            
            _logger.LogInformation("Business configuration uploaded for {HostId}:{HostType}", hostId, hostType);

            // Update existing K8s ConfigMaps with the new business configuration
            await _developerService.UpdateBusinessConfigurationAsync(hostId, "1", hostType); // Using default version "1"
            _logger.LogInformation("K8s ConfigMaps updated successfully for host {HostId}:{HostType}", hostId, hostType);
            return Ok(new
            {
                HostId = hostId,
                HostType = hostType.ToString(),
                UpdatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload business configuration for host {HostId}:{HostType}", hostId, hostType);
            return StatusCode(500, $"Failed to upload configuration: {ex.Message}");
        }
    }

    private string GetHostId()
    {
        var clientId = CurrentUser.GetAllClaims().First(o => o.Type == "client_id").Value;
        if (!clientId.IsNullOrEmpty() && clientId.Contains("Aevatar"))
        {
            _logger.LogWarning($" unSupport client {clientId} ");
            throw new UserFriendlyException("unSupport client");
        }
      return clientId;
    }

    /// <summary>
    /// Get current business configuration for a host and host type
    /// </summary>
    /// <param name="hostType">Host type enum</param>
    /// <returns>Current configuration JSON</returns>
    [HttpGet("get")]
    public async Task<IActionResult> GetBusinessConfiguration([FromQuery] HostTypeEnum hostType)
    {
        var hostId = GetHostId();
        try
        {
            // Get configuration from the specified host type
            var grainKey = $"{hostId}:{hostType}";
            var configAgent = _grainFactory.GetGrain<IHostConfigurationGAgent>(GuidUtil.StringToGuid(grainKey));
            
            var configResult = await configAgent.GetBusinessConfigurationJsonAsync();
            
            if (string.IsNullOrWhiteSpace(configResult.ConfigurationJson) || configResult.ConfigurationJson == "{}")
            {
                return NotFound($"No business configuration found for host {hostId} with type {hostType}");
            }

            return Ok(new
            {
                HostId = hostId,
                HostType = hostType.ToString(),
                Configuration = JsonConvert.DeserializeObject(configResult.ConfigurationJson),
                UpdatedAt = configResult.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get business configuration for host {HostId}:{HostType}", hostId, hostType);
            return StatusCode(500, $"Failed to get configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete business configuration for a host and host type
    /// </summary>
    /// <param name="hostType">Host type enum</param>
    /// <returns>Deletion result</returns>
    [HttpPost("delete")]
    public async Task<IActionResult> DeleteBusinessConfiguration([FromQuery] HostTypeEnum hostType)
    {
        var hostId = GetHostId();
        try
        {
            var updatedBy = User?.Identity?.Name ?? "System";
            var grainKey = $"{hostId}:{hostType}";
            var configAgent = _grainFactory.GetGrain<IHostConfigurationGAgent>(GuidUtil.StringToGuid(grainKey));
            
            // Check if there's existing configuration
            var existingConfigResult = await configAgent.GetBusinessConfigurationJsonAsync();
            if (string.IsNullOrWhiteSpace(existingConfigResult.ConfigurationJson) || existingConfigResult.ConfigurationJson == "{}")
            {
                return NotFound($"No business configuration found for host {hostId} with type {hostType}");
            }
            
            await configAgent.ClearBusinessConfigurationAsync(updatedBy);
            
            _logger.LogInformation("Business configuration deleted for {HostId}:{HostType}", hostId, hostType);
            await _developerService.UpdateBusinessConfigurationAsync(hostId, "1", hostType); // Using default version "1"
            _logger.LogInformation("K8s ConfigMaps delete successfully for host {HostId}:{HostType}", hostId, hostType);
            return Ok(new
            {
                HostId = hostId,
                HostType = hostType.ToString(),
                UpdatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete business configuration for host {HostId}:{HostType}", hostId, hostType);
            return StatusCode(500, $"Failed to delete configuration: {ex.Message}");
        }
    }
}