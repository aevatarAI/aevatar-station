using Aevatar.User;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;

namespace Aevatar.Provider;

public class UserInformationProvider: IUserInformationProvider, ISingletonDependency
{
    private readonly ILogger<UserInformationProvider> _logger;
    private readonly IRepository<IdentityUserExtension, Guid> _userExtensionRepository;
    private readonly IObjectMapper _objectMapper;
    

    public UserInformationProvider(IRepository<IdentityUserExtension, Guid> userExtensionRepository,
        IObjectMapper objectMapper,ILogger<UserInformationProvider> logger)
    {
        _userExtensionRepository = userExtensionRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task<bool> SaveUserExtensionInfoAsync(UserExtensionDto userExtensionDto)
    {
        var userExtension = await _userExtensionRepository.FirstOrDefaultAsync(x => x.Id == userExtensionDto.UserId);
        if (userExtension == null)
        {
            // var caAddressMain=userExtensionDto.CaAddressList
            userExtension = new IdentityUserExtension(userExtensionDto.UserId)
            {
                UserId = userExtensionDto.UserId,
                WalletAddress = userExtensionDto.WalletAddress
            };

            await _userExtensionRepository.InsertAsync(userExtension);
            return true;
        }

        return false;
    }

    public async Task<UserExtensionDto> GetUserExtensionInfoByIdAsync(Guid userId)
    {
        var userExtension = await _userExtensionRepository.FindAsync(userId);
        if (userExtension == null)
        {
            return new UserExtensionDto();
        }
        return _objectMapper.Map<IdentityUserExtension,UserExtensionDto>(userExtension);
    }

    public async Task<UserExtensionDto> GetUserExtensionInfoByWalletAddressAsync(string address)
    {
        var userExtension = await _userExtensionRepository.FindAsync(u => u.WalletAddress == address);
        if (userExtension != null)
        {
            return _objectMapper.Map<IdentityUserExtension, UserExtensionDto>(userExtension);
        }

        return null;
    }
}