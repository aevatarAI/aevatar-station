using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.AgentValidation;
using Aevatar.Controllers;
using Aevatar.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aevatar.HttpApi.Controllers;

/// <summary>
/// Agent configuration validation controller
/// </summary>
[Route("api/agent-validation")]
[ApiController]
public class AgentValidationController : AevatarController
{
    private readonly ILogger<AgentValidationController> _logger;
    private readonly IAgentValidationService _agentValidationService;

    public AgentValidationController(
        ILogger<AgentValidationController> logger,
        IAgentValidationService agentValidationService)
    {
        _logger = logger;
        _agentValidationService = agentValidationService;
    }

    /// <summary>
    /// Validate agent configuration
    /// </summary>
    /// <param name="request">Validation request containing GAgent namespace and configuration JSON</param>
    /// <returns>Validation result with success status and error details</returns>
    [HttpPost("validate-config")]
    [Authorize]
    public async Task<ConfigValidationResultDto> ValidateConfigAsync([FromBody] ValidationRequestDto request)
    {
        _logger.LogInformation("🔍 Received validation request for GAgent: {GAgentNamespace}", 
            request?.GAgentNamespace ?? "null");
        
        try
        {
            if (request == null)
            {
                return ConfigValidationResultDto.Failure(
                    new[] { new ValidationErrorDto { PropertyName = "Request", Message = "Request body is required" } },
                    "Invalid request");
            }

            var result = await _agentValidationService.ValidateConfigAsync(request);
            
            _logger.LogInformation("✅ Validation completed for GAgent: {GAgentNamespace}, IsValid: {IsValid}", 
                request.GAgentNamespace, result.IsValid);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during validation request for GAgent: {GAgentNamespace}", 
                request?.GAgentNamespace ?? "unknown");
            
            return ConfigValidationResultDto.Failure(
                new[] { new ValidationErrorDto { PropertyName = "System", Message = "Internal server error" } },
                "An unexpected error occurred during validation");
        }
    }

    /// <summary>
    /// Get list of available agent types that can be validated
    /// </summary>
    /// <returns>List of available agent type namespaces</returns>
    [HttpGet("available-agent-types")]
    [Authorize]
    public async Task<List<string>> GetAvailableAgentTypesAsync()
    {
        _logger.LogInformation("🔍 Retrieving available agent types");
        
        try
        {
            var agentTypes = await _agentValidationService.GetAvailableAgentTypesAsync();
            
            _logger.LogInformation("✅ Retrieved {Count} available agent types", agentTypes.Count);
            
            return agentTypes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error retrieving available agent types");
            return new List<string>();
        }
    }

    /// <summary>
    /// Health check endpoint for the validation service
    /// </summary>
    /// <returns>Service health status</returns>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}