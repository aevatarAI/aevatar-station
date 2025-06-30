using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;
using Volo.Abp.DependencyInjection;

namespace Aevatar.AuthServer.Grants.Providers;

public class GithubProvider : IGithubProvider, ITransientDependency
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GithubProvider> _logger;

    public GithubProvider(IConfiguration configuration, ILogger<GithubProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GithubUser?> GetUserInfoAsync(string code)
    {
        try
        {
            var clientId = _configuration["Github:ClientId"];
            var secret = _configuration["Github:ClientSecret"];
            
            var client = new GitHubClient(new ProductHeaderValue("Aevatar"));
            
            var oauthRequest = new OauthTokenRequest(clientId, secret, code);
            var token = await client.Oauth.CreateAccessToken(oauthRequest);

            if (token.AccessToken.IsNullOrWhiteSpace())
            {
                return null;
            }

            client.Credentials = new Credentials(token.AccessToken);

            var user = await client.User.Current();
            var email = user.Email;
            
            if (email.IsNullOrWhiteSpace())
            {
                var emails = await client.User.Email.GetAll();
                if (emails.Count > 0)
                {
                    email = emails.FirstOrDefault(o => o.Primary)?.Email;
                }
            }

            return new GithubUser
            {
                Id = user.Id,
                Email = email
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GithubProvider.GetUserInfoAsync failed");
            return null;
        }
    }
} 