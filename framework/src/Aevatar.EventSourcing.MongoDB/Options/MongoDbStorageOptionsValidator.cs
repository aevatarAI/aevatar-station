namespace Aevatar.EventSourcing.MongoDB.Options;

public class MongoDbStorageOptionsValidator : IConfigurationValidator
{
    private readonly MongoDbStorageOptions _options;
    private readonly string _name;

    public MongoDbStorageOptionsValidator(MongoDbStorageOptions options, string name)
    {
        _options = options ?? throw new OrleansConfigurationException(
            $"Invalid MongoDbStorageOptions for MongoDbLogConsistentStorage {name}. Options is required.");
        _name = name;
    }

    public void ValidateConfiguration()
    {
        if (_options.ClientSettings == null)
        {
            throw new OrleansConfigurationException(
                $"Invalid configuration for {nameof(MongoDbLogConsistentStorage)} with name {_name}. {nameof(MongoDbStorageOptions)}.{nameof(_options.ClientSettings)} is required.");
        }
    }
}