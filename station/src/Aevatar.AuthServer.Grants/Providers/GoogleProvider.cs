using Aevatar.AuthServer.Grants.Options;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace Aevatar.AuthServer.Grants.Providers;

public class GoogleProvider : IGoogleProvider, ITransientDependency
{
    private readonly ILogger<GoogleProvider> _logger;
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<GoogleOptions> _googleOptions;

    public GoogleProvider(ILogger<GoogleProvider> logger, IConfiguration configuration, IOptionsMonitor<GoogleOptions> googleOptions)
    {
        _logger = logger;
        _configuration = configuration;
        _googleOptions = googleOptions;
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

    public async Task<string> GetClientIdAsync(string source, string appId = "")
    {
        if (!appId.IsNullOrWhiteSpace() && _googleOptions.CurrentValue.AppConfigs != null && 
            _googleOptions.CurrentValue.AppConfigs.TryGetValue(appId, out var appConfig))
        {
            return source switch
            {
                "ios" => appConfig.IOSClientId ?? _googleOptions.CurrentValue.IOSClientId,
                "android" => appConfig.AndroidClientId ?? _googleOptions.CurrentValue.AndroidClientId,
                _ => appConfig.ClientId ?? _googleOptions.CurrentValue.ClientId
            };
        }

        return source switch
        {
            "ios" => _googleOptions.CurrentValue.IOSClientId,
            "android" => _googleOptions.CurrentValue.AndroidClientId,
            _ => _googleOptions.CurrentValue.ClientId
        };
    }
} 