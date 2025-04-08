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
    private readonly IOptionsMonitor<AppleAuthOption> _appleAuthOptions;
    private readonly ILogger<AppleAuthService> _logger;

    private const string QuestionMark = "?";

    public AppleAuthService(IOptionsMonitor<AppleAuthOption> appleAuthOptions)
    {
        _appleAuthOptions = appleAuthOptions;
    }

    public async Task<string> CallbackAsync(AppleAuthCallbackDto callbackDto)
    {
        _logger.LogDebug("Apple token: {token}", JsonConvert.SerializeObject(callbackDto));

        var idToken = callbackDto.Id_token;
        var code = callbackDto.Code;
        
        return GetRedirectUrl(_appleAuthOptions.CurrentValue.RedirectUrl, idToken, code);
    }
    
    private static string GetRedirectUrl(string redirectUrl, string token, string code)
    {
        if (redirectUrl.Contains(QuestionMark))
        {
            return $"{redirectUrl}&id_token={token}&code={code}";
        }

        return $"{redirectUrl}?id_token={token}&code={code}";
    }
}