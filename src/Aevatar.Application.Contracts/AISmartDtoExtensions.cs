using Volo.Abp.Threading;

namespace Aevatar;

public static class AevatarDtoExtensions
{
    private static readonly OneTimeRunner OneTimeRunner = new OneTimeRunner();

    public static void Configure()
    {
        OneTimeRunner.Run(() => { });
    }
}