using System.Security.Claims;
using Aevatar.AuthServer.Grants.Options;

namespace Aevatar.AuthServer.Grants.Providers;

public interface IAppleProvider
{
    Task<string> ExchangeCodeForTokenAsync(string code, string source, string platform,
        AppleAppOptions appOptions);
    Task<(bool IsValid, ClaimsPrincipal? Principal)> ValidateAppleTokenAsync(string idToken, string source,
        AppleAppOptions appOptions);
}

public class AppleUserInfo
{
    public string? SubjectId { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
} 