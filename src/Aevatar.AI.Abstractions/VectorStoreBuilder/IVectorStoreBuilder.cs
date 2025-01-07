namespace Aevatar.AI.VectorStoreBuilder;

public interface IVectorStoreBuilder
{
    void ConfigureCollection<TKey, TValue>(string collectionName)
        where TValue : class;
}