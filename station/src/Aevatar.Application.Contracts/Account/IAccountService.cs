using System.Threading.Tasks;
using Volo.Abp.Account;
using Volo.Abp.Identity;
using Aevatar.Domain.Shared;

namespace Aevatar.Account;

public interface IAccountService: IAccountAppService
{
    Task SendRegisterCodeAsync(SendRegisterCodeDto input,GodGPTChatLanguage language = GodGPTChatLanguage.English);
    Task<IdentityUserDto> RegisterAsync(AevatarRegisterDto input, GodGPTChatLanguage language = GodGPTChatLanguage.English);
    Task<bool> VerifyRegisterCodeAsync(VerifyRegisterCodeDto input);
    Task<bool> CheckEmailRegisteredAsync(CheckEmailRegisteredDto input);
    Task<bool> VerifyEmailRegistrationWithTimeAsync(CheckEmailRegisteredDto input);
    Task<IdentityUserDto> GodgptRegisterAsync(GodGptRegisterDto input, GodGPTChatLanguage language = GodGPTChatLanguage.English);
    Task SendPasswordResetCodeAsync(SendPasswordResetCodeDto input, GodGPTChatLanguage language);
}