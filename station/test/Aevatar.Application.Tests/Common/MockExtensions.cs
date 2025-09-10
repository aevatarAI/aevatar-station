using Moq;

namespace Aevatar.Common;

public static class MockExtensions
{
    public static Mock<T> AsMock<T>(this T instance) where T : class
    {
        return Moq.Mock.Get(instance);
    }
}