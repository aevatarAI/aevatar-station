namespace Aevatar.MongoDB;

public abstract class AevatarMongoDbTestBase : AevatarTestBase<AevatarMongoDbTestModule>
{
    /// <summary>
    /// MongoDB测试总是需要MongoDB
    /// </summary>
    /// <returns>总是返回true</returns>
    protected override bool ShouldUseMongoDB()
    {
        return true;
    }
}
