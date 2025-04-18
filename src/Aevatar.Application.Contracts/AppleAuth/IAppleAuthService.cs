using System.Threading.Tasks;

namespace Aevatar.AppleAuth;

public interface IAppleAuthService
{
    Task<string> CallbackAsync(string platform, AppleAuthCallbackDto callbackDto);
}