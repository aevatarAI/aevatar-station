using System.Threading.Tasks;
using Aevatar.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Aevatar.AppleAuth;

[RemoteService(IsEnabled = false)]
public class AppleAuthService : ApplicationService, IAppleAuthService
{
    private readonly ILogger<AppleAuthService> _logger;
    private readonly IOptionsMonitor<AppleAuthOption> _appleAuthOptions;

    private const string QuestionMark = "?";

    private const string LoginPlatformBrowser = "browser";
    private const string LoginPlatformAndroid = "android";
    
    public AppleAuthService(ILogger<AppleAuthService> logger, IOptionsMonitor<AppleAuthOption> appleAuthOptions)
    {
        _logger = logger;
        _appleAuthOptions = appleAuthOptions;
    }

    public async Task<string> CallbackAsync(string platform, AppleAuthCallbackDto callbackDto)
    {
        _logger.LogDebug("Apple token:{platform}, {token}", platform, JsonConvert.SerializeObject(callbackDto));

        var idToken = callbackDto.Id_token;
        var code = callbackDto.Code;

        return GetRedirectUrl(_appleAuthOptions.CurrentValue.RedirectUrl, idToken, code, platform);
    }
    
    private static string GetRedirectUrl(string redirectUrl, string token, string code, string platform)
    {
        if (redirectUrl.Contains(QuestionMark))
        {
            return $"{redirectUrl}&id_token={token}&code={code}&platform={platform}";
        }

        return $"{redirectUrl}?id_token={token}&code={code}&platform={platform}";
    }
}