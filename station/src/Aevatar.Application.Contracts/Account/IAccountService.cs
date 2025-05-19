using System.Threading.Tasks;
using Volo.Abp.Account;
using Volo.Abp.Identity;

namespace Aevatar.Account;

public interface IAccountService: IAccountAppService
{
    Task SendRegisterCodeAsync(SendRegisterCodeDto input);
    Task<IdentityUserDto> RegisterAsync(AevatarRegisterDto input);
}