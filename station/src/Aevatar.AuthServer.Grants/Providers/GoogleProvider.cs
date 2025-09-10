using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Aevatar.AuthServer.Grants.Providers;

public class GoogleProvider : IGoogleProvider, ITransientDependency
{
    private readonly ILogger<GoogleProvider> _logger;
    private readonly IConfiguration _configuration;

    public GoogleProvider(ILogger<GoogleProvider> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<GoogleJsonWebSignature.Payload?> ValidateGoogleTokenAsync(string idToken, string clientId)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };
            return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validate Google token failed");
            return null;
        }
    }

    public async Task<string> GetClientIdAsync(string source)
    {
        string clientId = source switch
        {
            "ios" => _configuration["Google:IOSClientId"],
            "android" => _configuration["Google:AndroidClientId"],
            _ => _configuration["Google:ClientId"]
        };

        return clientId;
    }
} 