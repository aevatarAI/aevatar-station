using System.Security.Claims;

namespace Aevatar.AuthServer.Grants.Providers;

public interface IAppleProvider
{
    Task<string> ExchangeCodeForTokenAsync(string code, string source);
    Task<(bool IsValid, ClaimsPrincipal? Principal)> ValidateAppleTokenAsync(string idToken, string source);
}

public class AppleUserInfo
{
    public string? SubjectId { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
} 