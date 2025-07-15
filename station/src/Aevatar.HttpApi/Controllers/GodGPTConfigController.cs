using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using Aevatar.Controllers;
using Aevatar.Quantum;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Volo.Abp;

[RemoteService]
[ControllerName("GodGPTConfig")]
[Route("api/godgpt/configuration")]
[Authorize]
public class GodGPTConfigController : AevatarController
{
    private readonly IGodGPTService _godGptService;
    private readonly ILogger<GodGPTConfigController> _logger;

    public GodGPTConfigController(IGodGPTService godGptService, ILogger<GodGPTConfigController> logger)
    {
        _godGptService = godGptService;
        _logger = logger;
    }

    [HttpGet("system-prompt")]
    public async Task<string> GetSystemPrompt()
    {
        var user = User.Identity?.Name ?? "Unknown User";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
        var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

        _logger.LogInformation("GET 'SystemPrompt' requested by User: {User}, IP: {IP}, Roles: {Roles}",
            user, ipAddress, string.Join(", ", roles));

        // Group User.Claims by Type and convert to a dictionary
        var claimsDict = User.Claims
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => g.Select(c => c.Value).ToList());

        // Log the claims dictionary using _logger.LogInformation
        foreach (var kvp in claimsDict)
        {
            _logger.LogInformation("{ClaimType}: {ClaimValues}", kvp.Key, string.Join(", ", kvp.Value));
        }

        var roleClaims = User.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .ToList();

        var roleNames = string.Join(",", roleClaims);

        if (!roleNames.Contains("systemPromptManager"))
        {
            return "Permission denied: 'systemPromptManager' role is required.";
        }

        var systemPrompt = await _godGptService.GetSystemPromptAsync();

        _logger.LogInformation("System Prompt Retrieved: {SystemPrompt}", systemPrompt);
        return systemPrompt;
    }

    [HttpPost("system-prompt")]
    // [Authorize(Roles = "systemPromptManager")]
    public async Task<IActionResult> UpdateSystemPrompt([FromBody] GodGPTConfigurationDto godGptConfigurationDto)
    {
        var user = HttpContext.User.Identity?.Name ?? "Unknown User";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
        var roles = HttpContext.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

        _logger.LogInformation(
            "POST 'UpdateSystemPrompt' by User: {User}, IP: {IP}, Roles: {Roles}, Payload: {@Payload}",
            user, ipAddress, string.Join(", ", roles), godGptConfigurationDto);

        // Group User.Claims by Type and convert to a dictionary
        var claimsDict = User.Claims
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => g.Select(c => c.Value).ToList());

        // Log the claims dictionary using _logger.LogInformation
        foreach (var kvp in claimsDict)
        {
            _logger.LogInformation("{ClaimType}: {ClaimValues}", kvp.Key, string.Join(", ", kvp.Value));
        }

        var roleClaims = User.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .ToList();
        
        var roleNames = string.Join(",", roleClaims);
        
        if (!roleNames.Contains("systemPromptManager"))
        {
            // Return a custom "Forbid" response with an error message
            return Forbid(new AuthenticationProperties
            {
                // Add custom error message
                Items = { { "ErrorMessage", "Permission denied: 'systemPromptManager' role is required." } }
            });
        }
        
        await _godGptService.UpdateSystemPromptAsync(godGptConfigurationDto);

        _logger.LogInformation("System Prompt Updated Successfully by User: {User}", user);

        return Ok(new { message = "System prompt updated successfully." });
    }
}