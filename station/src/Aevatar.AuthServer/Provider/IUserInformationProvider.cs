using Aevatar.User;

namespace Aevatar.Provider;

public interface IUserInformationProvider
{
    Task<bool> SaveUserExtensionInfoAsync(UserExtensionDto userExtensionDto);

    Task<UserExtensionDto> GetUserExtensionInfoByIdAsync(Guid userId);

    Task<UserExtensionDto> GetUserExtensionInfoByWalletAddressAsync(string address);
}