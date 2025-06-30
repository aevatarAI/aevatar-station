namespace Aevatar.AuthServer.Grants.Providers;

public interface IGithubProvider
{
    Task<GithubUser?> GetUserInfoAsync(string code);
}

public class GithubUser
{
    public long Id { get; set; }
    public string? Email { get; set; }
} 