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
using Volo.Abp.OpenIddict.Tokens;

namespace Aevatar.AuthServer.Controllers;

[ApiController]
[Route("api/token")]
public class TokenDiagnosticsController : AbpControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IOpenIddictApplicationRepository _applicationRepository;
    private readonly IOpenIddictTokenRepository _tokenRepository;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public TokenDiagnosticsController(
        IConfiguration configuration,
        IOpenIddictApplicationRepository applicationRepository,
        IOpenIddictTokenRepository tokenRepository)
    {
        _configuration = configuration;
        _applicationRepository = applicationRepository;
        _tokenRepository = tokenRepository;
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
            Step2_ReferenceTokenTest = await TestReferenceTokenStorage(refreshToken),
            Step3_DataProtectionTest = await TestDataProtectionDecryption(refreshToken),
            Step4_IssuerValidation = TestIssuerConfiguration(),
            Step5_SystemTime = TestSystemTime(),
            Step6_DatabaseConnection = await TestDatabaseConnection(),
            Recommendations = GetRecommendations()
        };

        return Ok(diagnostics);
    }

    [HttpGet("audience-debug")]
    public IActionResult DebugAudienceIssue([FromQuery] string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return BadRequest(new { Error = "access_token parameter is required" });
        }

        try
        {
            // 解码JWT token（不验证签名，只读取内容）
            var token = _tokenHandler.ReadJwtToken(accessToken);

            var audienceClaims = token.Claims.Where(c => c.Type == "aud").ToList();
            var issuer = token.Claims.FirstOrDefault(c => c.Type == "iss")?.Value;
            var subject = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var scopes = token.Claims.Where(c => c.Type == "scope").Select(c => c.Value).ToList();

            var diagnostics = new
            {
                TokenHeader = new
                {
                    Algorithm = token.Header.Alg,
                    Type = token.Header.Typ,
                    KeyId = token.Header.Kid
                },
                AudienceAnalysis = new
                {
                    ExpectedAudience = "Aevatar",
                    ActualAudiences = audienceClaims.Select(c => c.Value).ToList(),
                    AudienceCount = audienceClaims.Count,
                    HasCorrectAudience = audienceClaims.Any(c => c.Value == "Aevatar"),
                    Status = audienceClaims.Any(c => c.Value == "Aevatar") ? "✓ Success" : "❌ Failed",
                    Issue = !audienceClaims.Any(c => c.Value == "Aevatar")
                        ? "Token does not contain expected audience 'Aevatar'"
                        : null
                },
                TokenClaims = new
                {
                    Issuer = issuer,
                    Subject = subject,
                    Scopes = scopes,
                    IssuedAt = token.Claims.FirstOrDefault(c => c.Type == "iat")?.Value,
                    ExpiresAt = token.Claims.FirstOrDefault(c => c.Type == "exp")?.Value,
                    AllClaims = token.Claims.Select(c => new { c.Type, c.Value }).ToList()
                },
                ValidationConfiguration = new
                {
                    AuthServerConfiguration = new
                    {
                        IssuerUri = _configuration["AuthServer:IssuerUri"],
                        ExpectedAudience = "Aevatar"
                    },
                    ClientConfiguration = new
                    {
                        ExpectedAuthority = _configuration["AuthServer:Authority"] ?? "Not configured in AuthServer",
                        ExpectedAudience = "Aevatar"
                    }
                },
                Recommendations = GetAudienceRecommendations(audienceClaims.Any(c => c.Value == "Aevatar"))
            };

            return Ok(diagnostics);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Error = "Failed to decode token",
                Message = ex.Message,
                Issue = "Token may be malformed or encrypted",
                Recommendation = "Ensure you're using an access_token (JWT format), not a refresh_token"
            });
        }
    }

    private async Task<object> TestReferenceTokenStorage(string refreshToken)
    {
        try
        {
            // 尝试通过引用令牌查找数据库中的令牌记录
            var token = await _tokenRepository.FindByReferenceIdAsync(refreshToken);

            if (token == null)
            {
                return new
                {
                    Status = "❌ Failed",
                    Error = "Reference token not found in database",
                    Issue = "Token may have been deleted, expired, or created on different server",
                    ReferenceTokenId = refreshToken,
                    Recommendation = "Check if token exists in MongoDB collection OpenIddictTokens"
                };
            }

            return new
            {
                Status = "✓ Success",
                TokenFound = true,
                Message = "Reference token found in database",
                CurrentTime = DateTime.UtcNow,
                Note = "Token exists in database - this means UseReferenceRefreshTokens() is working"
            };
        }
        catch (Exception ex)
        {
            return new
            {
                Status = "❌ Failed",
                Error = ex.Message,
                Issue = "Database query failed",
                Recommendation = "Check MongoDB connection and OpenIddict token collection"
            };
        }
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

    private object GetAudienceRecommendations(bool hasCorrectAudience)
    {
        if (hasCorrectAudience)
        {
            return new
            {
                Status = "Token audience is correct",
                Note = "If client validation still fails, check client-side JWT configuration"
            };
        }

        return new
        {
            PossibleCauses = new[]
            {
                "AuthServer grant handlers not setting audience correctly",
                "OpenIddict validation configuration missing AddAudiences()",
                "Token issued before audience configuration was added"
            },
            Solutions = new[]
            {
                "Verify all grant handlers call claimsPrincipal.SetAudiences(\"Aevatar\")",
                "Check OpenIddict validation configuration has options.AddAudiences(\"Aevatar\")",
                "Request a new token after configuration changes"
            },
            GrantHandlersToCheck = new[]
            {
                "SignatureGrantHandler.cs",
                "GoogleGrantHandler.cs",
                "AppleGrantHandler.cs",
                "Standard OAuth2 grant handlers"
            }
        };
    }
}