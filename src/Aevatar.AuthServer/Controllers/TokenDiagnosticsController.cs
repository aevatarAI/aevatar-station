using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.OpenIddict.Applications;

namespace Aevatar.AuthServer.Controllers;

[ApiController]
[Route("api/token")]
public class TokenDiagnosticsController : AbpControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IOpenIddictApplicationRepository _applicationRepository;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public TokenDiagnosticsController(
        IConfiguration configuration,
        IOpenIddictApplicationRepository applicationRepository)
    {
        _configuration = configuration;
        _applicationRepository = applicationRepository;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    [HttpGet("validation")]
    public async Task<IActionResult> DebugTokenValidation([FromQuery] string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new { Error = "refresh_token parameter is required" });
        }

        var diagnostics = new
        {
            Step1_TokenReceived = new
            {
                Status = "✓ Success",
                TokenLength = refreshToken.Length,
                TokenStart = refreshToken.Substring(0, Math.Min(20, refreshToken.Length)) + "..."
            },
            Step2_DataProtectionTest = await TestDataProtectionDecryption(refreshToken),
            Step3_IssuerValidation = TestIssuerConfiguration(),
            Step4_SystemTime = TestSystemTime(),
            Step5_DatabaseConnection = await TestDatabaseConnection(),
            Recommendations = GetRecommendations()
        };

        return Ok(diagnostics);
    }

    private async Task<object> TestDataProtectionDecryption(string token)
    {
        try
        {
            // 简单测试DataProtection是否工作
            var dataProtectionProvider = HttpContext.RequestServices
                .GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>();
            var protector = dataProtectionProvider.CreateProtector("test");

            // 测试基本加解密功能
            var testData = System.Text.Encoding.UTF8.GetBytes("test-" + DateTime.UtcNow.Ticks);
            var encrypted = protector.Protect(testData);
            var decrypted = protector.Unprotect(encrypted);

            if (!testData.SequenceEqual(decrypted))
            {
                return new
                {
                    Status = "❌ Failed",
                    Error = "DataProtection encryption/decryption test failed",
                    Issue = "Keys may be corrupted or missing"
                };
            }

            return new
            {
                Status = "✓ Success",
                Message = "DataProtection is working correctly",
                Note = "If refresh token still fails, the specific token key may be missing"
            };
        }
        catch (Exception ex)
        {
            return new
            {
                Status = "❌ Failed",
                Error = ex.Message,
                Issue = "DataProtection system failure",
                Recommendation = "Check Redis connection and DataProtection keys"
            };
        }
    }

    private object TestIssuerConfiguration()
    {
        try
        {
            var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var issuerUri = configuration["AuthServer:IssuerUri"];
            var currentHost = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

            return new
            {
                Status = issuerUri == currentHost ? "✓ Success" : "⚠ Warning",
                ConfiguredIssuer = issuerUri,
                CurrentHost = currentHost,
                Match = issuerUri == currentHost,
                Issue = issuerUri != currentHost ? "Issuer URI mismatch may cause token validation failure" : null
            };
        }
        catch (Exception ex)
        {
            return new
            {
                Status = "❌ Failed",
                Error = ex.Message
            };
        }
    }

    private object TestSystemTime()
    {
        var utcNow = DateTime.UtcNow;
        var localNow = DateTime.Now;

        return new
        {
            Status = "✓ Success",
            UtcTime = utcNow,
            LocalTime = localNow,
            TimeZone = TimeZoneInfo.Local.DisplayName,
            Note = "Ensure time is synchronized across all servers"
        };
    }

    private async Task<object> TestDatabaseConnection()
    {
        try
        {
            return new
            {
                Status = "✓ Assumed OK",
                Note = "Database connection test not implemented yet",
                Recommendation = "Manually verify MongoDB connection and token storage"
            };
        }
        catch (Exception ex)
        {
            return new
            {
                Status = "❌ Failed",
                Error = ex.Message
            };
        }
    }

    private object GetRecommendations()
    {
        return new
        {
            MostLikelyIssue = "DataProtection keys inconsistency between old and new server",
            DiagnosisSteps = new[]
            {
                "1. Check Redis connection: redis-cli ping",
                "2. List DataProtection keys: redis-cli KEYS 'Aevatar-DataProtection-Keys*'",
                "3. Compare key counts between old and new server",
                "4. Verify IssuerUri configuration matches exactly",
                "5. Check server time synchronization"
            },
            QuickFix = new
            {
                Description = "If keys are missing, copy from old server or regenerate",
                Commands = new[]
                {
                    "# Backup keys from old server",
                    "redis-cli --rdb dump.rdb",
                    "# Restore to new server",
                    "redis-cli --pipe < dump.rdb"
                }
            }
        };
    }
}